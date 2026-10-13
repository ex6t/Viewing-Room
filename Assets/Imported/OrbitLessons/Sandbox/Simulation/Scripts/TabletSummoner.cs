using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class TabletSummoner : MonoBehaviour
{
    private readonly Dictionary<LocomotionProvider, bool> locomotionStates = new Dictionary<LocomotionProvider, bool>();
    private readonly Dictionary<XRRayInteractor, InteractionLayerMask> rayLayers = new Dictionary<XRRayInteractor, InteractionLayerMask>();
    private int closedFrame = -1;
    [Header("References")]
    public Transform playerCamera; // VR camera
    public Transform tabletCanvas; // tablet canvas root

    [Header("Placement Settings")]
    public float distanceInFront = 1.0f;
    public float heightOffset = 0.35f;
    public bool openOnStart = false;

    public bool IsTabletOpen => tabletCanvas != null && tabletCanvas.gameObject.activeSelf;
    public bool BlocksHandMenu => IsTabletOpen || closedFrame == Time.frameCount;

    void Start()
    {
        ResolvePlayerCamera();

        if (openOnStart)
            OpenTablet();
        else
            CloseTablet();
    }

    public void ToggleAndPlaceTablet()
    {
        if (IsTabletOpen)
            CloseTablet();
        else
            OpenTablet();
    }

    public void OpenTablet()
    {
        if (tabletCanvas == null)
        {
            Debug.LogWarning("[TabletSummoner] Missing tablet canvas reference.");
            return;
        }

        ResolvePlayerCamera();
        if (playerCamera == null)
        {
            Debug.LogWarning("[TabletSummoner] Missing player camera reference.");
            return;
        }

        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = playerCamera.forward;
        forward.Normalize();

        Vector3 targetPos = playerCamera.position
                            + forward * distanceInFront
                            + Vector3.up * heightOffset;

        Canvas canvas = tabletCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = playerCamera.GetComponent<Camera>();
        }

        TrackedDeviceGraphicRaycaster raycaster = tabletCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster == null)
            raycaster = tabletCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        raycaster.enabled = true;
        raycaster.ignoreReversedGraphics = true;

        CanvasFollower follower = tabletCanvas.GetComponent<CanvasFollower>();
        if (follower == null)
            follower = tabletCanvas.gameObject.AddComponent<CanvasFollower>();
        follower.playerCamera = playerCamera;
        follower.distanceInFront = distanceInFront;
        follower.heightOffset = heightOffset;
        follower.horizontalOffset = 0f;
        follower.keepUpright = true;
        follower.faceCamera = true;
        follower.followContinuously = true;
        follower.placementAnchor = null;
        follower.placementReference = null;
        follower.rotationOffset = Vector3.zero;
        follower.enabled = true;

        tabletCanvas.position = targetPos;
        // A Canvas faces its local -Z side, toward the viewer behind this forward vector.
        tabletCanvas.rotation = Quaternion.LookRotation(forward, Vector3.up);
        tabletCanvas.gameObject.SetActive(true);
        CanvasActionScript handMenu = FindObjectOfType<CanvasActionScript>();
        if (handMenu != null && handMenu.Menu != null) handMenu.Menu.SetActive(false);
        XROrigin origin = playerCamera.GetComponentInParent<XROrigin>();
        if (origin != null)
        {
            foreach (LocomotionProvider provider in origin.GetComponentsInChildren<LocomotionProvider>(true))
            {
                if (provider is SnapTurnProviderBase) continue;
                if (locomotionStates.ContainsKey(provider)) continue;
                locomotionStates.Add(provider, provider.enabled);
                provider.enabled = false;
            }
            foreach (XRRayInteractor ray in origin.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (rayLayers.ContainsKey(ray)) continue;
                rayLayers.Add(ray, ray.interactionLayers);
                ray.interactionLayers = ray.interactionLayers.value & ~(1 << 31);
            }
        }
        TabletMenu menu = FindObjectOfType<TabletMenu>();
        if (menu != null) menu.RefreshProgress();
    }

    public void CloseTablet()
    {
        closedFrame = Time.frameCount;
        if (tabletCanvas != null)
        {
            tabletCanvas.gameObject.SetActive(false);
        }
        foreach (var entry in locomotionStates)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        locomotionStates.Clear();
        foreach (var entry in rayLayers)
            if (entry.Key != null) entry.Key.interactionLayers = entry.Value;
        rayLayers.Clear();
    }

    void OnDisable()
    {
        CloseTablet();
    }

    private void ResolvePlayerCamera()
    {
        if (IsPlayerCamera(playerCamera))
            return;

        playerCamera = null;

        XROrigin origin = FindObjectOfType<XROrigin>();
        if (origin != null && origin.Camera != null)
        {
            playerCamera = origin.Camera.transform;
            return;
        }

        Camera mainCamera = Camera.main;
        if (IsPlayerCamera(mainCamera != null ? mainCamera.transform : null))
        {
            playerCamera = mainCamera.transform;
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        foreach (Camera camera in cameras)
        {
            if (IsPlayerCamera(camera.transform))
            {
                playerCamera = camera.transform;
                return;
            }
        }
    }

    private bool IsPlayerCamera(Transform cameraTransform)
    {
        if (cameraTransform == null)
            return false;

        Camera camera = cameraTransform.GetComponent<Camera>();
        if (camera == null || camera.targetTexture != null)
            return false;

        if (camera.CompareTag("MainCamera"))
            return true;

        Transform current = cameraTransform;
        while (current != null)
        {
            if (current.GetComponent<XROrigin>() != null)
                return true;

            current = current.parent;
        }

        return cameraTransform.name == "Main Camera";
    }
}
