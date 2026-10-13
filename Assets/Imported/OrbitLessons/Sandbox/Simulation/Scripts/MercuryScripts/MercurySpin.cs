using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MercurySpin : MonoBehaviour
{
    public bool spinning = true;
    public float rotationDuration = 58.6461f; // Takes 58.6461 Earth Days To Rotate
    public GameObject masterControl;
    SimulationController simuControl;

    [HideInInspector]
    public float direction = 1f;

    void Start()
    {
        if (masterControl != null)
            simuControl = masterControl.GetComponent<SimulationController>();
    }

    void Update()
    {
        if (spinning)
        {
            float rotationSpeed = (360f / rotationDuration) * direction * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationSpeed);
        }
    }
}
