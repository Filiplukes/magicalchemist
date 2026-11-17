using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scéna s knihou (level 2). Hráč si přečte stranu 1 a otočí stránku. Strana 2 se ukáže
/// jen na okamžik, pak knihou škubne (mistr ji vytrhne) a scéna skončí.
/// </summary>
public class StoryBook : MonoBehaviour
{
    public GameObject root;
    public RectTransform bookBody;
    public TextMeshProUGUI page1Text;
    public RectTransform page2;
    public TextMeshProUGUI page2Text;
    public Button turnButton;

    [Tooltip("Jak dlouho je strana 2 vidět, než mistr knihu vytrhne.")]
    public float glimpseSeconds = 1.5f;

    private bool turned;

    void Awake()
    {
        turnButton.onClick.AddListener(() => turned = true);
        root.SetActive(false);
    }

    public IEnumerator Play(string pageOne, string pageTwo)
    {
        page1Text.text = pageOne;
        page2Text.text = pageTwo;
        page2.localScale = new Vector3(0f, 1f, 1f);
        page2.gameObject.SetActive(false);
        bookBody.anchoredPosition = Vector2.zero;
        bookBody.localRotation = Quaternion.identity;
        turnButton.gameObject.SetActive(true);
        root.SetActive(true);

        turned = false;
        while (!turned) yield return null;
        turnButton.gameObject.SetActive(false);

        // Otočení stránky: strana 2 se "rozloží" od hřbetu.
        page2.gameObject.SetActive(true);
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            page2.localScale = new Vector3(Mathf.SmoothStep(0f, 1f, t / 0.45f), 1f, 1f);
            yield return null;
        }
        page2.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(glimpseSeconds);

        // Mistr knihu vytrhne: škubnutí a odlet nahoru.
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
