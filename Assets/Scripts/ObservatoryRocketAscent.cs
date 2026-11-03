using System;
using TMPro;
using UnityEngine;

//the cabin stays still while the outside view follows a compressed trip to space
public class ObservatoryRocketAscent : MonoBehaviour
{
    public float ascentSeconds = 30f;
    public bool IsActive { get; private set; }
    public bool IsPaused { get; private set; }
    public bool WasCancelled { get; private set; }
    public bool HasArrived => IsActive && elapsed >= Mathf.Max(1f, ascentSeconds);
    public float AltitudeMetres { get; private set; }

    const int flightLayer = 30;
    GameObject cabinRoot;
    ObservatoryRocketCabin cabin;
    ObservatoryModelDisplay display;
    Camera flightCamera;
    Skybox cameraSkybox;
    AudioSource engine;
    AudioClip engineClip;
    int previousMask;
    CameraClearFlags previousClearFlags;
    Color previousBackground;
    float previousFarClip;
    bool previousSkyboxEnabled;
    bool previousModelCameraEnabled;
    bool previousGroundCameraEnabled;
    bool previousFrameVisible;
    float elapsed;

    public bool Begin(ObservatoryModelDisplay modelDisplay, TMP_FontAsset font, int fadeLayer)
    {
        if (IsActive || !isActiveAndEnabled || modelDisplay == null || modelDisplay.IsSpaceView ||
            modelDisplay.playerCamera == null || modelDisplay.modelCamera == null || modelDisplay.groundCamera == null ||
            modelDisplay.frame == null || font == null) return false;
        var sun = modelDisplay.observation != null && modelDisplay.observation.earthOrbit != null ?
            modelDisplay.observation.earthOrbit.sun : null;
        var surface = sun != null ? sun.GetComponent<Renderer>() : null;
        if (surface == null || surface.sharedMaterial == null) return false;

        display = modelDisplay;
        flightCamera = display.playerCamera;
        if (cabinRoot == null)
        {
            cabinRoot = new GameObject("Rocket Ascent Prototype");
            cabinRoot.SetActive(false);
            cabinRoot.transform.SetParent(transform, false);
            cabin = new ObservatoryRocketCabin(cabinRoot.transform, flightLayer, surface.sharedMaterial, font);
            BuildEngineSound();
        }
        Vector3 forward = Vector3.ProjectOnPlane(flightCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        //capture the seat once so leaning and looking still come from real tracking
        cabinRoot.transform.SetPositionAndRotation(flightCamera.transform.position, Quaternion.LookRotation(forward, Vector3.up));
        previousMask = flightCamera.cullingMask;
        previousClearFlags = flightCamera.clearFlags;
        previousBackground = flightCamera.backgroundColor;
        previousFarClip = flightCamera.farClipPlane;
        cameraSkybox = flightCamera.GetComponent<Skybox>();
        previousSkyboxEnabled = cameraSkybox != null && cameraSkybox.enabled;
        previousModelCameraEnabled = display.modelCamera.enabled;
        previousGroundCameraEnabled = display.groundCamera.enabled;
        previousFrameVisible = display.frame.activeSelf;
        display.modelCamera.enabled = display.groundCamera.enabled = false;
        display.frame.SetActive(false);
        if (cameraSkybox != null) cameraSkybox.enabled = false;
        flightCamera.cullingMask = (1 << flightLayer) | (1 << fadeLayer);
        flightCamera.clearFlags = CameraClearFlags.SolidColor;
        flightCamera.farClipPlane = Mathf.Max(600f, previousFarClip);
        elapsed = AltitudeMetres = 0f;
        IsPaused = WasCancelled = false;
        IsActive = true;
        cabin.SetPaused(false);
        ApplyAltitude();
        cabinRoot.SetActive(true);
        engine.Play();
        return true;
    }

    void Update()
    {
        if (!IsActive || IsPaused || WasCancelled || HasArrived) return;
        elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, Mathf.Max(1f, ascentSeconds));
        float progress = elapsed / Mathf.Max(1f, ascentSeconds);
        //give the buildings and aircraft time on screen before the upper atmosphere
        if (progress < 0.2f) AltitudeMetres = Mathf.Lerp(0f, 1000f, progress / 0.2f);
        else if (progress < 0.45f) AltitudeMetres = Mathf.Lerp(1000f, 12000f, (progress - 0.2f) / 0.25f);
        else if (progress < 0.75f) AltitudeMetres = Mathf.Lerp(12000f, 50000f, (progress - 0.45f) / 0.3f);
        else AltitudeMetres = Mathf.Lerp(50000f, 100000f, (progress - 0.75f) / 0.25f);
        ApplyAltitude();
    }

    void ApplyAltitude()
    {
        cabin.SetAltitude(AltitudeMetres);
        flightCamera.backgroundColor = cabin.SkyColor;
        if (engine != null) engine.pitch = Mathf.Lerp(0.8f, 1.15f, AltitudeMetres / 100000f);
    }

    public void TogglePause()
    {
        if (!IsActive || WasCancelled || HasArrived) return;
        IsPaused = !IsPaused;
        cabin.SetPaused(IsPaused);
        if (IsPaused) engine.Pause();
        else engine.UnPause();
    }

    public void Cancel()
    {
        if (IsActive) WasCancelled = true;
    }

    public void End()
    {
        if (!IsActive) return;
        //restore this before the space display saves its ground camera settings
        if (flightCamera != null)
        {
            flightCamera.cullingMask = previousMask;
            flightCamera.clearFlags = previousClearFlags;
            flightCamera.backgroundColor = previousBackground;
            flightCamera.farClipPlane = previousFarClip;
        }
        if (cameraSkybox != null) cameraSkybox.enabled = previousSkyboxEnabled;
        if (display != null)
        {
            if (display.modelCamera != null) display.modelCamera.enabled = previousModelCameraEnabled;
            if (display.groundCamera != null) display.groundCamera.enabled = previousGroundCameraEnabled;
            if (display.frame != null) display.frame.SetActive(previousFrameVisible);
        }
        if (engine != null) engine.Stop();
        if (cabinRoot != null) cabinRoot.SetActive(false);
        IsActive = IsPaused = false;
    }

    void BuildEngineSound()
    {
        const int samples = 24000;
        var wave = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            double phase = 2.0 * Math.PI * i / samples;
            wave[i] = (float)(0.2 * Math.Sin(phase * 55) + 0.07 * Math.Sin(phase * 110) + 0.03 * Math.Sin(phase * 220));
        }
        engineClip = AudioClip.Create("Prototype cabin engine", samples, 1, samples, false);
        engineClip.SetData(wave, 0);
        engine = cabinRoot.AddComponent<AudioSource>();
        engine.clip = engineClip;
        engine.loop = true;
        engine.playOnAwake = false;
        engine.volume = 0.12f;
        engine.spatialBlend = 0f;
    }

    void OnDisable() { End(); }

    void OnDestroy()
    {
        End();
        cabin?.Dispose();
        if (cabinRoot != null) Destroy(cabinRoot);
        if (engineClip != null) Destroy(engineClip);
    }
}
