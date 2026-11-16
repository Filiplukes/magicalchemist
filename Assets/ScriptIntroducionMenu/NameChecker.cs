using UnityEngine;

public class NameChecker : MonoBehaviour
{
    public GameObject inputPanel; // Panel s inputem pro jméno
   
    private const string PlayerNameKey = "PlayerName";

    // Tuto metodu pøipojíš na OnClick tlaèítka
    public bool CheckName()
    {
        if (PlayerPrefs.HasKey(PlayerNameKey))
        {
            PlayerPrefs.GetString(PlayerNameKey);
            return true;
            // Tady mùžeš pokraèovat, protože jméno existuje
        }
        else
        {
            // Pokud jméno není, ukaž input pro vyplnìní
            Instantiate(inputPanel);
            return false;
        }
    }

}