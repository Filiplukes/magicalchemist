using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PoisonEffect : MonoBehaviour
{
    [Header("Volume")]
    public Volume volume;

  
    private ColorAdjustments colorAdjust;

    [Header("Poison nastavení")]
    public Color poisonColor = Color.green;
    [Tooltip("Maximální síla rozmazání. URP Gaussian blur dovoluje jen 0.5–1.5.")]
    [Range(0.5f, 1.5f)]
    public float maxBlur = 1.5f;
    public float basePulseSpeed = 1f;     // minimální rychlost pulzu
    public float maxPulseSpeed = 5f;      // maximální rychlost pulzu
    [Tooltip("Jak dlouho trvá, než se pohled dožene na novou sílu jedu.")]
    public float blurEaseDuration = 1.6f;
    [Tooltip("Při chybě se pohled rozmaže rychleji.")]
    public float errorBlurEaseDuration = 0.35f;

    [Header("Poison hodnoty")]
    [Range(0f, 1f)]
    public float poisonAmount = 0f;       // aktuální síla jedu (0–1)

    private float pulseTime;
    private float blurAmount;
    private float blurVelocity;
    private float currentBlurEase = 1.6f;
    private DepthOfField depthOfField;
    private bool idleApplied;

    void Awake()
    {
        if (volume.profile.TryGet(out colorAdjust))
        {
            colorAdjust.active = true;
            colorAdjust.colorFilter.overrideState = true;
            colorAdjust.colorFilter.value = Color.white;
            colorAdjust.contrast.overrideState = true;
            colorAdjust.saturation.overrideState = true;
            colorAdjust.contrast.value = 0f;
            colorAdjust.saturation.value = 0f;
        }

        if (volume.profile.TryGet(out depthOfField))
        {
            depthOfField.active = true;
            depthOfField.mode.overrideState = true;
            depthOfField.mode.value = DepthOfFieldMode.Off;
            depthOfField.gaussianStart.overrideState = true;
            depthOfField.gaussianStart.value = 0f;
            depthOfField.gaussianEnd.overrideState = true;
            depthOfField.gaussianEnd.value = 2f;
            depthOfField.gaussianMaxRadius.overrideState = true;
            depthOfField.gaussianMaxRadius.value = 0.5f;
            depthOfField.highQualitySampling.overrideState = true;
            depthOfField.highQualitySampling.value = true;
        }

        currentBlurEase = blurEaseDuration;
    }
    private void Start()
    {
        SetPoison(0.01f); // malá hodnota
        SetPoison(0f);
    }

    void Update()
    {
        // Bez jedu a s ustáleným rozmazáním není co přepočítávat – neutrální hodnoty stačí nastavit jednou.
        if (poisonAmount <= 0f && blurAmount < 0.0001f)
        {
            if (!idleApplied)
            {
                blurAmount = 0f;
                blurVelocity = 0f;
                if (colorAdjust != null)
                {
                    colorAdjust.colorFilter.value = Color.white;
                    colorAdjust.contrast.value = 0f;
                    colorAdjust.saturation.value = 0f;
                }
                ApplyBlur(0f);
                idleApplied = true;
            }
            return;
        }
        idleApplied = false;

        pulseTime += Time.deltaTime;
        float currentPulseSpeed = Mathf.Lerp(basePulseSpeed, maxPulseSpeed, poisonAmount);
        float pulse = (Mathf.Sin(pulseTime * currentPulseSpeed) + 1f) / 2f;
        float intensity = Mathf.Lerp(poisonAmount * 0.5f, poisonAmount, pulse);


        // Barva
        colorAdjust.colorFilter.value = Color.Lerp(Color.white, poisonColor, intensity);
        colorAdjust.contrast.value = Mathf.Lerp(0, -10f, intensity);
        colorAdjust.saturation.value = Mathf.Lerp(0, -30f, intensity);

        blurAmount = Mathf.SmoothDamp(blurAmount, poisonAmount, ref blurVelocity, currentBlurEase);
        ApplyBlur(blurAmount);
    }

    void ApplyBlur(float amount)
    {
        if (depthOfField == null)
            return;

        if (amount < 0.02f)
        {
            depthOfField.mode.value = DepthOfFieldMode.Off;
            return;
        }

        depthOfField.mode.value = DepthOfFieldMode.Gaussian;
        float radius = Mathf.Lerp(0.5f, Mathf.Clamp(maxBlur, 0.5f, 1.5f), amount);
        depthOfField.gaussianMaxRadius.value = radius;
    }

    // 🔼 Zvýší poison. fromError = rychlejší náběh rozmazání
    public void AddPoison(float amount, bool fromError = false)
    {
        poisonAmount = Mathf.Clamp01(poisonAmount + amount);
        currentBlurEase = fromError ? errorBlurEaseDuration : blurEaseDuration;
    }

    // 🔽 Sníží poison
    public void RemovePoison(float amount)
    {
        poisonAmount = Mathf.Clamp01(poisonAmount - amount);
    }

    // Přímo nastavit (pokud chceš)
    public void SetPoison(float value)
    {
        poisonAmount = Mathf.Clamp01(value);
    }
}