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

    const int SamplesPerSeason = 40;
    const float SnapDegrees = 2f;
    const float MaxRayDistance = 12f;
    const string Controls = "A / Enter: Observatory     Y / N: continue tour";
    static readonly Color[] SeasonColors =
    {
        new(0.43f, 0.93f, 0.63f), new(1f, 0.79f, 0.32f),
        new(1f, 0.55f, 0.36f), new(0.46f, 0.78f, 1f)
    };

    public EarthOrbitModel OrbitModel { get; private set; }
    public double SelectedDays { get; private set; }

    EarthObservationModel observation;
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
        if (playerCamera == null || orbitPrefab == null || tabletPrefab == null || robotPrefab == null)
        {
            Debug.LogError("[SeasonsOrbitDisplay] Assign the camera, orbit exhibit, robot and tablet.", this);
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
        Light sunlight = OrbitModel.GetComponentInChildren<Light>(true);
        foreach (Transform child in OrbitModel.transform)
            if (child != orbitPlane && (sunlight == null || child != sunlight.transform))
                child.gameObject.SetActive(false);
        if (sunlight != null) sunlight.transform.SetParent(orbitPlane, false);
        orbitPlane.localPosition = new Vector3(0f, 1.05f, 2.6f);
        orbitPlane.localRotation = Quaternion.AngleAxis(-28f, Vector3.right) * Quaternion.AngleAxis(37f, Vector3.up);
        OrbitModel.earth.localScale = Vector3.one * 0.15f;
        OrbitModel.sun.localScale = Vector3.one * 0.26f;
        observation = exhibit.AddComponent<EarthObservationModel>();
        observation.earthOrbit = OrbitModel;
        observation.globeAxis = OrbitModel.earth;

        var robot = Instantiate(robotPrefab, exhibit.transform);
        robot.GetComponent<NavMeshAgent>().enabled = false;
        robot.GetComponent<Collider>().enabled = false;
        robot.transform.localPosition = new Vector3(-1.62f, 0.84f, 3.05f);
        robot.transform.localRotation = Quaternion.Euler(0f, 151f, 0f);
        robot.transform.localScale = Vector3.one * 0.72f;
        exhibit.SetActive(true);

        if (!observation.InitializeModel())
        {
            enabled = false;
            return;
        }
        OrbitModel.SetDisplayScale(1.02f);
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
        Vector3 halfAxis = observation.AxisDirectionLocal * 0.24f;
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
        LineRenderer inheritedRing = OrbitModel.orbitPath;
        for (int i = 0; i < SeasonsCalendar.MilestoneCount; i++)
        {
            //reuse the orbit renderer's material and geometry settings for each arc
            LineRenderer arc = i == 0 ? inheritedRing : Instantiate(inheritedRing, OrbitModel.orbitPlane);
            arc.gameObject.name = SeasonsCalendar.MilestoneName(i) + " arc";
            arc.loop = false;
            arc.widthMultiplier = 0.023f;
            arc.positionCount = SamplesPerSeason + 1;
            for (int j = 0; j <= SamplesPerSeason; j++)
            {
                double longitude = (i + (double)j / SamplesPerSeason) * 90.0;
                arc.SetPosition(j, OrbitModel.PositionAtTrueAnomaly(SeasonsCalendar.TrueAnomalyAtSolarLongitude(longitude)));
            }
            SetLineColor(arc, SeasonColors[i]);

            Vector3 landmark = OrbitModel.PositionAtDays(SeasonsCalendar.MilestoneDays(i));
            LineRenderer marker = CreateLine(SeasonsCalendar.MilestoneName(i) + " marker", Color.white, 0.017f);
            marker.SetPosition(0, landmark * 0.97f);
            marker.SetPosition(1, landmark * 1.11f);
            TextMeshPro label = CreateLabel(SeasonsCalendar.MilestoneName(i),
                SeasonsCalendar.MilestoneName(i), OrbitModel.orbitPlane, 0.58f, SeasonColors[i]);
            label.transform.localPosition = landmark * 1.17f + Vector3.up * 0.07f;
            milestoneLabels[i] = label;
        }
    }

    void CreateAxis()
    {
        axisLine = CreateLine("Earth's tilt", Color.white, 0.012f);
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
        anchor.localPosition = new Vector3(0.12f, 1.95f, 2.62f);
        RobotTablet tablet = Instantiate(tabletPrefab, anchor);
        tablet.playerCamera = playerCamera.transform;
        tablet.fixedAnchor = anchor;
        tablet.transform.localScale *= 1.18f;
        tablet.heading.fontSize = 27f;
        tablet.message.fontSize = 31f;
        tablet.hint.fontSize = 24f;
        tablet.Show("SEASONS / ORBIT GUIDE",
            "Point at Earth and hold grip to drag it around the Sun.\nTry June and December at the same time of day.\nIn the Editor, use mouse drag or 1–4.",
            Controls);
        tablet.RevealText();

        readout = CreateLabel("Selected date and time", string.Empty, transform, 0.51f, Color.white);
        readout.transform.localPosition = new Vector3(0.12f, 1.59f, 2.62f);
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
