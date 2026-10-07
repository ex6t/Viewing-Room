using UnityEngine;

// Chooses the lesson stage. Input, display/audio, travel and tablet each have their own script.
public class ViewingRoomLesson : MonoBehaviour
{
    public enum LessonStep { Ready, Walking, Introduction, Listening, Question, Reflection, Complete }

    public RobotWelcomePrompt welcome;
    public RobotGuide robotGuide;
    public RobotTablet tablet;
    public Transform lessonPoint;
    public ViewingRoomLessonInput controls;
    public ViewingRoomDisplay display;
    public float activationDistance = 4.5f;

    public bool IsGuided { get; private set; }
    public bool HasVisited { get; private set; }
    public bool IsPresenting => display != null && display.IsOpen;
    public LessonStep CurrentStep { get; private set; }
    public int SelectedAnswer { get; private set; }
    public bool KeepsLocomotionPaused => IsPresenting || (IsGuided &&
        (CurrentStep == LessonStep.Walking || CurrentStep == LessonStep.Introduction));

    bool wasNear;
    bool choseEllipse;
    bool started;
    bool completionDismissed;

    void OnEnable()
    {
        controls?.EnableInput();
        if (started) RefreshScreen();
    }

    void Start()
    {
        if (welcome == null || robotGuide == null || tablet == null || lessonPoint == null ||
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
        if (welcome.IsShowing) { controls.ResetInput(); return; }
        if (display.IsPaused) ResumeLesson();
        if (CurrentStep == LessonStep.Walking && robotGuide.HasArrived && robotGuide.playerGuide.PlayerHasArrived)
        {
            robotGuide.playerGuide.StopFollowing();
            if (IsGuided)
            {
                welcome.PauseLocomotion();
                robotGuide.playerGuide.FaceDisplay(display.transform);
            }
            CurrentStep = LessonStep.Introduction;
            controls.ResetInput();
            RefreshScreen();
        }
        if (robotGuide.playerGuide.IsFacingDisplay || display.IsMoving) { controls.ResetInput(); return; }
        if (CurrentStep == LessonStep.Listening && !display.recordingPlayer.isPlaying)
        {
            CurrentStep = LessonStep.Question;
            SelectedAnswer = 0;
            controls.ResetInput();
            RefreshScreen();
        }
        bool near = IsNearScreen();
        if (near != wasNear)
        {
            wasNear = near;
            // Visiting a canvas in free roam never sends the robot away from its welcome spot.
            if (!IsGuided && CurrentStep == LessonStep.Ready && near) CurrentStep = LessonStep.Introduction;
            else if (!IsGuided && CurrentStep == LessonStep.Introduction && !near) CurrentStep = LessonStep.Ready;
            controls.ResetInput();
            RefreshScreen();
        }
        controls.ReadInput();
    }

    public void StartGuidedTour()
    {
        IsGuided = true;
        if (CurrentStep == LessonStep.Ready || CurrentStep == LessonStep.Complete) BeginLesson();
        else if (CurrentStep == LessonStep.Walking)
        {
            if (!robotGuide.playerGuide.StartFollowing()) { ChooseFreeRoam(); return; }
            robotGuide.ResumeWalk();
        }
        ResumeLesson();
    }

    public void ChooseFreeRoam()
    {
        IsGuided = false;
        robotGuide.ReturnToStart();
        display.Stop();
        CurrentStep = LessonStep.Ready;
        wasNear = false;
        welcome.ResumeLocomotion();
        controls.ResetInput();
        RefreshScreen();
    }

    void ResumeLesson()
    {
        display.Resume();
        if (KeepsLocomotionPaused) welcome.PauseLocomotion(IsGuided && CurrentStep == LessonStep.Walking);
        else welcome.ResumeLocomotion();
        controls.ResetInput();
        RefreshScreen();
    }

    void BeginLesson()
    {
        completionDismissed = false;
        if (!IsGuided)
        {
            CurrentStep = LessonStep.Introduction;
            controls.ResetInput();
            RefreshScreen();
            return;
        }
        if (IsGuided && !robotGuide.playerGuide.StartFollowing())
        {
            IsGuided = false;
            welcome.ResumeLocomotion();
            tablet.Show("ORBIT GUIDE", "Please teleport onto the room floor, then choose Guided Tour again.", "");
            return;
        }
        if (!robotGuide.WalkTo(lessonPoint)) { robotGuide.playerGuide.StopFollowing(); return; }
        CurrentStep = LessonStep.Walking;
        if (IsGuided) welcome.PauseLocomotion(true);
        controls.ResetInput();
        RefreshScreen();
    }

    // A reveals a sentence first; a separate press continues or dismisses it.
    public void ContinueLesson()
    {
        if (tablet.IsTyping) tablet.RevealText();
        else if (CurrentStep == LessonStep.Ready && IsNearScreen()) BeginLesson();
        else if (CurrentStep == LessonStep.Introduction && IsNearScreen()) BeginPresentation();
        else if (CurrentStep == LessonStep.Reflection) FinishLesson();
        else if (CurrentStep == LessonStep.Complete)
        {
            completionDismissed = true;
            tablet.Hide();
        }
    }

    public void BeginPresentation()
    {
        if (CurrentStep != LessonStep.Introduction && CurrentStep != LessonStep.Complete) return;
        welcome.PauseLocomotion();
        CurrentStep = LessonStep.Listening;
        tablet.Hide();
        display.Open();
        controls.ResetInput();
    }

    public void SelectAnswer()
    {
        if (CurrentStep != LessonStep.Question) return;
        choseEllipse = SelectedAnswer == 1;
        CurrentStep = LessonStep.Reflection;
        RefreshScreen();
    }

    public void HighlightAnswer(int direction)
    {
        if (CurrentStep != LessonStep.Question || welcome.IsShowing) return;
        SelectedAnswer = Mathf.Clamp(SelectedAnswer + direction, 0, 1);
        tablet.SelectAnswer(SelectedAnswer);
    }

    public void ReplayRecording()
    {
        // B keeps the question/feedback visible; only the first listen hides the tablet.
        display.PlayRecording();
    }

    public void FinishLesson()
    {
        if (CurrentStep != LessonStep.Reflection) return;
        if (choseEllipse)
        {
            HasVisited = true;
            CurrentStep = LessonStep.Complete;
            completionDismissed = false;
            display.Stop();
            welcome.ResumeLocomotion();
        }
        else { CurrentStep = LessonStep.Question; SelectedAnswer = 0; }
        controls.ResetInput();
        RefreshScreen();
    }

    public void OpenModes()
    {
        robotGuide.playerGuide.CancelViewTurn();
        robotGuide.PauseWalk();
        display.Pause();
        tablet.Hide();
        welcome.ShowWelcome();
        controls.ResetInput();
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
            case LessonStep.Ready:
                tablet.Hide(); // The welcome script shows the free-roam instructions only once.
                break;
            case LessonStep.Walking:
                tablet.Show("ORBIT GUIDE  /  LET'S GO", IsGuided ? "I'll take you to our first display. Look around as we travel together." : "Follow me to the orbit display using teleport. I'll introduce it when I arrive.", "A: show full text");
                break;
            case LessonStep.Introduction:
                if (!IsGuided && !IsNearScreen()) { tablet.Hide(); break; }
                tablet.Show("ORBIT GUIDE  /  ORBIT SHAPE", "This canvas compares a circle with an ellipse. Let's look closer and listen to what eccentricity tells us about an orbit.", IsNearScreen() ? "A: view and listen" : "Teleport closer, then press A");
                break;
            case LessonStep.Listening:
                tablet.Hide();
                break;
            case LessonStep.Question:
                tablet.Show("ORBIT GUIDE  /  QUICK CHECK", "Which orbit has greater eccentricity?", "Left stick: highlight     X: select     B: listen again", true);
                tablet.SetOptions("Orbit A  -  Circle", "Orbit B  -  Ellipse");
                tablet.SelectAnswer(SelectedAnswer);
                break;
            case LessonStep.Reflection:
                tablet.Show("ORBIT GUIDE  /  " + (choseEllipse ? "CORRECT!" : "TRY AGAIN"), choseEllipse ? "Exactly! Orbit B is more stretched. A circle has zero eccentricity." : "A circle has zero eccentricity. Look for the more stretched orbit, then try again.", choseEllipse ? "A: finish and return to the room     B: listen again" : "A: retry the question     B: listen again");
                break;
            case LessonStep.Complete:
                if (completionDismissed) tablet.Hide();
                else tablet.Show("ORBIT GUIDE  /  MODULE COMPLETE", "Nice work! You've completed the eccentricity module.", "A: dismiss");
                break;
        }
    }

    void OnDisable()
    {
        robotGuide?.playerGuide?.CancelViewTurn();
        robotGuide?.PauseWalk();
        robotGuide?.playerGuide?.StopFollowing();
        IsGuided = false;
        if (IsPresenting || (display != null && display.IsPaused)) CurrentStep = LessonStep.Introduction;
        else if (CurrentStep == LessonStep.Walking) CurrentStep = LessonStep.Ready;
        if (started) display.Stop();
        welcome?.ResumeLocomotion();
        if (tablet != null && (welcome == null || !welcome.IsShowing)) tablet.Hide();
        controls?.DisableInput();
    }
}
