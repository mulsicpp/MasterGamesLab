using Map;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class IngameInputs : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap controlsActionMap;
    private InputAction leftClickAction;
    private InputAction buildRoadAction;
    private InputAction buildCanalAction;
    private InputAction buildPortAction;
    private InputAction buyTruckAction;
    private InputAction buyFreighterAction;
    private InputAction confirmBuildPlanAction;
    private InputAction hideBuildPlanAction;

    private InputAction cancelAction;



    void Start()
    {
        controlsActionMap = inputActions.FindActionMap("Controls");

        leftClickAction = controlsActionMap.FindAction("LeftClick");
        buildRoadAction = controlsActionMap.FindAction("BuildRoad");
        buildCanalAction = controlsActionMap.FindAction("BuildCanal");
        buildPortAction = controlsActionMap.FindAction("BuildPort");
        buyTruckAction = controlsActionMap.FindAction("BuyTruck");
        buyFreighterAction = controlsActionMap.FindAction("BuyFreighter");
        confirmBuildPlanAction = controlsActionMap.FindAction("ConfirmBuildPlan");
        hideBuildPlanAction = controlsActionMap.FindAction("HideBuild");
        cancelAction = controlsActionMap.FindAction("Cancel");

        leftClickAction.performed += ctx => OnLeftClick();
        buildRoadAction.started += ctx => IngameUI.Instance.BuildMode = BuildMode.Road;
        buildCanalAction.started += ctx => IngameUI.Instance.BuildMode = BuildMode.Canal;
        buildPortAction.started += ctx => IngameUI.Instance.BuildMode = BuildMode.Port;
        buyTruckAction.started += ctx => IngameUI.Instance.BuildMode = BuildMode.Truck;
        buyFreighterAction.started += ctx => IngameUI.Instance.BuildMode = BuildMode.Freighter;
        confirmBuildPlanAction.started += ctx => IngameUI.Instance.OnConfirmPressed();
        hideBuildPlanAction.started += ctx => IngameUI.Instance.OnHidePressed();
        cancelAction.started += ctx => IngameUI.Instance.OnCancelPressed();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLeftClick()
    {
        
    }
}
