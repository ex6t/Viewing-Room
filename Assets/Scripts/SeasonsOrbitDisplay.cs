using System;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SeasonsOrbitDisplay : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] EarthOrbitModel orbitPrefab;
    [SerializeField] RobotTablet tabletPrefab;
    [SerializeField] GameObject robotPrefab;
    [SerializeField] Transform station;
    [SerializeField] Material deckMaterial;
    [SerializeField] Mesh inheritedPlanetMesh;
    [SerializeField] Material inheritedEarthMaterial;
    [SerializeField] Material inheritedSunMaterial;
    [SerializeField] Material earthLightingMaterial;
    [SerializeField] TrailRenderer[] inheritedSeasonTrails;

    const int SamplesPerSeason = 40;
    const float SnapDegrees = 2f;
    const float MaxRayDistance = 280f;
    const float OrbitRadius = 50f;
    const float EarthRadius = 5.2f;
    const float SunRadius = 15.14625f;
    const string Controls = "A / Enter: Observatory     Y / N: continue tour";

    public EarthOrbitModel OrbitModel { get; private set; }
    public double SelectedDays { get; private set; }

    EarthObservationModel observation;
    Material litEarth;
    TMP_Text readout;
    readonly TextMeshPro[] milestoneLabels = new TextMeshPro[SeasonsCalendar.MilestoneCount];
    LineRenderer axisLine;
    SphereCollider earthCollider;
    XRSimpleInteractable earthInteraction;
    IXRSelectInteractor grabbedBy;
    InputAction visitInput;
    InputAction continueInput;
    double selectedSolarHour;
    bool mouseDragging;
    bool navigationReady;
    bool sceneChanging;

    void Awake()
    {
        if (playerCamera == null || orbitPrefab == null || tabletPrefab == null || robotPrefab == null ||
            station == null || deckMaterial == null || inheritedPlanetMesh == null || inheritedEarthMaterial == null ||
            inheritedSunMaterial == null || earthLightingMaterial == null ||
            inheritedSeasonTrails == null || inheritedSeasonTrails.Length != SeasonsCalendar.MilestoneCount ||
            Array.Exists(inheritedSeasonTrails, trail => trail == null))
        {
            Debug.LogError("[SeasonsOrbitDisplay] Assign the camera, station, planet assets, season trails and guide.", this);
            enabled = false;
            return;
        }

        //set up the shared model before its Awake runs
        var exhibit = new GameObject("Seasons Exhibit");
        exhibit.transform.SetParent(transform, false);
        exhibit.SetActive(false);
        OrbitModel = Instantiate(orbitPrefab, exhibit.transform);
        OrbitModel.playerCamera = playerCamera.transform;
        Transform orbitPlane = OrbitModel.orbitPlane;
        foreach (Transform child in OrbitModel.transform)
            if (child != orbitPlane)
                child.gameObject.SetActive(false);
        orbitPlane.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        //reuse the inherited globe and texture with the shared observer model lighting
        litEarth = new Material(earthLightingMaterial);
        litEarth.SetTexture("_BaseMap", inheritedEarthMaterial.GetTexture("_BaseMap"));
        litEarth.SetFloat("_NightBrightness", 0.12f);
        OrbitModel.metresPerAU = OrbitRadius;
        MeshRenderer earthRenderer = SetPlanetSurface(OrbitModel.earth, EarthRadius, litEarth);
        SetPlanetSurface(OrbitModel.sun, SunRadius, inheritedSunMaterial);
        observation = exhibit.AddComponent<EarthObservationModel>();
        observation.earthOrbit = OrbitModel;
        observation.globeAxis = OrbitModel.earth;
        observation.globeRenderer = earthRenderer;
        AddViewingStep();
        AddStationLight();

        var robot = Instantiate(robotPrefab, exhibit.transform);
        robot.GetComponent<NavMeshAgent>().enabled = false;
        robot.GetComponent<Collider>().enabled = false;
        robot.transform.localPosition = new Vector3(-1.10f, 0f, 0.32f);
        robot.transform.localRotation = Quaternion.Euler(0f, 151f, 0f);
        robot.transform.localScale = Vector3.one * 0.72f;
        exhibit.SetActive(true);

        if (!observation.InitializeModel())
        {
            enabled = false;
            return;
        }
        CreateSegments();
        CreateAxis();
        SetUpGrab();
        ShowRobotPrompt();

        double start = SeasonsVisit.TryTakeOrbitSelection(out double savedDays) ? savedDays :
            SeasonsCalendar.WithSolarTime(SeasonsCalendar.MilestoneDays(0), EarthObservationModel.InitialSolarHours);
        SetSelectedDays(start);
        selectedSolarHour = observation.SolarTimeHours;

        visitInput = new InputAction("Visit Observatory", InputActionType.Button, "<XRController>{RightHand}/{PrimaryButton}");
        visitInput.AddBinding("<Keyboard>/enter");
        continueInput = new InputAction("Continue tour", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}");
        continueInput.AddBinding("<Keyboard>/n");
    }

    MeshRenderer SetPlanetSurface(Transform body, float radius, Material material)
    {
        body.GetComponent<MeshFilter>().sharedMesh = inheritedPlanetMesh;
        body.localScale = Vector3.one * (radius / inheritedPlanetMesh.bounds.extents.x);
        MeshRenderer renderer = body.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    void AddViewingStep()
    {
        //raise the standing spot just enough to see the close arc over the station rail
        var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = "Orbit Viewing Step";
        step.layer = 2;
        step.transform.SetParent(station, false);
        float height = transform.position.y - station.position.y;
        step.transform.SetPositionAndRotation(transform.position - Vector3.up * (height * 0.5f), transform.rotation);
        step.transform.localScale = new Vector3(2.6f, height, 1.6f);
        step.GetComponent<MeshRenderer>().sharedMaterial = deckMaterial;
    }

    void AddStationLight()
    {
        var light = new GameObject("Seasons Deck Light").AddComponent<Light>();
        light.transform.SetParent(transform, false);
        light.transform.localPosition = new Vector3(0f, 2.1f, 0.65f);
        light.type = LightType.Point;
        light.range = 13f;
        light.intensity = 9f;
    }

#if UNITY_EDITOR
    void Start()
    {
        //give mouse users the overlook view when no headset or simulated head pose is active
        if (!UnityEngine.XR.XRSettings.isDeviceActive &&
            !UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).isValid)
            playerCamera.transform.Rotate(26f, 0f, 0f, Space.Self);
    }
#endif

    void OnEnable()
    {
        navigationReady = false;
        visitInput?.Enable();
        continueInput?.Enable();
    }

    void OnDisable()
    {
        visitInput?.Disable();
        continueInput?.Disable();
        grabbedBy = null;
        mouseDragging = false;
    }

    void OnDestroy()
    {
        visitInput?.Dispose();
        continueInput?.Dispose();
        if (litEarth != null) Destroy(litEarth);
        if (earthInteraction == null) return;
        earthInteraction.selectEntered.RemoveListener(GrabEarth);
        earthInteraction.selectExited.RemoveListener(ReleaseEarth);
    }

    void Update()
    {
        if (sceneChanging) return;
        ReadDesktopInput();
        if (grabbedBy is XRRayInteractor rayInteractor)
        {
            Transform source = rayInteractor.rayOriginTransform;
            if (source != null) TrySelectFromRay(new Ray(source.position, source.forward));
        }
        else if (grabbedBy != null)
        {
            Transform source = grabbedBy.GetAttachTransform(earthInteraction);
            if (source != null) SelectLocalPoint(OrbitModel.orbitPlane.InverseTransformPoint(source.position));
        }

        //require a fresh press after arriving from another scene
        if (!navigationReady)
        {
            navigationReady = !visitInput.IsPressed() && !continueInput.IsPressed();
            return;
        }
        if (visitInput.WasPressedThisFrame()) sceneChanging = SeasonsVisit.TryEnterObservatory(SelectedDays);
        else if (continueInput.WasPressedThisFrame()) sceneChanging = SeasonsVisit.TryContinueToHubStation();
    }

    void LateUpdate()
    {
        foreach (TextMeshPro label in milestoneLabels)
            if (label != null) label.transform.rotation = playerCamera.transform.rotation;
    }

    void ReadDesktopInput()
    {
        if (grabbedBy != null) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) SelectMilestone(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectMilestone(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectMilestone(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) SelectMilestone(3);
            else if (keyboard.leftArrowKey.wasPressedThisFrame) SelectOrbitDay(SelectedDays - 1.0);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) SelectOrbitDay(SelectedDays + 1.0);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed)
        {
            mouseDragging = false;
            return;
        }
        Ray pointer = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (mouse.leftButton.wasPressedThisFrame)
            mouseDragging = earthCollider.Raycast(pointer, out _, MaxRayDistance);
        if (mouseDragging) TrySelectFromRay(pointer);
    }

    public bool TrySelectFromRay(Ray ray)
    {
        if (OrbitModel == null) return false;
        Transform orbitPlane = OrbitModel.orbitPlane;
        var plane = new Plane(orbitPlane.up, orbitPlane.position);
        return plane.Raycast(ray, out float distance) && distance <= MaxRayDistance &&
            SelectLocalPoint(orbitPlane.InverseTransformPoint(ray.GetPoint(distance)));
    }

    bool SelectLocalPoint(Vector3 point)
    {
        if (point.x * point.x + point.z * point.z < 0.04f) return false;
        double anomaly = Math.Atan2(point.z, point.x) * Mathf.Rad2Deg;
        double day = SeasonsCalendar.DaysAtTrueAnomaly(anomaly);
        double longitude = SeasonsCalendar.SolarLongitudeAtDays(day);
        int nearest = Mathf.RoundToInt((float)longitude / 90f);
        if (Math.Abs(longitude - nearest * 90.0) < SnapDegrees)
            SelectMilestone(nearest % SeasonsCalendar.MilestoneCount);
        else SelectOrbitDay(day);
        return true;
    }

    public void SelectMilestone(int index)
    {
        if (index >= 0 && index < SeasonsCalendar.MilestoneCount)
            SelectOrbitDay(SeasonsCalendar.MilestoneDays(index));
    }

    void SelectOrbitDay(double day)
    {
        //crossing perihelion keeps the simulation in the nearest orbital year
        double period = EarthOrbitModel.PeriodDays;
        double nearest = day + Math.Round((SelectedDays - day) / period) * period;
        double result = SeasonsCalendar.WithSolarTime(nearest, selectedSolarHour);
        if (Math.Abs(result - SelectedDays) > 0.00001) SetSelectedDays(result);
    }

    void SetSelectedDays(double days)
    {
        SelectedDays = days;
        observation.SetDays(days);
        Vector3 earth = OrbitModel.earth.localPosition;
        Vector3 halfAxis = observation.AxisDirectionLocal * (EarthRadius * 1.45f);
        axisLine.SetPosition(0, earth - halfAxis);
        axisLine.SetPosition(1, earth + halfAxis);
        int minutes = SeasonsCalendar.SolarClockMinutes(observation.SolarTimeHours);
        readout.text = $"{SeasonsCalendar.DateLabel(days)}   /   {minutes / 60:00}:{minutes % 60:00} California solar time";
    }

    void SetUpGrab()
    {
        GameObject earth = OrbitModel.earth.gameObject;
        earthCollider = earth.AddComponent<SphereCollider>();
        Bounds meshBounds = earth.GetComponent<MeshFilter>().sharedMesh.bounds;
        earthCollider.center = meshBounds.center;
        earthCollider.radius = meshBounds.extents.x * 1.22f;
        earthInteraction = earth.AddComponent<XRSimpleInteractable>();
        earthInteraction.selectEntered.AddListener(GrabEarth);
        earthInteraction.selectExited.AddListener(ReleaseEarth);
    }

    void GrabEarth(SelectEnterEventArgs args)
    {
        if (!isActiveAndEnabled || sceneChanging) return;
        mouseDragging = false;
        grabbedBy = args.interactorObject;
    }

    void ReleaseEarth(SelectExitEventArgs args)
    {
        if (grabbedBy == args.interactorObject) grabbedBy = null;
    }

    void CreateSegments()
    {
        OrbitModel.orbitPath.enabled = false;
        Transform plane = OrbitModel.orbitPlane;
        for (int i = 0; i < SeasonsCalendar.MilestoneCount; i++)
        {
            //keep the original trail style but draw the full ring before Earth moves
            TrailRenderer trail = inheritedSeasonTrails[i];
            LineRenderer arc = CreateLine(trail.name, trail.startColor, trail.widthMultiplier);
            arc.widthCurve = trail.widthCurve;
            arc.colorGradient = trail.colorGradient;
            arc.alignment = trail.alignment;
            arc.textureMode = trail.textureMode;
            arc.textureScale = trail.textureScale;
            arc.numCornerVertices = trail.numCornerVertices;
            arc.numCapVertices = trail.numCapVertices;
            arc.positionCount = SamplesPerSeason + 1;
            for (int j = 0; j <= SamplesPerSeason; j++)
            {
                double longitude = (i + (double)j / SamplesPerSeason) * 90.0;
                arc.SetPosition(j, OrbitModel.PositionAtTrueAnomaly(SeasonsCalendar.TrueAnomalyAtSolarLongitude(longitude)));
            }

            Vector3 landmark = OrbitModel.PositionAtDays(SeasonsCalendar.MilestoneDays(i));
            LineRenderer marker = CreateLine(SeasonsCalendar.MilestoneName(i) + " marker", Color.white, 0.5f);
            marker.SetPosition(0, landmark * 0.97f);
            marker.SetPosition(1, landmark * 1.11f);
            TextMeshPro label = CreateLabel(SeasonsCalendar.MilestoneName(i),
                SeasonsCalendar.MilestoneName(i), plane, 24f, Color.Lerp(trail.startColor, Color.white, 0.24f));
            label.transform.localPosition = landmark * 1.24f + Vector3.up * 3.8f;
            milestoneLabels[i] = label;
        }
    }

    void CreateAxis()
    {
        axisLine = CreateLine("Earth's tilt", Color.white, 0.28f);
    }

    LineRenderer CreateLine(string objectName, Color color, float width)
    {
        var line = new GameObject(objectName).AddComponent<LineRenderer>();
        line.transform.SetParent(OrbitModel.orbitPlane, false);
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = OrbitModel.orbitPath.sharedMaterial;
        SetLineColor(line, color);
        return line;
    }

    static void SetLineColor(LineRenderer line, Color color)
    {
        var tint = new MaterialPropertyBlock();
        tint.SetColor("_BaseColor", color);
        line.SetPropertyBlock(tint);
    }

    void ShowRobotPrompt()
    {
        var anchor = new GameObject("Seasons Guide Anchor").transform;
        anchor.SetParent(transform, false);
        anchor.localPosition = new Vector3(0.12f, 0.81f, 2.62f);
        RobotTablet tablet = Instantiate(tabletPrefab, anchor);
        tablet.playerCamera = playerCamera.transform;
        tablet.fixedAnchor = anchor;
        tablet.transform.localScale *= 1.18f;
        tablet.heading.fontSize = 27f;
        tablet.message.fontSize = 31f;
        tablet.hint.fontSize = 24f;
        tablet.Show("SEASONS / ORBIT GUIDE",
            "Look down over the ring. Point at Earth and hold grip to drag.\nTry June and December at the same time of day.\nIn the Editor, use mouse drag or 1–4.",
            Controls);
        tablet.RevealText();

        readout = CreateLabel("Selected date and time", string.Empty, transform, 0.51f, Color.white);
        readout.transform.localPosition = new Vector3(0.12f, 0.45f, 2.62f);
    }

    TextMeshPro CreateLabel(string objectName, string label, Transform parent, float size, Color color)
    {
        var text = new GameObject(objectName).AddComponent<TextMeshPro>();
        text.transform.SetParent(parent, false);
        text.font = OrbitModel.readout.font;
        text.text = label;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(1.4f, 0.14f);
        text.renderer.shadowCastingMode = ShadowCastingMode.Off;
        return text;
    }
}
