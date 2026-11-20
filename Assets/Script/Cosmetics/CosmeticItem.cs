using UnityEngine;

public enum CosmeticType
{
    Room = 0,
    Cauldron = 1,
    Fire = 2
}

/// <summary>
/// Jedna kosmetická položka (místnost, kotlík, oheň).
/// POZOR: <see cref="Id"/> se ukládá do hráčova save souboru – po vydání ho NIKDY neměň,
/// jinak hráči o položku přijdou. Ostatní hodnoty (cena, sprite, barvy) měnit lze.
/// </summary>
[CreateAssetMenu(fileName = "CosmeticItem", menuName = "Magic Alchemist/Cosmetic Item")]
public class CosmeticItem : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private CosmeticType type;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [SerializeField, Min(0)] private int price;
    [Tooltip("Položka je vlastněná od začátku (výchozí kosmetika).")]
    [SerializeField] private bool ownedByDefault;
    [Tooltip("Scéna z kampaně: hráč ji dostane zdarma, až dohraje celou kampaň. " +
             "S cenou 0 je do té doby zamčená, s cenou nad 0 jde koupit hned. " +
             "V kampani se místo ní jako dílna ukáže výchozí místnost.")]
    [SerializeField] private bool campaignReward;

    [Header("Jen pro typ Fire")]
    [Tooltip("Vypnuto = plamen si nechá barvu nastavenou v particle systému.")]
    [SerializeField] private bool overrideFlameColor;
    [SerializeField] private Gradient flameGradient = new Gradient();
    [Tooltip("Za kolik sekund plamen projde celým gradientem (délka smyčky particle systému). " +
             "0 = nechat délku z prefabu (5 s).")]
    [SerializeField, Min(0)] private float flameCycleSeconds;

    public string Id => id;
    public CosmeticType Type => type;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
    public Sprite Sprite => sprite;
    public int Price => price;
    public bool OwnedByDefault => ownedByDefault;
    public bool CampaignReward => campaignReward;
    public bool OverrideFlameColor => overrideFlameColor && flameGradient != null;
    public Gradient FlameGradient => flameGradient;
    public float FlameCycleSeconds => flameCycleSeconds;

#if UNITY_EDITOR
    /// <summary>Jen pro editorový generátor databáze.</summary>
    public void EditorSetup(string id, CosmeticType type, Sprite sprite, int price, bool ownedByDefault, Gradient flameGradient)
    {
        this.id = id;
        this.type = type;
        this.displayName = id;
        this.sprite = sprite;
        this.price = price;
        this.ownedByDefault = ownedByDefault;
        this.overrideFlameColor = flameGradient != null;
        if (flameGradient != null) this.flameGradient = flameGradient;
    }
#endif
}
