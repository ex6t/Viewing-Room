using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VenusOrbit : MonoBehaviour
{
    public GameObject VenusMasterControl;
    SimulationController simuControl;

    public OverlayFeed VenusDataFeed;
    public float VenusOrbitDuration = 224.7f; // Venus Year in Earth Days
    public float VenusRadius = 50f * 0.72f; // Venus' Raidus Compared To Earth
    public float VenusEccentricity = 0.0068f; // Least Eccentric In The Solar System

    public float VenusXRadius = 50f * 0.72f;
    public float VenusYRadius = 40f * 0.72f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool VenusConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = VenusMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getVenusPosition(simuControl.currentTime);
    }

    Vector3 getVenusPosition(float time)
    {
        Vector3 position = new Vector3(VenusXRadius * Mathf.Cos(2 * Mathf.PI * time / VenusOrbitDuration), 0.0f, VenusYRadius * Mathf.Sin(2 * Mathf.PI * time / VenusOrbitDuration));

        if (VenusConstantOrbit == false) position = new Vector3(VenusRadius * Mathf.Cos(2 * Mathf.PI * time / VenusOrbitDuration), 0.0f, VenusRadius * (1 - (float)VenusEccentricity) * Mathf.Sin(2 * Mathf.PI * time / VenusOrbitDuration));

        return position;
    }
}
