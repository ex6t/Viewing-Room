using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using TMPro;

public class PlanetNavigator : MonoBehaviour
{
    [Header("Planets (Transforms)")]
    public List<Transform> planetTransforms;

    [Header("Planet Data (optional)")]
    public List<PlanetInfo> planetInfos;

    [Header("UI (World Space Canvas)")]
    public Canvas planetCanvas;
    public Text planetNameText;
    public Text planetDescriptionText;
    public TMP_Text planetNameTextTMP;
    public TMP_Text planetDescriptionTextTMP;
    public Button nextButton;
    public Button prevButton;
    public Button returnToShipButton;

    [Header("Ship / Hub")]
    public Transform shipTransform;
    public bool hideShipDuringInspection = true;

    private int currentPlanetIndex = -1;
    private bool navigationActive = false;
    public bool IsNavigationActive => navigationActive;

    private readonly List<Camera> planetCameras = new List<Camera>();
    private readonly Dictionary<Behaviour, bool> pausedMotion = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<SimulationController, float> simulationSpeeds = new Dictionary<SimulationController, float>();
    private readonly Dictionary<XRRayInteractor, InteractionLayerMask> rayLayers = new Dictionary<XRRayInteractor, InteractionLayerMask>();
    private XROrigin playerOrigin;
    private Transform inspectionPlanet;
    private Vector3 originalPlanetScale;
    private Light inspectionLight;
    private Transform savedParent;
    private Vector3 savedLocalPosition;
    private Quaternion savedLocalRotation;
    private Vector3 savedLocalScale;
    private Vector3 savedWorldPosition;
    private Quaternion savedWorldRotation;
    private bool shipWasActive;
    private bool shipHidden;
    private int lastNavigationFrame = -1;

    public void Start()
    {
        planetCameras.Clear();
        if (planetTransforms != null)
        {
            foreach (Transform planet in planetTransforms)
            {
                Camera camera = planet != null ? planet.GetComponentInChildren<Camera>(true) : null;
                planetCameras.Add(camera);
                if (camera != null)
                {
                    camera.enabled = false;
                    camera.gameObject.SetActive(false);
                }
            }
        }

        if (planetCanvas != null) planetCanvas.gameObject.SetActive(false);
        if (planetDescriptionText != null && planetDescriptionTextTMP == null)
        {
            planetDescriptionText.resizeTextForBestFit = true;
            planetDescriptionText.resizeTextMinSize = 6;
            planetDescriptionText.resizeTextMaxSize = Mathf.Max(8, planetDescriptionText.fontSize);
            planetDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            planetDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform description = planetDescriptionText.rectTransform;
            description.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 115f);
            description.anchoredPosition = new Vector2(description.anchoredPosition.x, 35f);
        }
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPlanet);
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevPlanet);
        if (returnToShipButton != null) returnToShipButton.onClick.AddListener(ReturnToShip);
    }

    void Update()
    {
        if (!navigationActive && XRInputButtons.GetKeyboardKey(KeyCode.Space, true))
            StartPlanetTask();
    }

    public void StartPlanetTask()
    {
        if (navigationActive || !ListsValid()) return;
        playerOrigin = FindObjectOfType<XROrigin>();
        if (playerOrigin == null || playerOrigin.Camera == null)
        {
            Debug.LogError("[PlanetNavigator] A tracked XR Origin camera is required for planet inspection.");
            return;
        }

        Transform rig = playerOrigin.transform;
        savedParent = rig.parent;
        savedLocalPosition = rig.localPosition;
        savedLocalRotation = rig.localRotation;
        savedLocalScale = rig.localScale;
        savedWorldPosition = rig.position;
        savedWorldRotation = rig.rotation;
        rig.SetParent(null, true);

        TabletSummoner tablet = FindObjectOfType<TabletSummoner>();
        if (tablet != null) tablet.CloseTablet();
        if (hideShipDuringInspection && shipTransform != null)
        {
            shipWasActive = shipTransform.gameObject.activeSelf;
            shipHidden = true;
            shipTransform.gameObject.SetActive(false);
        }

        // Freeze orbital travel and lesson time; axial spin still reads the original speed.
        foreach (SimulationController simulation in FindObjectsOfType<SimulationController>())
        {
            simulationSpeeds.Add(simulation, simulation.simulationSpeed);
            PauseMotion(simulation);
        }
        foreach (Transform planet in planetTransforms)
        {
            if (planet == null) continue;
            foreach (MonoBehaviour behaviour in planet.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string type = behaviour.GetType().Name;
                if (type == "Orbit" || type.EndsWith("Orbit"))
                    PauseMotion(behaviour);
            }
        }
        foreach (LocomotionProvider provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
            if (!(provider is SnapTurnProviderBase)) PauseMotion(provider);
        foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
        {
            rayLayers.Add(ray, ray.interactionLayers);
            ray.interactionLayers = ray.interactionLayers.value & ~(1 << 31);
        }

        navigationActive = true;
        currentPlanetIndex = 0;
        ActivatePlanetCamera(currentPlanetIndex);
    }

    private void PauseMotion(Behaviour behaviour)
    {
        if (pausedMotion.ContainsKey(behaviour)) return;
        pausedMotion.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    private bool ListsValid()
    {
        if (planetTransforms == null || planetTransforms.Count == 0) return false;
        foreach (Transform planet in planetTransforms)
            if (planet == null) return false;
        return planetInfos == null || planetInfos.Count == 0 || planetInfos.Count == planetTransforms.Count;
    }

    private void ActivatePlanetCamera(int index)
    {
        if (!navigationActive || playerOrigin == null || index < 0 || index >= planetTransforms.Count) return;
        RestorePlanetScale();
        Transform planet = planetTransforms[index];
        Bounds bounds = GetPlanetBounds(planet);
        float radius = Mathf.Max(0.001f, bounds.extents.magnitude);
        // Keep miniature planets inspectable at a comfortable physical viewing distance.
        if (radius < 2f)
        {
            inspectionPlanet = planet;
            originalPlanetScale = planet.localScale;
            planet.localScale *= 2f / radius;
            bounds = GetPlanetBounds(planet);
            radius = Mathf.Max(0.001f, bounds.extents.magnitude);
        }
        Camera marker = index < planetCameras.Count ? planetCameras[index] : null;
        PlanetCameraController settings = marker != null ? marker.GetComponent<PlanetCameraController>() : null;
        Vector3 direction = marker != null ? marker.transform.position - bounds.center :
            playerOrigin.Camera.transform.position - bounds.center;
        if (settings != null && settings.useSunDirection && settings.sun != null)
            direction = settings.sun.position - bounds.center;
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.back;
        direction.Normalize();
        float distance = Mathf.Max(radius + 2f, radius / Mathf.Sin(25f * Mathf.Deg2Rad));
        float height = settings != null ? Mathf.Clamp(settings.heightOffset, 0f, distance * 0.25f) : 0f;
        Vector3 cameraPosition = bounds.center + direction * distance + Vector3.up * height;

        // Authored planet cameras remain disabled. Move the tracked rig only on a planet change.
        playerOrigin.MatchOriginUpCameraForward(Vector3.up, -direction);
        playerOrigin.MoveCameraToWorldLocation(cameraPosition);
        if (inspectionLight == null)
        {
            var lightObject = new GameObject("Planet Inspection Light");
            lightObject.transform.SetParent(transform, false);
            inspectionLight = lightObject.AddComponent<Light>();
            inspectionLight.type = LightType.Spot;
            inspectionLight.spotAngle = 80f;
            inspectionLight.intensity = 1.5f;
            inspectionLight.shadows = LightShadows.None;
        }
        // Outer planets sit beyond the original Sun light's range.
        inspectionLight.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up));
        inspectionLight.range = distance + radius * 3f;
        inspectionLight.enabled = true;
        PlacePlanetTablet();
        UpdateUIForIndex(index);
        if (prevButton != null) prevButton.interactable = index > 0;
        if (nextButton != null) nextButton.interactable = index < planetTransforms.Count - 1;
    }

    private Bounds GetPlanetBounds(Transform planet)
    {
        Bounds bounds = new Bounds(planet.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer renderer in planet.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    private void RestorePlanetScale()
    {
        if (inspectionPlanet != null) inspectionPlanet.localScale = originalPlanetScale;
        inspectionPlanet = null;
    }

    private void PlacePlanetTablet()
    {
        if (planetCanvas == null || playerOrigin == null) return;
        Transform camera = playerOrigin.Camera.transform;
        Vector3 forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = playerOrigin.transform.forward;
        planetCanvas.transform.SetParent(null, true);
        planetCanvas.renderMode = RenderMode.WorldSpace;
        planetCanvas.worldCamera = playerOrigin.Camera;
        planetCanvas.transform.position = camera.position + forward * 1.5f - Vector3.up * 0.1f;
        planetCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        planetCanvas.transform.localScale = Vector3.one * 0.003f;
        var raycaster = planetCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster == null) raycaster = planetCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        planetCanvas.gameObject.SetActive(true);
    }

    // Kept for existing scene callbacks; ending inspection restores the exact ship-relative pose.
    public void DeactivatePlanetCamera()
    {
        if (planetCanvas != null) planetCanvas.gameObject.SetActive(false);
        if (inspectionLight != null) inspectionLight.enabled = false;
        if (!navigationActive) return;
        // Close a task overlay before restoring the inspection's locomotion state.
        TabletSummoner tablet = FindObjectOfType<TabletSummoner>();
        if (tablet != null) tablet.CloseTablet();
        RestorePlanetScale();
        navigationActive = false;
        currentPlanetIndex = -1;
        foreach (var entry in pausedMotion)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        pausedMotion.Clear();
        foreach (var entry in simulationSpeeds)
            if (entry.Key != null) entry.Key.simulationSpeed = entry.Value;
        simulationSpeeds.Clear();
        foreach (var entry in rayLayers)
            if (entry.Key != null) entry.Key.interactionLayers = entry.Value;
        rayLayers.Clear();
        if (shipHidden && shipTransform != null) shipTransform.gameObject.SetActive(shipWasActive);
        shipHidden = false;
        if (playerOrigin != null)
        {
            Transform rig = playerOrigin.transform;
            rig.SetParent(savedParent, true);
            rig.localScale = savedLocalScale;
            if (savedParent != null)
            {
                rig.localPosition = savedLocalPosition;
                rig.localRotation = savedLocalRotation;
            }
            else
            {
                rig.SetPositionAndRotation(savedWorldPosition, savedWorldRotation);
            }
        }
    }

    void OnDisable()
    {
        DeactivatePlanetCamera();
    }

    void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextPlanet);
        if (prevButton != null) prevButton.onClick.RemoveListener(OnPrevPlanet);
        if (returnToShipButton != null) returnToShipButton.onClick.RemoveListener(ReturnToShip);
    }

    private void UpdateUIForIndex(int index)
    {
        if (planetInfos != null && planetInfos.Count == planetTransforms.Count)
        {
            PlanetInfo info = planetInfos[index];

            if (planetNameText != null)
                planetNameText.text = (info != null && !string.IsNullOrEmpty(info.planetName))
                    ? info.planetName
                    : planetTransforms[index].name;

            if (planetDescriptionText != null)
            {
                if (info != null && !string.IsNullOrEmpty(info.description))
                {
                    // Use description from PlanetInfo (the one in the Inspector)
                    planetDescriptionText.text = info.description;
                }
                else
                {
                    // Use Hardcoded descriptions based on planet name
                    string planetName = planetTransforms[index].name;

                    if (planetName == "Mercury")
                        planetDescriptionText.text = "-Orbit: 88 Earth Days\n-No Moons\n-No Rings\n-Second Densest Planet\n-Thinnest Atmosphere\n-Named After The Roman Messenger God";
                    else if (planetName == "Venus")
                        planetDescriptionText.text = "-Orbit: 225 Earth Days\n-Takes 117 Earth Days To Rotate\n-Rotates In Retrograde\n-No Moons\n-No Rings\n-Hottest Surface In The Solar System Apart From The Sun\n-Temperature Ranges: 86°F to 158°F\n-Scientists Believe That Studying The History Of Venus' Creation Can Help Us Better Earth's\n-Has An Induced Magnetic Field";
                    else if (planetName == "EarthModel" || planetName == "Earth")
                        planetDescriptionText.text = "-Orbit: 365 Days\n-Takes 23.9 Hours To Rotate\n-Is The Only Planet In The Solar System With 1 Moon\n-No Rings\n-Composed Of Four Main Layers\n-Global Ocean Covers 71% Of Planet's Surface\n-Atmosphere Consists Of 78% Nitrogen, 21% Oxygen, and 1% Other Gases\n-Named After The Germanic Word \"The Ground\"";
                    else if (planetName == "Mars")
                        planetDescriptionText.text = "-Orbit: 687 Earth Days\n-Takes 24.6 Hours To Rotate\n-Moons: 2\n-No Rings\n-Its Core Is Made Of Iron, Nickel, and Sulfur\n-Looks Reddish Due To Oxidization/Rusting Of Iron In Its Rocks\n-Its Canyon Named \"Valles Marineris\" Is Long Enough To Stretch From California To New York\n-No Global Magnetic Field";
                    else if (planetName == "Jupiter")
                        planetDescriptionText.text = "-Orbit: 4333 Earth Days\n-Takes 9.9 Hours To Rotate (Shortest Day In The Solar System)\n-Moons: 95\n-Does Have Rings\n-Composition Is Made Of Hydrogen and Helium\n-Known Of Its \"Great Red Spot\" Which Is A Swirling Oval Of Clouds Twice The Size Of Earth\n-Doesn't Have A True Surface, As It's A Gas Giant\n-Its Magnetic Field Is 16-54 Times As Powerful Of Earth's";
                    else if (planetName == "Saturn")
                        planetDescriptionText.text = "-Orbit: 10,759 Earth Days\n-Takes 10.7 Hours To Rotate (Second Shortest Day In The Solar Systemr\n-Moons: 146\n-Its Rings Are Made Of Billions Of Small Chunks Of Ice And Rocks\n-Composition Is Mostly Made Of Hydrogen And Helium\n-Doesn't Have A True Surface, As It's A Gas Giant\n-Saturn's Magnetic Field Is 578 Times More Powerful Than Earth's";
                    else if (planetName == "Uranus")
                        planetDescriptionText.text = "-Orbit: 30,687 Earth Days\n-Takes 17 Hours To Rotate\n-Rotates In Retrograde\n-Moons: 28\n-Has Two Sets of Rings\n-Is An Ice Giant That's Made Up Of 80% Of \"Icy\" Materials (Water, Methane, Ammonia)\n-Its Core Heats Up To Around 9,000°F\n-It's Blue Color Is Because Of The Methane\n-Has A Magnetosphere With A Tipped Over Magnetic Axis In It's Rotation By Nearly 60 Degrees";
                    else if (planetName == "Neptune")
                        planetDescriptionText.text = "-Orbit: 60,190 Earth Days\n-Takes About 16 Hours To Rotate\n-Moons: 16\n-Has At Least 5 Main Rings and 4 Prominent Ring Arcs\n-Is An Ice Giant That's Made Up Of 80% Of \"Icy\" Materials (Water, Methane, Ammonia)\n-Is The First Planet That Was Located Through Mathematical Predictions\n-Has The Strongest Winds In The Solar System\n-Has A Magnetic Field That Is 27 Stronger Than Earth's";
                    else if (planetName == "Pluto")
                        planetDescriptionText.text = "-Orbit: 90,560 Earth Days\n-Takes 6.4 Earth Days To Rotate\r\n-Moons: 5\n-No Rings\n-Some Speculate That There's An Ocean Inside Of Pluto's Interior\n-Is A Member Of The Kuiper Belt Along With The Other Dwarf Planets\n-Has A Heart-Shaped Feature Called The \"Tombaugh Regio\"\n-Has A Thin Atmosphere Made Of Molecular Nitrogen\n-Scientists Are Unsure If It Has A Magnetic Field, Assuming It's Either Extremely Small Or There Isn't One";
                    else
                        planetDescriptionText.text = "No Description Available.";
                }
            }
        }
        else
        {
            // If no planetInfos provided, use transform names as fallback
            if (planetNameText != null)
                planetNameText.text = planetTransforms[index].name;
            if (planetDescriptionText != null)
                planetDescriptionText.text = "";
        }
        // Keep the previous semesters' Text fields and data callbacks compatible.
        if (planetNameTextTMP != null && planetNameText != null) planetNameTextTMP.text = planetNameText.text;
        if (planetDescriptionTextTMP != null && planetDescriptionText != null) planetDescriptionTextTMP.text = planetDescriptionText.text;
    }

    private void OnNextPlanet()
    {
        if (!navigationActive || currentPlanetIndex >= planetTransforms.Count - 1) return;
        if (lastNavigationFrame == Time.frameCount) return;
        lastNavigationFrame = Time.frameCount;
        ActivatePlanetCamera(++currentPlanetIndex);
    }

    private void OnPrevPlanet()
    {
        if (!navigationActive || currentPlanetIndex <= 0) return;
        if (lastNavigationFrame == Time.frameCount) return;
        lastNavigationFrame = Time.frameCount;
        ActivatePlanetCamera(--currentPlanetIndex);
    }

    public void ReturnToShip()
    {
        if (!navigationActive) return;
        PlanetNavProgress.planetNavCompleted = true;
        DeactivatePlanetCamera();
        TabletMenu menu = FindObjectOfType<TabletMenu>();
        if (menu != null) menu.RefreshProgress();
    }

    public void nextPlanetFromHand()
    {
        OnNextPlanet();
    }

    public void prevPlanetFromHand()
    {
        OnPrevPlanet();
    }
}
