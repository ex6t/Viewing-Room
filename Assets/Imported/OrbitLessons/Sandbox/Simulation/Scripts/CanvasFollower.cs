using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform playerCamera;      // VR camera / head

    [Header("Placement Settings")]
    public float distanceInFront = 1.2f;
    public float heightOffset = 0.0f;
    public float horizontalOffset = 0.0f;

    [Header("Rotation")]
    public bool keepUpright = true;   // ignore camera tilt
    public bool faceCamera = true;   // turn to face the player
    public bool followContinuously = true;
    public Transform placementAnchor;
    public Vector3 rotationOffset;
    public Transform placementReference;

    private bool placed;

    void OnEnable()
    {
        placed = false;
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;
        if (placed && !followContinuously) return;
        // A one-shot tablet must not be left behind at the pre-tracking camera pose.
        if (!placed && !XRInputButtons.IsHeadPoseReady()) return;

        // Direction in front of the camera
        Transform reference = placementReference != null ? placementReference : playerCamera;
        Vector3 forward = reference.forward;

        if (keepUpright)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = reference.forward;
            forward.Normalize();
        }

        // Position canvas in front of the player
        Vector3 targetPos =
            reference.position +
            forward * distanceInFront +
            Vector3.up * heightOffset +
            Vector3.Cross(Vector3.up, forward).normalized * horizontalOffset;

        transform.position = targetPos;
        placed = true;

        // Rotate to face the player
        if (faceCamera)
        {
            if (keepUpright)
            {
                transform.rotation = Quaternion.LookRotation(forward);
            }
            else
            {
                Vector3 lookDir = transform.position - playerCamera.position;
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        if (faceCamera)
            transform.rotation *= Quaternion.Euler(rotationOffset);
        if (placementAnchor != null)
            transform.position += targetPos - placementAnchor.position;
    }
}
