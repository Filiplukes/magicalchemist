using UnityEditor;
using UnityEngine;

/// <summary>
/// Nástroje pro testování kampaně v Editoru. Ve hře žádné tlačítko pro reset není.
/// </summary>
public static class CampaignProgressTools
{
    [MenuItem("Tools/Magic Alchemist/Reset Campaign Progress")]
    private static void ResetProgress()
    {
        var data = CampaignData.Load();
        if (data == null) return;
        if (!EditorUtility.DisplayDialog("Reset Campaign Progress",
                "Smazat postup v kampani (dohrané levely a zhlédnuté konce)?\n" +
                "Achievementy a odemčené recepty zůstanou.", "Smazat", "Zrušit"))
            return;

        CampaignProgress.Reset(data.levels, data.endings);
        Debug.Log("Postup v kampani smazán.");
    }
}
