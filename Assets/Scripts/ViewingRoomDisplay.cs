using System.Collections;
using UnityEngine;

// Uses the wall position saved in the Editor, then restores it after reading.
public class ViewingRoomDisplay : MonoBehaviour
{
    public Transform playerCamera;
    public AudioSource recordingPlayer;
    public AudioClip recording;
    public float presentationDistance = 3f;
    public float presentationScale = 0.0024f;
    public float presentationHeight = 0.6f;

    public bool IsOpen { get; private set; }
    public bool IsMoving => transition != null;
    public bool IsPaused { get; private set; }

    Vector3 wallPosition;
    Quaternion wallRotation;
    Vector3 wallScale;
    Coroutine transition;
    bool pausedFirstListen;
    bool narrationOnOpen;

    void Awake()
    {
        wallPosition = transform.position;
        wallRotation = transform.rotation;
        wallScale = transform.localScale;
    }

    public void Open(bool playNarration = true)
    {
        if (transition != null) StopCoroutine(transition);
        IsOpen = true;
        narrationOnOpen = playNarration;
        transition = StartCoroutine(BringCloser(playNarration));
    }

    IEnumerator BringCloser(bool playNarration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        Vector3 forward = ReadingDirection();
        Vector3 targetPosition = playerCamera.position + forward * presentationDistance + Vector3.up * presentationHeight;
        Quaternion targetRotation = Quaternion.LookRotation(forward);
        for (float elapsed = 0f; elapsed < 0.45f; elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
            transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, progress), Quaternion.Slerp(startRotation, targetRotation, progress));
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * presentationScale, progress);
            yield return null;
        }
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        transform.localScale = Vector3.one * presentationScale;
        transition = null;
        if (playNarration) PlayRecording();
    }

    Vector3 ReadingDirection()
    {
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.forward, Vector3.up).normalized;
        return forward.sqrMagnitude < 0.01f ? Vector3.forward : forward;
    }

    public void PlayRecording()
    {
        if (!IsOpen) return;
        recordingPlayer.Stop();
        recordingPlayer.clip = recording;
        recordingPlayer.Play();
    }

    public void Close()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        transform.SetPositionAndRotation(wallPosition, wallRotation);
        transform.localScale = wallScale;
        IsOpen = false;
    }

    public void Pause()
    {
        IsPaused = IsOpen;
        pausedFirstListen = IsMoving && narrationOnOpen;
        recordingPlayer.Pause();
        Close();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        IsOpen = true;
        Vector3 forward = ReadingDirection();
        transform.SetPositionAndRotation(playerCamera.position + forward * presentationDistance + Vector3.up * presentationHeight, Quaternion.LookRotation(forward));
        transform.localScale = Vector3.one * presentationScale;
        if (pausedFirstListen) PlayRecording();
        else recordingPlayer.UnPause();
        pausedFirstListen = false;
    }

    public void Stop()
    {
        recordingPlayer.Stop();
        Close();
        IsPaused = pausedFirstListen = false;
    }
}
