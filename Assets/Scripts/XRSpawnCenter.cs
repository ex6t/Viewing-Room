using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XROrigin))]
public class XRSpawnCenter : MonoBehaviour
{
    XROrigin playerOrigin;
    Vector3 spawnPosition;
    Vector3 spawnForward;

    void Awake()
    {
        playerOrigin = GetComponent<XROrigin>();
        spawnPosition = transform.position;
        spawnForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    }

    void LateUpdate()
    {
        //wait for the headset so its room position doesnt shift u to the side
        if (playerOrigin.Camera == null || !XRInputButtons.IsHeadPoseReady()) return;

        playerOrigin.MatchOriginUpCameraForward(Vector3.up, spawnForward);
        Vector3 centeredView = playerOrigin.Camera.transform.position;
        centeredView.x = spawnPosition.x;
        centeredView.z = spawnPosition.z;
        playerOrigin.MoveCameraToWorldLocation(centeredView);

        //only center once so u can look and move around normally after spawning
        enabled = false;
    }
}
