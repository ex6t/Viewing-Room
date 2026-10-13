using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerOnEarthFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Earth object the player should sit on.")]
    public Transform earth;

    [Header("Radius / Height Settings")]
    [Tooltip("Try to compute Earth radius from its Renderer bounds.")]
    public bool autoComputeEarthRadius = true;

    [Tooltip("Approximate radius of Earth in Unity units (used if auto-compute fails).")]
    public float earthRadius = 1.0f;

    [Tooltip("Extra height above the surface for the player.")]
    public float heightOffset = 0.2f;

    [Header("Follow Options")]
    [Tooltip("If true, start following Earth as soon as the scene starts.")]
    public bool followFromStart = false;

    [Tooltip("If true, player rotation matches Earth's rotation.")]
    public bool matchEarthRotation = true;

    private bool following = false;
    private XROrigin playerOrigin;

    private void Start()
    {
        playerOrigin = GetComponent<XROrigin>();
        if (earth == null)
        {
            Debug.LogError("PlayerOnEarthFollower: Earth reference is not assigned.");
            return;
        }

        if (autoComputeEarthRadius)
        {
            // Try to grab a Renderer from Earth or its children
            var rend = earth.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float computed = rend.bounds.extents.y;
                if (computed > 0f)
                    earthRadius = computed;
            }
        }

        following = followFromStart;
        if (following)
        {
            SnapToEarth();
        }
    }

    private void Update()
    {
        if (!following || earth == null)
            return;

        SnapToEarth();
    }

    private void SnapToEarth()
    {
        // "Top" of Earth is along earth.up
        Vector3 surfacePos = earth.position + (playerOrigin != null ? Vector3.up : earth.up) * (earthRadius + heightOffset);
        transform.position = surfacePos;

        // Earth may spin underneath the viewer; never rotate a tracked rig or undo snap turns.
        if (matchEarthRotation && playerOrigin == null)
        {
            transform.rotation = earth.rotation;
        }
    }

    // Call this from a button or script to begin following
    public void StartFollowing()
    {
        following = true;
        SnapToEarth();
    }

    // Optional: stop following
    public void StopFollowing()
    {
        following = false;
    }
}
