using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Přehrává dialog kampaně: portrét vlevo / vpravo, bublina se jménem a postupně vypisovaným
/// textem. Klepnutí dopíše řádek, další klepnutí jde dál. Kdo právě nemluví, je ztmavený.
/// Řádky s akcí (kniha, volba) předává volajícímu.
/// </summary>
public class DialoguePlayer : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public Button tapCatcher;
    public Button skipButton;
    public RectTransform leftFrame;
    public Image leftPortrait;
    public RectTransform rightFrame;
    public Image rightPortrait;
    public GameObject bubble;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI lineText;
    public GameObject narrationRoot;
    public TextMeshProUGUI narrationText;
    public GameObject continueHint;

    [Header("Nastavení")]
    public float charsPerSecond = 45f;

    private static readonly Color Dimmed = new Color(0.45f, 0.45f, 0.45f, 1f);
    private const float InactiveScale = 0.9f;

    private readonly Dictionary<string, CampaignCharacter> characters = new Dictionary<string, CampaignCharacter>();
    private bool tapped;
    private bool skipped;

    void Awake()
    {
        tapCatcher.onClick.AddListener(() => tapped = true);
        if (skipButton != null) skipButton.onClick.AddListener(() => { skipped = true; tapped = true; });
        root.SetActive(false);
    }

    public void Init(List<CampaignCharacter> list)
    {
        characters.Clear();
        foreach (var c in list) characters[c.id] = c;
    }

    /// <summary>
    /// Přehraje řádky. Skip přeskočí běžné řádky, ale akce (kniha, volba) se vždy provedou.
    /// </summary>
    public IEnumerator Play(List<CampaignLine> lines, Func<CampaignLine, IEnumerator> onAction)
    {
        if (lines == null || lines.Count == 0) yield break;

        skipped = false;
        HidePortraits();
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line.action))
            {
                root.SetActive(false);
                if (onAction != null) yield return onAction(line);
                skipped = false;
                HidePortraits();
                continue;
            }
            if (skipped) continue;

            root.SetActive(true);
            yield return ShowLine(line);
        }
        root.SetActive(false);
    }

    private IEnumerator ShowLine(CampaignLine line)
    {
        bool narration = line.who == "narrator" || !characters.ContainsKey(line.who ?? "");
        bubble.SetActive(!narration);
        narrationRoot.SetActive(narration);

        TextMeshProUGUI target;
        if (narration)
        {
            leftFrame.gameObject.SetActive(false);
            rightFrame.gameObject.SetActive(false);
            target = narrationText;
        }
        else
        {
            var c = characters[line.who];
            bool left = c.side == "left";
            var frame = left ? leftFrame : rightFrame;
            var portrait = left ? leftPortrait : rightPortrait;
            var sprite = ResourceCache.Load<Sprite>("Campaign/Portraits/" + c.portrait);
            if (sprite != null) portrait.sprite = sprite;
            frame.gameObject.SetActive(true);
            Highlight(left);
            nameText.text = c.name;
            target = lineText;
        }

        target.text = CampaignText.With(line.text, line.cs);
        target.ForceMeshUpdate();
        int total = target.textInfo.characterCount;
        target.maxVisibleCharacters = 0;
        continueHint.SetActive(false);

        // Postupné vypisování; klepnutí ho dokončí.
        tapped = false;
        float shown = 0f;
        while (shown < total && !tapped)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            target.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        target.maxVisibleCharacters = total;
        if (skipped) yield break;

        continueHint.SetActive(true);
        tapped = false;
        while (!tapped) yield return null;
    }

    private void Highlight(bool leftSpeaks)
    {
        SetActiveLook(leftFrame, leftPortrait, leftSpeaks);
        SetActiveLook(rightFrame, rightPortrait, !leftSpeaks);
    }

    private static void SetActiveLook(RectTransform frame, Image portrait, bool active)
    {
        frame.localScale = Vector3.one * (active ? 1f : InactiveScale);
        portrait.color = active ? Color.white : Dimmed;
        var frameImage = frame.GetComponent<Image>();
        if (frameImage != null) frameImage.color = active ? Color.white : Dimmed;
    }

    private void HidePortraits()
    {
        leftFrame.gameObject.SetActive(false);
        rightFrame.gameObject.SetActive(false);
    }
}
