using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A short observation and its explanation share one clock and one comfortable viewing position.
public class ObservatoryTour : MonoBehaviour
{
    public enum TourStep { Ready, Sky, Earth, Complete }
    public RobotWelcomePrompt welcome;
    public ViewingRoomLessonInput controls;
    public EarthObservationModel observation;
    public Transform playerCamera;
    public GameObject groundView;
    public GameObject modelView;
    public Light modelLight;
    public Canvas captionCanvas;
    public TMP_Text caption;
    public TMP_Text hint;
    public TMP_Text clock;
    public Transform clockStand;
    public Image viewFade;
    public AudioSource voice;
    public AudioClip skyLine;
    public AudioClip earthLine;
    public AudioClip nextLine;
    public float automaticHoursPerSecond = 0.5f;

    public TourStep CurrentStep { get; private set; }
    public bool IsGuided { get; private set; }
    public bool IsPresenting { get; private set; }
    public bool KeepsLocomotionPaused => IsPresenting;
    public bool CanInvite => !IsGuided && !IsPresenting;
    public bool IsPaused { get; private set; }
    public bool IsTransitioning => transition != null;

    bool initialized;
    bool hasResume;
    bool menuOpen;
    bool usedTime;
    bool timeReady;
    TourStep resumeStep;
    Coroutine transition;
    Material runtimeSky;
    Material previousSky;
    double nextClock;
    float captionUntil;
    float holdUntil;
    Color hintColor;

    void Awake()
    {
        if (observation != null && observation.skyMaterial != null)
        {
            previousSky = RenderSettings.skybox;
            runtimeSky = new Material(observation.skyMaterial);
            observation.skyMaterial = runtimeSky;
            RenderSettings.skybox = runtimeSky;
        }
    }

    void Start()
    {
        if (welcome == null || controls == null || observation == null || playerCamera == null ||
            groundView == null || modelView == null || captionCanvas == null || caption == null ||
            hint == null || clock == null || clockStand == null || viewFade == null || !controls.IsConfigured ||
            !observation.InitializeModel())
        {
            Debug.LogError("[ObservatoryTour] Assign the observation, input and presentation references.", this);
            enabled = false;
            return;
        }
        hintColor = hint.color;
        initialized = true;
        PlaceDisplays();
        ShowView(true);
        observation.SetSuspended(true);
        captionCanvas.gameObject.SetActive(false);
        clockStand.gameObject.SetActive(false);
        ClearFade();
    }

    void OnEnable()
    {
        controls?.EnableInput();
    }

    void Update()
    {
        if (!initialized || welcome.IsShowing) return;
        controls.ReadInput();
        if (!IsPresenting || welcome.IsShowing || IsTransitioning) return;

        float direction = controls.ReadTimeDirection();
        if (Mathf.Abs(direction) < 0.01f) timeReady = true;
        bool scrubbing = timeReady && Mathf.Abs(direction) > 0.01f;
        if (scrubbing)
        {
            observation.Scrub(direction, Time.unscaledDeltaTime);
            usedTime = true;
        }
        else if (!IsPaused && Time.unscaledTime >= holdUntil)
            observation.SetDays(observation.SimulationDays + Time.unscaledDeltaTime * automaticHoursPerSecond / 24.0);

        if (CurrentStep == TourStep.Sky)
            RenderSettings.ambientLight = Color.Lerp(new Color(0.018f, 0.024f, 0.04f), new Color(0.16f, 0.18f, 0.22f),
                Mathf.Clamp01((observation.SunDirectionLocal.y + 0.08f) / 0.35f));

        if (Time.unscaledTimeAsDouble >= nextClock)
        {
            RefreshClock();
            nextClock = Time.unscaledTimeAsDouble + 0.1;
        }
        caption.enabled = Time.unscaledTime < captionUntil;
        string nextAction = CurrentStep == TourStep.Sky ? "A: see Earth" : "A: continue   B: sky";
        bool introduceTime = !usedTime && CurrentStep == TourStep.Sky;
        hint.text = introduceTime ? "Left stick: time   " + nextAction : nextAction;
        float glow = introduceTime ? 0.3f + 0.3f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
        hint.color = Color.Lerp(hintColor, new Color(0.35f, 1f, 0.95f), glow);
    }

    public void StartGuidedTour()
    {
        if (!initialized) return;
        bool placeDisplays = !menuOpen || !hasResume;
        IsGuided = IsPresenting = true;
        welcome.PauseLocomotion(true);
        captionCanvas.gameObject.SetActive(true);
        clockStand.gameObject.SetActive(true);
        if (hasResume)
        {
            CurrentStep = resumeStep;
            hasResume = false;
        }
        else
        {
            CurrentStep = TourStep.Sky;
            observation.ResetDay();
            IsPaused = usedTime = false;
        }
        if (placeDisplays) PlaceDisplays();
        menuOpen = false;
        ShowView(CurrentStep == TourStep.Sky);
        observation.SetSuspended(false);
        timeReady = false;
        controls.ResetInput();
        ShowCaption(CurrentStep == TourStep.Sky ? "Watch the Sun move." : "Earth turns. Day becomes night.", 7f);
        PlayLine(CurrentStep == TourStep.Sky ? skyLine : earthLine);
        holdUntil = Time.unscaledTime + 3f;
        RefreshClock();
    }

    public void ContinueLesson()
    {
        if (!IsPresenting || IsTransitioning || welcome.IsShowing) return;
        if (CurrentStep == TourStep.Sky) transition = StartCoroutine(ChangeView(false));
        else if (CurrentStep == TourStep.Earth) Finish();
    }

    public void Interact()
    {
        if (!IsPresenting || IsTransitioning) return;
        IsPaused = !IsPaused;
        RefreshClock();
    }

    public void ResetOrGoBack()
    {
        if (IsTransitioning || welcome.IsShowing) return;
        if (CurrentStep == TourStep.Complete) { hasResume = false; StartGuidedTour(); }
        else if (!IsPresenting) return;
        else if (CurrentStep == TourStep.Earth) transition = StartCoroutine(ChangeView(true));
        else
        {
            observation.ResetDay();
            IsPaused = false;
            timeReady = false;
            RefreshClock();
        }
    }

    IEnumerator ChangeView(bool fromGround)
    {
        observation.SetSuspended(true);
        viewFade.gameObject.SetActive(true);
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            viewFade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.25f));
            yield return null;
        }
        viewFade.color = Color.black;
        CurrentStep = fromGround ? TourStep.Sky : TourStep.Earth;
        ShowView(fromGround);
        ShowCaption(fromGround ? "Watch the same moment from Earth." : "You were at this dot.", 8f);
        PlayLine(fromGround ? skyLine : earthLine);
        timeReady = false;
        controls.ResetInput();
        holdUntil = Time.unscaledTime + 4f;
        RefreshClock();
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            viewFade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / 0.25f));
            yield return null;
        }
        ClearFade();
        observation.SetSuspended(false);
        transition = null;
    }

    void Finish()
    {
        CurrentStep = TourStep.Complete;
        IsGuided = IsPresenting = false;
        hasResume = false;
        observation.SetSuspended(true);
        ShowCaption("Next: why seasons change.", float.PositiveInfinity);
        hint.text = "B: watch again     Y: menu";
        hint.color = hintColor;
        RefreshClock();
        PlayLine(nextLine);
        welcome.ResumeLocomotion();
    }

    public void OpenModes()
    {
        if (!initialized) return;
        CancelTransition();
        if (IsPresenting) { resumeStep = CurrentStep; hasResume = true; }
        menuOpen = true;
        IsPresenting = false;
        observation.SetSuspended(true);
        captionCanvas.gameObject.SetActive(false);
        clockStand.gameObject.SetActive(false);
        if (voice != null) voice.Stop();
        controls.ResetInput();
        welcome.ShowWelcome();
    }

    public void ChooseFreeRoam()
    {
        CancelTransition();
        if (IsPresenting) { resumeStep = CurrentStep; hasResume = true; }
        IsGuided = IsPresenting = menuOpen = false;
        CurrentStep = TourStep.Ready;
        observation.SetSuspended(true);
        captionCanvas.gameObject.SetActive(false);
        clockStand.gameObject.SetActive(false);
        if (voice != null) voice.Stop();
        controls.ResetInput();
        welcome.ResumeLocomotion();
    }

    void ShowView(bool fromGround)
    {
        groundView.SetActive(fromGround);
        modelView.SetActive(!fromGround);
        if (modelLight != null) modelLight.enabled = !fromGround;
        observation.ApplyView(fromGround);
        if (runtimeSky != null) runtimeSky.SetFloat("_GroundView", fromGround ? 1f : 0f);
        RenderSettings.ambientLight = fromGround ? new Color(0.16f, 0.18f, 0.22f) : new Color(0.025f, 0.03f, 0.045f);
    }

    void PlaceDisplays()
    {
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.right;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 eye = playerCamera.position;
        modelView.transform.position = eye + forward * 3.4f - Vector3.up * 0.25f;
        // A view from above the northern hemisphere keeps our observation point visible through the day.
        Vector3 modelNorth = observation.earthOrbit.orbitPlane.localRotation * observation.AxisDirectionLocal;
        modelView.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.FromToRotation(modelNorth, Vector3.back);
        captionCanvas.transform.SetPositionAndRotation(eye + forward * 2.8f - right * 1.45f - Vector3.up * 0.73f,
            Quaternion.LookRotation((forward * 2.8f - right * 1.45f).normalized));
        clockStand.SetPositionAndRotation(eye + forward * 3.4f - Vector3.up * 1.15f, Quaternion.LookRotation(forward));
        if (observation.skyFrame != null)
        {
            Vector3 initialSun = Vector3.ProjectOnPlane(observation.SunDirectionLocal, Vector3.up).normalized;
            observation.skyFrame.rotation = Quaternion.FromToRotation(initialSun, forward);
        }
        observation.SetDays(observation.SimulationDays);
    }

    void ShowCaption(string text, float seconds)
    {
        caption.text = text;
        caption.enabled = true;
        captionUntil = Time.unscaledTime + seconds;
    }

    void RefreshClock()
    {
        int minutes = (int)Math.Floor(observation.SolarTimeHours * 60.0) % 1440;
        clock.text = (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00") +
            (IsPresenting ? (IsPaused ? "   X: play" : "   X: pause") : "");
    }

    void PlayLine(AudioClip clip)
    {
        if (voice == null || clip == null) return;
        voice.Stop();
        voice.clip = clip;
        voice.Play();
    }

    void ClearFade()
    {
        if (viewFade == null) return;
        viewFade.color = Color.clear;
        viewFade.gameObject.SetActive(false);
    }

    void CancelTransition()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        ClearFade();
    }

    void OnDisable()
    {
        if (IsPresenting) { resumeStep = CurrentStep; hasResume = true; }
        CancelTransition();
        if (observation != null) observation.SetSuspended(true);
        if (voice != null) voice.Stop();
        controls?.DisableInput();
        IsGuided = IsPresenting = false;
        if (welcome != null) welcome.ResumeLocomotion();
        if (captionCanvas != null) captionCanvas.gameObject.SetActive(false);
        if (clockStand != null) clockStand.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (RenderSettings.skybox == runtimeSky) RenderSettings.skybox = previousSky;
        if (runtimeSky != null) Destroy(runtimeSky);
    }
}
