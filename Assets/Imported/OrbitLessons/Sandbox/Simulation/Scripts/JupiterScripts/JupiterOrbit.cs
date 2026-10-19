using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JupiterOrbit : MonoBehaviour
{
    public GameObject JupiterMasterControl;
    SimulationController simuControl;

    public OverlayFeed JupiterDataFeed;
    public float JupiterOrbitDuration = 4330.6f; // Jupiter Takes About 12 Earth Years To Orbit The Sun
    public float JupiterRadius = 50f * 5.2f; // Jupiter's Raidus Compared To Earth
    public float JupiterEccentricity = 0.0489f; 

    public float JupiterXRadius = 50f * 5.2f;
    public float JupiterYRadius = 40f * 5.2f;

    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool JupiterConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        simuControl = JupiterMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getJupiterPosition(simuControl.currentTime);
    }

    Vector3 getJupiterPosition(float time)
    {
        Vector3 position = new Vector3(JupiterXRadius * Mathf.Cos(2 * Mathf.PI * time / JupiterOrbitDuration), 0.0f,JupiterYRadius * Mathf.Sin(2 * Mathf.PI * time / JupiterOrbitDuration));

        if (JupiterConstantOrbit == false) position = new Vector3(JupiterRadius * Mathf.Cos(2 * Mathf.PI * time / JupiterOrbitDuration), 0.0f, JupiterRadius * (1 - (float)JupiterEccentricity) * Mathf.Sin(2 * Mathf.PI * time / JupiterOrbitDuration));

        return position;
    }
}
