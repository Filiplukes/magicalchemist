using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HerbItemScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string herbName; // třeba "Virelen"

    private bool isDragging = false;
    private GameObject currentHerbObj;
    private RectTransform canvasRectTransform;
    private RectTransform currentHerbRect;
    private Renderer cauldronRenderer;
    public GameManager gameManager;

    void Start()
    {
        GameObject canvasObj = GameObject.FindWithTag("Controll");
        if (canvasObj != null)
            canvasRectTransform = canvasObj.GetComponent<RectTransform>();
        else
            Debug.LogWarning("Canvas s tagem 'Controll' nebyl nalezen.");

        var cauldronObj = GameObject.FindWithTag("Cauldorn");
        if (cauldronObj != null)
            cauldronRenderer = cauldronObj.GetComponent<Renderer>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentHerbObj = Instantiate(ResourceCache.Load<GameObject>("Prefabs/Herb"));
        currentHerbRect = currentHerbObj.GetComponent<RectTransform>();

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

            Sprite sprite = ResourceCache.Load<Sprite>($"Herbs/{herbName}");
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
            Vector3 cauldornScreenPos = Camera.main.WorldToScreenPoint(cauldronRenderer.transform.position);
            Vector3 uiScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();

            float distance = Vector2.Distance(uiScreenPos, cauldornScreenPos);

            if (distance < 300f)
            {
                Bounds bounds = cauldronRenderer.bounds;
                float offsetY = bounds.size.y * 0.1f;

                Vector3 kotelTop = new Vector3(
                bounds.center.x,
                bounds.max.y - offsetY,
                bounds.center.z
                );
                // animace s efektem
                StartCoroutine(FlyToTargetAndShrink(currentHerbRect, kotelTop));
                gameManager.SetUsedHerbs(herbName); 
            }
            else
            {
                Destroy(currentHerbObj); // nebo vrácení zpět
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

                // Převod na lokální pozici v rámci canvasu
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPos, null, out touchPos))
                {
                    currentHerbRect.localPosition = touchPos;
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
