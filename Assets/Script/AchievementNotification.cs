using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AchievementNotification : MonoBehaviour
{
    public float slideTime = 0.5f;          // èas pro vjetí a zajetí
    public float visibleTime = 2.0f;        // jak dlouho zùstane na obrazovce
    public Vector2 hiddenPos = new Vector2(-500f, 0f);
    public Vector2 visiblePos = new Vector2(0f, 0f);
    public TextMeshProUGUI textNameOfAchiv;

    private List<string> queNameAchiv = new List<string>();
    private bool activeNotif = false;

    private Coroutine currentRoutine;

    public void ShowNotification(string name)
    {
        if (!activeNotif)
        {
            gameObject.SetActive(true);
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }
            textNameOfAchiv.text = name;
            currentRoutine = StartCoroutine(SlideNotification());
            activeNotif = true;
        }
        else {
            queNameAchiv.Add(name);
        }
    }

    IEnumerator SlideNotification()
    {
        GetComponent<AudioSource>().Play();
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        

        // Slide in
        yield return StartCoroutine(MoveUI(rect, hiddenPos, visiblePos, slideTime));

        // Wait
        yield return new WaitForSeconds(visibleTime);

        // Slide out
        yield return StartCoroutine(MoveUI(rect, visiblePos, hiddenPos, slideTime));

        gameObject.SetActive(false);
        activeNotif = false;
        if (queNameAchiv.Count > 0)
        {
            string nextName = queNameAchiv[0];
            queNameAchiv.RemoveAt(0);
            ShowNotification(nextName);
        }
    }

    IEnumerator MoveUI(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector2.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = to;
    }
}
