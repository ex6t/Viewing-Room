using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class HandSeasonsMenuInput : MonoBehaviour
{
    [Header("Tablets")]
    public GameObject tablet1; // First Tablet GameObject
    public GameObject tablet2; // Second Tablet GameObject

    // How Many Times Continue Has Been Pressed
    private int aPressCount = 0;
    private bool previousContinue = true;

    private void Start()
    {
        // Check If Tablets Are Null
        if (tablet1 == null || tablet2 == null)
            Debug.LogError("[HandSeasonsMenuInput] One of the tablet GameObjects is not assigned!");

        // Start With Tablet1 Active, Tablet2 Inactive
        if (tablet1 != null)
            tablet1.SetActive(true);

        if (tablet2 != null)
            tablet2.SetActive(false);
    }

    private void Update()
    {
        if (XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.PrimaryButton, KeyCode.Return, ref previousContinue))
        {
            aPressCount++;

            switch (aPressCount)
            {
                // First Time Pressing Continue: Switch To Tablet2
                case 1:
                    if (tablet1 != null) tablet1.SetActive(false);
                    if (tablet2 != null) tablet2.SetActive(true);
                    break;

                // Second Time Pressing Continue: Load Next Scene
                case 2:
                    SeasonsProgress.seasonsCompleted = true;
                    SceneManager.LoadScene("OrbitalModel");
                    break;

                default:
                    Debug.Log("Didn't Swap Scenes Properly, Counted An Extra Press!");
                    break;
            }
        }
    }
}
