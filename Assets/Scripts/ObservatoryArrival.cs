using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The arrival introduces the journey once, then opens the existing observation.
public class ObservatoryArrival : MonoBehaviour
{
    public static bool BeginObservation;
    public RobotTablet tablet;
    public PlayerLocomotionPause locomotion;
    public InputActionProperty continueInput;
    public AudioSource voice;
    public AudioClip arrivalLine;
    public Image viewFade;
    public string observationScene = "Observatory";
    public float fadeSeconds = .45f;
    public float settleSeconds = 1.5f;

    public bool IsTransitioning { get; private set; }
    public bool IsReady { get; private set; }
    bool previousContinue = true;
    bool previouslyEnabled;
    bool started;
    Coroutine transition;
    float readyAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStartFlag() { BeginObservation = false; }

    void OnEnable()
    {
        previousContinue = true;
        if (continueInput.action != null)
        {
            previouslyEnabled = continueInput.action.enabled;
            continueInput.action.Enable();
        }
        if (started) ShowArrival();
    }

    void Start()
    {
        if (tablet == null || tablet.fixedAnchor == null || locomotion == null || continueInput.action == null ||
            voice == null || arrivalLine == null || viewFade == null ||
            locomotion.locomotionProviders == null || locomotion.locomotionProviders.Length == 0)
        {
            Debug.LogError("[ObservatoryArrival] Assign the fixed tablet, locomotion, continue input, narration and fade.", this);
            enabled = false;
            return;
        }
        started = true;
        ShowArrival();
    }

    void ShowArrival()
    {
        IsReady = false;
        IsTransitioning = false;
        previousContinue = true;
        locomotion.Pause();
        tablet.Show("WELCOME", "Day / Seasons / Planets / Sun", "A: enter observatory", false);
        tablet.RevealText();
        SetFade(0f);
        voice.Stop();
        voice.clip = arrivalLine;
        voice.Play();
        readyAt = Time.unscaledTime + Mathf.Max(0f, settleSeconds);
    }

    void Update()
    {
        if (!started || IsTransitioning) return;
        locomotion.UpdateLock();
        if (!IsReady && Time.unscaledTime >= readyAt)
        {
            IsReady = true;
            tablet.SetControlCue("A: enter observatory", true);
        }
        bool pressed = continueInput.action.IsPressed() ||
            (Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed));
        if (IsReady && pressed && !previousContinue) ContinueToObservatory();
        previousContinue = pressed;
    }

    public void ContinueToObservatory()
    {
        if (!started || !IsReady || IsTransitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(observationScene))
        {
            Debug.LogError("[ObservatoryArrival] Observatory must be enabled in Build Settings.", this);
            return;
        }
        IsTransitioning = true;
        transition = StartCoroutine(EnterObservatory());
    }

    IEnumerator EnterObservatory()
    {
        voice.Stop();
        tablet.Hide();
        float startedFade = Time.unscaledTime;
        float seconds = Mathf.Max(.01f, fadeSeconds);
        while (Time.unscaledTime - startedFade < seconds)
        {
            SetFade((Time.unscaledTime - startedFade) / seconds);
            yield return null;
        }
        SetFade(1f);
        // Release the old rig's lock before unloading so its UI rays unregister normally.
        locomotion.Restore();
        yield return null;
        BeginObservation = true;
        AsyncOperation load = SceneManager.LoadSceneAsync(observationScene, LoadSceneMode.Single);
        if (load == null)
        {
            BeginObservation = false;
            IsTransitioning = false;
            SetFade(0f);
            tablet.Show("WELCOME", "Day / Seasons / Planets / Sun", "A: enter observatory", false);
            tablet.RevealText();
            Debug.LogError("[ObservatoryArrival] Could not load Observatory.", this);
            yield break;
        }
        yield return load;
    }

    void SetFade(float alpha)
    {
        if (viewFade == null) return;
        Color color = viewFade.color;
        color.a = Mathf.Clamp01(alpha);
        viewFade.color = color;
        viewFade.raycastTarget = false;
        viewFade.gameObject.SetActive(alpha > 0f);
    }

    void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        IsTransitioning = false;
        IsReady = false;
        SetFade(0f);
        if (voice != null) voice.Stop();
        if (tablet != null) tablet.Hide();
        locomotion?.Restore();
        if (!previouslyEnabled) continueInput.action?.Disable();
    }

    void OnDestroy()
    {
        if (continueInput.reference == null) continueInput.action?.Dispose();
    }
}
