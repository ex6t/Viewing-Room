using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CanvasActionScript : MonoBehaviour
{
    public GameObject Menu;
    private bool previousMenuButton = true;
    private TabletSummoner tablet;
    private PlanetNavigator navigator;

    void Start()
    {
        tablet = FindObjectOfType<TabletSummoner>();
        navigator = FindObjectOfType<PlanetNavigator>();
        if (Menu != null)
            Menu.SetActive(false);
    }

    public void ButtonUp()
    {
        Debug.Log("Button is up");
        if (Menu != null)
            Menu.SetActive(false);
    }

    public void ButtonDown()
    {
        if (BlockedByTask()) return;
        Debug.Log("Button is down");
        if (Menu != null)
            Menu.SetActive(true);
    }

    void Update()
    {
        if (XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.SecondaryButton, KeyCode.M, ref previousMenuButton))
        {
            if (BlockedByTask()) return;
            if (Menu != null)
                Menu.SetActive(!Menu.activeSelf);
        }
    }

    private bool BlockedByTask()
    {
        return (tablet != null && tablet.BlocksHandMenu) ||
            (navigator != null && navigator.IsNavigationActive);
    }
}
