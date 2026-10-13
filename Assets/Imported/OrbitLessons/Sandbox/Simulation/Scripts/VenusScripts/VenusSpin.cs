using UnityEngine;

public class VenusSpin : MonoBehaviour
{
    public bool spinning = true;
    public float rotationDuration = 243.0226f; // Takes 243.0226 Earth Days To Rotate
    public GameObject masterControl;
    SimulationController simuControl;

    public float direction = -1f; // retrograde

    void Start()
    {
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
