using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class S1TabletMenu : MonoBehaviour
{

    [Tooltip("Options in order (top to bottom).")]
    public List<Selectable> options = new List<Selectable>();

    [Tooltip("Highlight boxes (RedBox1, RedBox2, ...) in the SAME order as options.")]
    public List<GameObject> highlightBoxes = new List<GameObject>();

    [Header("Canvases")]
    [Tooltip("Game Canvas")]
    public GameObject gameCanvas; // Canvas That Has The Question And Options

    [Tooltip("Correct Canvas")]
    public GameObject correctCanvas; // Canvas Shown On Correct Answer

    [Tooltip("Incorrect Canvas")]
    public GameObject incorrectCanvas; // Canvas Shown On Incorrect Answer

    [Tooltip("Current and Correct Indexes")]
    private int currentIndex = 0; // Starts At The First Option
    public int correctIndex = 2;

    public bool finishedGame = false; // To Prevent Multiple Confirmations
    private readonly List<UnityAction> answerActions = new List<UnityAction>();

    private void Awake()
    {
        finishedGame = false;
        currentIndex = 0;
        if (gameCanvas != null) gameCanvas.SetActive(true);
        if (correctCanvas != null) correctCanvas.SetActive(false);
        if (incorrectCanvas != null) incorrectCanvas.SetActive(false);
    }

    private void Start()
    {
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            UnityAction action = () => SelectAnswer(index);
            answerActions.Add(action);
            if (options[i] == null) continue;
            options[i].navigation = new Navigation { mode = Navigation.Mode.None };
            if (options[i] is Button button) button.onClick.AddListener(action);
        }
        // Makes sure index is valid
        if (options.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Count - 1);
        }

        // Makes sure result canvases start OFF
        if (correctCanvas != null) correctCanvas.SetActive(false);
        if (incorrectCanvas != null) incorrectCanvas.SetActive(false);

        // Turn on only the correct highlight
        UpdateHighlight();

        // Select the first option for UI focus
        HoverCurrent();
    }

    // FUNCTION FOR MOVING DOWN THE MENU
    public void MoveDown()
    {
        if (finishedGame || options.Count == 0) return;

        currentIndex++;
        if (currentIndex >= options.Count)
            currentIndex = options.Count - 1; // clamp at bottom

        HoverCurrent(); // Hovers The Current Option For UI Focus
        UpdateHighlight(); // Update highlights
    }

    // FUNCTION FOR MOVING UP THE MENU
    public void MoveUp()
    {
        if (finishedGame || options.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = 0;

        HoverCurrent(); // Hovers The Current Option For UI Focus
        UpdateHighlight(); // Update highlights
    }

    // FUNCTION FOR GETTING THE CURRENT OPTION
    public Selectable GetCurrentOption()
    {
        if (currentIndex >= 0 && currentIndex < options.Count)
            return options[currentIndex];
        return null;
    }

    // FUNCTION FOR CONFIRMING THE CURRENT SELECTION
    public void ConfirmCurrent()
    {
        if (finishedGame) return;
        var option = GetCurrentOption();
        if (option == null || !option.IsActive() || !option.IsInteractable())
        {
            Debug.LogWarning("[S1TabletMenu] ConfirmCurrent called but no current option.");
            return;
        }

        if (option is Button button) button.onClick.Invoke();
        else SelectAnswer(currentIndex);
    }

    private void SelectAnswer(int index)
    {
        if (finishedGame || index < 0 || index >= options.Count) return;
        currentIndex = index;
        finishedGame = true;

        //  Checks If The Selected Option Is Correct
        bool isCorrect = (currentIndex == correctIndex);

        if (isCorrect) // Correct Answer
        {
            Debug.Log("[S1TabletMenu] CORRECT ANSWER selected!");

            if (gameCanvas != null)
                gameCanvas.SetActive(false); // Turns Off The Game Canvas

            if (correctCanvas != null)
            {
                correctCanvas.SetActive(true); // Turns On The Correct Canvas
                finishedGame = true; // Marks The Game As Finished
            }

            if (incorrectCanvas != null)
                incorrectCanvas.SetActive(false); // Turns Off The Incorrect Canvas In Case It Was On
        }
        else // Incorrect Answer
        {
            Debug.Log("[S1TabletMenu] INCORRECT ANSWER selected!");

            if (gameCanvas != null)
                gameCanvas.SetActive(false); // Turns Off The Game Canvas

            if (correctCanvas != null)
                correctCanvas.SetActive(false); // Turns Off The Correct Canvas In Case In Case It Was On

            if (incorrectCanvas != null)
            {
                incorrectCanvas.SetActive(true); // Turns On The Incorrect Canvas
                finishedGame = true; // Marks The Game As Finished
            }
        }

    }

    // FUNCTION FOR HOVERING THE CURRENT OPTION FOR UI FOCUS
    private void HoverCurrent()
    {
        var option = GetCurrentOption();
        if (option != null)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(option.gameObject);
            Debug.Log("[S1TabletMenu] Hovering: " + option.name);
        }
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < highlightBoxes.Count; i++)
        {
            if (highlightBoxes[i] == null) continue;

            bool shouldBeActive = (i == currentIndex);
            highlightBoxes[i].SetActive(shouldBeActive);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < answerActions.Count; i++)
            if (options[i] is Button button) button.onClick.RemoveListener(answerActions[i]);
    }
}
