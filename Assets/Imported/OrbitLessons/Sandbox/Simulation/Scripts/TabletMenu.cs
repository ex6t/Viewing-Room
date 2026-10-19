using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TabletMenu : MonoBehaviour
{
    [Header("Buttons")]
    public List<Selectable> options = new List<Selectable>();

    [Header("Highlight Boxes")]
    public List<GameObject> highlightBoxes = new List<GameObject>();

    [Header("Checkmarks")]
    public GameObject planetNavCheckmark;
    public GameObject redSunCheckmark;
    public GameObject seasonsCheckmark;

    private int currentIndex = 0;

    private void Start()
    {
        RefreshProgress();
    }

    public void RefreshProgress()
    {
        // Selection is owned by the task menu, not automatic EventSystem stick navigation.
        foreach (Selectable option in options)
            if (option != null) option.navigation = new Navigation { mode = Navigation.Mode.None };
        if (planetNavCheckmark != null)
        {
            planetNavCheckmark.SetActive(PlanetNavProgress.planetNavCompleted);
        }

        if (redSunCheckmark != null)
        {
            redSunCheckmark.SetActive(RedSunProgress.redSunCompleted);
        }

        if (seasonsCheckmark != null)
        {
            seasonsCheckmark.SetActive(SeasonsProgress.seasonsCompleted);
        }

        // Ensure only one highlight starts active
        UpdateHighlight();

        SelectCurrent();
    }

    public void MoveDown()
    {
        if (options.Count == 0) return;

        currentIndex++;
        if (currentIndex >= options.Count)
            currentIndex = options.Count - 1; // clamp

        SelectCurrent();
        UpdateHighlight();
    }

    public void MoveUp()
    {
        if (options.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = 0; // clamp

        SelectCurrent();
        UpdateHighlight();
    }

    public Selectable GetCurrentOption()
    {
        if (currentIndex >= 0 && currentIndex < options.Count)
            return options[currentIndex];
        return null;
    }

    public void ConfirmCurrent()
    {
        var option = GetCurrentOption();
        if (option == null || !option.IsActive() || !option.IsInteractable())
        {
            Debug.LogWarning("[TabletMenu] ConfirmCurrent called but no current option.");
            return;
        }

        if (option.TryGetComponent<Button>(out Button button))
        {
            button.onClick.Invoke();
            return;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(option.gameObject);
    }

    private void SelectCurrent()
    {
        var option = GetCurrentOption();
        if (option != null)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(option.gameObject);
        }
    }

    // Turns RedBoxes on/off based on the currentIndex
    private void UpdateHighlight()
    {
        for (int i = 0; i < highlightBoxes.Count; i++)
        {
            if (highlightBoxes[i] == null) continue;

            bool shouldBeActive = (i == currentIndex);
            highlightBoxes[i].SetActive(shouldBeActive);
        }
    }
}
