using UnityEngine;

public class PlutoSpin : MonoBehaviour
{
    public bool spinning = true;
    public float rotationDuration = 6.3872f; // Takes 6.3872 Earth Days To Rotate
    public GameObject masterControl;
    SimulationController simuControl;

    [HideInInspector]
    public float direction = 1f; // prograde

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