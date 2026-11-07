using System;
using UnityEngine;
using UnityEngine.SceneManagement;

//carry one model timestamp across each scene load
public static class SeasonsVisit
{
    const string ObservatoryScene = "Assets/Scenes/Observatory.unity";
    const string HubStationScene = "Assets/Scenes/OrbitalModel.unity";
    static double? observationDays;
    static double? orbitDays;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        observationDays = orbitDays = null;
    }

    public static bool TryEnterObservatory(double days)
    {
        if (observationDays.HasValue || !IsValid(days)) return false;
        observationDays = days;
        if (Load(ObservatoryScene) != null) return true;
        observationDays = null;
        return false;
    }

    public static bool TryTakeObservation(out double days)
    {
        return Take(ref observationDays, out days);
    }

    public static AsyncOperation EnterSeasons(string scene, double days)
    {
        if (orbitDays.HasValue || !IsValid(days)) return null;
        orbitDays = days;
        AsyncOperation load = Load(scene);
        if (load == null) orbitDays = null;
        return load;
    }

    public static bool TryTakeOrbitSelection(out double days)
    {
        return Take(ref orbitDays, out days);
    }

    public static bool TryContinueToHubStation()
    {
        if (Load(HubStationScene) == null) return false;
        SeasonsProgress.seasonsCompleted = true;
        return true;
    }

    static bool Take(ref double? selection, out double days)
    {
        days = selection.GetValueOrDefault();
        bool hasSelection = selection.HasValue;
        selection = null;
        return hasSelection;
    }

    static bool IsValid(double days)
    {
        return !double.IsNaN(days) && !double.IsInfinity(days);
    }

    static AsyncOperation Load(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogWarning("[SeasonsVisit] Enable the destination scene in Build Settings: " + scene);
            return null;
        }
        try
        {
            return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[SeasonsVisit] Could not open " + scene + ": " + error.Message);
            return null;
        }
    }
}
