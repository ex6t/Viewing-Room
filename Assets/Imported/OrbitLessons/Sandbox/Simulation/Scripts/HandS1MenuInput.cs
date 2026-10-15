using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class HandS1MenuInput : MonoBehaviour
{
    [Header("S1 Tablet Menu")]
    public S1TabletMenu s1TabletMenu;

    private bool previousMoveUp = true;
    private bool previousMoveDown = true;
    private bool previousConfirm = true;
    private bool previousContinue = true;

    private void Reset()
    {
        if (s1TabletMenu == null) s1TabletMenu = FindObjectOfType<S1TabletMenu>();
    }

    private void Awake()
    {
        if (s1TabletMenu == null)
            s1TabletMenu = FindObjectOfType<S1TabletMenu>();
    }

    private void Update()
    {
        if (s1TabletMenu == null)
            return;

        bool continuePressed = XRInputButtons.GetButtonDown(XRNode.RightHand, XRMenuButton.PrimaryButton, KeyCode.Return, ref previousContinue);

        if (XRInputButtons.GetVerticalDown(XRNode.LeftHand, true, KeyCode.UpArrow, ref previousMoveUp))
        {
            s1TabletMenu.MoveUp();
        }

        if (XRInputButtons.GetVerticalDown(XRNode.LeftHand, false, KeyCode.DownArrow, ref previousMoveDown))
        {
            s1TabletMenu.MoveDown();
        }

        if (XRInputButtons.GetButtonDown(XRNode.LeftHand, XRMenuButton.PrimaryButton, KeyCode.X, ref previousConfirm))
        {
            s1TabletMenu.ConfirmCurrent();
        }

        if (s1TabletMenu.finishedGame && continuePressed)
        {
            SceneManager.LoadScene("S2View");
        }
    }
}
