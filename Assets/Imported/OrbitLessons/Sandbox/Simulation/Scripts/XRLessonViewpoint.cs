using UnityEngine;
using Unity.XR.CoreUtils;

[DefaultExecutionOrder(-200)]
public class XRLessonViewpoint : MonoBehaviour
{
    public XROrigin playerOrigin;
    public Transform viewpoint;

    private bool placed;

    void LateUpdate()
    {
        if (placed || playerOrigin == null || playerOrigin.Camera == null || viewpoint == null) return;
        if (!XRInputButtons.IsHeadPoseReady()) return;

        // Align once after tracking starts. Never orbit, tilt, or lock the tracked headset.
        playerOrigin.MatchOriginUpCameraForward(Vector3.up, viewpoint.forward);
        playerOrigin.MoveCameraToWorldLocation(viewpoint.position);
        placed = true;
    }
}
