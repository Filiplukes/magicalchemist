
using TMPro;
using UnityEngine;

public class SetName : MonoBehaviour
{

    public TMP_InputField InputName;
    public void SaveName()
    {
        string playerName = InputName.text.Trim();

        if (!string.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            SetNameForLeaerboadr();
            Destroy(gameObject);
        }
        else
        {


        }
    }
    private async void SetNameForLeaerboadr() {
        await LeaderBoardNonDestroyObject.Instance.SetNameForScore();
    }

}
