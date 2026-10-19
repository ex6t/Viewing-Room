using UnityEngine;

public class SeasonAssetFromTrails : MonoBehaviour
{
    [Header("Season Logic Source")]
    [Tooltip("Reference to the SeasonsRender component that controls the trails.")]
    public SeasonsRender seasonsRender;

    [Header("Season Assets (only one will be active)")]
    public GameObject springAsset;
    public GameObject summerAsset;
    public GameObject autumnAsset;
    public GameObject winterAsset;

    void Start()
    {
        // Optional: start with all off, script will turn on the right one once trails update
        SetAll(false);
    }

    void Update()
    {
        if (seasonsRender == null)
            return;

        bool springOn = seasonsRender.spring != null && seasonsRender.spring.emitting;
        bool summerOn = seasonsRender.summer != null && seasonsRender.summer.emitting;
        bool autumnOn = seasonsRender.autumn != null && seasonsRender.autumn.emitting;
        bool winterOn = seasonsRender.winter != null && seasonsRender.winter.emitting;

        // Priorities in case of any overlap (shouldn’t happen, but just in case)
        if (winterOn)
        {
            SetActiveSeason(winterAsset);
        }
        else if (springOn)
        {
            SetActiveSeason(springAsset);
        }
        else if (summerOn)
        {
            SetActiveSeason(summerAsset);
        }
        else if (autumnOn)
        {
            SetActiveSeason(autumnAsset);
        }
    }

    private void SetActiveSeason(GameObject active)
    {
        if (springAsset != null) springAsset.SetActive(active == springAsset);
        if (summerAsset != null) summerAsset.SetActive(active == summerAsset);
        if (autumnAsset != null) autumnAsset.SetActive(active == autumnAsset);
        if (winterAsset != null) winterAsset.SetActive(active == winterAsset);
    }

    private void SetAll(bool state)
    {
        if (springAsset != null) springAsset.SetActive(state);
        if (summerAsset != null) summerAsset.SetActive(state);
        if (autumnAsset != null) autumnAsset.SetActive(state);
        if (winterAsset != null) winterAsset.SetActive(state);
    }
}
