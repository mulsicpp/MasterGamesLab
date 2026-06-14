using Map;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class IngameInputs : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private static InputActionMap controlsActionMap;

    public static InputAction selectClickAction;
    public static InputAction buildRoadAction;
    public static InputAction buildCanalAction;
    public static InputAction buildPortAction;
    public static InputAction buyTruckAction;
    public static InputAction buyFreighterAction;
    public static InputAction confirmBuildPlanAction;
    public static InputAction hideBuildPlanAction;
    public static InputAction cancelClickAction;
    public static InputAction openTabMenuAction;

    [SerializeField] private ConstructionControls constructionControls;

    void Awake()
    {
        controlsActionMap = inputActions.FindActionMap("Controls");

        selectClickAction = controlsActionMap.FindAction("LeftClick");
        buildRoadAction = controlsActionMap.FindAction("BuildRoad");
        buildCanalAction = controlsActionMap.FindAction("BuildCanal");
        buildPortAction = controlsActionMap.FindAction("BuildPort");
        buyTruckAction = controlsActionMap.FindAction("BuyTruck");
        buyFreighterAction = controlsActionMap.FindAction("BuyFreighter");
        confirmBuildPlanAction = controlsActionMap.FindAction("ConfirmBuildPlan");
        hideBuildPlanAction = controlsActionMap.FindAction("HideBuild");
        cancelClickAction = controlsActionMap.FindAction("Cancel");
        openTabMenuAction = controlsActionMap.FindAction("OpenTabMenu");
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
        // cancelAction.started += OnCancel;
        openTabMenuAction.started += OnTabPressed;
        openTabMenuAction.canceled += OnTabReleased;

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
        // cancelAction.started -= OnCancel;
        openTabMenuAction.started -= OnTabPressed;
        openTabMenuAction.canceled -= OnTabReleased;
    }

    private void OnBuildRoad(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Road;
    private void OnBuildCanal(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Canal;
    private void OnBuildPort(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Port;
    private void OnBuyTruck(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Truck;
    private void OnBuyFreighter(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Freighter;
    private void OnConfirm(InputAction.CallbackContext ctx) => constructionControls.ConfirmConstruction();
    // private void OnCancel(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.None;
    private void OnHide(InputAction.CallbackContext ctx) => constructionControls.ToggleHide();
    private void OnTabPressed(InputAction.CallbackContext ctx) => IngameUI.Instance.ShowTabMenu(true);
    private void OnTabReleased(InputAction.CallbackContext ctx) => IngameUI.Instance.ShowTabMenu(false);


}