using UnityEngine;

// Owns the stop's flow. Input, model, display, travel and tablet each keep their own role.
public class ViewingRoomLesson : MonoBehaviour
{
    public enum LessonStep { Ready, Route, Walking, Introduction, ExploreTime, ExploreShape, Reflection, NextStop, Complete }

    public RobotWelcomePrompt welcome;
    public RobotGuide robotGuide;
    public RobotTablet tablet;
    public Transform lessonPoint;
    public ViewingRoomLessonInput controls;
    public ViewingRoomDisplay display;
    public OrbitLearningModel model;
    public float activationDistance = 4.5f;

    public bool IsGuided { get; private set; }
    public bool HasVisited { get; private set; }
    public bool IsPresenting => display != null && display.IsOpen;
    public LessonStep CurrentStep { get; private set; }
    public bool KeepsLocomotionPaused => IsPresenting || (IsGuided && CurrentStep != LessonStep.Ready && CurrentStep != LessonStep.Complete);

    bool wasNear;
    bool started;
    bool completionDismissed;
    bool hasResume;
    LessonStep resumeStep = LessonStep.Introduction;

    void OnEnable()
    {
        controls?.EnableInput();
        if (started) RefreshScreen();
    }

    void Start()
    {
        if (welcome == null || robotGuide == null || tablet == null || lessonPoint == null || model == null ||
            controls == null || display == null || robotGuide.playerGuide == null || display.playerCamera == null ||
            display.recordingPlayer == null || display.recording == null || !controls.IsConfigured)
        {
            Debug.LogError("[ViewingRoomLesson] Assign the lesson references in the Inspector.", this);
            enabled = false;
            return;
        }
        started = true;
        RefreshScreen();
    }

    void Update()
    {
        if (!started) return;
        if (welcome.IsShowing) { controls.ResetInput(); return; }
        if (CurrentStep == LessonStep.Walking && robotGuide.HasArrived && robotGuide.playerGuide.PlayerHasArrived)
        {
            robotGuide.playerGuide.StopFollowing();
            robotGuide.playerGuide.FaceDisplay(display.transform);
            SetStep(LessonStep.Introduction);
        }
        if (robotGuide.playerGuide.IsFacingDisplay || display.IsMoving) { controls.ResetInput(); return; }
        bool near = IsNearScreen();
        if (near != wasNear)
        {
            wasNear = near;
            if (!IsGuided && CurrentStep == LessonStep.Ready && near) SetStep(LessonStep.Introduction);
            else if (!IsGuided && CurrentStep == LessonStep.Introduction && !near) SetStep(LessonStep.Ready);
        }
        controls.ReadInput();
    }

    public void StartGuidedTour()
    {
        bool wasGuided = IsGuided;
        IsGuided = true;
        if (CurrentStep == LessonStep.Ready || CurrentStep == LessonStep.Complete ||
            (!wasGuided && !IsPresenting && !display.IsPaused))
            SetStep(LessonStep.Route);
        else if (CurrentStep == LessonStep.Walking)
        {
            if (!robotGuide.playerGuide.StartFollowing()) { ChooseFreeRoam(); return; }
            robotGuide.ResumeWalk();
        }
        display.Resume();
        model.SetSuspended(!IsPresenting);
        UpdateLocomotion();
        controls.ResetInput();
        RefreshScreen();
    }

    public void ChooseFreeRoam()
    {
        if ((IsPresenting || display.IsPaused) && CurrentStep != LessonStep.NextStop)
        {
            resumeStep = CurrentStep;
            hasResume = true;
        }
        IsGuided = false;
        robotGuide.ReturnToStart();
        model.SetSuspended(true);
        display.Stop();
        wasNear = false;
        SetStep(LessonStep.Ready);
    }

    void BeginLesson()
    {
        completionDismissed = false;
        if (!robotGuide.playerGuide.StartFollowing())
        {
            ChooseFreeRoam();
            tablet.Show("ORBIT GUIDE", "Teleport onto the room floor, then choose Guided Tour again.", "A: show full text");
            return;
        }
        if (!robotGuide.WalkTo(lessonPoint))
        {
            ChooseFreeRoam();
            return;
        }
        SetStep(LessonStep.Walking);
    }

    // A reveals text first. The learner decides when to advance; no answer gates or timer.
    public void ContinueLesson()
    {
        if (welcome.IsShowing || display.IsMoving || robotGuide.playerGuide.IsFacingDisplay) return;
        if (tablet.IsTyping) { tablet.RevealText(); return; }
        switch (CurrentStep)
        {
            case LessonStep.Route: BeginLesson(); break;
            case LessonStep.Ready:
            case LessonStep.Introduction:
                if (IsNearScreen()) BeginPresentation();
                break;
            case LessonStep.ExploreTime: SetStep(LessonStep.ExploreShape); break;
            case LessonStep.ExploreShape: SetStep(LessonStep.Reflection); break;
            case LessonStep.Reflection: FinishLesson(); break;
            case LessonStep.NextStop:
                hasResume = false;
                model.ResetModel();
                SetStep(LessonStep.ExploreTime);
                break;
            case LessonStep.Complete:
                completionDismissed = true;
                tablet.Hide();
                break;
        }
    }

    public void BeginPresentation()
    {
        if (CurrentStep != LessonStep.Introduction) return;
        display.Open(false);
        model.SetSuspended(false);
        SetStep(hasResume ? resumeStep : LessonStep.ExploreTime);
        hasResume = false;
    }

    public void Interact()
    {
        if (CurrentStep == LessonStep.ExploreTime) model.TogglePause();
        else if (CurrentStep == LessonStep.ExploreShape) model.CompareShapes();
        else if (CurrentStep == LessonStep.NextStop) ReturnToRoom();
    }

    public void AdjustModel(int direction)
    {
        if (CurrentStep == LessonStep.ExploreTime) model.ChangeSpeed(direction);
        else if (CurrentStep == LessonStep.ExploreShape) model.ChangeShape(direction);
    }

    public void ResetOrGoBack()
    {
        if (CurrentStep == LessonStep.ExploreTime || CurrentStep == LessonStep.ExploreShape) model.ResetModel();
        else if (CurrentStep == LessonStep.Reflection) SetStep(LessonStep.ExploreShape);
        else if (CurrentStep == LessonStep.Route) ChooseFreeRoam();
    }

    public void ReplayRecording()
    {
        display.PlayRecording();
    }

    public void FinishLesson()
    {
        if (CurrentStep != LessonStep.Reflection) return;
        HasVisited = true;
        SetStep(LessonStep.NextStop);
    }

    public void ReturnToRoom()
    {
        hasResume = false;
        IsGuided = false;
        model.SetSuspended(true);
        display.Stop();
        robotGuide.ReturnToStart();
        completionDismissed = false;
        SetStep(LessonStep.Complete);
    }

    public void OpenModes()
    {
        robotGuide.playerGuide.CancelViewTurn();
        robotGuide.PauseWalk();
        model.SetSuspended(true);
        display.Pause();
        tablet.Hide();
        welcome.ShowWelcome();
        controls.ResetInput();
    }

    void SetStep(LessonStep step)
    {
        CurrentStep = step;
        UpdateLocomotion();
        controls.ResetInput();
        RefreshScreen();
    }

    void UpdateLocomotion()
    {
        if (KeepsLocomotionPaused) welcome.PauseLocomotion(CurrentStep == LessonStep.Walking);
        else welcome.ResumeLocomotion();
    }

    bool IsNearScreen()
    {
        return Vector3.ProjectOnPlane(display.playerCamera.position - lessonPoint.position, Vector3.up).sqrMagnitude <= activationDistance * activationDistance;
    }

    void RefreshScreen()
    {
        if (welcome.IsShowing) return;
        switch (CurrentStep)
        {
            case LessonStep.Ready: tablet.Hide(); break;
            case LessonStep.Route:
                tablet.Show("OUR TOUR  /  YOU SET THE PACE", "Orbit controls -> Planet gallery -> Kepler's laws -> Earth and seasons -> Habitable zone -> Our changing Sun.\nFirst, we'll try a model together. Pause, revisit or return to the room whenever you need.", "A: ready to travel     B: stay in the room     Y: modes");
                break;
            case LessonStep.Walking:
                tablet.Show("01  /  ORBIT OBSERVATORY", "We're going to the orbit display. We'll try time and shape controls here before boarding the Hub Station for the planet gallery.", "Right stick: snap turn     Y: pause / modes");
                break;
            case LessonStep.Introduction:
                tablet.Show("01  /  TAKE TIME INTO YOUR HANDS", hasResume ? "Welcome back. Your model settings are saved. Let's continue where you paused." : "Watch the planet travel around the Sun. You'll control its motion, then change the shape of its path. This is an illustrative model, with scaled sizes and time.", IsNearScreen() ? "A: open the model     Y: modes" : "Teleport closer, then press A");
                break;
            case LessonStep.ExploreTime:
                tablet.Show("01  /  CONTROL TIME", "Press X to freeze the planet. Press again to let it move. Move the left stick left or right to change playback speed. The path stays the same; only the pace changes.", "X: pause / play   Stick left/right: speed\nB: reset   A: orbit shape   Y: modes\nRight stick click: optional narration");
                break;
            case LessonStep.ExploreShape:
                tablet.Show("01  /  SHAPE THE ORBIT", "Press X to compare a circle and a stretched ellipse. Use the left stick left or right for smaller changes. The dots mark equal time steps: wider gaps show faster motion. Watch the Sun stay at one focus.", "X: circle / ellipse   Stick left/right: shape\nB: reset   A: explanation   Y: modes\nRight stick click: optional narration");
                break;
            case LessonStep.Reflection:
                tablet.Show("01  /  WHAT THE MODEL REVEALS", "A circle has eccentricity zero. Increasing it stretches the ellipse. The Sun sits at one focus. With equal time steps, the planet travels farther near the Sun. We'll explore these relationships through Kepler's laws later.", "B: explore again     A: what's next     Y: modes\nRight stick click: optional narration");
                break;
            case LessonStep.NextStop:
                tablet.Show("NEXT  /  MEET THE PLANETS", "Our next destination is the planet gallery aboard the Hub Station. We'll compare the worlds in this system before exploring why their orbits differ.\nYou've reached the end of today's first-stop prototype.", "A: revisit this model     X: return to the room     Y: modes");
                break;
            case LessonStep.Complete:
                if (completionDismissed) tablet.Hide();
                else tablet.Show("01  /  BACK IN THE ROOM", "You can explore the room or return to me to revisit the model. There is no score or time limit.", "A: dismiss     Triggers: teleport     Right stick: snap turn");
                break;
        }
    }

    void OnDisable()
    {
        robotGuide?.playerGuide?.CancelViewTurn();
        robotGuide?.PauseWalk();
        robotGuide?.playerGuide?.StopFollowing();
        IsGuided = false;
        CurrentStep = LessonStep.Ready;
        model?.SetSuspended(true);
        if (started) display.Stop();
        welcome?.ResumeLocomotion();
        if (tablet != null && (welcome == null || !welcome.IsShowing)) tablet.Hide();
        controls?.DisableInput();
    }
}
