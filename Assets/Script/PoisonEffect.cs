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

    [Header("UI (bylinky)")]
    [Tooltip("Maximální poloměr rozmazání UI prvků s materiálem UI/Poison (v UV, 0.03 = 3 % velikosti sprite).")]
    [Range(0f, 0.15f)]
    public float uiMaxBlurRadius = 0.06f;
    [Tooltip("Kolik mip úrovní navíc se při plném jedu použije (měkčí rozmazání zdarma). Vyžaduje mipmapy na sprite.")]
    [Range(0f, 6f)]
    public float uiMaxMipBias = 3.5f;

    [Header("Rozmazání světa")]
    [Tooltip("Zapnuto = starý URP Depth of Field (max 1.5) místo PoisonBlurFeature. Vypnuto (default) = hlubší a levnější PoisonBlurFeature na rendereru. Nikdy neběží oba.")]
    public bool useDepthOfField = false;

    [Header("Poison hodnoty")]
    [Range(0f, 1f)]
    public float poisonAmount = 0f;       // aktuální síla jedu (0–1)

    private float pulseTime;
    private float blurAmount;
    private float blurVelocity;
    private float currentBlurEase = 1.6f;
    private DepthOfField depthOfField;
    private bool idleApplied;

    static readonly int PoisonTintId = Shader.PropertyToID("_PoisonTint");
    static readonly int PoisonBlurId = Shader.PropertyToID("_PoisonBlur");
    static readonly int PoisonBlurRadiusId = Shader.PropertyToID("_PoisonBlurRadius");
    static readonly int PoisonColorId = Shader.PropertyToID("_PoisonColor");
    static readonly int PoisonMipBiasId = Shader.PropertyToID("_PoisonMipBias");
    static readonly int PoisonBlurEasedId = Shader.PropertyToID("_PoisonBlurEased");
    // Čte PoisonBlurFeature; 0 = feature se nespustí (např. když se používá DoF).
    static readonly int PoisonWorldBlurId = Shader.PropertyToID("_PoisonWorldBlur");

    /// <summary>Křivka rychlého nástupu rozmazání – sdílí ji PoisonBlurFeature (prolnutí světa) i UI shader.</summary>
    public static float EaseBlur(float blur)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blur * 2.5f));
    }

    private static Material uiMaterial;

    /// <summary>
    /// Sdílený materiál pro UI prvky, které mají zelenat a rozmazávat se s jedem
    /// (overlay canvas post-processing z Volume nezasáhne). Null, pokud shader chybí.
    /// </summary>
    public static Material UIMaterial
    {
        get
        {
            if (uiMaterial == null)
            {
                Shader shader = Shader.Find("UI/Poison");
                if (shader == null)
                {
                    Debug.LogWarning("Shader 'UI/Poison' nebyl nalezen.");
                    return null;
                }
                uiMaterial = new Material(shader) { name = "UIPoison (runtime)" };
            }
            return uiMaterial;
        }
    }

    /// <summary>Přiřadí Graphicu poison materiál (pokud existuje).</summary>
    public static void ApplyTo(UnityEngine.UI.Graphic graphic)
    {
        if (graphic == null) return;
        Material mat = UIMaterial;
        if (mat == null) return;
        graphic.material = mat;
        // Průhledný okraj, aby rozmazání nebylo useknuté na hraně obdélníku.
        if (graphic.GetComponent<PoisonUIPadding>() == null)
            graphic.gameObject.AddComponent<PoisonUIPadding>();
    }

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
        ApplyConstantGlobals();
        SetBlurGlobals(0f, 0f);
    }

    // Konstanty se mění jen v Inspectoru – nastavují se v Awake a při úpravě, ne každý snímek.
    void OnValidate()
    {
        if (Application.isPlaying) ApplyConstantGlobals();
    }

    void ApplyConstantGlobals()
    {
        Shader.SetGlobalColor(PoisonColorId, poisonColor);
        Shader.SetGlobalFloat(PoisonBlurRadiusId, uiMaxBlurRadius);
        Shader.SetGlobalFloat(PoisonMipBiasId, uiMaxMipBias);
    }

    void OnDestroy()
    {
        // Globální hodnoty přežijí změnu scény – vrátit do neutrálu.
        SetBlurGlobals(0f, 0f);
    }

    void SetBlurGlobals(float tint, float blur)
    {
        Shader.SetGlobalFloat(PoisonTintId, tint);
        Shader.SetGlobalFloat(PoisonBlurId, blur);
        Shader.SetGlobalFloat(PoisonBlurEasedId, EaseBlur(blur));
        Shader.SetGlobalFloat(PoisonWorldBlurId, useDepthOfField ? 0f : blur);
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
                SetBlurGlobals(0f, 0f);
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

        SetBlurGlobals(intensity, blurAmount);
    }

    void ApplyBlur(float amount)
    {
        if (depthOfField == null)
            return;

        if (!useDepthOfField || amount < 0.02f)
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

    /// <summary>
    /// Plynulý přírůstek (otrava stoupající sama od sebe, volá se každý snímek). Na rozdíl od AddPoison
    /// nemění rychlost náběhu rozmazání, takže nepřepíše rychlý náběh po zkaženém lektvaru.
    /// </summary>
    public void AddPoisonGradual(float amount)
    {
        poisonAmount = Mathf.Clamp01(poisonAmount + amount);
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