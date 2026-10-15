using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SunAlbedoFader : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Renderer that has the sun material. If empty, this will use the Renderer on this GameObject.")]
    public Renderer sunRenderer;

    [Header("Fade settings")]
    [Tooltip("Fade duration in seconds.")]
    public float duration = 5f;

    [Tooltip("Start automatically when the scene starts.")]
    public bool triggerOnStart = true;

    [Tooltip("Start when the Player (by tag) enters this object's trigger collider.")]
    public bool triggerOnPlayerEnter = false;

    [Tooltip("Tag used to detect the player for OnTriggerEnter.")]
    public string playerTag = "Player";

    // The inherited visualization fades from white to red.
    private readonly Color startColor = new Color(1f, 1f, 1f, 1f);
    private readonly Color endColor = new Color(1f, 0f, 0f, 1f);

    [Header("Emission (optional)")]
    [Tooltip("Animate the emission color as well as the albedo. Disable to avoid overriding the texture or creating strong bloom effects.")]
    public bool animateEmission = false;

    [Tooltip("Multiplier applied to the emission color when animating. Set to 0 to effectively disable emission output even if animateEmission is true.")]
    public float emissionIntensity = 1f;

    [Header("Scale (optional)")]
    [Tooltip("Animate the GameObject local scale while fading color.")]
    public bool animateScale = true;

    [Tooltip("Starting local scale for the object when fading begins.")]
    public Vector3 startScale = new Vector3(0.75f, 0.75f, 0.75f);

    [Tooltip("Ending local scale for the object when fading finishes.")]
    public Vector3 endScale = new Vector3(1.05f, 1.05f, 1.05f);

    [Header("Timer UI (optional)")]
    [Tooltip("World-space Canvas that will display the timer. If empty, no timer is shown.")]
    public Canvas timerCanvas;

    [Tooltip("UI Text (under the canvas) used to show the year. Assign a Text child from the canvas.")]
    public Text timerText;

    [Tooltip("TextMesh Pro UGUI text (preferred for crisp VR text). If assigned this will be used instead of UnityEngine.UI.Text.")]
    public TextMeshProUGUI timerTextTMP;

    [Tooltip("If true, the script will parent the timer canvas to the Sun transform. If false, the canvas is left where you placed it in the scene.")]
    public bool parentTimerToSun = false;

    [Tooltip("Local position of the timer canvas relative to the sun when placed in world-space.")]
    public Vector3 timerLocalPosition = new Vector3(0f, 2f, 0f);

    [Tooltip("Local scale for the timer canvas (world-space canvases are often scaled small).")]
    public Vector3 timerLocalScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Tooltip("Starting year shown on the timer.")]
    public long timerStartYear = 2025L;

    [Tooltip("Ending year shown on the timer.")]
    public long timerEndYear = 1000002025L;

    private bool isFading = false;
    private string colorProp = "_Color";
    private bool hasEmission = false;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        if (sunRenderer == null)
            sunRenderer = GetComponent<Renderer>();
        if (sunRenderer == null)
        {
            Debug.LogWarning("[SunAlbedoFader] No Renderer assigned or found to fade.", this);
            return;
        }

        mpb = new MaterialPropertyBlock();
        sunRenderer.GetPropertyBlock(mpb);
        Material material = sunRenderer.sharedMaterial;
        if (material != null)
        {
            if (material.HasProperty("_BaseColor"))
                colorProp = "_BaseColor";
            hasEmission = material.HasProperty("_EmissionColor");
            if (hasEmission)
            {
                if (animateEmission)
                    material.EnableKeyword("_EMISSION");
                else
                    material.DisableKeyword("_EMISSION");
            }
        }

        ApplyColorToBlock(startColor);
        if (animateScale)
            transform.localScale = startScale;
        SetupTimer();
        if (triggerOnStart)
            StartFade();
    }

    private void SetupTimer()
    {
        if (timerCanvas == null)
            return;
        timerCanvas.renderMode = RenderMode.WorldSpace;
        if (parentTimerToSun)
        {
            timerCanvas.transform.SetParent(transform, false);
            timerCanvas.transform.localPosition = timerLocalPosition;
            timerCanvas.transform.localRotation = Quaternion.identity;
        }
        timerCanvas.transform.localScale = timerLocalScale;
        if (timerTextTMP == null && timerText == null)
        {
            timerTextTMP = timerCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (timerTextTMP == null)
                timerText = timerCanvas.GetComponentInChildren<Text>(true);
        }
        UpdateTimer(timerStartYear);
    }

    public void StartFade()
    {
        if (isFading)
            return;
        if (sunRenderer == null)
        {
            Debug.LogWarning("[SunAlbedoFader] No Renderer assigned or found to fade.", this);
            return;
        }
        StartCoroutine(FadeCoroutine(duration));
    }

    private IEnumerator FadeCoroutine(float secs)
    {
        isFading = true;
        float elapsed = 0f;
        while (elapsed < secs)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / secs);
            ApplyColorToBlock(Color.Lerp(startColor, endColor, progress));
            if (animateScale)
                transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            // Preserve double precision for this animation's large year values.
            double year = (1.0 - progress) * timerStartYear + progress * timerEndYear;
            UpdateTimer((long)Math.Round(year));
            yield return null;
        }
        ApplyColorToBlock(endColor);
        if (animateScale)
            transform.localScale = endScale;
        UpdateTimer(timerEndYear);
        isFading = false;
    }

    private void UpdateTimer(long year)
    {
        if (timerCanvas == null)
            return;
        string value = year.ToString("N0");
        if (timerTextTMP != null)
            timerTextTMP.text = value;
        else if (timerText != null)
            timerText.text = value;
    }

    private void ApplyColorToBlock(Color color)
    {
        if (mpb == null)
            mpb = new MaterialPropertyBlock();
        mpb.SetColor(colorProp, color);
        if (hasEmission && animateEmission && emissionIntensity > 0f)
            mpb.SetColor("_EmissionColor", color * emissionIntensity);
        else if (hasEmission && !animateEmission)
            mpb.SetColor("_EmissionColor", Color.black);
        sunRenderer.SetPropertyBlock(mpb);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnPlayerEnter && other.CompareTag(playerTag))
            StartFade();
    }
}
