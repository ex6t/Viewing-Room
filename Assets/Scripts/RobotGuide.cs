using UnityEngine;
using UnityEngine.AI;

// The robot walks to Editor-placed stops and faces the learner when standing.
public class RobotGuide : MonoBehaviour
{
    [Header("Robot References")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform playerCamera;

    public GuidedPlayer playerGuide;
    public RobotWelcomePrompt welcome;
    public float invitationDistance = 2.5f;

    public bool IsWalking { get; private set; }
    public bool HasArrived { get; private set; }

    bool walkPaused;
    bool wasNearPlayer;
    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public bool WalkTo(Transform destination)
    {
        if (agent == null || animator == null || destination == null || !agent.isOnNavMesh) return false;
        var path = new NavMeshPath();
        if (!agent.CalculatePath(destination.position, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("[RobotGuide] The lesson point needs a complete walkable path.", this);
            return false;
        }
        HasArrived = false;
        walkPaused = false;
        IsWalking = true;
        agent.isStopped = false;
        agent.SetPath(path);
        return true;
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh || animator == null) return;
        if (IsWalking && !walkPaused && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.08f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            IsWalking = false;
            HasArrived = true;
        }
        animator.SetBool("Walking", IsWalking && !walkPaused && agent.velocity.sqrMagnitude > 0.01f);
        if (!IsWalking && playerCamera != null)
        {
            Vector3 direction = Vector3.ProjectOnPlane(playerCamera.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 100f * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        CheckForReturningLearner();
    }

    // A dismissed invitation stays hidden until the learner leaves and returns.
    void CheckForReturningLearner()
    {
        if (welcome == null || playerCamera == null) return;
        float distance = wasNearPlayer ? invitationDistance + 0.4f : invitationDistance;
        bool near = Vector3.ProjectOnPlane(playerCamera.position - transform.position, Vector3.up).sqrMagnitude <= distance * distance;
        if (!near) welcome.HideTourInvitation();
        else if (!wasNearPlayer) welcome.ShowTourInvitation();
        wasNearPlayer = near;
    }

    public void ReturnToStart()
    {
        PauseWalk();
        playerGuide?.StopFollowing();
        playerGuide?.CancelViewTurn();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPosition);
        }
        else transform.position = startPosition;
        transform.rotation = startRotation;
        IsWalking = HasArrived = false;
    }

    public void PauseWalk()
    {
        walkPaused = true;
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        playerGuide?.PauseFollow();
        if (animator != null) animator.SetBool("Walking", false);
    }

    public void ResumeWalk()
    {
        walkPaused = false;
        if (IsWalking && agent != null && agent.isOnNavMesh) agent.isStopped = false;
        playerGuide?.ResumeFollow();
    }

    void OnDisable()
    {
        playerGuide?.CancelViewTurn();
        PauseWalk();
        playerGuide?.StopFollowing();
    }
}
