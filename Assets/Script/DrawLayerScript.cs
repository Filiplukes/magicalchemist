using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage))]
public class SpellDrawScript : MonoBehaviour,
                         IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Kreslení")]
    [Range(1, 100)]
    public int brushSize = 20;
    public bool scaleBrushByResolution = true;
    public Color drawColor = Color.red; // <--- sem přidat

    [Header("Vyhodnocení")]
    private string spellName;  // název sprite v Resources/Spells/
    private Sprite referenceSprite;
    public TextMeshProUGUI resultText;
    public GameManager gameManager;

    private bool canDraw = false;
    private Vector2? lastPaintPos = null;

    Texture2D drawTex;
    RawImage raw;

    Color[] referencePixels;
    int refW, refH;
    int blackPixelCount;

    private int movesToSpell = 0;

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
        raw = GetComponent<RawImage>();

        RectTransform rt = (RectTransform)transform;
        int w = Mathf.RoundToInt(rt.rect.width);
        int h = Mathf.RoundToInt(rt.rect.height);

        drawTex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        drawTex.filterMode = FilterMode.Point;

        ClearTexture();
        raw.texture = drawTex;

        if (scaleBrushByResolution)
        {
            brushSize = Mathf.Max(1, Mathf.RoundToInt(drawTex.width * 0.04f));
        }
    }

    public void SetSpell(string spellName)
    {
        this.spellName = spellName;
        movesToSpell = spellMove[spellName];
        drawColor = spellColor[spellName];
        LoadReferenceSprite();

    }

    void LoadReferenceSprite()
    {
        // Načti sprite z Resources/Spells/
        referenceSprite = Resources.Load<Sprite>("Spells/" + spellName);
        if (referenceSprite == null)
        {
            Debug.LogError($"Sprite '{spellName}' nenalezen ve Resources/Spells/");
        }

        if (referenceSprite != null)
        {
            canDraw = true;
            FillBackgroundWithReference();
            CountReferenceBlackPixels();
            drawTex.Apply();
        }
    }

    public void ClearTexture()
    {
        var cols = new Color[drawTex.width * drawTex.height];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;
        drawTex.SetPixels(cols);
        drawTex.Apply();
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
                gameManager.GetComponent<GameManager>().SetPecentSpell(correctness * 100f);

                //bodovani nastavit pak text vyskakujici ukazujici jak dobre caroval


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

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, e.position, e.pressEventCamera, out local);

        Rect r = ((RectTransform)transform).rect;
        int x = Mathf.RoundToInt((local.x - r.xMin) / r.width * drawTex.width);
        int y = Mathf.RoundToInt((local.y - r.yMin) / r.height * drawTex.height);

        Vector2 currentPos = new Vector2(x, y);

        if (lastPaintPos == null)
        {
            DrawCircle(x, y);
        }
        else
        {
            Vector2 last = lastPaintPos.Value;
            Vector2 delta = currentPos - last;

            float distance = delta.magnitude;
            float stepSize = 2.4f; // kompromis: nižší počet bodů
            int steps = Mathf.CeilToInt(distance / stepSize);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 interpolated = Vector2.LerpUnclamped(last, currentPos, t);
                DrawCircle(Mathf.RoundToInt(interpolated.x), Mathf.RoundToInt(interpolated.y));
            }
        }

        lastPaintPos = currentPos;

        // ❗️Voláme Apply jen jednou, až po všech bodech
        if (e.dragging)
            drawTex.Apply();
    }
    void DrawCircle(int cx, int cy)
    {
        for (int y = -brushSize; y <= brushSize; y++)
            for (int x = -brushSize; x <= brushSize; x++)
            {
                if (x * x + y * y > brushSize * brushSize) continue;

                int px = cx + x;
                int py = cy + y;
                if (px < 0 || py < 0 || px >= drawTex.width || py >= drawTex.height) continue;

                drawTex.SetPixel(px, py, drawColor); // <--- místo Color.white
            }
    }

    void FillBackgroundWithReference()
    {
        Rect spriteRect = referenceSprite.textureRect;
        int x = Mathf.RoundToInt(spriteRect.x);
        int y = Mathf.RoundToInt(spriteRect.y);
        int w = Mathf.RoundToInt(spriteRect.width);
        int h = Mathf.RoundToInt(spriteRect.height);

        referencePixels = referenceSprite.texture.GetPixels(x, y, w, h);
        refW = w;
        refH = h;

        for (int py = 0; py < drawTex.height; py++)
        {
            for (int px = 0; px < drawTex.width; px++)
            {
                int refX = px * refW / drawTex.width;
                int refY = py * refH / drawTex.height;
                Color refCol = referencePixels[refY * refW + refX];

                if (refCol.a > 0.1f && refCol.grayscale < 0.1f)
                    drawTex.SetPixel(px, py, Color.black);
                else
                    drawTex.SetPixel(px, py, Color.clear);
            }
        }
        drawTex.Apply();
    }

    void CountReferenceBlackPixels()
    {
        blackPixelCount = 0;
        Color[] pixels = drawTex.GetPixels();

        foreach (var c in pixels)
        {
            if (c.a > 0.1f && c.grayscale < 0.1f)
                blackPixelCount++;
        }
    }


    bool IsSimilarColor(Color a, Color b, float threshold = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold &&
               a.a > 0.1f;
    }


    float EvaluateCorrectness()
    {
        int remainingBlackPixels = 0;
        int drawnColorPixels = 0;
        int matchedDrawnPixels = 0;

        Color[] pixels = drawTex.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color current = pixels[i];
            int px = i % drawTex.width;
            int py = i / drawTex.width;
            int refX = px * refW / drawTex.width;
            int refY = py * refH / drawTex.height;
            int refIndex = refY * refW + refX;

            if (refIndex >= referencePixels.Length)
                continue;

            Color refCol = referencePixels[refIndex];
            bool isRefBlack = refCol.a > 0.1f && refCol.grayscale < 0.1f;
            bool isDrawn = IsSimilarColor(current, drawColor);

            if (isRefBlack && !isDrawn)
                remainingBlackPixels++;

            if (isDrawn)
            {
                drawnColorPixels++;

                if (isRefBlack)
                    matchedDrawnPixels++;
            }
        }

        if (blackPixelCount == 0) return 0f;

        float baseAccuracy = 1f - (float)remainingBlackPixels / blackPixelCount;
        float excessDraw = Mathf.Max(0, (float)(drawnColorPixels - matchedDrawnPixels) / blackPixelCount);
        float finalScore = Mathf.Clamp01(baseAccuracy - excessDraw);
        return finalScore;
    }

    void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCircle(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
}
