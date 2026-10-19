using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MercuryOrbit : MonoBehaviour
{
    public GameObject MercuryMasterControl;
    SimulationController simuControl;

    public OverlayFeed MercuryDataFeed;
    public float MercuryOrbitDuration = 88f; // Mercury Year in Earth Days
    public float MercuryRadius = 50f * 0.4f; // Mercury's Raidus Compared To Earth
    public float MercuryEccentricity = 0.206f;

    public float MercuryXRadius = 50f * 0.4f;
    public float MercuryYRadius = 40f * 0.4f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool MercuryConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = MercuryMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getMercuryPosition(simuControl.currentTime);
    }

    Vector3 getMercuryPosition(float time)
    {
        Vector3 position = new Vector3(MercuryXRadius * Mathf.Cos(2 * Mathf.PI * time / MercuryOrbitDuration), 0.0f, MercuryYRadius * Mathf.Sin(2 * Mathf.PI * time / MercuryOrbitDuration));

        if (MercuryConstantOrbit == false) position = new Vector3(MercuryRadius * Mathf.Cos(2 * Mathf.PI * time / MercuryOrbitDuration), 0.0f, MercuryRadius * (1 - (float)MercuryEccentricity) * Mathf.Sin(2 * Mathf.PI * time / MercuryOrbitDuration));

        return position;
    }
}
