using UnityEngine;

public class UranusSpin : MonoBehaviour
{
    public bool spinning = true;
    public float rotationDuration = 0.7183f; // 17.24 Hours Divided By 24 (Earth's Hours) B/C It's In Retrospect Of Earth
    public GameObject masterControl;
    SimulationController simuControl;


    public float direction = -1f; // Retrograde

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