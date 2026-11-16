using UnityEngine;

public class HerbPrefabScript : MonoBehaviour
{
    public GameObject controll;
    public string herbName;


    void Start()
    {
        controll = GameObject.FindWithTag("Controll");

        transform.SetParent(controll.transform, false);
        transform.SetAsLastSibling();

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
    }
}