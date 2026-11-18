using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HerbItemScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IDragHandler
{
    public string herbName; // třeba "Virelen"

    // Prst (nebo myš), který bylinku drží. Ostatní dotyky se ignorují.
    private const int NoPointer = int.MinValue;
    private int activePointerId = NoPointer;
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
        // Bylinku už drží jiný prst: druhý dotyk nesmí vytvořit další ani ji přebrat.
        if (activePointerId != NoPointer) return;

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
                {
                    image.sprite = sprite;
                    PoisonEffect.ApplyTo(image); // tažená bylinka se otráví stejně jako výběr
                }
                else
                    Debug.LogWarning("Prefab nemá komponentu Image.");
            }
            else
            {
                Debug.LogWarning($"Sprite '{herbName}' nebyl nalezen v Resources/Herbs.");
            }
        }

        activePointerId = eventData.pointerId;
        MoveHerbTo(eventData);
    }

    // Bylinka má jet za prstem hned, ne až po prahu tažení EventSystemu.
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        MoveHerbTo(eventData);
    }

    private void MoveHerbTo(PointerEventData eventData)
    {
        if (currentHerbRect == null || canvasRectTransform == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, null, out Vector2 localPos))
            currentHerbRect.localPosition = localPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        activePointerId = NoPointer;

        if (currentHerbObj != null)
        {
            // Bez kotle (nebo kamery) nejde trefit, bylinka se jen zahodí.
            float distance = float.MaxValue;
            if (cauldronRenderer != null && Camera.main != null)
            {
                Vector3 cauldornScreenPos = Camera.main.WorldToScreenPoint(cauldronRenderer.transform.position);
                distance = Vector2.Distance(eventData.position, cauldornScreenPos);
            }

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

    // Konec hry nebo zničení panelu během tažení: bylinka nesmí zůstat viset na obrazovce.
    void OnDisable()
    {
        activePointerId = NoPointer;
        if (currentHerbObj != null)
        {
            Destroy(currentHerbObj);
            currentHerbObj = null;
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
