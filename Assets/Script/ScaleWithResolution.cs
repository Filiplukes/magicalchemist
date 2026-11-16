using UnityEngine;

public class ScaleWithResolution : MonoBehaviour
{
    private void Awake()
    {
        var room = Cosmetics.GetSelected(CosmeticType.Room);
        var sr = GetComponent<SpriteRenderer>();
        if (room != null && room.Sprite != null && sr != null)
            sr.sprite = room.Sprite;
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
}
