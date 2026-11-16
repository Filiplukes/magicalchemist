using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class RiseAndScalePotion : MonoBehaviour
{
    public float duration = 2f;          // jak dlouho to bude trvat
    public Vector3 moveUpAmount = new Vector3(0, 3f, 0);  // o kolik jednotek nahoru se objekt posune
    public Vector3 targetScale = new Vector3(0.5f, 0.5f, 0.5f);
    public GameObject smoke;

    Vector3 startPos;
    Vector3 endPos;
    Vector3 startScale;
    private string sceneName;
    private Material glowMaterial;

    private void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;

    }
    void Start()
    {

        startPos = transform.position;
        endPos = startPos + moveUpAmount;

        startScale = Vector3.zero;
        transform.localScale = startScale;

        StartCoroutine(RiseAndGrow());
    }

    IEnumerator RiseAndGrow()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // plynulý přechod (SmoothStep pro hezčí efekt)
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = Vector3.Lerp(startScale, targetScale, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        // na konci nastav přesně cílové hodnoty (pro jistotu)
        transform.position = endPos;
        transform.localScale = targetScale;
        Destroy(gameObject);
    }

    public void SetSprite(string name , int quality)
    {
       
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = GetRating(quality);
        text.color = GetRatingColor(quality);
        // fontMaterial vytváří kopii materiálu – uvolní se v OnDestroy.
        glowMaterial = text.fontMaterial;
        glowMaterial.EnableKeyword("GLOW_ON");
        glowMaterial.SetColor("_GlowColor", text.color);
        glowMaterial.SetFloat("_GlowPower", 0.5f);
        gameObject.GetComponent<SpriteRenderer>().sprite = ResourceCache.Load<Sprite>("Potions/" + name);
        if (name == "Broken") {
            smoke.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
    }

   private string GetRating(int quality)
    {
        GameObject gameManagerObj = GameObject.FindWithTag("GameManager");
        TimerInGame timerScript = null;
        HpScript hpScript = null;


        if (sceneName != "LifeMode")
        {
            timerScript = gameManagerObj.GetComponent<TimerInGame>();
        }
        else {

            hpScript = gameManagerObj.GetComponent<HpScript>();
        }
        

        if (quality >= 80)
        {
            if (sceneName != "LifeMode")
            {
                timerScript.AddTime(20);

            }
            else {

                hpScript.AddLife();
            }
           
            return "Legendary";
        }
        else if (quality >= 65)
        {
            if (sceneName != "LifeMode")
            {
                timerScript.AddTime(15);
            }
            else
            {

                hpScript.AddLife();
            }
            return "Divine";
        }         
        else if (quality >= 50)
        {
            if (sceneName != "LifeMode")
            {
                timerScript.AddTime(10);
            }
            return "Masterwork";
        }
        else if (quality >= 35)
        {
            if (sceneName != "LifeMode")
            {
                timerScript.AddTime(5);
            }
            return "Excellent";
        }
        else if (quality >= 20)
            return "Good";
        else if (quality >= 10)
            return "Poor";
        else
        {
            if (sceneName == "LifeMode")
            {
                hpScript.RemoveLife();
            }

                return "Flop";
        }
    }

    private Color GetRatingColor(int quality)
    {
        if (quality >= 80)
            return new Color(1f, 0.84f, 0f); // Zlatá
        else if (quality >= 65)
            return new Color(0.6f, 0.4f, 1f); // Fialová
        else if (quality >= 50)
            return new Color(0f, 0.8f, 1f); // Tyrkysová
        else if (quality >= 35)
            return new Color(0.3f, 1f, 0.3f); // Zelená
        else if (quality >= 20)
            return Color.white;
        else if (quality >= 10)
            return new Color(0.5f, 0.1f, 0.1f); // Temně červená (Poor)
        else
            return new Color(0.7f, 0.7f, 0.7f); // Šedá (Flop)
    }

}