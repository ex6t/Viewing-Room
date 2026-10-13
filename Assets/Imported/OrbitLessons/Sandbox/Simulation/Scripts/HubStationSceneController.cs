using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

public class HubStationSceneController : MonoBehaviour
{
    private GameObject hub;
    private bool wasActive = false;
    private XROrigin player;
    private Transform playerParent;
    private Scene lessonScene;

    void Start()
    {
        lessonScene = gameObject.scene;
        // Hide the station only for this lesson, preserving any player nested inside it.
        hub = GameObject.Find("HubStation");

        if (hub != null)
        {
            wasActive = hub.activeSelf;
            player = hub.GetComponentInChildren<XROrigin>();
            if (player != null)
            {
                playerParent = player.transform.parent;
                player.transform.SetParent(hub.transform.parent, true);
            }
            hub.SetActive(false);
            Debug.Log("[" + lessonScene.name + "] HubStation detected and DISABLED.");
        }

        // Listen for scene changes
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // Restore a surviving station when leaving the lesson that hid it.
        if (hub != null && wasActive && oldScene == lessonScene && newScene != lessonScene)
        {
            hub.SetActive(true);
            if (player != null && playerParent != null)
                player.transform.SetParent(playerParent, true);
            Debug.Log("[" + lessonScene.name + "] HubStation RE-ENABLED (left lesson).");

            // Stop listening — avoids multiple calls
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
    }

    private void OnDestroy()
    {
        // Clean up listener if script is destroyed
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}
