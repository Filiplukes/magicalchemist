using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scéna s knihou (level 2). Kniha je otevřená: vlevo text knihy, vpravo (volitelně) jeho překlad.
/// Jakmile se hráč pokusí otočit stránku, mistr mu knihu hned vytrhne a scéna skončí.
/// </summary>
public class StoryBook : MonoBehaviour
{
    public GameObject root;
    public RectTransform bookBody;
    public TextMeshProUGUI page1Text;
    public RectTransform page2;
    public TextMeshProUGUI page2Text;
    public Button turnButton;

    [Header("Písmo stránek")]
    public float pageFontMin = 50f;
    public float pageFontMax = 80f;
    [Tooltip("Řádkování v TMP jednotkách, záporné = řádky blíž u sebe.")]
    public float pageLineSpacing = -12f;

    private bool turned;

    void Awake()
    {
        turnButton.onClick.AddListener(() => turned = true);
        ApplyPageFont(page1Text);
        ApplyPageFont(page2Text);
        root.SetActive(false);
    }

    private void ApplyPageFont(TextMeshProUGUI text)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = pageFontMin;
        text.fontSizeMax = pageFontMax;
        text.lineSpacing = pageLineSpacing;
    }

    /// <param name="page">Text knihy na levé stránce.</param>
    /// <param name="facingPage">Text na pravé stránce (překlad); prázdný = pravá stránka se neukáže.</param>
    public IEnumerator Play(string page, string facingPage)
    {
        page1Text.text = page;
        bool facing = !string.IsNullOrEmpty(facingPage);
        page2Text.text = facing ? facingPage : "";
        page2.localScale = Vector3.one;
        page2.gameObject.SetActive(facing);
        bookBody.anchoredPosition = Vector2.zero;
        bookBody.localRotation = Quaternion.identity;
        turnButton.gameObject.SetActive(true);
        root.SetActive(true);

        turned = false;
        while (!turned) yield return null;
        turnButton.gameObject.SetActive(false);

        // Mistr knihu vytrhne hned, jak chce hráč otočit: škubnutí a odlet nahoru.
        Vector2 start = bookBody.anchoredPosition;
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            bookBody.anchoredPosition = start + Random.insideUnitCircle * 25f;
            yield return null;
        }
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.35f;
            bookBody.anchoredPosition = start + new Vector2(300f * k, 1400f * k * k);
            bookBody.localRotation = Quaternion.Euler(0f, 0f, -25f * k);
            yield return null;
        }
        root.SetActive(false);
    }
}
