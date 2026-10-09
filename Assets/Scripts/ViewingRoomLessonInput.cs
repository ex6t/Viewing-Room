using UnityEngine;
using UnityEngine.InputSystem;

// Same separation as the inherited HandS1MenuInput and S1TabletMenu.
public class ViewingRoomLessonInput : MonoBehaviour
{
    public ViewingRoomLesson lesson;
    public InputActionProperty continueInput; // A / Return
    public InputActionProperty selectInput; // X: operate the current instrument.
    public InputActionProperty replayInput; // B: reset / previous.
    public InputActionProperty narrationInput; // Right stick click / R: optional narration.
    public InputActionProperty modeInput; // Y / M: pause and choose a mode.
    public InputActionReference navigationInput; // Left stick; arrow keys in Editor.

    public bool IsConfigured => lesson != null && continueInput.action != null && selectInput.action != null &&
        replayInput.action != null && narrationInput.action != null && modeInput.action != null && navigationInput != null;

    bool previousContinue = true;
    bool previousSelect = true;
    bool previousReplay = true;
    bool previousMode = true;
    bool previousNarration = true;
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
        bool narration = narrationInput.action.IsPressed();
        float horizontal = navigationInput.action.ReadValue<Vector2>().x;
        bool left = horizontal < -0.6f || (Keyboard.current != null && Keyboard.current.leftArrowKey.isPressed);
        bool right = horizontal > 0.6f || (Keyboard.current != null && Keyboard.current.rightArrowKey.isPressed);

        if (modes && !previousMode && lesson.CurrentStep != ViewingRoomLesson.LessonStep.Ready) lesson.OpenModes();
        else if (replay && !previousReplay) lesson.ResetOrGoBack();
        else if (narration && !previousNarration && lesson.IsPresenting) lesson.ReplayRecording();
        else if (confirm && !previousContinue) lesson.ContinueLesson();
        else if (select && !previousSelect) lesson.Interact();
        else if (left && !previousLeft) lesson.AdjustModel(-1);
        else if (right && !previousRight) lesson.AdjustModel(1);

        previousContinue = confirm;
        previousSelect = select;
        previousReplay = replay;
        previousMode = modes;
        previousNarration = narration;
        previousLeft = left;
        previousRight = right;
    }

    // A held button cannot also advance the next prompt. Release it, then press again.
    public void ResetInput()
    {
        previousContinue = previousSelect = previousReplay = previousMode = previousNarration = previousLeft = previousRight = true;
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
