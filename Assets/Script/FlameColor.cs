using UnityEngine;

public class FlameColor : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;
    private Gradient[] gradients;
    private string PlayerPrefsKey = "Fire";

    private void Awake()
    {
        var indexString = PlayerPrefs.GetString(PlayerPrefsKey,"Fire0_0");
        int start = "Fire".Length;          // zaèátek za slovem Fire
        int end = indexString.IndexOf("_");         // pozice prvního _

        int index = int.Parse(indexString.Substring(start, end - start));
        gradients = new Gradient[]
        {
            CreateGradient("00B5FF", "00CAFF"),                      
            CreateGradient("1270E5", "1A00FF"),
            CreateGradient("8312E5", "9A00FF"),
            CreateGradient("B90500", "D10B17"),
            CreateGradient("E55312", "FF0000"),
            CreateGradient("49E512", "00FF44")
        };
        SetColor(index);
        particle.Play();
    }

    public void SetColor(int index)
    {
        if (index < 0 || index >= gradients.Length)
            return;

        var main = particle.main;
        main.startColor = gradients[index];
    }

    private Gradient CreateGradient(string startHex, string endHex)
    {
        Color start;
        Color end;

        ColorUtility.TryParseHtmlString("#" + startHex, out start);
        ColorUtility.TryParseHtmlString("#" + endHex, out end);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(start, 0f),
                new GradientColorKey(end, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        return gradient;
    }
}
