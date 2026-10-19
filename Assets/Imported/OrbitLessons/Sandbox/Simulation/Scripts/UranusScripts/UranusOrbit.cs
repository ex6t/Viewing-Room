using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UranusOrbit : MonoBehaviour
{
    public GameObject UranusMasterControl;
    SimulationController simuControl;

    public OverlayFeed UranusDataFeed;
    public float UranusOrbitDuration = 30687f; // Uranus Takes About 84 Earth Years To Orbit The Sun
    public float UranusRadius = 50f * 19f; // Uranus' Raidus Compared To Earth
    public float UranusEccentricity = 0.047f;

    public float UranusXRadius = 50f * 19f;
    public float UranusYRadius = 40f * 19f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool UranusConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = UranusMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getUranusPosition(simuControl.currentTime);
    }

    Vector3 getUranusPosition(float time)
    {
        Vector3 position = new Vector3(UranusXRadius * Mathf.Cos(2 * Mathf.PI * time / UranusOrbitDuration), 0.0f, UranusYRadius * Mathf.Sin(2 * Mathf.PI * time / UranusOrbitDuration));

        if (UranusConstantOrbit == false) position = new Vector3(UranusRadius * Mathf.Cos(2 * Mathf.PI * time / UranusOrbitDuration), 0.0f, UranusRadius * (1 - (float)UranusEccentricity) * Mathf.Sin(2 * Mathf.PI * time / UranusOrbitDuration));

        return position;
    }
}
