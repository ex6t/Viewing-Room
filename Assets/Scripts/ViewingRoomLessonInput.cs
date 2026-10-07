using UnityEngine;
using UnityEngine.InputSystem;

// Same separation as the inherited HandS1MenuInput and S1TabletMenu.
public class ViewingRoomLessonInput : MonoBehaviour
{
    public ViewingRoomLesson lesson;
    public InputActionProperty continueInput; // A / Return
    public InputActionProperty selectInput; // X
    public InputActionProperty replayInput; // B / R
    public InputActionProperty modeInput; // Existing Y / M shortcut, not shown in prompts.
    public InputActionReference navigationInput; // Left stick; arrow keys in Editor.

    public bool IsConfigured => lesson != null && continueInput.action != null && selectInput.action != null &&
        replayInput.action != null && modeInput.action != null && navigationInput != null;

    bool previousContinue = true;
    bool previousSelect = true;
    bool previousReplay = true;
    bool previousMode = true;
    bool previousUp = true;
    bool previousDown = true;

    public void EnableInput()
    {
        continueInput.action?.Enable();
        selectInput.action?.Enable();
        replayInput.action?.Enable();
        modeInput.action?.Enable();
        ResetInput();
    }

    public void DisableInput()
    {
        continueInput.action?.Disable();
        selectInput.action?.Disable();
        replayInput.action?.Disable();
        modeInput.action?.Disable();
    }

    // Called by the lesson after its automatic transitions, avoiding Update-order surprises.
    public void ReadInput()
    {
        if (!isActiveAndEnabled) return;
        bool confirm = continueInput.action.IsPressed();
        bool select = selectInput.action.IsPressed();
        bool replay = replayInput.action.IsPressed();
        bool modes = modeInput.action.IsPressed();
        float vertical = navigationInput.action.ReadValue<Vector2>().y;
        bool up = vertical > 0.6f || (Keyboard.current != null && Keyboard.current.upArrowKey.isPressed);
        bool down = vertical < -0.6f || (Keyboard.current != null && Keyboard.current.downArrowKey.isPressed);

        if (modes && !previousMode && lesson.IsGuided && lesson.CurrentStep != ViewingRoomLesson.LessonStep.Complete) lesson.OpenModes();
        else if (replay && !previousReplay && lesson.IsPresenting) lesson.ReplayRecording();
        else if (confirm && !previousContinue) lesson.ContinueLesson();
        else if (select && !previousSelect) lesson.SelectAnswer();
        if (up && !previousUp) lesson.HighlightAnswer(-1);
        if (down && !previousDown) lesson.HighlightAnswer(1);

        previousContinue = confirm;
        previousSelect = select;
        previousReplay = replay;
        previousMode = modes;
        previousUp = up;
        previousDown = down;
    }

    // A held button cannot also advance the next prompt. Release it, then press again.
    public void ResetInput()
    {
        previousContinue = previousSelect = previousReplay = previousMode = previousUp = previousDown = true;
    }

    void OnDisable()
    {
        DisableInput();
    }

    void OnDestroy()
    {
        if (continueInput.reference == null) continueInput.action?.Dispose();
        if (selectInput.reference == null) selectInput.action?.Dispose();
        if (replayInput.reference == null) replayInput.action?.Dispose();
        if (modeInput.reference == null) modeInput.action?.Dispose();
    }
}
