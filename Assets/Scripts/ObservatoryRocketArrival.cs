using UnityEngine;

//the ship stays parked while the rig makes one short guided move to the ledge
public class ObservatoryRocketArrival : MonoBehaviour
{
    public float walkDistance = 2.4f;
    public float hatchSeconds = 0.85f;
    public float walkSeconds = 3.6f;
    public bool IsActive { get; private set; }
    public bool IsPaused { get; private set; }
    public bool HasArrived { get; private set; }
    public bool WasCancelled { get; private set; }
    public Vector3 AppliedOffset { get; private set; }
    public ObservatoryRocketHatch ParkedShip { get; private set; }

    ObservatoryModelDisplay display;
    Transform origin, captions, clock;
    Vector3 destinationOffset;
    Vector3 presentationOffset;
    bool ownsPresentation;
    bool exiting;
    float elapsed;

    public bool Begin(ObservatoryModelDisplay modelDisplay, Transform xrOrigin, Pose shipPose,
        Transform captionStand, Transform clockStand)
    {
        if (!isActiveAndEnabled || ownsPresentation || modelDisplay == null || !modelDisplay.IsSpaceView ||
            xrOrigin == null || modelDisplay.playerCamera == null) return false;
        if (ParkedShip == null)
        {
            var prefab = Resources.Load<GameObject>("Observatory/Rocket Ship");
            if (prefab == null)
            {
                Debug.LogWarning("[ObservatoryRocketArrival] The Rocket Ship prefab is unavailable.", this);
                return false;
            }
            var instance = Instantiate(prefab, transform);
            ParkedShip = instance.GetComponent<ObservatoryRocketHatch>();
            if (ParkedShip == null)
            {
                Destroy(instance);
                Debug.LogWarning("[ObservatoryRocketArrival] The ship needs its hatch component.", this);
                return false;
            }
            foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = modelDisplay.modelLayer;
        }

        display = modelDisplay;
        origin = xrOrigin;
        captions = captionStand;
        clock = clockStand;
        Vector3 forward = Vector3.ProjectOnPlane(shipPose.rotation * Vector3.forward, Vector3.up).normalized;
        destinationOffset = forward * Mathf.Max(0f, walkDistance);
        presentationOffset = destinationOffset;
        AppliedOffset = Vector3.zero;
        ownsPresentation = true;
        //the old space viewpoint becomes the end of the walk instead of its start
        display.modelRoot.position += presentationOffset;
        if (display.spaceStation != null) display.spaceStation.transform.position += presentationOffset;
        display.frame.transform.position += presentationOffset;
        if (captions != null) captions.position += presentationOffset;
        if (clock != null) clock.position += presentationOffset;
        ParkedShip.transform.SetPositionAndRotation(shipPose.position, shipPose.rotation);
        ParkedShip.gameObject.SetActive(true);
        ParkedShip.SetDeckHeight(origin.position.y - shipPose.position.y);
        ParkedShip.SetOpenAmount(0f);
        ParkedShip.SetStatus("DOCKED / hatch opening");
        elapsed = 0f;
        IsPaused = HasArrived = WasCancelled = exiting = false;
        IsActive = true;
        return true;
    }

    public void StartExit() { if (IsActive) exiting = true; }

    void Update()
    {
        if (!ownsPresentation) return;
        if (display == null || !display.IsSpaceView || origin == null) { Restore(); return; }
        if (!IsActive || !exiting || IsPaused || WasCancelled) return;
        //a stalled editor frame should slow the trip instead of jumping down the deck
        elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float doorTime = Mathf.Max(0.1f, hatchSeconds);
        if (ParkedShip.OpenAmount < 1f) ParkedShip.SetOpenAmount(Mathf.SmoothStep(0f, 1f, elapsed / doorTime));
        if (elapsed < doorTime) return;
        ParkedShip.SetStatus("To the ledge / X: pause   B: back");
        float progress = Mathf.Clamp01((elapsed - doorTime) / Mathf.Max(0.1f, walkSeconds));
        Vector3 nextOffset = destinationOffset * Mathf.SmoothStep(0f, 1f, progress);
        //add only our travel delta so real tracking and recentering stay independent
        origin.position += nextOffset - AppliedOffset;
        AppliedOffset = nextOffset;
        if (progress < 1f) return;
        HasArrived = true;
        IsActive = false;
        ParkedShip.SetStatus("PARKED / observation station");
    }

    public void TogglePause()
    {
        if (!IsActive || WasCancelled) return;
        IsPaused = !IsPaused;
        ParkedShip.SetStatus(IsPaused ? "PAUSED / X: resume   B: back" : "Guided exit to the observation ledge");
    }

    public void Cancel() { if (IsActive) WasCancelled = true; }

    public void Restore()
    {
        if (origin != null && AppliedOffset.sqrMagnitude > 0f) origin.position -= AppliedOffset;
        AppliedOffset = Vector3.zero;
        if (ownsPresentation)
        {
            if (display != null && display.IsSpaceView)
            {
                if (display.modelRoot != null) display.modelRoot.position -= presentationOffset;
                if (display.spaceStation != null) display.spaceStation.transform.position -= presentationOffset;
                if (display.frame != null) display.frame.transform.position -= presentationOffset;
            }
            if (captions != null) captions.position -= presentationOffset;
            if (clock != null) clock.position -= presentationOffset;
        }
        if (ParkedShip != null) ParkedShip.gameObject.SetActive(false);
        ownsPresentation = IsActive = IsPaused = exiting = false;
        presentationOffset = Vector3.zero;
        HasArrived = false;
    }

    void OnDisable()
    {
        bool hadPresentation = ownsPresentation;
        WasCancelled = IsActive;
        Restore();
        if (hadPresentation && display != null && display.IsSpaceView) display.SetSpaceView(false);
    }

    void OnDestroy()
    {
        Restore();
        if (ParkedShip != null) Destroy(ParkedShip.gameObject);
    }
}
