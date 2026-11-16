using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopScript : MonoBehaviour
{
    [SerializeField] private List<GameObject> items;
    [SerializeField] private List<Button> buttons;
    private int activeIndex = 0;
    public void BackMenu()
    {

        Destroy(gameObject);


    }
    public void changeItems(int index) {


        if (index < 0 || index >= items.Count)
            return;

        if (index == activeIndex)
            return;

        // schovej starý aktivní objekt
        if (activeIndex >= 0)
        {
            items[activeIndex].SetActive(false);
            buttons[activeIndex].interactable = true; // znovu aktivuj button
        }

        // nastav nový aktivní
        items[index].SetActive(true);
        buttons[index].interactable = false; // vypni button

        activeIndex = index;

    }
}
