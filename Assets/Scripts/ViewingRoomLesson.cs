using UnityEngine;

// The first stop introduces our home before the planet gallery and Kepler experiments.
public class ViewingRoomLesson : MonoBehaviour
{
    public enum LessonStep { Ready, Route, Walking, Introduction, ExploreHome, Reflection, NextStop, Complete }

    public RobotWelcomePrompt welcome;
    public RobotGuide robotGuide;
    public RobotTablet tablet;
    public Transform lessonPoint;
    public ViewingRoomLessonInput controls;
    public EarthOrbitModel homeModel;
    public float activationDistance = 2f;

    [Header("Retained Kepler Wall Exhibit")]
    public ViewingRoomDisplay display;
    public OrbitLearningModel model;

    public bool IsGuided { get; private set; }
    public bool HasVisited { get; private set; }
    public bool IsPresenting { get; private set; }
    public LessonStep CurrentStep { get; private set; }
    public bool KeepsLocomotionPaused => IsPresenting || (IsGuided && CurrentStep != LessonStep.Ready && CurrentStep != LessonStep.Complete);

    bool wasNear;
    bool started;
    bool completionDismissed;
    bool hasResume;
    bool presentationPaused;
    bool usedSpeed, usedPause, usedReset;
    LessonStep resumeStep = LessonStep.Introduction;

    void OnEnable()
    {
        controls?.EnableInput();
        if (started) RefreshScreen();
    }

    void Start()
    {
        if (welcome == null || robotGuide == null || tablet == null || lessonPoint == null || homeModel == null ||
            controls == null || model == null || display == null || robotGuide.playerGuide == null ||
            homeModel.playerCamera == null || !controls.IsConfigured)
        {
            Debug.LogError("[ViewingRoomLesson] Assign the lesson references in the Inspector.", this);
            enabled = false;
            return;
        }
        model.SetSuspended(true);
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
            robotGuide.playerGuide.FaceDisplay(homeModel.transform);
            SetStep(LessonStep.Introduction);
        }
        if (robotGuide.playerGuide.IsFacingDisplay) { controls.ResetInput(); return; }
        bool near = IsNearModel();
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
            (!wasGuided && !IsPresenting && !presentationPaused))
            SetStep(LessonStep.Route);
        else if (CurrentStep == LessonStep.Walking)
        {
            if (!robotGuide.playerGuide.StartFollowing()) { ChooseFreeRoam(); return; }
            robotGuide.ResumeWalk();
        }
        if (presentationPaused) IsPresenting = true;
        presentationPaused = false;
        homeModel.SetSuspended(!IsPresenting);
        UpdateLocomotion();
        controls.ResetInput();
        RefreshScreen();
    }

    public void ChooseFreeRoam()
    {
        if (IsPresenting || presentationPaused)
        {
            resumeStep = CurrentStep;
            hasResume = true;
        }
        IsGuided = IsPresenting = presentationPaused = false;
        robotGuide.ReturnToStart();
        homeModel.SetSuspended(true);
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
        if (!robotGuide.WalkTo(lessonPoint)) { ChooseFreeRoam(); return; }
        SetStep(LessonStep.Walking);
    }

    // A reveals text first, then continues. Trying a control is an invitation, never a score gate.
    public void ContinueLesson()
    {
        if (welcome.IsShowing || robotGuide.playerGuide.IsFacingDisplay) return;
        if (tablet.IsTyping) { tablet.RevealText(); return; }
        switch (CurrentStep)
        {
            case LessonStep.Route: BeginLesson(); break;
            case LessonStep.Ready:
            case LessonStep.Introduction:
                if (IsGuided || IsNearModel()) BeginPresentation();
                break;
            case LessonStep.ExploreHome: SetStep(LessonStep.Reflection); break;
            case LessonStep.Reflection: FinishLesson(); break;
            case LessonStep.NextStop: ReturnToRoom(); break;
            case LessonStep.Complete:
                completionDismissed = true;
                tablet.Hide();
                break;
        }
    }

    public void BeginPresentation()
    {
        if (CurrentStep != LessonStep.Introduction) return;
        IsPresenting = true;
        homeModel.SetSuspended(false);
        SetStep(hasResume ? resumeStep : LessonStep.ExploreHome);
        hasResume = false;
    }

    public void Interact()
    {
        if (CurrentStep != LessonStep.ExploreHome) return;
        homeModel.TogglePause();
        usedPause = true;
        RefreshControlCue();
    }

    public void AdjustModel(int direction)
    {
        if (CurrentStep != LessonStep.ExploreHome) return;
        homeModel.ChangeSpeed(direction);
        usedSpeed = true;
        RefreshControlCue();
    }

    public void ResetOrGoBack()
    {
        if (CurrentStep == LessonStep.ExploreHome)
        {
            homeModel.ResetModel();
            usedReset = true;
            RefreshControlCue();
        }
        else if (CurrentStep == LessonStep.Reflection || CurrentStep == LessonStep.NextStop) SetStep(LessonStep.ExploreHome);
        else if (CurrentStep == LessonStep.Route) ChooseFreeRoam();
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
        IsGuided = IsPresenting = presentationPaused = false;
        homeModel.SetSuspended(true);
        display.Stop();
        robotGuide.ReturnToStart();
        completionDismissed = false;
        SetStep(LessonStep.Complete);
    }

    public void OpenModes()
    {
        robotGuide.playerGuide.CancelViewTurn();
        robotGuide.PauseWalk();
        homeModel.SetSuspended(true);
        presentationPaused = IsPresenting;
        IsPresenting = false;
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

    bool IsNearModel()
    {
        return Vector3.ProjectOnPlane(homeModel.playerCamera.position - lessonPoint.position, Vector3.up).sqrMagnitude <= activationDistance * activationDistance;
    }

    void RefreshScreen()
    {
        if (welcome.IsShowing) return;
        switch (CurrentStep)
        {
            case LessonStep.Ready: tablet.Hide(); break;
            case LessonStep.Route:
                tablet.Show("OUR JOURNEY  /  START WITH HOME", "First, find Earth and the Sun at the central model. Then meet the other planets, explore how their orbits work, discover Earth and seasons, and investigate habitable worlds and our changing Sun.\nYou set the pace. Y pauses the tour and opens the menu.", "A: let's go     B: stay in the room");
                break;
            case LessonStep.Walking:
                tablet.Show("01  /  FIND OUR HOME", "Follow me to the central model. We'll start with a familiar world: Earth, travelling around the Sun. You can look around as we go.", "Right stick: turn     Y: pause");
                break;
            case LessonStep.Introduction:
                tablet.Show("01  /  HOME IN MOTION", hasResume ? "Welcome back. Your time settings are saved. Let's pick up where you paused." : "The blue world is Earth. The glowing body is our Sun. This miniature shows Earth's orbital path; the bodies are enlarged so you can see them. Let's watch our home move.", "A: begin", modelView: true);
                break;
            case LessonStep.ExploreHome:
                tablet.Show("01  /  TAKE TIME INTO YOUR HANDS", "Move the left stick left or right to change time. Watch Earth travel around the Sun. You're changing the viewing speed; Earth's orbital shape stays the same.\nWhen you're ready, press A to see where this journey leads. Y opens the menu.", "", modelView: true);
                RefreshControlCue();
                break;
            case LessonStep.Reflection:
                tablet.Show("01  /  ONE WORLD AMONG MANY", "One trip around the Sun takes Earth about a year. Its path is nearly circular. Earth is one planet in a system of different worlds. How do our neighbours compare, and what explains their motion? That's what we'll explore next.", "A: next destination     B: explore again", modelView: true);
                break;
            case LessonStep.NextStop:
                tablet.Show("NEXT  /  MEET OUR NEIGHBOURS", "The Hub Station's planet gallery is our next destination. We'll meet the other worlds before experimenting with orbit shapes and Kepler's laws. Later, we'll return our attention to Earth, habitability, and how the Sun changes.", "B: revisit Earth     A: return to the room", modelView: true);
                break;
            case LessonStep.Complete:
                if (completionDismissed) tablet.Hide();
                else tablet.Show("BACK IN THE ROOM", "You can explore or return to me to revisit Earth. Use a trigger to teleport and the right stick to turn.", "A: dismiss");
                break;
        }
    }

    void RefreshControlCue()
    {
        if (CurrentStep != LessonStep.ExploreHome || welcome.IsShowing) return;
        if (!usedSpeed) tablet.SetControlCue("LEFT STICK left / right: change time", true);
        else if (!usedPause) tablet.SetControlCue("X: pause / play", true);
        else if (!usedReset) tablet.SetControlCue("B: reset time", true);
        else tablet.SetControlCue("A: continue when you're ready");
    }

    void OnDisable()
    {
        robotGuide?.playerGuide?.CancelViewTurn();
        robotGuide?.PauseWalk();
        robotGuide?.playerGuide?.StopFollowing();
        IsGuided = IsPresenting = presentationPaused = false;
        CurrentStep = LessonStep.Ready;
        homeModel?.SetSuspended(true);
        model?.SetSuspended(true);
        if (started) display.Stop();
        welcome?.ResumeLocomotion();
        if (tablet != null && (welcome == null || !welcome.IsShowing)) tablet.Hide();
        controls?.DisableInput();
    }
}
