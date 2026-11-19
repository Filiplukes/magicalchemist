using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jedna záložka obchodu (místnosti / kotlíky / ohně). Listuje položkami z <see cref="CosmeticDatabase"/>
/// a přes <see cref="Cosmetics"/> je kupuje a vybírá.
/// Název třídy zůstává kvůli odkazům v Shop.prefab.
/// </summary>
public class RoomManager : MonoBehaviour
{
    [SerializeField] private CosmeticType type;
    public Button applyBuyButton;
    public GameObject gemsTable;

    private int index;
    private Coroutine flashCoroutine;
    private TMP_Text buttonText;
    private GameObject gemIcon;
    private Image previewImage;
    private SpriteRenderer previewRenderer;

    private void Awake()
    {
        previewImage = GetComponent<Image>();
        previewRenderer = GetComponent<SpriteRenderer>();

        if (applyBuyButton != null)
        {
            buttonText = applyBuyButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonText != null)
            {
                // "Finish campaign" je delší než cena, zmenší se a zalomí; ostatní texty zůstanou stejně velké.
                buttonText.fontSizeMax = buttonText.fontSize;
                buttonText.fontSizeMin = Mathf.Min(24f, buttonText.fontSize);
                buttonText.enableAutoSizing = true;
            }
            gemIcon = applyBuyButton.GetComponentsInChildren<RawImage>(true)
                .FirstOrDefault(r => r.gameObject.name == "GemIcon")?.gameObject;
        }

        index = Cosmetics.GetSelectedIndex(type);
        Refresh();
    }

    private CosmeticItem Current
    {
        get
        {
            var items = Cosmetics.GetItems(type);
            return items.Count == 0 ? null : items[Mathf.Clamp(index, 0, items.Count - 1)];
        }
    }

    /// <summary>Volá se z tlačítek šipek (+1 / -1).</summary>
    public void AssignNextRoomName(int step)
    {
        int count = Cosmetics.GetItems(type).Count;
        if (count == 0) return;
        index = ((index + step) % count + count) % count;
        Refresh();
    }

    /// <summary>Volá se z tlačítka Koupit / Použít.</summary>
    public void BuyAcitevRoom()
    {
        var result = Cosmetics.BuyOrSelect(Current);
        if (result == CosmeticPurchaseResult.NotEnoughGems || result == CosmeticPurchaseResult.Invalid
            || result == CosmeticPurchaseResult.Locked)
        {
            FlashBuyButtonRed();
            return;
        }

        Refresh();
        if (gemsTable != null)
            gemsTable.GetComponent<GemsTable>().UpdateGems();
    }

    private void Refresh()
    {
        var item = Current;
        if (item == null)
        {
            Debug.LogWarning($"[{nameof(RoomManager)}] Databáze neobsahuje žádné položky typu {type}.");
            return;
        }

        if (previewImage != null) previewImage.sprite = item.Sprite;
        else if (previewRenderer != null) previewRenderer.sprite = item.Sprite;

        UpdateBuyButton(item);
    }

    private void UpdateBuyButton(CosmeticItem item)
    {
        if (applyBuyButton == null) return;

        bool owned = Cosmetics.IsOwned(item);
        bool selected = Cosmetics.IsSelected(item);
        bool locked = Cosmetics.IsLocked(item);

        applyBuyButton.interactable = !selected && !locked;
        if (buttonText != null)
            buttonText.text = locked ? "Finish campaign" : selected ? "Active" : owned ? "Apply" : item.Price.ToString();
        if (gemIcon != null)
            gemIcon.SetActive(!owned && !locked);
    }

    public void FlashBuyButtonRed(float duration = 0.4f)
    {
        if (applyBuyButton == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
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
        flashCoroutine = null;
    }
}
