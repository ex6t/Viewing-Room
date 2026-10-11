using System;
using TMPro;
using UnityEngine;

// A scale model for exploring Kepler motion. It has no calendar or real Earth ephemeris.
public class OrbitLearningModel : MonoBehaviour
{
    public Transform planet;
    public Transform sun;
    public LineRenderer orbitPath;
    public LineRenderer radiusLine;
    public TMP_Text readout;
    public Transform[] timeMarkers;
    public float displayRadius = 450f;
    public float secondsPerOrbit = 20f;
    [Range(0f, 0.6f)] public float startingEccentricity = 0.35f;

    public bool IsPaused { get; private set; }
    public float PlaybackSpeed { get; private set; } = 1f;
    public double Eccentricity { get; private set; }
    public double OrbitPhase { get; private set; }
    public bool IsSuspended { get; private set; } = true;

    double nextReadout;
    readonly float[] speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
    int speedIndex = 2;

    void Awake()
    {
        if (planet == null || sun == null || orbitPath == null || radiusLine == null || readout == null ||
            displayRadius <= 0f || secondsPerOrbit <= 0f)
        {
            Debug.LogError("[OrbitLearningModel] Assign the model references and positive display settings.", this);
            enabled = false;
            return;
        }
        ResetModel();
    }

    void Update()
    {
        if (IsSuspended) return;
        if (!IsPaused)
        {
            OrbitPhase = (OrbitPhase + Time.unscaledDeltaTime * PlaybackSpeed / secondsPerOrbit) % 1.0;
            UpdatePosition();
        }
        if (Time.unscaledTimeAsDouble >= nextReadout)
        {
            RefreshReadout();
            nextReadout = Time.unscaledTimeAsDouble + 0.2;
        }
    }

    public void SetSuspended(bool suspended)
    {
        IsSuspended = suspended;
    }

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

    public void ChangeShape(int direction)
    {
        SetEccentricity(Eccentricity + direction * 0.05);
    }

    public void CompareShapes()
    {
        SetEccentricity(Eccentricity < 0.01 ? 0.5 : 0.0);
    }

    public void SetEccentricity(double value)
    {
        Eccentricity = Math.Clamp(value, 0.0, 0.6);
        DrawOrbit();
        if (timeMarkers != null)
        {
            for (int i = 0; i < timeMarkers.Length; i++)
                if (timeMarkers[i] != null) timeMarkers[i].localPosition = PositionAtPhase((double)i / timeMarkers.Length);
        }
        UpdatePosition();
        RefreshReadout();
    }

    public void ResetModel()
    {
        OrbitPhase = 0.0;
        speedIndex = 2;
        PlaybackSpeed = 1f;
        IsPaused = false;
        SetEccentricity(startingEccentricity);
    }

    // Time is the fraction of one period since periapsis; the inherited solver takes degrees.
    public Vector3 PositionAtPhase(double phase)
    {
        Keplerian.KeplerianSolver.keplerian_inverse(Eccentricity, phase * 360.0, out double trueAnomaly);
        double angle = trueAnomaly * Math.PI / 180.0;
        double radius = displayRadius * (1.0 - Eccentricity * Eccentricity) /
            (1.0 + Eccentricity * Math.Cos(angle));
        // Display plane: +X is periapsis, +Y is counterclockwise, Sun is the origin/focus.
        return new Vector3((float)(radius * Math.Cos(angle)), (float)(radius * Math.Sin(angle)), 0f);
    }

    void UpdatePosition()
    {
        planet.localPosition = PositionAtPhase(OrbitPhase);
        sun.localPosition = Vector3.zero;
        radiusLine.SetPosition(0, Vector3.zero);
        radiusLine.SetPosition(1, planet.localPosition);
    }

    void DrawOrbit()
    {
        double minorAxis = displayRadius * Math.Sqrt(1.0 - Eccentricity * Eccentricity);
        for (int i = 0; i < orbitPath.positionCount; i++)
        {
            double angle = 2.0 * Math.PI * i / orbitPath.positionCount;
            orbitPath.SetPosition(i, new Vector3((float)(displayRadius * (Math.Cos(angle) - Eccentricity)),
                (float)(minorAxis * Math.Sin(angle)), 0f));
        }
    }

    void RefreshReadout()
    {
        readout.text = (IsPaused ? "PAUSED" : "PLAYING") + "   /   " + PlaybackSpeed.ToString("0.##") +
            "x playback   /   e = " + Eccentricity.ToString("0.00") +
            "\nIllustrative orbit. Sizes and time are scaled; this is not Earth's current orbit.";
    }

    void OnDisable()
    {
        IsSuspended = true;
    }
}
