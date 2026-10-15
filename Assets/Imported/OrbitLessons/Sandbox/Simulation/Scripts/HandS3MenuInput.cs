using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class HandS3MenuInput : MonoBehaviour
{
    private bool previousContinue = true; // Require release after the preceding scene's A press.

    private void Update()
    {
        if (XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.PrimaryButton, KeyCode.Return, ref previousContinue))
        {
            RedSunProgress.redSunCompleted = true; // Mark Red Sun Simulation As Completed
            SceneManager.LoadScene("OrbitalModel"); // Continue The Main Scene
        }
    }
}
