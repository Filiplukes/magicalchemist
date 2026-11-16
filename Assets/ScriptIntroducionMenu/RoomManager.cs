using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class RoomManager : MonoBehaviour
{
    public string PlayerPrefsKey;
    [Tooltip("Základní název pro místnost (bude použito jako prefix), napø. 'Room'")]
    public string baseName;
    public string baseName1 = "_0";
    public Button applyBuyButton;
    public GameObject gemsTable;
    public string managerName;
    public string fileWithSprites;
    public int basePrice;

    private string _currentName;
    private int max = 0;
    private int assigned = 0;
    private ItemStateManager itemManager;
    private ItemState aktualItemData;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        // Pokud je v PlayerPrefs uložené vlastní jméno, naèti ho (volitelné chování)
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            _currentName = PlayerPrefs.GetString(PlayerPrefsKey);
            string numberPart = _currentName.Replace(baseName, "");
            numberPart = numberPart.Split('_')[0];
            assigned = int.Parse(numberPart);
        }
        else
        {
            _currentName = baseName+"0_0";
            PlayerPrefs.SetString(PlayerPrefsKey, _currentName);
        }    
        max = GetMaxRoomNumber();
        Debug.Log($"Awake: Loaded room name: {_currentName}, max room number: {max}");
        itemManager = new ItemStateManager(managerName);
        itemManager.GenerateItems(max, baseName, baseName, basePrice);
        ApplySpriteByCurrentName(_currentName.Substring(0, _currentName.Length - 2));

    }



    // Vrátí aktuální (interní) uložené jméno Room
    public string CurrentRoomName => _currentName;

    // Uloží aktuální jméno do PlayerPrefs
    public void SaveRoomName()
    {
        if (string.IsNullOrEmpty(_currentName))
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] Nelze uložit prázdné jméno.");
            return;
        }

        PlayerPrefs.SetString(PlayerPrefsKey, _currentName);
        PlayerPrefs.Save();
    }

    // Nastaví jméno ruènì (pokud chcete)
    public void SetRoomName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] SetRoomName: neplatné jméno.");
            return;
        }

        _currentName = name;
 
    }

    public void AssignNextRoomName(int value)
    {           
   
        if ((assigned <= 0) && (value == -1))
        {
            assigned = max;
        }
        else if ((assigned >= max) && (value == 1))
        {
            // pokud dosáhneme (nebo pøekroèíme) maximální poèet, vrátíme na 0
            assigned = 0;
        }
        else
        {
            assigned += value;
        }

        string newName = $"{baseName}{assigned}{baseName1}";
        _currentName = newName;
       
        ApplySpriteByCurrentName( $"{baseName}{assigned}");
        //SaveRoomName();
        Debug.Log($"[{nameof(RoomManager)}] Assigned new room name: {newName} (value param: {assigned}, maxSprites: {max})");
    }

    // Naète spritey z Resources/Rooms a vrátí jejich poèet (používá se jako 'max')
    private int GetMaxRoomNumber()
    {
        try
        {
            var sprites = Resources.LoadAll<Sprite>(fileWithSprites);
            Debug.Log(fileWithSprites);
            Debug.Log(sprites.Length);
            foreach (var sprite in sprites)
            {
                Debug.Log(sprite.name);
            }
            return sprites != null ? sprites.Length-1 : 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] Chyba pøi naèítání Resources/: {ex.Message}");
            return 0;
        }
    }

    public void ApplySpriteByCurrentName(string shortName,bool setNativeSize = false)
    {
        if (string.IsNullOrEmpty(_currentName))
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] ApplySpriteByCurrentName: _currentName je prázdné.");
            return;
        }

        aktualItemData = itemManager.GetById(shortName);
        UpdateBuyButton(aktualItemData);
        Sprite found = null;
        try
        {
            var sprites = Resources.LoadAll<Sprite>(fileWithSprites);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"[{nameof(RoomManager)}] Nebyly nalezeny žádné sprite v Resources/.");
                return;
            }

            foreach (var s in sprites)
            {
                if (s != null && string.Equals(s.name, _currentName, StringComparison.OrdinalIgnoreCase))
                {
                    found = s;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] Chyba pøi naèítání sprite z Resources/: {ex.Message}");
            return;
        }

        if (found == null)
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] Sprite s názvem '{_currentName}' nebyl nalezen v Resources/.");
            return;
        }

        // Pokud je komponenta Image (UI), nastavíme sprite
        var img = GetComponent<Image>();
        if (img != null)
        {
            img.sprite = found;
            if (setNativeSize)
            {
                try { img.SetNativeSize(); } catch { /* ignorovat pøípadné chyby */ }
            }
            return;
        }

        // Fallback na SpriteRenderer (2D)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = found;
            return;
        }

        Debug.LogWarning($"[{nameof(RoomManager)}] ApplySpriteByCurrentName: Tento GameObject nemá komponentu Image ani SpriteRenderer.");
    }

    public void UpdateBuyButton(ItemState item)
    {
        if (applyBuyButton == null) return;
     
        // najdi Text uvnitø buttonu
        TMP_Text btnText = applyBuyButton.GetComponentInChildren<TMP_Text>();
        // najdi RawImage s názvem GemIcon uvnitø buttonu
        RawImage gemImage = applyBuyButton.GetComponentsInChildren<RawImage>(true)
                            .FirstOrDefault(r => r.gameObject.name == "GemIcon");

        if (btnText == null) Debug.LogWarning("Button nemá Text komponentu!");
        if (gemImage == null) Debug.LogWarning("Button nemá RawImage s názvem GemIcon!");

        // logika podle stavu itemu
        if (item.Owned && item.Active)
        {
            // Vlastnìný a aktivní
            applyBuyButton.interactable = false;
            if (btnText != null) btnText.text = "Active";
            if (gemImage != null) gemImage.gameObject.SetActive(false);
        }
        else if (item.Owned && !item.Active)
        {
            // Vlastnìný ale neaktivní
            applyBuyButton.interactable = true;
            if (btnText != null) btnText.text = "Apply";
            if (gemImage != null) gemImage.gameObject.SetActive(false);
        }
        else if (!item.Owned)
        {
            // Nevlastnìný
            applyBuyButton.interactable = true;
            if (btnText != null) btnText.text = item.Price.ToString();
            if (gemImage != null) gemImage.gameObject.SetActive(true);
        }
    }

    public void BuyAcitevRoom()
    {

        if (aktualItemData == null)
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] BuyRoom: aktualItemData je null.");
            return;
        }

        if (itemManager.BuyOrActivate(aktualItemData.Id))
        {
            UpdateBuyButton(itemManager.GetById(aktualItemData.Id));
            PlayerPrefs.SetString(PlayerPrefsKey, _currentName);
            gemsTable.GetComponent<GemsTable>().UpdateGems();
        }
        else {
            FlashBuyButtonRed();
        }
    }

    public void FlashBuyButtonRed(float duration = 0.4f)
    {
        if (applyBuyButton == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        Image buttonImage = applyBuyButton.GetComponent<Image>();
        if (buttonImage == null) yield break;

        Color originalColor = buttonImage.color;

        buttonImage.color = Color.red;

        yield return new WaitForSeconds(duration);

        buttonImage.color = originalColor;
    }
}