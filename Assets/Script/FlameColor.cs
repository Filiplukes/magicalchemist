using UnityEngine;

/// <summary>Obarví plamen pod kotlíkem podle vybrané kosmetiky typu Fire.</summary>
public class FlameColor : MonoBehaviour
{
    [SerializeField] private ParticleSystem particle;

    private void Awake()
    {
        var fire = Cosmetics.GetSelected(CosmeticType.Fire);
        if (fire != null && fire.OverrideFlameColor && particle != null)
        {
            var main = particle.main;
            main.startColor = fire.FlameGradient;
            // Délku smyčky jde měnit jen u zastaveného systému (playOnAwake je vypnuté).
            if (fire.FlameCycleSeconds > 0f && !particle.isPlaying)
                main.duration = fire.FlameCycleSeconds;
        }
        if (particle != null) particle.Play();
    }
}
