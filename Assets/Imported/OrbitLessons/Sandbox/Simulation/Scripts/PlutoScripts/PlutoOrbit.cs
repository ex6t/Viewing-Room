using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlutoOrbit : MonoBehaviour
{
    public GameObject PlutoMasterControl;
    SimulationController simuControl;

    public OverlayFeed PlutoDataFeed;
    public float PlutoOrbitDuration = 90560f; // Pluto Year in Earth Days
    public float PlutoRadius = 50f * 49.3f; // Pluto's Raidus Compared To Earth
    public float PlutoEccentricity = 0.2488f;

    public float PlutoXRadius = 50f * 49.3f;
    public float PlutoYRadius = 40f * 49.3f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool PlutoConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = PlutoMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getPlutoPosition(simuControl.currentTime);
    }

    Vector3 getPlutoPosition(float time)
    {
        Vector3 position = new Vector3(PlutoXRadius * Mathf.Cos(2 * Mathf.PI * time / PlutoOrbitDuration), 0.0f, PlutoYRadius * Mathf.Sin(2 * Mathf.PI * time / PlutoOrbitDuration));

        if (PlutoConstantOrbit == false) position = new Vector3(PlutoRadius * Mathf.Cos(2 * Mathf.PI * time / PlutoOrbitDuration), 0.0f, PlutoRadius * (1 - (float)PlutoEccentricity) * Mathf.Sin(2 * Mathf.PI * time / PlutoOrbitDuration));

        return position;
    }
}
