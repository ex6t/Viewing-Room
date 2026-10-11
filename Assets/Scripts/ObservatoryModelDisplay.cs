using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// A fixed window shows the existing model without placing large bodies in the room.
public class ObservatoryModelDisplay : MonoBehaviour
{
    public EarthObservationModel observation;
    public Camera modelCamera;
    public RawImage image;
    public GameObject frame;
    public Material starsMaterial;
    public RenderTexture Texture { get; private set; }

    Material runtimeStars;
    Matrix4x4 celestialFrame;
    bool requestedVisible;

    void Awake()
    {
        Texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        Texture.name = "Earth Model Window";
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
    }

    public void SetVisible(bool visible)
    {
        requestedVisible = visible;
        ApplyVisibility();
    }

    void ApplyVisibility()
    {
        bool visible = requestedVisible && isActiveAndEnabled;
        if (frame != null) frame.SetActive(visible);
        if (modelCamera != null) modelCamera.enabled = visible;
    }

    void OnEnable() { ApplyVisibility(); }

    void LateUpdate()
    {
        if (!modelCamera.enabled) return;
        // Track the model's position, never the learner's head. The orbit itself is unchanged.
        Vector3 centre = (observation.earthOrbit.sun.position + observation.earthOrbit.earth.position) * 0.5f;
        modelCamera.transform.position = centre - modelCamera.transform.forward * 10f;
        runtimeStars.SetMatrix("_WorldToStars", celestialFrame * Matrix4x4.Rotate(Quaternion.Inverse(observation.earthOrbit.orbitPlane.rotation)));
    }

    void OnDisable()
    {
        ApplyVisibility();
    }

    void OnDestroy()
    {
        if (modelCamera != null) modelCamera.targetTexture = null;
        if (Texture != null) { Texture.Release(); Destroy(Texture); }
        if (runtimeStars != null) Destroy(runtimeStars);
    }
}
