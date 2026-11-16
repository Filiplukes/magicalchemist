using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Vygeneruje / doplní kosmetickou databázi z textur v Resources/{Rooms,Cauldrons,Fires}.
/// Existující CosmeticItem assety nepřepisuje (ruční úpravy cen a barev zůstanou),
/// jen přidá nové a znovu sestaví seznamy v databázi.
/// Nový kotlík tedy přidáš tak, že nahraješ CauldronN.png a spustíš tento příkaz.
/// </summary>
public static class CosmeticDatabaseBuilder
{
    private const string ItemsFolder = "Assets/Cosmetics/Items";
    private const string DatabasePath = "Assets/Resources/Cosmetics/CosmeticDatabase.asset";

    private static readonly (CosmeticType type, string prefix, string spriteFolder, int basePrice)[] Sources =
    {
        (CosmeticType.Room, "Room", "Assets/Resources/Rooms", 1000),
        (CosmeticType.Cauldron, "Cauldron", "Assets/Resources/Cauldrons", 500),
        (CosmeticType.Fire, "Fire", "Assets/Resources/Fires", 250),
    };

    // Původní barvy z FlameColor.cs (index = číslo ohně).
    private static readonly (string from, string to)[] FireColors =
    {
        ("00B5FF", "00CAFF"),
        ("1270E5", "1A00FF"),
        ("8312E5", "9A00FF"),
        ("B90500", "D10B17"),
        ("E55312", "FF0000"),
        ("49E512", "00FF44"),
    };

    [MenuItem("Tools/Magic Alchemist/Build Cosmetic Database")]
    public static void Build()
    {
        EnsureFolder(ItemsFolder);
        EnsureFolder(Path.GetDirectoryName(DatabasePath).Replace('\\', '/'));

        var db = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CosmeticDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        int created = 0;
        foreach (var (type, prefix, spriteFolder, basePrice) in Sources)
        {
            // Zachovat stávající pořadí i ručně přidané položky; nové jen připojit na konec.
            var list = db.GetItems(type).Where(i => i != null).Distinct().ToList();
            var regex = new Regex("^" + prefix + @"(\d+)$");
            var found = new List<(int number, CosmeticItem item)>();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder }))
            {
                string texPath = AssetDatabase.GUIDToAssetPath(guid);
                string id = Path.GetFileNameWithoutExtension(texPath);
                var match = regex.Match(id);
                if (!match.Success) continue;
                if (list.Any(i => i.Id == id)) continue;

                int number = int.Parse(match.Groups[1].Value);
                string itemPath = $"{ItemsFolder}/{id}.asset";
                var item = AssetDatabase.LoadAssetAtPath<CosmeticItem>(itemPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<CosmeticItem>();
                    // Stejný vzorec jako původní ItemStateManager.GenerateItems: +50 % za každou pětici.
                    int price = basePrice + (int)(basePrice * 0.5 * (number / 5));
                    item.EditorSetup(id, type, FindSprite(texPath, id), price, number == 0,
                        type == CosmeticType.Fire ? FireGradient(number) : null);
                    AssetDatabase.CreateAsset(item, itemPath);
                    created++;
                }
                found.Add((number, item));
            }

            list.AddRange(found.OrderBy(x => x.number).Select(x => x.item));
            db.EditorSet(type, list);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CosmeticDatabaseBuilder] Hotovo. Nových položek: {created}. " +
                  string.Join(", ", Sources.Select(s => $"{s.type}: {db.GetItems(s.type).Count}")));
        Selection.activeObject = db;
    }

    private static Sprite FindSprite(string texPath, string id)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToList();
        return sprites.FirstOrDefault(s => s.name == id + "_0")
            ?? sprites.FirstOrDefault(s => s.name == id)
            ?? sprites.FirstOrDefault();
    }

    private static Gradient FireGradient(int number)
    {
        if (number < 0 || number >= FireColors.Length) return null;
        ColorUtility.TryParseHtmlString("#" + FireColors[number].from, out var from);
        ColorUtility.TryParseHtmlString("#" + FireColors[number].to, out var to);
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
