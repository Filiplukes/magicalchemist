
using TMPro;
using UnityEngine;

public class SetName : MonoBehaviour
{

    public TMP_InputField InputName;
    public void SaveName()
    {
        // Unity jmeno s mezerou odmitne, proto se ocisti (mezery -> podtrzitko, max. delka).
        string playerName = NameRules.Sanitize(InputName.text);

        if (!string.IsNullOrEmpty(playerName) && NameRules.IsAllowed(playerName))
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            SetNameForLeaerboadr();
            Destroy(gameObject);
        }
        else
        {
            // Prazdne nebo zakazane jmeno: vymazat a poprosit o jine.
            InputName.text = "";
            if (InputName.placeholder is TMP_Text placeholder)
                placeholder.text = string.IsNullOrEmpty(playerName) ? "Enter a name" : "Choose another name";
        }
    }
    private async void SetNameForLeaerboadr() {
        await LeaderBoardNonDestroyObject.Instance.SetNameForScore();
    }

}
