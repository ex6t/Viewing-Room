using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HubStationMover : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Hub Station Transform that will be moved. If null, the script will try to find a GameObject by name at Start.")]

    public Transform hubStation;
    [Tooltip("If hubStation is not set, try to find this name in scene.")]
    public string hubStationName = "Hub Station";

    [Header("Targets")]
    [Tooltip("Target Transforms to move the hub station to (create empty GameObjects as markers).")]
    public List<Transform> targets = new List<Transform>();

    [Header("Movement")]
    [Tooltip("If true, the hub station will smoothly move/rotate to the target over smoothDuration seconds.")]
    public bool smoothMove = true;
    [Tooltip("Duration (seconds) for smooth movement.")]
    public float smoothDuration = 1.0f;

    private int currentIndex = -1;
    private Coroutine movingCoroutine = null;

    void Start()
    {
        if (hubStation == null && !string.IsNullOrEmpty(hubStationName))
        {
            var go = GameObject.Find(hubStationName);
            if (go != null) hubStation = go.transform;
            else Debug.LogWarning($"[HubStationMover] hubStation not assigned and GameObject '{hubStationName}' not found in scene.");
        }

        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("[HubStationMover] No targets assigned. Create empty GameObjects for each desired hub position and add them to the targets list.");
        }
    }

    // FUNCTION TO MOVE TO NEXT TARGET
    public void MoveToNext()
    {
        if (targets == null || targets.Count == 0) return;
        int next = (currentIndex + 1) % targets.Count;
        MoveToIndex(next);
    }

    // FUNCTION TO MOVE TO PREVIOUS TARGET
    public void MoveToPrevious()
    {
        if (targets == null || targets.Count == 0) return;
        int prev = (currentIndex - 1 + targets.Count) % targets.Count;
        MoveToIndex(prev);
    }

    // FUNCTION TO MOVE TO SPECIFIC TARGET INDEX
    public void MoveToIndex(int index)
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("[HubStationMover] MoveToIndex called but no targets are configured.");
            return;
        }
        if (index < 0 || index >= targets.Count)
        {
            Debug.LogWarning($"[HubStationMover] MoveToIndex: index {index} out of range (0..{targets.Count-1}).");
            return;
        }
        if (hubStation == null)
        {
            Debug.LogWarning("[HubStationMover] MoveToIndex called but hubStation is null.");
            return;
        }

        currentIndex = index;
        Transform t = targets[index];
        if (t == null)
        {
            Debug.LogWarning($"[HubStationMover] target at index {index} is null.");
            return;
        }

        // Stops Any Movement In Progress
        if (movingCoroutine != null) StopCoroutine(movingCoroutine);

        if (!smoothMove || smoothDuration <= 0f)
        {
            // Teleport Instantly
            hubStation.position = t.position;
            hubStation.rotation = t.rotation;
        }
        else
        {
            movingCoroutine = StartCoroutine(SmoothMove(hubStation, t.position, t.rotation, smoothDuration));
        }
    }

    // FUNCTION TO TELEPORT TO AN ARBITRARY WORLD POSITION
    public void TeleportToPosition(Vector3 worldPosition)
    {
        if (hubStation == null) return;
        if (movingCoroutine != null) StopCoroutine(movingCoroutine);
        hubStation.position = worldPosition;
    }

    private IEnumerator SmoothMove(Transform subject, Vector3 goalPos, Quaternion goalRot, float duration)
    {
        if (subject == null) yield break;
        float elapsed = 0f;
        Vector3 startPos = subject.position;
        Quaternion startRot = subject.rotation;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            subject.position = Vector3.Lerp(startPos, goalPos, t);
            subject.rotation = Quaternion.Slerp(startRot, goalRot, t);
            yield return null;
        }
        subject.position = goalPos;
        subject.rotation = goalRot;
        movingCoroutine = null;
    }

    // IF YOU WANT TO MOVE TO A TARGET BY NAME
    public void MoveToTargetByName(string name)
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targets[i].name == name)
            {
                MoveToIndex(i);
                return;
            }
        }
        Debug.LogWarning($"[HubStationMover] MoveToTargetByName: no target named '{name}' found.");
    }

    // SMALL EDITOR CONTEXT MENU HELPERS
    [ContextMenu("MoveToNext (Editor)")]
    void ContextMoveNext() { MoveToNext(); }
    [ContextMenu("MoveToPrevious (Editor)")]
    void ContextMovePrev() { MoveToPrevious(); }
}
