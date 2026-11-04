using TMPro;
using UnityEngine;

//the parked ship opens its front panels while the arrival moves u onto the deck
public class ObservatoryRocketHatch : MonoBehaviour
{
    public Transform[] hatchPanels = new Transform[0];
    public Transform[] cabinWalls = new Transform[0];
    public Transform deckFloor;
    public TMP_Text statusLabel;
    public float panelDrop = 2.2f;

    public float OpenAmount { get; private set; }
    public float FloorLocalY { get; private set; } = -1.42f;

    Vector3[] closedPositions;

    void Awake() { RememberClosedPositions(); }

    void RememberClosedPositions()
    {
        if (closedPositions != null && closedPositions.Length == hatchPanels.Length) return;
        closedPositions = new Vector3[hatchPanels.Length];
        for (int i = 0; i < hatchPanels.Length; i++)
            if (hatchPanels[i] != null) closedPositions[i] = hatchPanels[i].localPosition;
    }

    public void SetOpenAmount(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount)) return;
        RememberClosedPositions();
        OpenAmount = Mathf.Clamp01(amount);
        //even a tall headset pose leaves the open panels below the aligned floor
        float drop = Mathf.Max(panelDrop, -0.30f - FloorLocalY + 0.20f);
        for (int i = 0; i < hatchPanels.Length; i++)
            if (hatchPanels[i] != null)
                hatchPanels[i].localPosition = closedPositions[i] + Vector3.down * (OpenAmount * drop);
    }

    public void SetDeckHeight(float localFloorY)
    {
        if (float.IsNaN(localFloorY) || float.IsInfinity(localFloorY)) return;
        FloorLocalY = localFloorY;
        if (deckFloor != null)
        {
            Vector3 position = deckFloor.localPosition;
            position.y = localFloorY - Mathf.Abs(deckFloor.localScale.y) * 0.5f;
            deckFloor.localPosition = position;
        }
        //keep the wall tops still while the lower edges meet the dock floor
        foreach (Transform wall in cabinWalls)
        {
            if (wall == null) continue;
            Vector3 position = wall.localPosition;
            Vector3 scale = wall.localScale;
            float top = position.y + Mathf.Abs(scale.y) * 0.5f;
            float bottom = Mathf.Min(localFloorY, top - 0.05f);
            position.y = (top + bottom) * 0.5f;
            scale.y = top - bottom;
            wall.localPosition = position;
            wall.localScale = scale;
        }
        SetOpenAmount(OpenAmount);
    }

    public void SetStatus(string status)
    {
        if (statusLabel != null) statusLabel.text = status ?? "";
    }

    void OnDisable() { SetOpenAmount(0f); }
}
