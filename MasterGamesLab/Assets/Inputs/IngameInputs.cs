using Map;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class IngameInputs : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private static InputActionMap controlsActionMap;

    public static InputAction leftClickAction;
    private InputAction buildRoadAction;
    private InputAction buildCanalAction;
    private InputAction buildPortAction;
    private InputAction buyTruckAction;
    private InputAction buyFreighterAction;
    private InputAction confirmBuildPlanAction;
    private InputAction hideBuildPlanAction;
    private InputAction cancelAction;

    [SerializeField] private ConstructionControls constructionControls;

    void Awake()
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
    }

    void OnEnable()
    {
        buildRoadAction.started += OnBuildRoad;
        buildCanalAction.started += OnBuildCanal;
        buildPortAction.started += OnBuildPort;
        buyTruckAction.started += OnBuyTruck;
        buyFreighterAction.started += OnBuyFreighter;
        confirmBuildPlanAction.started += OnConfirm;
        hideBuildPlanAction.started += OnHide;
        cancelAction.started += OnCancel;
    }

    void OnDisable()
    {
        buildRoadAction.started -= OnBuildRoad;
        buildCanalAction.started -= OnBuildCanal;
        buildPortAction.started -= OnBuildPort;
        buyTruckAction.started -= OnBuyTruck;
        buyFreighterAction.started -= OnBuyFreighter;
        confirmBuildPlanAction.started -= OnConfirm;
        hideBuildPlanAction.started -= OnHide;
        cancelAction.started -= OnCancel;
    }

    private void OnBuildRoad(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Road;
    private void OnBuildCanal(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Canal;
    private void OnBuildPort(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Port;
    private void OnBuyTruck(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Truck;
    private void OnBuyFreighter(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Freighter;
    private void OnConfirm(InputAction.CallbackContext ctx) => constructionControls.ConfirmConstruction();
    private void OnCancel(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.None; 
    private void OnHide(InputAction.CallbackContext ctx) => constructionControls.ToggleHide();
}