using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// A short observation and its explanation share one clock and one comfortable viewing position.
public class ObservatoryTour : MonoBehaviour
{
    public enum TourStep { Ready, Sky, Earth, Complete, Introduction }
    public RobotWelcomePrompt welcome;
    public ViewingRoomLessonInput controls;
    public EarthObservationModel observation;
    public Transform playerCamera;
    public Transform displayAnchor;
    public GameObject groundView;
    public GameObject modelView;
    public ObservatoryModelDisplay modelDisplay;
    public Light modelLight;
    public Canvas captionCanvas;
    public TMP_Text caption;
    public TMP_Text hint;
    public TMP_Text clock;
    public Transform clockStand;
    public Image viewFade;
    public AudioSource voice;
    public AudioClip introductionLine;
    public AudioClip skyLine;
    public AudioClip earthLine;
    public AudioClip nextLine;
    public float automaticHoursPerSecond = 0.5f;
    public string seasonsScene = "Assets/Scenes/Seasons.unity";

    public TourStep CurrentStep { get; private set; }
    public bool IsGuided { get; private set; }
    public bool IsPresenting { get; private set; }
    public bool KeepsLocomotionPaused => IsGuided;
    public bool IsPaused { get; private set; }
    public bool IsTransitioning => transition != null;

    bool initialized;
    bool hasResume;
    bool usedTime;
    bool timeReady;
    bool skyPlaced;
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
            groundView == null || modelView == null || modelDisplay == null || captionCanvas == null || caption == null ||
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
        modelDisplay.SetVisible(false);
        ClearFade();
        if (ObservatoryArrival.BeginObservation)
        {
            ObservatoryArrival.BeginObservation = false;
            viewFade.color = Color.black;
            viewFade.gameObject.SetActive(true);
            StartCoroutine(ArriveFromEntrance());
        }
    }

    IEnumerator ArriveFromEntrance()
    {
        // Wait for every Start callback so the old welcome cannot reopen over the lesson.
        yield return null;
        if (!isActiveAndEnabled) yield break;
        welcome.ClearWelcome();
        StartGuidedTour();
        transition = StartCoroutine(BeginObservation(true));
    }

    void OnEnable()
    {
        controls?.EnableInput();
    }

    void Update()
    {
        if (!initialized || welcome.IsShowing) return;
        controls.ReadInput();
        if (!IsPresenting || welcome.IsShowing || IsTransitioning || CurrentStep == TourStep.Introduction) return;

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

        RenderSettings.ambientLight = Color.Lerp(new Color(0.018f, 0.024f, 0.04f), new Color(0.48f, 0.55f, 0.62f),
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
            CurrentStep = TourStep.Introduction;
            observation.ResetDay();
            IsPaused = usedTime = false;
        }
        PlaceDisplays();
        ShowView(CurrentStep != TourStep.Earth);
        bool introducing = CurrentStep == TourStep.Introduction;
        clockStand.gameObject.SetActive(!introducing);
        observation.SetSuspended(introducing);
        timeReady = false;
        controls.ResetInput();
        ShowCaption(introducing ? "Start with the sky above us." :
            CurrentStep == TourStep.Sky ? "A winter day in California." : "Earth turns. Day becomes night.", introducing ? float.PositiveInfinity : 8f);
        hint.text = introducing ? "A: watch a day" : "Left stick: time";
        PlayLine(introducing ? introductionLine : CurrentStep == TourStep.Sky ? skyLine : earthLine);
        HoldForLine(3f);
        RefreshClock();
    }

    public void ContinueLesson()
    {
        if (IsTransitioning || welcome.IsShowing) return;
        if (CurrentStep == TourStep.Complete)
        {
            if (string.IsNullOrWhiteSpace(seasonsScene) || !Application.CanStreamedLevelBeLoaded(seasonsScene))
            {
                ShowCaption("Seasons is unavailable. You can replay this lesson.", float.PositiveInfinity);
                hint.text = "A: retry seasons   B: watch again";
                controls.ResetInput();
                Debug.LogWarning("[ObservatoryTour] Enable the Seasons scene in Build Settings.", this);
                return;
            }
            transition = StartCoroutine(EnterSeasons());
            return;
        }
        if (!IsPresenting) return;
        if (CurrentStep == TourStep.Introduction) transition = StartCoroutine(BeginObservation());
        else if (CurrentStep == TourStep.Sky) transition = StartCoroutine(ChangeView(false));
        else if (CurrentStep == TourStep.Earth) Finish();
    }

    public void Interact()
    {
        if (!IsPresenting || IsTransitioning || CurrentStep == TourStep.Introduction) return;
        IsPaused = !IsPaused;
        RefreshClock();
    }

    public void ResetOrGoBack()
    {
        if (IsTransitioning || welcome.IsShowing) return;
        if (CurrentStep == TourStep.Complete) { hasResume = false; StartGuidedTour(); }
        else if (!IsPresenting) return;
        else if (CurrentStep == TourStep.Introduction) return;
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
        HoldForLine(4f);
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
        IsPresenting = false;
        hasResume = false;
        observation.SetSuspended(true);
        ShowCaption("Next: why seasons change.", float.PositiveInfinity);
        modelDisplay.SetVisible(false);
        hint.text = "A: seasons   B: watch again";
        hint.color = hintColor;
        controls.ResetInput();
        RefreshClock();
        PlayLine(nextLine);
    }

    IEnumerator EnterSeasons()
    {
        if (voice != null) voice.Stop();
        observation.SetSuspended(true);
        viewFade.gameObject.SetActive(true);
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            viewFade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        viewFade.color = Color.black;
        // Restore the outgoing rig while its EventSystem is still alive.
        welcome.locomotion.Restore();
        yield return null;

        AsyncOperation load = null;
        try
        {
            load = SceneManager.LoadSceneAsync(seasonsScene, LoadSceneMode.Single);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[ObservatoryTour] Could not load Seasons: " + error.Message, this);
        }
        if (load == null)
        {
            welcome.PauseLocomotion(true);
            ClearFade();
            ShowCaption("Seasons could not open. Try again or replay this lesson.", float.PositiveInfinity);
            hint.text = "A: retry seasons   B: watch again";
            controls.ResetInput();
            transition = null;
            yield break;
        }
        yield return load;
    }

    void ShowView(bool fromGround)
    {
        groundView.SetActive(true); // The local sky remains visible beside its Earth explanation.
        modelView.SetActive(!fromGround);
        modelDisplay.SetVisible(!fromGround);
        if (modelLight != null) modelLight.enabled = !fromGround;
        observation.ApplyView();
        if (runtimeSky != null) runtimeSky.SetFloat("_GroundView", 1f);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.018f, 0.024f, 0.04f), new Color(0.48f, 0.55f, 0.62f),
            Mathf.Clamp01((observation.SunDirectionLocal.y + 0.08f) / 0.35f));
    }

    void PlaceDisplays()
    {
        Vector3 forward = Vector3.ProjectOnPlane(displayAnchor != null ? displayAnchor.forward : playerCamera.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.right;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 eye = playerCamera.position;
        modelView.transform.position = new Vector3(0f, -100f, 0f);
        // Show a tilted, north-up globe. A point on the far side is naturally hidden.
        Vector3 modelNorth = observation.earthOrbit.orbitPlane.localRotation * observation.AxisDirectionLocal;
        modelView.transform.rotation = Quaternion.FromToRotation(modelNorth, new Vector3(0f, 0.65f, -0.76f).normalized);
        if (displayAnchor != null)
            captionCanvas.transform.SetPositionAndRotation(displayAnchor.position, displayAnchor.rotation);
        else captionCanvas.transform.SetPositionAndRotation(eye + forward * 2.8f - right * 1.45f - Vector3.up * 0.73f,
            Quaternion.LookRotation((forward * 2.8f - right * 1.45f).normalized));
        clockStand.SetPositionAndRotation(captionCanvas.transform.position - Vector3.up * 0.26f, captionCanvas.transform.rotation);
        if (observation.skyFrame != null && !skyPlaced)
        {
            Vector3 initialSun = Vector3.ProjectOnPlane(observation.SunDirectionLocal, Vector3.up).normalized;
            observation.skyFrame.rotation = Quaternion.FromToRotation(initialSun, forward);
            skyPlaced = true;
        }
        observation.SetDays(observation.SimulationDays);
    }

    IEnumerator BeginObservation(bool arriving = false)
    {
        if (voice != null) voice.Stop();
        viewFade.gameObject.SetActive(true);
        for (float t = 0f; !arriving && t < 0.35f; t += Time.unscaledDeltaTime)
        {
            viewFade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        viewFade.color = Color.black;
        CurrentStep = TourStep.Sky;
        clockStand.gameObject.SetActive(true);
        ShowView(true);
        ShowCaption("A winter day in California.", 8f);
        hint.text = "Left stick: time     A: see Earth";
        PlayLine(skyLine);
        HoldForLine(3f);
        timeReady = false;
        controls.ResetInput();
        RefreshClock();
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            viewFade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        ClearFade();
        observation.SetSuspended(false);
        transition = null;
    }

    void HoldForLine(float minimumSeconds)
    {
        float seconds = voice != null && voice.clip != null ? voice.clip.length : minimumSeconds;
        holdUntil = Time.unscaledTime + Mathf.Max(minimumSeconds, seconds);
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
        if (modelDisplay != null) modelDisplay.SetVisible(false);
    }

    void OnDestroy()
    {
        if (RenderSettings.skybox == runtimeSky) RenderSettings.skybox = previousSky;
        if (runtimeSky != null) Destroy(runtimeSky);
    }
}
