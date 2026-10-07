using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reusable lower-view tablet, following the inherited CanvasFollower / S1TabletMenu pattern.
public class RobotTablet : MonoBehaviour
{
    [Header("Tablet References")]
    public Transform playerCamera;
    public TMP_Text heading;
    public TMP_Text message;
    public TMP_Text hint;
    public GameObject answers;
    public Image[] highlightBoxes;
    public TMP_Text[] answerLabels;

    [Header("View Placement")]
    public float distanceInFront = 1.35f;
    public float heightOffset = -0.43f;
    public float charactersPerSecond = 45f;

    public bool IsTyping => gameObject.activeSelf && message.maxVisibleCharacters < characterCount;
    int characterCount;
    float visibleCharacters;

    public void Show(string title, string text, string controls, bool showAnswers = false)
    {
        // Parenting once also follows the headset's final before-render tracking update.
        if (playerCamera != null && transform.parent != playerCamera) transform.SetParent(playerCamera, true);
        gameObject.SetActive(true);
        heading.text = title;
        hint.text = controls;
        answers.SetActive(showAnswers);
        message.text = text;
        message.maxVisibleCharacters = int.MaxValue;
        message.ForceMeshUpdate();
        characterCount = message.textInfo.characterCount;
        visibleCharacters = 0f;
        message.maxVisibleCharacters = 0;
        SelectAnswer(0);
        PlaceTablet();
    }

    public void SelectAnswer(int index)
    {
        for (int i = 0; i < highlightBoxes.Length; i++)
            highlightBoxes[i].color = i == index ? new Color(0.12f, 0.48f, 0.54f, 1f) : new Color(0.055f, 0.10f, 0.15f, 1f);
    }

    public void SetOptions(string first, string second)
    {
        answerLabels[0].text = first;
        answerLabels[1].text = second;
    }

    // A first press finishes the sentence; a fresh press then continues the lesson.
    public void RevealText()
    {
        visibleCharacters = characterCount;
        message.maxVisibleCharacters = characterCount;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        PlaceTablet();
        if (!IsTyping) return;
        // A long Editor startup frame should not skip the entire typing effect.
        visibleCharacters += Mathf.Max(1f, charactersPerSecond) * Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        message.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(visibleCharacters));
    }

    void PlaceTablet()
    {
        if (playerCamera == null) return;
        // Follow the full headset pose, including looking up/down, so every prompt stays in view.
        transform.SetPositionAndRotation(playerCamera.position + playerCamera.forward * distanceInFront + playerCamera.up * heightOffset,
            playerCamera.rotation);
    }
}
