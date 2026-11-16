using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class AchivDescriptionScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject descriptionObject; // Nastav v Inspectoru
    public GameObject bg; // Nastav v Inspectoru
    public float holdTime = 0.5f;

    private bool isHolding = false;
    private Coroutine holdRoutine;
    private bool isActive = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isActive) {
            isHolding = true;
            holdRoutine = StartCoroutine(HoldCheck());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        if (holdRoutine != null)
            StopCoroutine(holdRoutine);

        descriptionObject.SetActive(false); // Skryj popis pøi puštìní
        bg.SetActive(false); // Skryj popis pøi puštìní
    }

    IEnumerator HoldCheck()
    {
        yield return new WaitForSeconds(holdTime);

        if (isHolding)
        {
            descriptionObject.SetActive(true); // Zobraz popis po podržení
            bg.SetActive(true); // Zobraz popis po podržení
        }
    }

    public void SetIsActiveAchiv(bool isActive) {

        this.isActive = isActive;
    }
}
