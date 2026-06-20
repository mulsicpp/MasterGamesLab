using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RoadPin : Pin
{
    private Label cost, duration;

    protected override float pinHeightPercent => 0.3f;

    protected override float pinAspectRatio => 4;

    public bool FacingLeft = false;

    protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
    {
        Vector3 rawPosition = gameObject.transform.position;
        Vector3 projectedPosition = Map.Map.Instance.GetProjectedPosition(rawPosition);
        upVector = (Map.Map.Instance.GetProjectedPosition(rawPosition * 1.01f) - projectedPosition).normalized;
        return projectedPosition;
    }

    protected override void InitializeUiComponents()
    {
        cost = UiElement.Q<Label>("CostLabel");
        duration = UiElement.Q<Label>("DurationLabel");
    }
}
