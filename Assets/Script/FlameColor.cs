using UnityEngine;

/// <summary>Obarví plamen pod kotlíkem podle vybrané kosmetiky typu Fire.</summary>
public class FlameColor : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;

    private void Awake()
    {
        var fire = Cosmetics.GetSelected(CosmeticType.Fire);
        if (fire != null && particle != null)
        {
            var main = particle.main;
            main.startColor = fire.FlameGradient;
        }
        if (particle != null) particle.Play();
    }
}
