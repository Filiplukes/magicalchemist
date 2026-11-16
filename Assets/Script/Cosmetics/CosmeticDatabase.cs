using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seznam veškeré kosmetiky ve hře. Leží v Resources/Cosmetics/CosmeticDatabase.asset.
/// Pořadí v seznamech = pořadí v obchodě.
/// </summary>
[CreateAssetMenu(fileName = "CosmeticDatabase", menuName = "Magic Alchemist/Cosmetic Database")]
public class CosmeticDatabase : ScriptableObject
{
    public const string ResourcesPath = "Cosmetics/CosmeticDatabase";

    [SerializeField] private List<CosmeticItem> rooms = new List<CosmeticItem>();
    [SerializeField] private List<CosmeticItem> cauldrons = new List<CosmeticItem>();
    [SerializeField] private List<CosmeticItem> fires = new List<CosmeticItem>();

    public IReadOnlyList<CosmeticItem> GetItems(CosmeticType type)
    {
        switch (type)
        {
            case CosmeticType.Room: return rooms;
            case CosmeticType.Cauldron: return cauldrons;
            case CosmeticType.Fire: return fires;
            default: return System.Array.Empty<CosmeticItem>();
        }
    }

    public CosmeticItem FindById(CosmeticType type, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var item in GetItems(type))
        {
            if (item != null && item.Id == id) return item;
        }
        return null;
    }

    /// <summary>První položka vlastněná od začátku, jinak první v seznamu.</summary>
    public CosmeticItem GetDefault(CosmeticType type)
    {
        var items = GetItems(type);
        foreach (var item in items)
        {
            if (item != null && item.OwnedByDefault) return item;
        }
        return items.Count > 0 ? items[0] : null;
    }

#if UNITY_EDITOR
    public void EditorSet(CosmeticType type, List<CosmeticItem> items)
    {
        switch (type)
        {
            case CosmeticType.Room: rooms = items; break;
            case CosmeticType.Cauldron: cauldrons = items; break;
            case CosmeticType.Fire: fires = items; break;
        }
    }

    private void OnValidate()
    {
        var seen = new HashSet<string>();
        foreach (CosmeticType type in System.Enum.GetValues(typeof(CosmeticType)))
        {
            foreach (var item in GetItems(type))
            {
                if (item == null) continue;
                if (item.Type != type)
                    Debug.LogWarning($"[CosmeticDatabase] '{item.name}' má typ {item.Type}, ale je v seznamu {type}.", item);
                if (string.IsNullOrEmpty(item.Id))
                    Debug.LogWarning($"[CosmeticDatabase] '{item.name}' nemá Id.", item);
                else if (!seen.Add(item.Id))
                    Debug.LogError($"[CosmeticDatabase] Duplicitní Id '{item.Id}'.", item);
            }
        }
    }
#endif
}
