using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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
    public Material spaceSkyboxMaterial;
    public int groundLayer = 13;
    public int modelLayer = 12;
    public GameObject spaceStation;
    public float spaceMetresPerAU = 70f;
    public float spaceEarthRadius = 6.551f;
    public float spaceSunRadius = 15.14625f;
    public RenderTexture Texture { get; private set; }
    public bool IsSpaceView { get; private set; }

    Material runtimeStars;
    Material runtimeSpaceSky;
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
    Vector3 originalWindowPosition, originalWindowScale;
    Quaternion originalWindowRotation;
    Vector3 originalEarthScale, originalSunScale, originalMarkerScale;
    float originalMetresPerAU, originalGlobeRadius;
    bool originalOrbitVisible;
    readonly Vector3[] monitorCorners = new Vector3[4];

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
        if (spaceSkyboxMaterial != null) runtimeSpaceSky = new Material(spaceSkyboxMaterial);
        modelCamera.GetComponent<Skybox>().material = runtimeStars;
        if (groundCamera != null)
        {
            groundCamera.targetTexture = Texture;
            groundCamera.stereoTargetEye = StereoTargetEyeMask.None;
            groundCamera.GetUniversalAdditionalCameraData().allowXRRendering = false;
            groundCamera.cullingMask = 1 << groundLayer;
            groundCamera.clearFlags = CameraClearFlags.Skybox;
        }
        if (spaceStation != null)
        {
            // The station keeps its own fill light while California passes into night.
            var stationLight = new SphericalHarmonicsL2();
            stationLight.AddAmbientLight(new Color(0.48f, 0.55f, 0.62f));
            var lighting = new MaterialPropertyBlock();
            foreach (var renderer in spaceStation.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                renderer.GetPropertyBlock(lighting);
                lighting.CopySHCoefficientArraysFrom(new[] { stationLight });
                renderer.SetPropertyBlock(lighting);
            }
        }
        SetVisible(false);
    }

    void Start()
    {
        observation.InitializeModel();
        // Express the inherited celestial cube in the model's orbital frame.
        Bergers.BergerSol.CalculateOrbitalParameters(EarthOrbitModel.ReferenceYear, out _, out _, out double longitude);
        float angle = (float)((180.0 - longitude) * Mathf.Deg2Rad);
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
            if (!isActiveAndEnabled || playerCamera == null || groundCamera == null || modelRoot == null || frame == null) return;
            originalPosition = modelRoot.localPosition;
            originalRotation = modelRoot.localRotation;
            originalScale = modelRoot.localScale;
            originalWindowPosition = frame.transform.localPosition;
            originalWindowRotation = frame.transform.localRotation;
            originalWindowScale = frame.transform.localScale;
            var orbit = observation.earthOrbit;
            originalEarthScale = observation.globeSpin.localScale;
            originalSunScale = orbit.sun.localScale;
            originalMarkerScale = observation.observerMarker.localScale;
            originalMetresPerAU = orbit.metresPerAU;
            originalGlobeRadius = observation.globeRadius;
            originalOrbitVisible = orbit.orbitPath.enabled;
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
            playerSkybox.material = runtimeSpaceSky != null ? runtimeSpaceSky : runtimeStars;
            if (runtimeSpaceSky != null)
                // Face the inherited red nebula as it appears from the original station's 270-degree view.
                runtimeSpaceSky.SetFloat("_Rotation", spaceSkyboxMaterial.GetFloat("_Rotation") + playerCamera.transform.eulerAngles.y - 270f);
            playerSkybox.enabled = true;
            playerCamera.clearFlags = CameraClearFlags.Skybox;
            playerCamera.cullingMask = (previousCameraMask & ~(1 << groundLayer)) | (1 << modelLayer);

            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Quaternion viewingRotation = Quaternion.LookRotation(forward, Vector3.up);
            // Keep the inherited HubStation view level, with Earth closer than the Sun.
            // Look farther around the daylight side while keeping both bodies visible.
            Quaternion relativeOrbit = Quaternion.Inverse(modelRoot.rotation) * orbit.orbitPlane.rotation;
            modelRoot.rotation = viewingRotation * Quaternion.Euler(0f, 20.46f, 0f) * Quaternion.Inverse(relativeOrbit);
            float sunRadius = orbit.sun.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * Mathf.Abs(orbit.sun.lossyScale.x);
            float earthRadius = observation.globeRenderer.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x *
                Mathf.Abs(observation.globeRenderer.transform.lossyScale.x);
            orbit.sun.localScale = originalSunScale * (spaceSunRadius / sunRadius);
            observation.globeSpin.localScale = originalEarthScale * (spaceEarthRadius / earthRadius);
            observation.observerMarker.localScale = originalMarkerScale * (spaceEarthRadius / originalGlobeRadius);
            observation.globeRadius = spaceEarthRadius;
            orbit.SetDisplayScale(spaceMetresPerAU);
            orbit.orbitPath.enabled = false;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 feet = playerCamera.transform.position;
            feet.y = playerCamera.transform.root.position.y;
            Vector3 sunPosition = feet + forward * 68.758f - right * 36.559f;
            modelRoot.position += sunPosition - orbit.sun.position;
            if (spaceStation != null)
            {
                // Move the deck behind the fixed XR viewpoint to stand near its forward edge.
                spaceStation.transform.SetPositionAndRotation(feet - forward * 3f, viewingRotation);
                spaceStation.SetActive(true);
            }
            // Keep the sky as a small side monitor, below the Sun and Earth.
            Vector3 monitorOffset = forward * 1.6f - right * 0.9f;
            frame.transform.SetPositionAndRotation(playerCamera.transform.position + monitorOffset - Vector3.up * 0.75f,
                Quaternion.LookRotation(monitorOffset.normalized, Vector3.up) * Quaternion.Euler(40f, 0f, 0f));
            frame.transform.localScale = originalWindowScale * 0.38f;
            //keep the whole monitor above the deck when the simulator reports a low head pose
            frame.GetComponent<RectTransform>().GetWorldCorners(monitorCorners);
            float lowestCorner = monitorCorners[0].y;
            for (int i = 1; i < monitorCorners.Length; i++)
                lowestCorner = Mathf.Min(lowestCorner, monitorCorners[i].y);
            frame.transform.position += Vector3.up * Mathf.Max(0f, feet.y + 0.25f - lowestCorner);
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
                    orbit.orbitPath.enabled = originalOrbitVisible;
                }
            }
            if (spaceStation != null) spaceStation.SetActive(false);
            if (frame != null)
            {
                frame.transform.localPosition = originalWindowPosition;
                frame.transform.localRotation = originalWindowRotation;
                frame.transform.localScale = originalWindowScale;
            }
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
        // Recalculate world directions and the pointer after moving the model, without advancing time.
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

    void LateUpdate()
    {
        // Both cameras use the same fixed celestial frame, including when the window shows the ground.
        runtimeStars.SetMatrix("_WorldToStars", celestialFrame * Matrix4x4.Rotate(Quaternion.Inverse(observation.earthOrbit.orbitPlane.rotation)));
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
        if (runtimeSpaceSky != null) Destroy(runtimeSpaceSky);
    }
}
