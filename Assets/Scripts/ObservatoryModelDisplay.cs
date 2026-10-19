using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Two views of the same clock: Earth in the window, or California in the window.
public class ObservatoryModelDisplay : MonoBehaviour
{
    public EarthObservationModel observation;
    public Camera modelCamera;
    public Camera playerCamera;
    public Camera groundCamera;
    public Transform modelRoot;
    public RawImage image;
    public GameObject frame;
    public TMP_Text viewLabel;
    public Material starsMaterial;
    public int groundLayer = 13;
    public int modelLayer = 12;
    public float spaceMetresPerAU = 12f;
    public float spaceEarthRadius = 1f;
    public float spaceSunRadius = 3.6f;
    public float spaceOrbitWidth = 0.025f;
    public Transform[] seasonMarkers;
    public TMP_Text[] seasonLabels;
    public RenderTexture Texture { get; private set; }
    public bool IsSpaceView { get; private set; }

    Material runtimeStars;
    Matrix4x4 celestialFrame;
    bool requestedVisible;
    Skybox playerSkybox;
    Material previousPlayerSky;
    bool previousSkyboxEnabled;
    bool addedPlayerSkybox;
    int previousCameraMask;
    CameraClearFlags previousClearFlags;
    Vector3 originalPosition, originalScale;
    Quaternion originalRotation;
    Vector3 originalEarthScale, originalSunScale, originalMarkerScale;
    float originalMetresPerAU, originalGlobeRadius, originalOrbitWidth;
    bool originalOrbitVisible;
    double springAnomaly;

    void Awake()
    {
        Texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        Texture.name = "Earth and Sky Window";
        Texture.filterMode = FilterMode.Bilinear;
        Texture.wrapMode = TextureWrapMode.Clamp;
        Texture.useMipMap = false;
        Texture.autoGenerateMips = false;
        Texture.Create();
        image.texture = Texture;
        image.raycastTarget = false;
        modelCamera.targetTexture = Texture;
        modelCamera.stereoTargetEye = StereoTargetEyeMask.None;
        modelCamera.GetUniversalAdditionalCameraData().allowXRRendering = false;
        runtimeStars = new Material(starsMaterial);
        modelCamera.GetComponent<Skybox>().material = runtimeStars;
        if (groundCamera != null)
        {
            groundCamera.targetTexture = Texture;
            groundCamera.stereoTargetEye = StereoTargetEyeMask.None;
            groundCamera.GetUniversalAdditionalCameraData().allowXRRendering = false;
            groundCamera.cullingMask = 1 << groundLayer;
            groundCamera.clearFlags = CameraClearFlags.Skybox;
        }
        SetVisible(false);
    }

    void Start()
    {
        observation.InitializeModel();
        // Express the inherited celestial cube in the model's orbital frame.
        Bergers.BergerSol.CalculateOrbitalParameters(EarthOrbitModel.ReferenceYear, out _, out _, out double longitude);
        float angle = (float)((180.0 - longitude) * Mathf.Deg2Rad);
        springAnomaly = 180.0 - longitude;
        Vector3 spring = new Vector3(-Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
        Vector3 north = observation.AxisDirectionLocal;
        Vector3 east = Vector3.Cross(north, spring).normalized;
        celestialFrame = Matrix4x4.identity;
        celestialFrame.SetRow(0, new Vector4(east.x, east.y, east.z, 0f));
        celestialFrame.SetRow(1, new Vector4(north.x, north.y, north.z, 0f));
        celestialFrame.SetRow(2, new Vector4(spring.x, spring.y, spring.z, 0f));
        if (groundCamera != null && playerCamera != null)
        {
            // This is a fixed observation window, not a second head-tracked camera.
            groundCamera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
            Skybox groundSky = groundCamera.GetComponent<Skybox>();
            if (groundSky == null) groundSky = groundCamera.gameObject.AddComponent<Skybox>();
            groundSky.material = observation.skyMaterial;
        }
    }

    public void SetVisible(bool visible)
    {
        requestedVisible = visible;
        if (!visible) SetSpaceView(false);
        ApplyVisibility();
    }

    public void SetSpaceView(bool space)
    {
        if (space == IsSpaceView) return;
        if (space)
        {
            if (!isActiveAndEnabled || playerCamera == null || groundCamera == null || modelRoot == null) return;
            originalPosition = modelRoot.localPosition;
            originalRotation = modelRoot.localRotation;
            originalScale = modelRoot.localScale;
            var orbit = observation.earthOrbit;
            originalEarthScale = observation.globeSpin.localScale;
            originalSunScale = orbit.sun.localScale;
            originalMarkerScale = observation.observerMarker.localScale;
            originalMetresPerAU = orbit.metresPerAU;
            originalGlobeRadius = observation.globeRadius;
            originalOrbitVisible = orbit.orbitPath.enabled;
            originalOrbitWidth = orbit.orbitPath.widthMultiplier;
            previousCameraMask = playerCamera.cullingMask;
            previousClearFlags = playerCamera.clearFlags;
            // Capture the settled headset position once; tracking/recentering may finish after Start.
            groundCamera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
            playerSkybox = playerCamera.GetComponent<Skybox>();
            if (playerSkybox == null)
            {
                playerSkybox = playerCamera.gameObject.AddComponent<Skybox>();
                playerSkybox.enabled = false;
                addedPlayerSkybox = true;
            }
            previousPlayerSky = playerSkybox.material;
            previousSkyboxEnabled = playerSkybox.enabled;
            playerSkybox.material = runtimeStars;
            playerSkybox.enabled = true;
            playerCamera.clearFlags = CameraClearFlags.Skybox;
            playerCamera.cullingMask = (previousCameraMask & ~(1 << groundLayer)) | (1 << modelLayer);

            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Quaternion viewingRotation = Quaternion.LookRotation(forward, Vector3.up);
            // Show the complete orbit from above its edge, with winter Earth in the foreground.
            // Work from the orbital frame so nested model transforms cannot reverse the tilt.
            Quaternion relativeOrbit = Quaternion.Inverse(modelRoot.rotation) * orbit.orbitPlane.rotation;
            modelRoot.rotation = viewingRotation * Quaternion.Euler(18f, 0f, 0f) *
                Quaternion.Euler(0f, 10f, 0f) * Quaternion.Inverse(relativeOrbit);
            float sunRadius = orbit.sun.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * Mathf.Abs(orbit.sun.lossyScale.x);
            float earthRadius = observation.globeRenderer.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x *
                Mathf.Abs(observation.globeRenderer.transform.lossyScale.x);
            orbit.sun.localScale = originalSunScale * (spaceSunRadius / sunRadius);
            observation.globeSpin.localScale = originalEarthScale * (spaceEarthRadius / earthRadius);
            observation.observerMarker.localScale = originalMarkerScale * (spaceEarthRadius / originalGlobeRadius);
            observation.globeRadius = spaceEarthRadius;
            orbit.SetDisplayScale(spaceMetresPerAU);
            orbit.orbitPath.widthMultiplier = spaceOrbitWidth;
            orbit.orbitPath.enabled = true;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 sunPosition = playerCamera.transform.position + forward * 24f - right * 1.5f + Vector3.up * 2f;
            modelRoot.position += sunPosition - orbit.sun.position;
            SetSeasonMarkers(true);
        }
        else
        {
            if (modelRoot != null)
            {
                modelRoot.localPosition = originalPosition;
                modelRoot.localRotation = originalRotation;
                modelRoot.localScale = originalScale;
            }
            if (observation != null && observation.earthOrbit != null)
            {
                var orbit = observation.earthOrbit;
                if (observation.globeSpin != null) observation.globeSpin.localScale = originalEarthScale;
                if (observation.observerMarker != null) observation.observerMarker.localScale = originalMarkerScale;
                if (orbit.sun != null) orbit.sun.localScale = originalSunScale;
                observation.globeRadius = originalGlobeRadius;
                if (orbit.sun != null && orbit.earth != null && orbit.orbitPath != null)
                {
                    orbit.SetDisplayScale(originalMetresPerAU);
                    orbit.orbitPath.widthMultiplier = originalOrbitWidth;
                    orbit.orbitPath.enabled = originalOrbitVisible;
                }
            }
            SetSeasonMarkers(false);
            if (playerCamera != null)
            {
                playerCamera.cullingMask = previousCameraMask;
                playerCamera.clearFlags = previousClearFlags;
            }
            if (playerSkybox != null)
            {
                playerSkybox.material = previousPlayerSky;
                playerSkybox.enabled = previousSkyboxEnabled;
            }
        }
        IsSpaceView = space;
        // Recalculate world directions and the tack after moving the model, without advancing time.
        if (observation != null) observation.SetDays(observation.SimulationDays);
        ApplyVisibility();
    }

    void ApplyVisibility()
    {
        bool visible = requestedVisible && isActiveAndEnabled;
        if (frame != null) frame.SetActive(visible);
        if (modelCamera != null) modelCamera.enabled = visible && !IsSpaceView;
        if (groundCamera != null) groundCamera.enabled = visible && IsSpaceView;
        if (viewLabel != null) viewLabel.text = IsSpaceView ? "CALIFORNIA SKY / SAME TIME" : "EARTH & SUN / SIZES ENLARGED";
    }

    void OnEnable() { ApplyVisibility(); }

    void SetSeasonMarkers(bool visible)
    {
        if (seasonMarkers == null) return;
        for (int i = 0; i < seasonMarkers.Length; i++)
        {
            if (seasonMarkers[i] == null) continue;
            // Solar longitudes 0, 90, 180 and 270 degrees define the equinoxes and solstices.
            if (visible)
            {
                seasonMarkers[i].localPosition = observation.earthOrbit.PositionAtTrueAnomaly(springAnomaly + i * 90.0);
                if (seasonLabels != null && i < seasonLabels.Length && seasonLabels[i] != null)
                    seasonLabels[i].transform.localPosition = seasonMarkers[i].localPosition.normalized *
                        (spaceMetresPerAU * 0.08f) + Vector3.up * 0.5f;
            }
            seasonMarkers[i].gameObject.SetActive(visible);
        }
    }

    void LateUpdate()
    {
        // Both cameras use the same fixed celestial frame, including when the window shows the ground.
        runtimeStars.SetMatrix("_WorldToStars", celestialFrame * Matrix4x4.Rotate(Quaternion.Inverse(observation.earthOrbit.orbitPlane.rotation)));
        if (IsSpaceView && seasonLabels != null)
            foreach (var label in seasonLabels)
                if (label != null)
                {
                    label.transform.rotation = playerCamera.transform.rotation;
                    // Keep distant orbital annotations readable without enlarging their reference ticks.
                    float distance = Vector3.Distance(playerCamera.transform.position, label.transform.position);
                    label.transform.localScale = Vector3.one * Mathf.Clamp(distance / 12f, 0.75f, 4f);
                }
        if (!modelCamera.enabled) return;
        // Track the model's position, never the learner's head. The orbit itself is unchanged.
        Vector3 centre = (observation.earthOrbit.sun.position + observation.earthOrbit.earth.position) * 0.5f;
        modelCamera.transform.position = centre - modelCamera.transform.forward * 10f;
    }

    void OnDisable()
    {
        SetSpaceView(false);
        ApplyVisibility();
    }

    void OnDestroy()
    {
        SetSpaceView(false);
        if (modelCamera != null) modelCamera.targetTexture = null;
        if (groundCamera != null) groundCamera.targetTexture = null;
        if (addedPlayerSkybox && playerSkybox != null) Destroy(playerSkybox);
        if (Texture != null) { Texture.Release(); Destroy(Texture); }
        if (runtimeStars != null) Destroy(runtimeStars);
    }
}
