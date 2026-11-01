using UnityEngine;

//the path uses the same latitude and Sun position as our day lesson
public class ObservatorySunPath : MonoBehaviour
{
    public EarthObservationModel observation;
    public Renderer[] scenery = new Renderer[0];
    public GameObject standingReference;

    public bool IsPathVisible { get; private set; }
    public bool IsTerrainHidden { get; private set; }

    bool[] sceneryWasEnabled;
    bool available;

    static readonly int showPathId = Shader.PropertyToID("_ShowSunPath");
    static readonly int latitudeId = Shader.PropertyToID("_ObserverLatitude");
    static readonly int declinationId = Shader.PropertyToID("_SolarDeclination");
    static readonly int revealId = Shader.PropertyToID("_RevealBelowHorizon");

    public void SetAvailable(bool value)
    {
        available = value;
        if (!available || !isActiveAndEnabled) SetPathVisible(false);
    }

    public void TogglePath() { SetPathVisible(!IsPathVisible); }

    public void SetPathVisible(bool visible)
    {
        IsPathVisible = visible && available && isActiveAndEnabled;
        if (!IsPathVisible) SetTerrainHidden(false);
        ApplySky();
    }

    public void ToggleTerrain() { SetTerrainHidden(!IsTerrainHidden); }

    public void SetTerrainHidden(bool hidden)
    {
        hidden &= available && IsPathVisible && isActiveAndEnabled;
        if (hidden == IsTerrainHidden) return;
        if (hidden)
        {
            sceneryWasEnabled = new bool[scenery.Length];
            for (int i = 0; i < scenery.Length; i++)
            {
                if (scenery[i] == null) continue;
                sceneryWasEnabled[i] = scenery[i].enabled;
                //only hide the visuals so the floor and collisions still work
                scenery[i].enabled = false;
            }
        }
        else if (sceneryWasEnabled != null)
        {
            for (int i = 0; i < scenery.Length; i++)
                if (scenery[i] != null) scenery[i].enabled = sceneryWasEnabled[i];
            sceneryWasEnabled = null;
        }
        IsTerrainHidden = hidden;
        if (standingReference != null) standingReference.SetActive(hidden);
        ApplySky();
    }

    void ApplySky()
    {
        if (observation == null || observation.skyMaterial == null) return;
        Material sky = observation.skyMaterial;
        sky.SetFloat(showPathId, IsPathVisible ? 1f : 0f);
        sky.SetFloat(latitudeId, (float)EarthObservationModel.LatitudeDegrees * Mathf.Deg2Rad);
        sky.SetFloat(declinationId, (float)observation.SolarDeclinationDegrees * Mathf.Deg2Rad);
        sky.SetFloat(revealId, IsTerrainHidden ? 1f : 0f);
    }

    void LateUpdate()
    {
        if (IsPathVisible) ApplySky();
    }

    void OnDisable()
    {
        SetPathVisible(false);
    }
}
