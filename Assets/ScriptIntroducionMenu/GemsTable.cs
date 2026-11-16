using UnityEngine;
using TMPro;

public class GemsTable : MonoBehaviour
{
    private const string PlayerPrefsKey = Cosmetics.GemsPrefsKey;
    private int gems;
    private TMP_Text countText;

    void Awake()
    {
        
        //PlayerPrefs.SetInt(PlayerPrefsKey,10000);
        // Najde TextMeshPro komponentu na child objektu pojmenovaném "Count"
        var countTransform = transform.Find("Count");
        if (countTransform != null)
        {
            countText = countTransform.GetComponent<TMP_Text>();
        }

        // Fallback: najít libovolný TMP_Text v potomcích
        if (countText == null)
        {
            countText = GetComponentInChildren<TMP_Text>(true);
        }

        if (countText == null)
        {
            Debug.LogWarning($"[{nameof(GemsTable)}] Nenašel jsem TMP_Text v child objektu 'Count'. Zobrazení nebude fungovat.");
        }

        UpdateGems();
    }

    // Zkusí odeèíst daný poèet gemù.
    // Pokud je požadované množství vìtší než aktuální, vrátí false a nic neodeète.
    // Pokud je množství menší nebo rovno, odeète, uloží do PlayerPrefs a vrátí true.
    public bool TrySpendGems(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (amount > gems)
        {
            return false;
        }

        gems -= amount;
        SaveAndRefresh();
        return true;
    }

    public void UpdateGems() {

        gems = PlayerPrefs.GetInt(PlayerPrefsKey, 0);
        UpdateText();
    }

    // Pomocná metoda pro pøidání gemù
    public void AddGems(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gems += amount;
        SaveAndRefresh();
    }

    // Vrací aktuální poèet gemù
    public int CurrentGems => gems;

    private void SaveAndRefresh()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, gems);
        PlayerPrefs.Save();
        UpdateText();
    }

    private void UpdateText()
    {
        if (countText != null)
        {
            countText.text = gems.ToString();
        }
    }
}