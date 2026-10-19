using UnityEngine;
using Unity.XR.CoreUtils;

public class MenuController : MonoBehaviour
{
    public GameObject player;
    public GameObject hub;
    private float toggleCooldown = 0f;
    public bool UIToggled = false;
    public GameObject trail;

    public GameObject earthLatLong;

    public Transform hubSpot;
    public Transform freeRoamSpot;

    public GameObject freeRoamZone;
    public void ToggleUI()
    {
        if (Time.time > toggleCooldown)
        {
            toggleCooldown = Time.time + 0.5f;
            UIToggled = !UIToggled;
            trail.SetActive(UIToggled);
            earthLatLong.SetActive(UIToggled);
        }
    }

    public void TeleportHome()
    {
        hub.SetActive(true);
        freeRoamZone.SetActive(false);
        player.transform.SetParent(hub.transform, true);
        MovePlayerToFloor(hubSpot.position);
    }

    public void FreeRoam()
    {
        // Keep the tracked rig active when the ship is hidden for free roam.
        if (player.transform.IsChildOf(hub.transform))
            player.transform.SetParent(hub.transform.parent, true);
        hub.SetActive(false);
        freeRoamZone.SetActive(true);
        MovePlayerToFloor(freeRoamSpot.position);
    }

    private void MovePlayerToFloor(Vector3 position)
    {
        XROrigin origin = player.GetComponent<XROrigin>();
        if (origin != null)
            origin.MoveCameraToWorldLocation(position + Vector3.up * origin.CameraInOriginSpaceHeight);
        else
            player.transform.position = position;
    }
}
