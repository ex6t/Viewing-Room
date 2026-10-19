using UnityEngine;

public class SceneSpeedController : MonoBehaviour
{
    [Header("References")]
    public GameObject masterControl;   // object with SimulationController

    [Header("Speed Settings")]
    [Tooltip("Base simulation speed (1 = normal).")]
    public float baseSimulationSpeed = 1f;

    [Tooltip("Global multiplier applied to the whole scene.")]
    public float speedMultiplier = 1f;

    [Tooltip("Also scale Unity's Time.timeScale?")]
    public bool affectTimeScale = true;

    private SimulationController simuControl;
    private float originalFixedDeltaTime;

    void Awake()
    {
        if (masterControl != null)
        {
            simuControl = masterControl.GetComponent<SimulationController>();
        }

        originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    void Update()
    {
       
        if (simuControl != null)
        {
            simuControl.simulationSpeed = baseSimulationSpeed * speedMultiplier;
        }

        
        if (affectTimeScale)
        {
            Time.timeScale = speedMultiplier;
            Time.fixedDeltaTime = originalFixedDeltaTime * speedMultiplier;
        }
    }

    public void SetSpeedMultiplier(float newMultiplier)
    {
        speedMultiplier = newMultiplier;
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
    }
}
