using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HpScript : MonoBehaviour
{
    private int maxHP = 3;
    private int aktualHP = 3;
    private GameObject ad;
    public GameManager gameManager;
    [SerializeField] private Transform parent;
    private Image[] images;
    void Start()
    {
        ad = GameObject.Find("AdIns");
        images = parent.GetComponentsInChildren<Image>(includeInactive: true)
               .Where(img => img.transform != parent)
               .OrderBy(img => img.transform.GetSiblingIndex())
               .ToArray();
    }

    void EndRound()
    {
        gameManager.EndGame("LifeMode");
        if (ad != null) { ad.GetComponent<AdIns>().ShowInterstitialAd(); }
    }

    public void AddLife() {
        if (aktualHP < maxHP) {
            aktualHP++;
            ShowAktualLife(aktualHP-1, true);
        }

    }

    public void RemoveLife()
    {
        aktualHP--;
        ShowAktualLife(aktualHP,false);
        if (aktualHP < 1)
        {
            EndRound();
        }
    }

    public void ShowAktualLife(int index , bool visible)
    {
        if (index >= 0 && index < images.Length)
            images[index].enabled = visible;
    }

}
