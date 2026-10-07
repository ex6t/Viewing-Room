using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

// Shared by the welcome and lessons. Tracking remains active while travel is paused.
public class PlayerLocomotionPause : MonoBehaviour
{
    public LocomotionProvider[] locomotionProviders;
    public Rigidbody playerBody;
    public Behaviour[] teleportInteractors;
    public InputActionReference rightTurnInput;
    public InputActionReference[] teleportSelectInputs;
    public XROrigin playerOrigin;
    public bool IsPaused => previousProviderStates != null;

    bool[] previousProviderStates;
    bool[] previousInteractorStates;
    bool previousKinematic;
    Vector3 previousVelocity;
    Vector3 previousAngularVelocity;
    bool waitingForRelease;
    bool allowGuidedSnapTurn;

    public void Pause(bool allowSnapTurn = false)
    {
        waitingForRelease = false;
        allowGuidedSnapTurn = allowSnapTurn;
        if (IsPaused)
        {
            // A walking tour may turn; welcome and reading prompts must pause turning again.
            foreach (var provider in locomotionProviders)
                if (provider is SnapTurnProvider) provider.enabled = false;
            return;
        }
        previousProviderStates = new bool[locomotionProviders.Length];
        for (int i = 0; i < locomotionProviders.Length; i++)
        {
            if (locomotionProviders[i] == null) continue;
            previousProviderStates[i] = locomotionProviders[i].enabled;
            locomotionProviders[i].enabled = false;
        }

        previousInteractorStates = new bool[teleportInteractors == null ? 0 : teleportInteractors.Length];
        for (int i = 0; i < previousInteractorStates.Length; i++)
        {
            if (teleportInteractors[i] == null) continue;
            previousInteractorStates[i] = teleportInteractors[i].enabled;
            teleportInteractors[i].enabled = false;
        }

        // Keep physics from nudging the rig while a dialogue is open.
        if (playerBody != null)
        {
            previousKinematic = playerBody.isKinematic;
            if (!previousKinematic)
            {
                previousVelocity = playerBody.linearVelocity;
                previousAngularVelocity = playerBody.angularVelocity;
                playerBody.isKinematic = true;
            }
        }

    }

    public void Resume()
    {
        if (!IsPaused) return;
        waitingForRelease = true;
        if (LocomotionInputReleased()) Restore();
    }

    bool LocomotionInputReleased()
    {
        if (rightTurnInput != null && rightTurnInput.action.ReadValue<Vector2>().sqrMagnitude > 0.01f) return false;
        if (teleportSelectInputs != null)
        {
            foreach (var input in teleportSelectInputs)
            {
                if (input != null && input.action.IsPressed()) return false;
            }
        }
        return true;
    }

    // The prompt calls this before reading its menu buttons.
    public void UpdateLock()
    {
        if (waitingForRelease && LocomotionInputReleased()) Restore();
        // Require a centered stick before enabling guided snap turning.
        if (!allowGuidedSnapTurn || !LocomotionInputReleased()) return;
        for (int i = 0; i < locomotionProviders.Length; i++)
            if (locomotionProviders[i] is SnapTurnProvider)
                locomotionProviders[i].enabled = previousProviderStates[i];
    }

    public void Restore()
    {
        if (!IsPaused) return;
        waitingForRelease = false;
        allowGuidedSnapTurn = false;
        for (int i = 0; i < locomotionProviders.Length; i++)
        {
            if (locomotionProviders[i] != null)
                locomotionProviders[i].enabled = previousProviderStates[i];
        }

        for (int i = 0; i < previousInteractorStates.Length; i++)
        {
            if (teleportInteractors[i] != null) teleportInteractors[i].enabled = previousInteractorStates[i];
        }
        previousProviderStates = null;
        previousInteractorStates = null;

        if (playerBody != null)
        {
            playerBody.isKinematic = previousKinematic;
            if (!previousKinematic)
            {
                playerBody.linearVelocity = previousVelocity;
                playerBody.angularVelocity = previousAngularVelocity;
            }
        }

    }

    void OnDisable()
    {
        Restore();
    }
}
