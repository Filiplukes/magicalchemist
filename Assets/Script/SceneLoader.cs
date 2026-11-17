using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Načítací obrazovka mezi scénami: přes celou obrazovku obrázek (pozadí z menu)
/// a dole ukazatel načítání. Scéna se načítá asynchronně a obrazovka zmizí až po
/// prvních snímcích nové scény, kdy proběhnou Start() metody (např. příprava
/// předloh kouzel), takže hráč nevidí zamrzlou hru.
/// Volání: SceneLoader.Load("TimeMode");
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // Obrázek pozadí si loader pamatuje, aby šel použít i při načítání ze hry (Znovu, Menu).
    private static Sprite background;
    private static SceneLoader active;

    private const float BarHeightFraction = 0.018f;  // výška lišty vůči výšce obrazovky
    private const float BarWidthFraction = 0.7f;
    private const float BarBottomFraction = 0.08f;   // odsazení lišty od spodního okraje
    private const float MinShowTime = 0.4f;          // ať obrazovka jen neblikne
    private const float FillSpeed = 2.5f;            // plynulost dojezdu lišty

    private RectTransform fill;
    private float shown;

    /// <summary>Nastaví obrázek, který se zobrazí při načítání.</summary>
    public static void SetBackground(Sprite sprite)
    {
        if (sprite != null) background = sprite;
    }

    public static void Load(string sceneName)
    {
        if (active != null) return; // už se něco načítá

        var go = new GameObject("SceneLoader");
        DontDestroyOnLoad(go);
        active = go.AddComponent<SceneLoader>();
        active.BuildUI();
        active.StartCoroutine(active.LoadRoutine(sceneName));
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        gameObject.AddComponent<GraphicRaycaster>(); // blokuje dotyky během načítání

        // Černý podklad pro případ, že obrázek chybí.
        var backdrop = CreateImage("Backdrop", transform, Color.black);
        Stretch(backdrop.rectTransform);

        if (background != null)
        {
            var bg = CreateImage("Background", transform, Color.white);
            bg.sprite = background;
            // Stejně jako v menu: obrázek přesně na celou obrazovku, nic se neořízne.
            Stretch(bg.rectTransform);
        }

        // Lišta: tmavý rámeček, uvnitř zlatá výplň rostoucí zleva.
        var frame = CreateImage("BarFrame", transform, new Color(0f, 0f, 0f, 0.6f));
        var frameRt = frame.rectTransform;
        frameRt.anchorMin = new Vector2(0.5f - BarWidthFraction / 2f, BarBottomFraction);
        frameRt.anchorMax = new Vector2(0.5f + BarWidthFraction / 2f, BarBottomFraction + BarHeightFraction);
        frameRt.offsetMin = frameRt.offsetMax = Vector2.zero;

        var fillImg = CreateImage("BarFill", frameRt, new Color(1f, 0.84f, 0f));
        fill = fillImg.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = new Vector2(0f, 3f);
        fill.offsetMax = new Vector2(0f, -3f);
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // Jeden snímek, aby se načítací obrazovka stihla vykreslit.
        yield return null;

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float display = 0f;
        // Načítání má rozsah 0..0.9, zbytek je aktivace scény.
        while (op.progress < 0.9f || shown < MinShowTime)
        {
            display = Mathf.MoveTowards(display, Mathf.Clamp01(op.progress / 0.9f) * 0.9f, FillSpeed * Time.unscaledDeltaTime);
            SetProgress(display);
            shown += Time.unscaledDeltaTime;
            yield return null;
        }

        SetProgress(0.9f);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // Počkat, až nová scéna doběhne Start() a vykreslí první snímek.
        yield return null;
        yield return new WaitForEndOfFrame();

        while (display < 1f)
        {
            display = Mathf.MoveTowards(display, 1f, FillSpeed * Time.unscaledDeltaTime);
            SetProgress(display);
            yield return null;
        }

        active = null;
        Destroy(gameObject);
    }

    private void SetProgress(float value)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
