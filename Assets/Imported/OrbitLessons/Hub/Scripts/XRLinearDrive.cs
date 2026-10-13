using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using OrbitSimulation;

public class XRLinearDrive : XRSimpleInteractable
{
    public Transform startPosition;
    public Transform endPosition;
    public LinearMapping linearMapping;
    public bool repositionGameObject = true;
    public bool maintainMomemntum = true;
    public float momemtumDampenRate = 5f;

    private float initialMappingOffset;
    private float mappingChangeRate;

    protected override void Awake()
    {
        base.Awake();
        if (linearMapping == null)
            linearMapping = GetComponent<LinearMapping>();
        if (linearMapping == null || startPosition == null || endPosition == null)
            Debug.LogError("[XRLinearDrive] Assign the mapping and both slider endpoints.", this);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (linearMapping == null)
            return;
        initialMappingOffset = linearMapping.value - CalculateMapping(args.interactorObject.GetAttachTransform(this).position);
        mappingChangeRate = 0f;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic ||
            linearMapping == null || startPosition == null || endPosition == null)
            return;

        float previousMapping = linearMapping.value;
        if (isSelected && interactorsSelecting.Count > 0)
        {
            Vector3 position = interactorsSelecting[0].GetAttachTransform(this).position;
            linearMapping.value = Mathf.Clamp01(initialMappingOffset + CalculateMapping(position));
            if (Time.deltaTime > 0f)
                mappingChangeRate = (linearMapping.value - previousMapping) / Time.deltaTime;
        }
        else if (maintainMomemntum)
        {
            mappingChangeRate = Mathf.Lerp(mappingChangeRate, 0f, momemtumDampenRate * Time.deltaTime);
            linearMapping.value = Mathf.Clamp01(previousMapping + mappingChangeRate * Time.deltaTime);
        }

        if (repositionGameObject)
            transform.position = Vector3.Lerp(startPosition.position, endPosition.position, linearMapping.value);
    }

    private float CalculateMapping(Vector3 position)
    {
        if (startPosition == null || endPosition == null)
            return 0f;
        Vector3 direction = endPosition.position - startPosition.position;
        return direction.sqrMagnitude > 0.000001f
            ? Vector3.Dot(position - startPosition.position, direction) / direction.sqrMagnitude
            : 0f;
    }
}
