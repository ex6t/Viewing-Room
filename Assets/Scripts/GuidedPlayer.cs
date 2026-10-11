using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

// Moves the XR Origin alongside the robot, without moving the tracked camera locally.
public class GuidedPlayer : MonoBehaviour
{
    public RobotGuide robotGuide;
    public Transform playerCamera;
    public NavMeshAgent playerFollower;
    public XROrigin playerOrigin;
    public CharacterController playerController;
    public Image viewFade;
    public bool IsFacingDisplay { get; private set; }
    public bool IsFollowing { get; private set; }
    public bool PlayerHasArrived => !IsFollowing || (!playerFollower.pathPending &&
        Vector3.ProjectOnPlane(playerCamera.position - robotGuide.transform.position, Vector3.up).magnitude <= playerFollower.stoppingDistance + 0.25f);

    bool followPaused;
    Vector3 previousFollowPosition;
    float nextPathTime;
    Coroutine viewTurn;

    public bool StartFollowing()
    {
        if (IsFollowing) return true;
        if (robotGuide == null || robotGuide.agent == null || playerFollower == null || playerOrigin == null || playerCamera == null) return false;
        Vector3 ground = new Vector3(playerCamera.position.x, transform.position.y - robotGuide.agent.baseOffset, playerCamera.position.z);
        if (!NavMesh.SamplePosition(ground, out var hit, 1f, robotGuide.agent.areaMask)) return false;
        playerFollower.transform.position = hit.position;
        playerFollower.enabled = true;
        if (!playerFollower.isOnNavMesh) { playerFollower.enabled = false; return false; }
        followPaused = false;
        playerFollower.isStopped = false;
        previousFollowPosition = playerFollower.transform.position;
        IsFollowing = true;
        nextPathTime = 0f;
        return true;
    }

    public void StopFollowing()
    {
        IsFollowing = false;
        if (playerFollower != null) playerFollower.enabled = false;
    }

    void Update()
    {
        if (!IsFollowing || followPaused || !robotGuide.isActiveAndEnabled || !robotGuide.agent.isOnNavMesh || Time.time < nextPathTime) return;
        playerFollower.SetDestination(robotGuide.agent.nextPosition - Vector3.up * robotGuide.agent.baseOffset);
        nextPathTime = Time.time + 0.2f;
    }

    void LateUpdate()
    {
        if (!IsFollowing || followPaused) return;
        Vector3 position = playerFollower.transform.position;
        Vector3 movement = Vector3.ProjectOnPlane(position - previousFollowPosition, Vector3.up);
        // Accumulate small frame steps so the controller's minimum distance cannot discard travel.
        if (playerController != null && playerController.enabled &&
            movement.sqrMagnitude < playerController.minMoveDistance * playerController.minMoveDistance) return;
        // Translate the origin, never the tracked camera. Head movement and viewing direction remain the user's.
        if (playerController != null && playerController.enabled) playerController.Move(movement);
        else playerOrigin.transform.position += movement;
        previousFollowPosition = position;
    }

    public void PauseFollow()
    {
        followPaused = true;
        if (IsFollowing && playerFollower.isOnNavMesh)
        {
            playerFollower.isStopped = true;
            playerFollower.velocity = Vector3.zero;
        }
    }

    public void ResumeFollow()
    {
        followPaused = false;
        if (IsFollowing && playerFollower.isOnNavMesh) playerFollower.isStopped = false;
    }

    // A short fade makes a single arrival turn clear in both headset and desktop preview.
    public void FaceDisplay(Transform display)
    {
        if (display == null || playerOrigin == null || viewFade == null) return;
        CancelViewTurn();
        IsFacingDisplay = true;
        viewTurn = StartCoroutine(TurnToDisplay(display));
    }

    IEnumerator TurnToDisplay(Transform display)
    {
        viewFade.gameObject.SetActive(true);
        for (float elapsed = 0f; elapsed < 0.15f; elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
        {
            viewFade.color = new Color(0f, 0f, 0f, elapsed / 0.15f);
            yield return null;
        }
        viewFade.color = Color.black;
        yield return null;
        Vector3 direction = Vector3.ProjectOnPlane(display.position - playerCamera.position, Vector3.up);
        if (direction.sqrMagnitude > 0.01f) playerOrigin.MatchOriginUpCameraForward(Vector3.up, direction.normalized);
        for (float elapsed = 0f; elapsed < 0.15f; elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
        {
            viewFade.color = new Color(0f, 0f, 0f, 1f - elapsed / 0.15f);
            yield return null;
        }
        viewFade.gameObject.SetActive(false);
        IsFacingDisplay = false;
        viewTurn = null;
    }

    public void CancelViewTurn()
    {
        if (viewTurn != null) StopCoroutine(viewTurn);
        viewTurn = null;
        IsFacingDisplay = false;
        if (viewFade != null) viewFade.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        CancelViewTurn();
        PauseFollow();
        StopFollowing();
    }
}
