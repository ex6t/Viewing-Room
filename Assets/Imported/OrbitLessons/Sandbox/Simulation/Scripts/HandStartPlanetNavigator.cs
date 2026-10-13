using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class HandStartPlanetNavigator : MonoBehaviour
{
    private static HandStartPlanetNavigator inputOwner;
    [Header("Task Checkmarks")]
    public GameObject planetTaskCheckmark;

    [SerializeField] private PlanetNavigator planetNavigator;
    [SerializeField] private TabletSummoner tabletSummoner;
    [SerializeField] private TabletMenu tabletMenu;
    [SerializeField] private XRRayInteractor leftRay;
    [SerializeField] private XRRayInteractor rightRay;

    private bool previousTabletToggle;
    private bool previousCloseTablet;
    private bool previousConfirm;
    private bool previousMoveUp;
    private bool previousMoveDown;
    private bool previousNextPlanet;
    private bool previousPreviousPlanet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInputOwner()
    {
        inputOwner = null;
    }

    void OnEnable()
    {
        if (tabletSummoner == null) return;
        // Older scenes attached this two-hand input handler to both gloves.
        if (inputOwner != null && inputOwner != this && inputOwner.tabletSummoner == tabletSummoner)
            enabled = false;
        else inputOwner = this;
    }

    void OnDisable()
    {
        if (inputOwner == this) inputOwner = null;
    }

    void Reset()
    {
        if (planetNavigator == null) planetNavigator = FindObjectOfType<PlanetNavigator>();
        if (tabletSummoner == null) tabletSummoner = FindObjectOfType<TabletSummoner>();
        if (tabletMenu == null) tabletMenu = FindObjectOfType<TabletMenu>();

        if (planetTaskCheckmark != null)
            planetTaskCheckmark.SetActive(false);
    }

    void Update()
    {
        if (tabletSummoner != null &&
            XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.PrimaryButton, KeyCode.T, ref previousTabletToggle))
        {
            tabletSummoner.OpenTablet();
        }

        if (XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.SecondaryButton, KeyCode.Escape, ref previousCloseTablet))
        {
            if (planetNavigator != null && planetNavigator.IsNavigationActive)
            {
                planetNavigator.ReturnToShip();
                if (tabletSummoner != null) tabletSummoner.CloseTablet();
            }
            else if (tabletSummoner != null && tabletSummoner.IsTabletOpen)
            {
                tabletSummoner.CloseTablet();
            }
        }

        if (tabletSummoner != null && tabletSummoner.IsTabletOpen && tabletMenu != null)
        {
            // A trigger used to select the task must not also skip the first planet.
            XRInputButtons.GetTriggerDown(XRNode.RightHand, KeyCode.RightArrow, ref previousNextPlanet);
            XRInputButtons.GetTriggerDown(XRNode.LeftHand, KeyCode.LeftArrow, ref previousPreviousPlanet);
            if (XRInputButtons.GetVerticalDown(XRNode.LeftHand, true, KeyCode.UpArrow, ref previousMoveUp))
            {
                tabletMenu.MoveUp();
            }

            if (XRInputButtons.GetVerticalDown(XRNode.LeftHand, false, KeyCode.DownArrow, ref previousMoveDown))
            {
                tabletMenu.MoveDown();
            }

            if (XRInputButtons.GetButtonDown(XRNode.LeftHand, XRMenuButton.PrimaryButton, KeyCode.Return, ref previousConfirm))
            {
                tabletMenu.ConfirmCurrent();
            }

            return;
        }

        if (planetNavigator == null)
            return;

        if (XRInputButtons.GetTriggerDown(XRNode.RightHand, KeyCode.RightArrow, ref previousNextPlanet) && !PointingAtButton(rightRay))
        {
            planetNavigator.nextPlanetFromHand();
        }

        if (XRInputButtons.GetTriggerDown(XRNode.LeftHand, KeyCode.LeftArrow, ref previousPreviousPlanet) && !PointingAtButton(leftRay))
        {
            planetNavigator.prevPlanetFromHand();
        }
    }

    private bool PointingAtButton(XRRayInteractor ray)
    {
        // UI clicks fire on release. Let that path own the press instead of also advancing on squeeze.
        return ray != null && ray.TryGetCurrentUIRaycastResult(out var hit) &&
            hit.gameObject != null && hit.gameObject.GetComponentInParent<Button>() != null;
    }
}
