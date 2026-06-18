using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RoadPin : Pin
{
    Label cost, duration;

    protected override float pinHeightPercent => 0.3f;

    protected override float pinAspectRatio => 4;

    protected override Vector3 GetTargetWorldPosition(out Vector3 upVector)
    {
        throw new System.NotImplementedException();
    }

    protected override void InitializeUiComponents()
    {
        cost = UiElement.Q<Label>("CostLabel");
        duration = UiElement.Q<Label>("DurationLabel");
    }
}
