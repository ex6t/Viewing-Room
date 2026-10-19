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
            // Preserve the window's north-up view while presenting larger bodies in stereo.
            Quaternion cameraRotation = modelCamera.transform.rotation;
            modelRoot.rotation = viewingRotation * Quaternion.Inverse(cameraRotation) * modelRoot.rotation;
            modelRoot.localScale = originalScale * 1.6f;
            Vector3 centre = (observation.earthOrbit.sun.position + observation.earthOrbit.earth.position) * 0.5f;
            // Leave the lower part of the view clear for the fixed guide and comparison window.
            modelRoot.position += playerCamera.transform.position + forward * 5f + Vector3.up * 1.1f - centre;
        }
        else
        {
            if (modelRoot != null)
            {
                modelRoot.localPosition = originalPosition;
                modelRoot.localRotation = originalRotation;
                modelRoot.localScale = originalScale;
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
    }
}
