using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeptuneOrbit : MonoBehaviour
{
    public GameObject NeptuneMasterControl;
    SimulationController simuControl;

    public OverlayFeed NeptuneDataFeed;
    public float NeptuneOrbitDuration = 60190f; // Neptune Takes About 165 Earth Years To Orbit The Sun
    public float NeptuneRadius = 50f * 30f; // Neptune's Raidus Compared To Earth
    public float NeptuneEccentricity = 0.0086f; 

    public float NeptuneXRadius = 50f * 30f;
    public float NeptuneYRadius = 40f * 30f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool NeptuneConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = NeptuneMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getNeptunePosition(simuControl.currentTime);
    }

    Vector3 getNeptunePosition(float time)
    {
        Vector3 position = new Vector3(NeptuneXRadius * Mathf.Cos(2 * Mathf.PI * time / NeptuneOrbitDuration), 0.0f, NeptuneYRadius * Mathf.Sin(2 * Mathf.PI * time / NeptuneOrbitDuration));

        if (NeptuneConstantOrbit == false) position = new Vector3(NeptuneRadius * Mathf.Cos(2 * Mathf.PI * time / NeptuneOrbitDuration), 0.0f, NeptuneRadius * (1 - (float)NeptuneEccentricity) * Mathf.Sin(2 * Mathf.PI * time / NeptuneOrbitDuration));

        return position;
    }
}
