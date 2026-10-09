using System;
using TMPro;
using UnityEngine;

// Presents the inherited research calculation in metres; playback never changes Earth's orbit.
public class EarthOrbitModel : MonoBehaviour
{
    public Transform orbitPlane;
    public Transform earth;
    public Transform sun;
    public LineRenderer orbitPath;
    public TMP_Text readout;
    public Transform earthLabel;
    public Transform playerCamera;
    public float metresPerAU = 1.4f;
    public float daysPerSecond = 12f;

    // Same reference period and semi-major axis as the inherited research example (Program.cs).
    public const int ReferenceYear = 2000;
    public const double PeriodDays = 365.256363;
    public const double SemiMajorAxisAU = 1.00000261;
    public double Eccentricity { get; private set; }
    public double ElapsedDays { get; private set; }
    public float PlaybackSpeed { get; private set; } = 1f;
    public bool IsPaused { get; private set; }
    public bool IsSuspended { get; private set; } = true;

    readonly float[] speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
    int speedIndex = 2;
    double nextReadout;

    void Awake()
    {
        if (orbitPlane == null || earth == null || sun == null || orbitPath == null || readout == null || earthLabel == null ||
            playerCamera == null || metresPerAU <= 0f || daysPerSecond <= 0f)
        {
            Debug.LogError("[EarthOrbitModel] Assign the model references and positive display settings.", this);
            enabled = false;
            return;
        }
        Bergers.BergerSol.CalculateOrbitalParameters(ReferenceYear, out double eccentricity, out _, out _);
        Eccentricity = eccentricity;
        DrawOrbit();
        ResetModel();
    }

    void Update()
    {
        if (IsSuspended) return;
        if (!IsPaused)
        {
            ElapsedDays = (ElapsedDays + Time.unscaledDeltaTime * daysPerSecond * PlaybackSpeed) % PeriodDays;
            UpdatePosition();
        }
        if (Time.unscaledTimeAsDouble >= nextReadout)
        {
            RefreshReadout();
            nextReadout = Time.unscaledTimeAsDouble + 0.2;
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null || earthLabel == null) return;
        earthLabel.rotation = playerCamera.rotation;
    }

    public void SetSuspended(bool suspended) { IsSuspended = suspended; }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        RefreshReadout();
    }

    public void ChangeSpeed(int direction)
    {
        speedIndex = Mathf.Clamp(speedIndex + direction, 0, speeds.Length - 1);
        PlaybackSpeed = speeds[speedIndex];
        RefreshReadout();
    }

    public void ResetModel()
    {
        ElapsedDays = 0.0;
        speedIndex = 2;
        PlaybackSpeed = 1f;
        IsPaused = false;
        UpdatePosition();
        RefreshReadout();
    }

    public double TrueAnomalyAtDays(double days)
    {
        double phase = ((days % PeriodDays) + PeriodDays) % PeriodDays / PeriodDays;
        Keplerian.KeplerianSolver.keplerian_inverse(Eccentricity, phase * 360.0, out double anomaly);
        return anomaly;
    }

    // The Sun is the focus. Convert the research XY plane into Unity's horizontal XZ plane.
    // +X is perihelion; phase is elapsed time since perihelion, not a calendar date.
    public Vector3 PositionAtDays(double days)
    {
        double angle = TrueAnomalyAtDays(days) * Math.PI / 180.0;
        double radiusAU = SemiMajorAxisAU * (1.0 - Eccentricity * Eccentricity) /
            (1.0 + Eccentricity * Math.Cos(angle));
        return new Vector3((float)(radiusAU * Math.Cos(angle) * metresPerAU), 0f,
            (float)(radiusAU * Math.Sin(angle) * metresPerAU));
    }

    void UpdatePosition()
    {
        earth.localPosition = PositionAtDays(ElapsedDays);
        sun.localPosition = Vector3.zero;
        earthLabel.position = earth.position + Vector3.up * 0.24f;
    }

    void DrawOrbit()
    {
        double a = SemiMajorAxisAU * metresPerAU;
        double b = a * Math.Sqrt(1.0 - Eccentricity * Eccentricity);
        for (int i = 0; i < orbitPath.positionCount; i++)
        {
            double angle = 2.0 * Math.PI * i / orbitPath.positionCount;
            orbitPath.SetPosition(i, new Vector3((float)(a * (Math.Cos(angle) - Eccentricity)), 0f,
                (float)(b * Math.Sin(angle))));
        }
    }

    void RefreshReadout()
    {
        readout.text = (IsPaused ? "PAUSED" : "PLAYING") + "  /  " + PlaybackSpeed.ToString("0.##") + "x time" +
            "\nEarth orbit: " + PeriodDays.ToString("0.00") + " days  /  e = " + Eccentricity.ToString("0.00000") +
            "\nReference year 2000. Orbital phase, not a live date. Body sizes are enlarged.";
    }

    void OnDisable() { IsSuspended = true; }
}
