using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// XRI keeps queued requests while disabled. A dialogue must cancel them, not postpone them.
public class ViewingRoomTeleportation : TeleportationProvider
{
    public override bool QueueTeleportRequest(TeleportRequest request)
    {
        return isActiveAndEnabled && base.QueueTeleportRequest(request);
    }

    protected override void OnDisable()
    {
        validRequest = false;
        base.OnDisable();
    }
}
