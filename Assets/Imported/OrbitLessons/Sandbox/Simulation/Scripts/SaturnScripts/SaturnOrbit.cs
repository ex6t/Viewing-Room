using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaturnOrbit : MonoBehaviour
{
    public GameObject SaturnMasterControl;
    SimulationController simuControl;

    public OverlayFeed SaturnDataFeed;
    public float SaturnOrbitDuration = 10756f; // Saturn Year in Earth Days
    public float SaturnRadius = 50f * 9.5f; // Saturn's Raidus Compared To Earth
    public float SaturnEccentricity = 0.0565f;

    public float SaturnXRadius = 50f * 9.5f;
    public float SaturnYRadius = 40f * 9.5f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool SaturnConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = SaturnMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getSaturnPosition(simuControl.currentTime);
    }

    Vector3 getSaturnPosition(float time)
    {
        Vector3 position = new Vector3(SaturnXRadius * Mathf.Cos(2 * Mathf.PI * time / SaturnOrbitDuration), 0.0f, SaturnYRadius * Mathf.Sin(2 * Mathf.PI * time / SaturnOrbitDuration));

        if (SaturnConstantOrbit == false) position = new Vector3(SaturnRadius * Mathf.Cos(2 * Mathf.PI * time / SaturnOrbitDuration), 0.0f, SaturnRadius * (1 - (float)SaturnEccentricity) * Mathf.Sin(2 * Mathf.PI * time / SaturnOrbitDuration));

        return position;
    }
}
