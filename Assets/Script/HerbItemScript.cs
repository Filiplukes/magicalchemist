using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HerbItemScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string herbName; // tøeba "Virelen"

    private bool isDragging = false;
    private GameObject currentHerbObj;
    private RectTransform canvasRectTransform;
    public GameManager gameManager;

    void Start()
    {
        GameObject canvasObj = GameObject.FindWithTag("Controll");
        if (canvasObj != null)
            canvasRectTransform = canvasObj.GetComponent<RectTransform>();
        else
            Debug.LogWarning("Canvas s tagem 'Controll' nebyl nalezen.");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentHerbObj = Instantiate(Resources.Load<GameObject>("Prefabs/Herb"));

        if (canvasRectTransform != null)
        {
            currentHerbObj.transform.SetParent(canvasRectTransform, false);
        }
        else
        {
            Debug.LogWarning("Canvas RectTransform nenalezen, nelze nastavit parent.");
        }

        HerbPrefabScript script = currentHerbObj.GetComponent<HerbPrefabScript>();
        if (script != null)
        {
            script.herbName = herbName;

            Sprite sprite = Resources.Load<Sprite>($"Herbs/{herbName}");
            if (sprite != null)
            {
                Image image = currentHerbObj.GetComponent<Image>();
                if (image != null)
                    image.sprite = sprite;
                else
                    Debug.LogWarning("Prefab nemá komponentu Image.");
            }
            else
            {
                Debug.LogWarning($"Sprite '{herbName}' nebyl nalezen v Resources/Herbs.");
            }
        }

        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        if (currentHerbObj != null)
        {
            var cauldorn = GameObject.FindWithTag("Cauldorn");
            Vector3 cauldornScreenPos = Camera.main.WorldToScreenPoint(cauldorn.transform.position);
            Vector3 uiScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();

            float distance = Vector2.Distance(uiScreenPos, cauldornScreenPos);

            if (distance < 300f)
            {
                float height = cauldorn.GetComponent<Renderer>().bounds.size.y;
                float offsetY = height * 0.1f;

                Vector3 kotelTop = new Vector3(
                cauldorn.GetComponent<Renderer>().bounds.center.x,
                cauldorn.GetComponent<Renderer>().bounds.max.y-offsetY,
                cauldorn.GetComponent<Renderer>().bounds.center.z
                );
                // animace s efektem
                StartCoroutine(FlyToTargetAndShrink(currentHerbObj.GetComponent<RectTransform>(), kotelTop));
                gameManager.GetComponent<GameManager>().SetUsedHerbs(herbName); 
            }
            else
            {
                Destroy(currentHerbObj); // nebo vrácení zpìt
            }

            currentHerbObj = null;
        }
    }

    void Update()
    {
        if (isDragging && currentHerbObj != null)
        {

            Vector2 touchPos;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                // Získání pozice dotyku
                Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();

                // Pøevod na lokální pozici v rámci canvasu
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPos, null, out touchPos))
                {
                    currentHerbObj.GetComponent<RectTransform>().localPosition = touchPos;
                }
            }
        }
    }



    IEnumerator FlyToTargetAndShrink(RectTransform uiObj, Vector3 targetWorldPos)
    {

        if (Camera.main == null)
        {
            Debug.LogError("Camera.main is null!");
            yield break;
        }

        if (uiObj == null)
        {
            Debug.LogError("uiObj is null!");
            yield break;
        }

        Vector3 start = uiObj.position;
        Vector3 target = Camera.main.WorldToScreenPoint(targetWorldPos);

        Vector3 startScale = uiObj.localScale;
        Vector3 endScale = Vector3.zero;

        float duration = 0.5f;
        float t = 0f;
        
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            uiObj.position = Vector3.Lerp(start, target, t);
            uiObj.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
                   
        Destroy(uiObj.gameObject);
    }


}
