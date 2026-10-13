using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MarsOrbit : MonoBehaviour
{
    public GameObject MarsMasterControl;
    private SimulationController marsSimuControl;

    public OverlayFeed MarsDataFeed;
    public float MarsOrbitDuration = 686.92971f; // // Mars year in earth days
    public float MarsRadius = 50f * 1.52f; // Mars radius in relation to earths
    public float MarsEccentricity = 0.09339f;

    public float MarsXRadius = 50f * 1.52f;
    public float MarsYRadius = 40f * 1.52f;
    //* Mathf.Sqrt(1 - Mathf.Pow(0.09339f, 2));
    public bool MarsConstantOrbit = true;

    // Start is called before the first frame update
    void Start()
    {
        marsSimuControl = MarsMasterControl.GetComponent<SimulationController>();
    }
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = getMarsPosition(marsSimuControl.currentTime);
    }

    Vector3 getMarsPosition(float time)
    {
        Vector3 position = new Vector3(MarsXRadius * Mathf.Cos(2 * Mathf.PI * time / MarsOrbitDuration), 0.0f, MarsYRadius * Mathf.Sin(2 * Mathf.PI * time / MarsOrbitDuration));

        if (MarsConstantOrbit == false) position = new Vector3(MarsRadius * Mathf.Cos(2 * Mathf.PI * time / MarsOrbitDuration), 0.0f, MarsRadius * (1 - (float)MarsEccentricity) * Mathf.Sin(2 * Mathf.PI * time / MarsOrbitDuration));

        return position;
    }


}