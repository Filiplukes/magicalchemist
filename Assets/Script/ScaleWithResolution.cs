using UnityEngine;
using UnityEngine.Windows;

public class ScaleWithResolution : MonoBehaviour
{
    private const string PlayerPrefsKey = "RoomName";
    private void Awake()
    {
        LoadSpriteFromResources(PlayerPrefs.GetString(PlayerPrefsKey));

    }
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Získáme velikost sprite v jednotkách
        float width = sr.sprite.bounds.size.x;
        float height = sr.sprite.bounds.size.y;

        // Výška orthographic kamery je od středu k hornímu okraji → 2x pro celou obrazovku
        float worldScreenHeight = Camera.main.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * Screen.width / Screen.height;

        // Spočítáme scale, aby sprite pokryl celou obrazovku
        transform.localScale = new Vector3(
            worldScreenWidth / width,
            worldScreenHeight / height,
            1f
        );
    }

    public void LoadSpriteFromResources(string spriteName)
    {
        spriteName = spriteName.Substring(0, spriteName.Length - 2);
        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.LogWarning("LoadSpriteFromResources: spriteName je prázdný.");
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogWarning("Na objektu není SpriteRenderer!");
            return;
        }

        Sprite loadedSprite = Resources.Load<Sprite>("Rooms/" + spriteName);

        if (loadedSprite == null)
        {
            Debug.LogWarning($"Sprite '{spriteName}' nebyl nalezen v Resources/Rooms.");
            return;
        }

        sr.sprite = loadedSprite;
    }
}