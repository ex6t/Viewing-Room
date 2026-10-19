using System;
using UnityEngine;
using Seasons;   // SeasonHelper + SeasonsCalc
using Bergers;  // BergerSol (if available)

public class SeasonTreeSwitcher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object that has SimulationController (your Master Controller).")]
    public GameObject masterController;

    [Tooltip("Orbiting tree objects for each season.")]
    public GameObject winterTree;
    public GameObject springTree;
    public GameObject summerTree;
    public GameObject fallTree;

    [Header("Season / Orbit Params")]
    [Tooltip("Sidereal year length in days.")]
    public double T = 365.256363;

    [Tooltip("Orbital eccentricity used by SeasonsCalc.")]
    public double eccentricity = 0.01670236225492288;

    [Tooltip("Precession angle in radians.")]
    public double precession = Mathf.PI - 1.796256991128036f;

    private SimulationController sim;

    private double winterLen, springLen, summerLen, fallLen;
    private int cachedYear = int.MinValue;

    private bool simulationStarted = false;
    private SeasonHelper.SeasonType currentSeason;

    void Start()
    {
        if (masterController == null)
        {
            Debug.LogError("SeasonTreeSwitcher: masterController is not assigned.");
            return;
        }

        sim = masterController.GetComponent<SimulationController>();
        if (sim == null)
        {
            Debug.LogError("SeasonTreeSwitcher: SimulationController not found on masterController.");
            return;
        }

        // Optional: start with all trees off until simulation starts
        SetAllTreesActive(false);
    }

    void Update()
    {
        if (!simulationStarted || sim == null)
            return;

        int year = sim.getYear();
        if (year != cachedYear)
        {
            UpdateSeasonLengths(year);
        }

        UpdateSeason(false);
    }

    // Call this from your VR/UI button
    public void StartSimulation()
    {
        if (sim == null)
        {
            Debug.LogError("SeasonTreeSwitcher: Cannot start simulation, missing SimulationController.");
            return;
        }

        int year = sim.getYear();
        UpdateSeasonLengths(year);

        simulationStarted = true;
        UpdateSeason(true);
    }

    private void UpdateSeasonLengths(int year)
    {
        cachedYear = year;

        // Optional: pull eccentricity from BergerSol if your project uses it
        try
        {
            double obliquity, longPeri;
            BergerSol.CalculateOrbitalParameters(year, out eccentricity, out obliquity, out longPeri);
        }
        catch
        {
            // fine, just use existing eccentricity
        }

        SeasonsCalc.CalculateSeasonLengths(
            T,
            eccentricity,
            precession,
            out winterLen,
            out springLen,
            out summerLen,
            out fallLen
        );
    }

    private void UpdateSeason(bool force)
    {
        if (winterLen <= 0 || springLen <= 0 || summerLen <= 0 || fallLen <= 0)
            return;

        double day = GetDayOfYearFromSimulation();

        var newSeason = SeasonHelper.GetSeasonForTime(
            day,
            T,
            winterLen,
            springLen,
            summerLen,
            fallLen
        );

        if (!force && newSeason == currentSeason)
            return;

        currentSeason = newSeason;
        ApplySeasonObjects(newSeason);
    }

    private double GetDayOfYearFromSimulation()
    {
        string dateStr = sim.dateRead();  // e.g. "03/21/2000"

        if (DateTime.TryParse(dateStr, out DateTime dt))
            return dt.DayOfYear;

        Debug.LogWarning("SeasonTreeSwitcher: Could not parse date from SimulationController: " + dateStr);
        return 0;
    }

    private void ApplySeasonObjects(SeasonHelper.SeasonType season)
    {
        if (winterTree != null) winterTree.SetActive(season == SeasonHelper.SeasonType.Winter);
        if (springTree != null) springTree.SetActive(season == SeasonHelper.SeasonType.Spring);
        if (summerTree != null) summerTree.SetActive(season == SeasonHelper.SeasonType.Summer);
        if (fallTree != null) fallTree.SetActive(season == SeasonHelper.SeasonType.Fall);
    }

    private void SetAllTreesActive(bool active)
    {
        if (winterTree != null) winterTree.SetActive(active);
        if (springTree != null) springTree.SetActive(active);
        if (summerTree != null) summerTree.SetActive(active);
        if (fallTree != null) fallTree.SetActive(active);
    }
}
