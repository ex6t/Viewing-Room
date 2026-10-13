using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

public enum XRMenuButton
{
    PrimaryButton,
    SecondaryButton,
    TriggerButton
}

public static class XRInputButtons
{
    private const float AxisThreshold = 0.65f;
    private const float TriggerThreshold = 0.65f;
    private const float TriggerReleaseThreshold = 0.25f;

    public static bool GetTriggerDown(XRNode node, KeyCode fallbackKey, ref bool previousState)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool button;
        float value;
        if (device.isValid)
        {
            button = TryGetBool(device, CommonUsages.triggerButton);
            device.TryGetFeatureValue(CommonUsages.trigger, out value);
        }
        else
        {
            var trigger = GetControllerControl<AxisControl>(node, "Trigger");
            var pressed = GetControllerControl<ButtonControl>(node, "TriggerButton");
            value = trigger != null ? trigger.ReadValue() : 0f;
            button = pressed != null && pressed.isPressed;
        }

        bool keyboard = GetKeyboardKey(fallbackKey);
        // A partly released trigger must not rearm when its press threshold jitters.
        if (previousState)
        {
            if (!button && !keyboard && value <= TriggerReleaseThreshold) previousState = false;
            return false;
        }
        previousState = button || keyboard || value >= TriggerThreshold;
        return previousState;
    }

    public static bool IsHeadPoseReady()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (device.isValid)
            return TryGetBool(device, CommonUsages.isTracked);

        foreach (var input in InputSystem.devices)
            if (input is UnityEngine.InputSystem.XR.XRHMD head && head.enabled)
                return head.isTracked.isPressed && (head.trackingState.ReadValue() & 3) == 3;

        // Keep non-XR Editor previews usable; a player waits for its actual headset pose.
        return Application.isEditor;
    }

    public static bool GetButtonDown(XRNode node, XRMenuButton button, KeyCode fallbackKey, ref bool previousState)
    {
        bool pressed = GetButton(node, button) || GetKeyboardKey(fallbackKey);
        bool pressedThisFrame = pressed && !previousState;
        previousState = pressed;
        return pressedThisFrame;
    }

    public static bool GetVerticalDown(XRNode node, bool up, KeyCode fallbackKey, ref bool previousState)
    {
        bool pressed = GetKeyboardKey(fallbackKey) || GetPrimary2DAxisVertical(node, up);
        bool pressedThisFrame = pressed && !previousState;
        previousState = pressed;
        return pressedThisFrame;
    }

    private static bool GetButton(XRNode node, XRMenuButton button)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            string usage = button == XRMenuButton.PrimaryButton ? "PrimaryButton" :
                button == XRMenuButton.SecondaryButton ? "SecondaryButton" : "TriggerButton";
            var control = GetControllerControl<ButtonControl>(node, usage);
            return control != null && control.isPressed;
        }

        switch (button)
        {
            case XRMenuButton.PrimaryButton:
                return TryGetBool(device, CommonUsages.primaryButton);
            case XRMenuButton.SecondaryButton:
                return TryGetBool(device, CommonUsages.secondaryButton);
            case XRMenuButton.TriggerButton:
                return TryGetBool(device, CommonUsages.triggerButton) || TryGetAxis(device, CommonUsages.trigger, TriggerThreshold);
            default:
                return false;
        }
    }

    public static bool GetKeyboardKey(KeyCode keyCode, bool pressedThisFrame = false)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        Key key;
        switch (keyCode)
        {
            case KeyCode.Return: key = Key.Enter; break;
            case KeyCode.UpArrow: key = Key.UpArrow; break;
            case KeyCode.DownArrow: key = Key.DownArrow; break;
            case KeyCode.LeftArrow: key = Key.LeftArrow; break;
            case KeyCode.RightArrow: key = Key.RightArrow; break;
            default:
                if (!System.Enum.TryParse(keyCode.ToString(), out key) || key == Key.None)
                    return false;
                break;
        }

        return pressedThisFrame ? keyboard[key].wasPressedThisFrame : keyboard[key].isPressed;
    }

    private static bool GetPrimary2DAxisVertical(XRNode node, bool up)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        Vector2 axis;
        if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis))
        {
            var control = GetControllerControl<Vector2Control>(node, "Primary2DAxis");
            if (control == null) return false;
            axis = control.ReadValue();
        }

        return up ? axis.y >= AxisThreshold : axis.y <= -AxisThreshold;
    }

    private static bool TryGetBool(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.TryGetFeatureValue(usage, out bool value) && value;
    }

    private static T GetControllerControl<T>(XRNode node, string usage) where T : InputControl
    {
        var controller = node == XRNode.LeftHand ? UnityEngine.InputSystem.XR.XRController.leftHand :
            node == XRNode.RightHand ? UnityEngine.InputSystem.XR.XRController.rightHand : null;
        return controller != null ? controller.TryGetChildControl<T>("{" + usage + "}") : null;
    }

    private static bool TryGetAxis(InputDevice device, InputFeatureUsage<float> usage, float threshold)
    {
        return device.TryGetFeatureValue(usage, out float value) && value >= threshold;
    }
}
