using UnityEngine;
using UnityEngine.InputSystem;

// Same separation as the inherited HandS1MenuInput and S1TabletMenu.
public class ViewingRoomLessonInput : MonoBehaviour
{
    public ViewingRoomLesson lesson;
    public ObservatoryTour observatoryTour;
    public InputActionProperty continueInput; // A / Return
    public InputActionProperty selectInput; // X: pause / play.
    public InputActionProperty replayInput; // B: reset time / go back.
    public InputActionProperty narrationInput; // Retained binding for the later Kepler recording.
    public InputActionProperty modeInput; // Y / M: pause and choose a mode.
    public InputActionReference navigationInput; // Left stick: time; left/right arrows in Editor.

    public bool IsConfigured => (lesson != null || observatoryTour != null) && continueInput.action != null && selectInput.action != null &&
        replayInput.action != null && narrationInput.action != null && modeInput.action != null && navigationInput != null;

    bool previousContinue = true;
    bool previousSelect = true;
    bool previousReplay = true;
    bool previousMode = true;
    bool previousLeft = true;
    bool previousRight = true;

    public void EnableInput()
    {
        continueInput.action?.Enable();
        selectInput.action?.Enable();
        replayInput.action?.Enable();
        narrationInput.action?.Enable();
        modeInput.action?.Enable();
        ResetInput();
    }

    public void DisableInput()
    {
        continueInput.action?.Disable();
        selectInput.action?.Disable();
        replayInput.action?.Disable();
        narrationInput.action?.Disable();
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
        float horizontal = navigationInput.action.ReadValue<Vector2>().x;
        bool left = horizontal < -0.6f || (Keyboard.current != null && Keyboard.current.leftArrowKey.isPressed);
        bool right = horizontal > 0.6f || (Keyboard.current != null && Keyboard.current.rightArrowKey.isPressed);

        if (observatoryTour != null)
        {
            if (modes && !previousMode) observatoryTour.OpenModes();
            else if (replay && !previousReplay) observatoryTour.ResetOrGoBack();
            else if (confirm && !previousContinue) observatoryTour.ContinueLesson();
            else if (select && !previousSelect) observatoryTour.Interact();
        }
        else if (modes && !previousMode && lesson.CurrentStep != ViewingRoomLesson.LessonStep.Ready) lesson.OpenModes();
        else if (replay && !previousReplay) lesson.ResetOrGoBack();
        else if (confirm && !previousContinue) lesson.ContinueLesson();
        else if (select && !previousSelect) lesson.Interact();
        else if (left && !previousLeft) lesson.AdjustModel(-1);
        else if (right && !previousRight) lesson.AdjustModel(1);

        previousContinue = confirm;
        previousSelect = select;
        previousReplay = replay;
        previousMode = modes;
        previousLeft = left;
        previousRight = right;
    }

    public float ReadTimeDirection()
    {
        if (!isActiveAndEnabled || navigationInput == null) return 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) return -1f;
            if (Keyboard.current.rightArrowKey.isPressed) return 1f;
        }
        float horizontal = navigationInput.action.ReadValue<Vector2>().x;
        return Mathf.Abs(horizontal) > 0.25f ? horizontal : 0f;
    }

    // A held button cannot also advance the next prompt. Release it, then press again.
    public void ResetInput()
    {
        previousContinue = previousSelect = previousReplay = previousMode = previousLeft = previousRight = true;
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
        if (narrationInput.reference == null) narrationInput.action?.Dispose();
        if (modeInput.reference == null) modeInput.action?.Dispose();
    }
}
