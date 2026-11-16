using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Kreslení kouzla prstem a vyhodnocení přesnosti.
///
/// Výkon: kreslí se přímo do pixel bufferu textury (žádné SetPixel), textura se do GPU posílá
/// nejvýš jednou za snímek (LateUpdate) a jen když se změnila. Maska reference se počítá jednou
/// na kouzlo a cachuje se. Počty pixelů pro hodnocení se udržují průběžně při kreslení, takže
/// vyhodnocení je okamžité.
///
/// Hodnocení je stejné jako dřív: pokrytí čáry minus kreslení mimo čáru (viz EvaluateCorrectness).
/// </summary>
[RequireComponent(typeof(RawImage))]
public class SpellDrawScript : MonoBehaviour,
                         IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Kreslení")]
    [Range(1, 100)]
    public int brushSize = 20;
    public bool scaleBrushByResolution = true;
    public Color drawColor = Color.red;

    [Header("Vyhodnocení")]
    public TextMeshProUGUI resultText;
    public GameManager gameManager;

    private const float InterpolationStep = 2.4f;
    private static readonly Color32 BackgroundBlack = new Color32(0, 0, 0, 255);

    private string spellName;
    private bool canDraw = false;
    private Vector2? lastPaintPos = null;
    private int movesToSpell = 0;

    private Texture2D drawTex;
    private NativeArray<Color32> texPixels;   // přímý přístup k datům textury
    private NativeArray<Color32> zeroPixels;  // průhledný obraz pro rychlé mazání (memcpy)
    private int texW, texH;
    private bool textureDirty;
    private Color32 drawColor32;

    // Maska "kde je černá čára reference" v rozlišení textury (cache podle kouzla).
    private readonly Dictionary<string, ReferenceMask> maskCache = new Dictionary<string, ReferenceMask>();
    private ReferenceMask currentMask;
    private bool[] drawn;                     // co už hráč nakreslil

    // Průběžné počty pro hodnocení.
    private int drawnCount;
    private int matchedCount;

    // Štětec jako řádkové úseky: pro dy = -r..r je půlšířka brushHalfWidth[dy + r].
    private int brushRadiusCached = -1;
    private int[] brushHalfWidth;

    private class ReferenceMask
    {
        public bool[] isBlack;
        public int[] blackIndices;
        public int blackCount => blackIndices.Length;
    }

    readonly Dictionary<string, int> spellMove = new Dictionary<string, int> {
        { "Air", 3 },
        { "Bolt", 1 },
        { "Fire", 1 },
        { "Water", 2 },
        { "Dark", 1 },
        { "Earth", 3 },
        { "Life", 1 },
        { "Light", 1 },
        { "Time", 1 }
    };

    readonly Dictionary<string, Color> spellColor = new Dictionary<string, Color> {
        { "Air", new Color(0.68f, 0.85f, 0.90f) },    // light blue
        { "Bolt", Color.yellow },
        { "Fire", Color.red },
        { "Water", Color.blue },
        { "Dark", new Color(0.5f, 0f, 0.5f) },        // purple
        { "Earth", new Color(0.59f, 0.29f, 0f) },     // brown
        { "Life", Color.green },
        { "Light", Color.white },
        { "Time", new Color(1f, 0.65f, 0f) }          // orange
    };

    void Awake()
    {
        var raw = GetComponent<RawImage>();

        RectTransform rt = (RectTransform)transform;
        texW = Mathf.Max(1, Mathf.RoundToInt(rt.rect.width));
        texH = Mathf.Max(1, Mathf.RoundToInt(rt.rect.height));

        // RGBA32 = stejné pořadí bajtů jako Color32, takže lze psát přímo do dat textury.
        drawTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        drawTex.filterMode = FilterMode.Point;
        texPixels = drawTex.GetPixelData<Color32>(0);
        zeroPixels = new NativeArray<Color32>(texW * texH, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        drawn = new bool[texW * texH];

        ClearTexture();
        raw.texture = drawTex;

        if (scaleBrushByResolution)
        {
            brushSize = Mathf.Max(1, Mathf.RoundToInt(texW * 0.04f));
        }
        EnsureBrush();
    }

    void OnDestroy()
    {
        if (zeroPixels.IsCreated) zeroPixels.Dispose();
        if (drawTex != null) Destroy(drawTex);
    }

    void LateUpdate()
    {
        FlushTexture();
    }

    /// <summary>Pošle změny textury do GPU (max. jednou za snímek).</summary>
    private void FlushTexture()
    {
        if (!textureDirty) return;
        drawTex.Apply(false);
        textureDirty = false;
    }

    public void SetSpell(string spellName)
    {
        this.spellName = spellName;
        movesToSpell = spellMove[spellName];
        drawColor = spellColor[spellName];
        drawColor32 = drawColor;

        currentMask = GetMask(spellName);
        if (currentMask == null)
        {
            Debug.LogError($"Sprite '{spellName}' nenalezen ve Resources/Spells/");
            return;
        }

        canDraw = true;
        FillBackgroundWithReference();
    }

    private ReferenceMask GetMask(string name)
    {
        if (maskCache.TryGetValue(name, out var cached)) return cached;

        var sprite = Resources.Load<Sprite>("Spells/" + name);
        if (sprite == null) return null;

        Rect spriteRect = sprite.textureRect;
        int x = Mathf.RoundToInt(spriteRect.x);
        int y = Mathf.RoundToInt(spriteRect.y);
        int refW = Mathf.RoundToInt(spriteRect.width);
        int refH = Mathf.RoundToInt(spriteRect.height);
        Color[] referencePixels = sprite.texture.GetPixels(x, y, refW, refH);

        // Stejné převzorkování a práh jako původní verze.
        var isBlack = new bool[texW * texH];
        var blackIndices = new List<int>();
        for (int py = 0; py < texH; py++)
        {
            int refRow = (py * refH / texH) * refW;
            int row = py * texW;
            for (int px = 0; px < texW; px++)
            {
                Color refCol = referencePixels[refRow + px * refW / texW];
                if (refCol.a > 0.1f && refCol.grayscale < 0.1f)
                {
                    isBlack[row + px] = true;
                    blackIndices.Add(row + px);
                }
            }
        }

        var mask = new ReferenceMask { isBlack = isBlack, blackIndices = blackIndices.ToArray() };
        maskCache[name] = mask;
        return mask;
    }

    void FillBackgroundWithReference()
    {
        ClearTexture();
        foreach (int i in currentMask.blackIndices)
            texPixels[i] = BackgroundBlack;
    }

    public void ClearTexture()
    {
        texPixels.CopyFrom(zeroPixels);

        System.Array.Clear(drawn, 0, drawn.Length);
        drawnCount = 0;
        matchedCount = 0;
        textureDirty = true;
    }

    public void OnPointerDown(PointerEventData e)
    {
        Paint(e);
    }

    public void OnDrag(PointerEventData e)
    {
        Paint(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (movesToSpell < 2)
        {
            if (canDraw)
            {
                float correctness = EvaluateCorrectness();
                gameManager.SetPecentSpell(correctness * 100f);

                if (resultText != null)
                    resultText.text = $"Přesnost: {(correctness * 100f):0}%";
                canDraw = false;
                movesToSpell = 0;
                ClearTexture();
            }
        }
        else
        {
            movesToSpell--;
        }
        lastPaintPos = null;
    }

    void Paint(PointerEventData e)
    {
        if (!canDraw) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, e.position, e.pressEventCamera, out Vector2 local);

        Rect r = ((RectTransform)transform).rect;
        int x = Mathf.RoundToInt((local.x - r.xMin) / r.width * texW);
        int y = Mathf.RoundToInt((local.y - r.yMin) / r.height * texH);
        Vector2 currentPos = new Vector2(x, y);

        EnsureBrush();

        if (lastPaintPos == null)
        {
            DrawCircle(x, y);
        }
        else
        {
            Vector2 last = lastPaintPos.Value;
            float distance = (currentPos - last).magnitude;
            int steps = Mathf.CeilToInt(distance / InterpolationStep);

            if (steps == 0)
            {
                DrawCircle(x, y);
            }
            else
            {
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 p = Vector2.LerpUnclamped(last, currentPos, (float)i / steps);
                    DrawCircle(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
                }
            }
        }

        lastPaintPos = currentPos;
    }

    private void EnsureBrush()
    {
        if (brushRadiusCached == brushSize && brushHalfWidth != null) return;

        int rad = brushSize;
        brushHalfWidth = new int[2 * rad + 1];
        for (int dy = -rad; dy <= rad; dy++)
        {
            // Největší dx, pro které platí dx² + dy² <= r² (stejná podmínka jako původní kruh).
            int limit = rad * rad - dy * dy;
            int hw = Mathf.FloorToInt(Mathf.Sqrt(limit));
            while (hw * hw > limit) hw--;
            while ((hw + 1) * (hw + 1) <= limit) hw++;
            brushHalfWidth[dy + rad] = hw;
        }
        brushRadiusCached = rad;
    }

    void DrawCircle(int cx, int cy)
    {
        int rad = brushRadiusCached;
        bool[] isBlack = currentMask.isBlack;
        bool changed = false;

        int yMin = Mathf.Max(0, cy - rad);
        int yMax = Mathf.Min(texH - 1, cy + rad);
        for (int py = yMin; py <= yMax; py++)
        {
            int hw = brushHalfWidth[py - cy + rad];
            int xMin = Mathf.Max(0, cx - hw);
            int xMax = Mathf.Min(texW - 1, cx + hw);
            int row = py * texW;
            for (int px = xMin; px <= xMax; px++)
            {
                int i = row + px;
                if (drawn[i]) continue;

                drawn[i] = true;
                texPixels[i] = drawColor32;
                drawnCount++;
                if (isBlack[i]) matchedCount++;
                changed = true;
            }
        }

        if (changed) textureDirty = true;
    }

    /// <summary>
    /// 1 - (nepokrytá čára / celá čára) - (kreslení mimo čáru / celá čára), oříznuto na 0..1.
    /// </summary>
    float EvaluateCorrectness()
    {
        if (currentMask == null || currentMask.blackCount == 0) return 0f;

        int blackCount = currentMask.blackCount;
        int remainingBlackPixels = blackCount - matchedCount;
        float baseAccuracy = 1f - (float)remainingBlackPixels / blackCount;
        float excessDraw = Mathf.Max(0, (float)(drawnCount - matchedCount) / blackCount);
        return Mathf.Clamp01(baseAccuracy - excessDraw);
    }
}
