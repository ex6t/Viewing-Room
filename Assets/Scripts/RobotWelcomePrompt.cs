using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

// Welcome wording, mode selection and fresh A/X presses. Travel locking lives on the rig.
public class RobotWelcomePrompt : MonoBehaviour
{
    [Header("Welcome Prompt")]
    public RobotTablet tablet;
    public Transform playerCamera;
    public InputActionProperty continueInput;
    public InputActionProperty freeRoamInput; // X: select the highlighted mode.
    public InputActionReference navigationInput;
    public ViewingRoomLesson firstLesson;

    public PlayerLocomotionPause locomotion;

    public bool IsShowing { get; private set; }
    public bool IsLocomotionPaused => locomotion != null && locomotion.IsPaused;
    public int SelectedMode { get; private set; }

    bool previousContinue = true;
    bool previousSelect = true;
    bool previousInputEnabled;
    bool previousFreeRoamEnabled;
    bool inputReady;
    bool promptPlaced;
    bool started;
    bool headingAligned;
    bool previousUp = true;
    bool previousDown = true;

    void Start()
    {
        started = true;
        ShowWelcome();
    }

    void OnEnable()
    {
        if (started) ShowWelcome();
    }

    public void ShowWelcome()
    {
        if (IsShowing || !isActiveAndEnabled) return;
        if (firstLesson != null && firstLesson.IsPresenting)
        {
            firstLesson.OpenModes();
            return;
        }

        if (tablet == null || playerCamera == null || continueInput.action == null || navigationInput == null ||
            locomotion == null || locomotion.locomotionProviders == null || locomotion.locomotionProviders.Length == 0)
        {
            Debug.LogError("[RobotWelcome] Assign the prompt, camera, continue input, and locomotion pause component.", this);
            return;
        }

        PauseLocomotion();

        previousInputEnabled = continueInput.action.enabled;
        inputReady = previousInputEnabled;
        if (!inputReady) InputSystem.onAfterUpdate += OnInputUpdated;
        continueInput.action.Enable();
        if (freeRoamInput.action != null)
        {
            previousFreeRoamEnabled = freeRoamInput.action.enabled;
            freeRoamInput.action.Enable();
        }
        previousContinue = true;
        previousSelect = true;
        previousUp = previousDown = true;
        SelectedMode = 0;
        promptPlaced = false;
        tablet.Hide();
        IsShowing = true;
    }

    // Both welcome and lesson share the same movement lock.
    public void PauseLocomotion(bool allowSnapTurn = false)
    {
        locomotion.Pause(allowSnapTurn);
    }

    public void ResumeLocomotion()
    {
        if (IsShowing || (firstLesson != null && firstLesson.KeepsLocomotionPaused)) return;
        locomotion?.Resume();
    }

    void Update()
    {
        locomotion?.UpdateLock();
        if (!IsShowing) return;

        if (!promptPlaced)
        {
            // Wait for the headset height before placing the card, without moving the tracked camera.
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool tracked = head.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
            if (!tracked && !Application.isEditor) return;

            // Align the origin once after tracking starts; never edit the tracked camera pose.
            if (!headingAligned && locomotion.playerOrigin != null)
            {
                Vector3 towardRobot = Vector3.ProjectOnPlane(transform.position - playerCamera.position, Vector3.up);
                if (towardRobot.sqrMagnitude > 0.01f)
                    locomotion.playerOrigin.MatchOriginUpCameraForward(Vector3.up, towardRobot.normalized);
                headingAligned = true;
            }

            tablet.Show("ORBIT GUIDE  /  WELCOME", "Welcome to VR Orbit! Choose a guided tour with me, or explore at your own pace.",
                "Left stick: highlight     X: select     A: show full text", true);
            tablet.SetOptions("Guided Tour", "Free Roam");
            promptPlaced = true;
        }

        if (!inputReady) return;
        float vertical = navigationInput.action.ReadValue<Vector2>().y;
        bool up = vertical > 0.6f || (Keyboard.current != null && Keyboard.current.upArrowKey.isPressed);
        bool down = vertical < -0.6f || (Keyboard.current != null && Keyboard.current.downArrowKey.isPressed);
        if (up && !previousUp) SelectedMode = Mathf.Max(0, SelectedMode - 1);
        if (down && !previousDown) SelectedMode = Mathf.Min(1, SelectedMode + 1);
        tablet.SelectAnswer(SelectedMode);
        previousUp = up;
        previousDown = down;
        bool select = freeRoamInput.action != null && freeRoamInput.action.IsPressed();
        bool pressed = continueInput.action.IsPressed();
        if (pressed && !previousContinue && tablet.IsTyping)
        {
            tablet.RevealText();
        }
        else if (select && !previousSelect)
        {
            ClearWelcome();
            if (firstLesson != null)
            {
                if (SelectedMode == 1) firstLesson.ChooseFreeRoam();
                else firstLesson.StartGuidedTour();
            }
        }
        previousContinue = pressed;
        previousSelect = select;
    }

    void OnInputUpdated()
    {
        inputReady = true;
        InputSystem.onAfterUpdate -= OnInputUpdated;
    }

    public void ClearWelcome()
    {
        if (!IsShowing) return;
        IsShowing = false;
        InputSystem.onAfterUpdate -= OnInputUpdated;
        tablet?.Hide();

        ResumeLocomotion();
        if (!previousInputEnabled) continueInput.action.Disable();
        if (!previousFreeRoamEnabled) freeRoamInput.action?.Disable();
    }

    void OnDisable()
    {
        ClearWelcome(); // Disabling or unloading the guide must never leave the player locked.
        locomotion?.Restore();
    }

    void OnDestroy()
    {
        // An inline action belongs to this component; a referenced shared action belongs to its asset.
        if (continueInput.reference == null) continueInput.action?.Dispose();
        if (freeRoamInput.reference == null) freeRoamInput.action?.Dispose();
    }
}
