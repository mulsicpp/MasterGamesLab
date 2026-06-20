using Map;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    public static InputAction changeTruckAction;
    public static InputAction chooseFastestRoute;
    public static InputAction chooseCheapestRoute;

    // Arrays to store our dynamic hotkey actions dynamically
    private List<InputAction> truckHotkeyActions = new List<InputAction>();
    private List<InputAction> freighterHotkeyActions = new List<InputAction>();

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
        changeTruckAction = controlsActionMap.FindAction("ChangeVehicle");

        for (int i = 0; i < 10; i++)
        {
            var truckAct = controlsActionMap.FindAction($"Truck{i}");
            if (truckAct != null) truckHotkeyActions.Add(truckAct);

            var freighterAct = controlsActionMap.FindAction($"Freighter{i}");
            if (freighterAct != null) freighterHotkeyActions.Add(freighterAct);
        }
        chooseFastestRoute = controlsActionMap.FindAction("ChooseFastestRoute");
        chooseCheapestRoute = controlsActionMap.FindAction("ChooseCheapestRoute");
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
        openTabMenuAction.started += OnTabPressed;
        openTabMenuAction.canceled += OnTabReleased;
        changeTruckAction.started += OnChangeVehicle;

        for (int i = 0; i < truckHotkeyActions.Count; i++)
        {
            int slotIndex = i;
            truckHotkeyActions[slotIndex].started += ctx => OnSelectVehicleSlot(Map.Fleet.Vehicle.VehicleType.Truck, slotIndex);
        }

        for (int i = 0; i < freighterHotkeyActions.Count; i++)
        {
            int slotIndex = i;
            freighterHotkeyActions[slotIndex].started += ctx => OnSelectVehicleSlot(Map.Fleet.Vehicle.VehicleType.Freighter, slotIndex);
        }
        chooseFastestRoute.started += OnChooseFastestRoute;
        chooseCheapestRoute.started += OnChooseCheapestRoute;
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
        openTabMenuAction.started -= OnTabPressed;
        openTabMenuAction.canceled -= OnTabReleased;
        changeTruckAction.started -= OnChangeVehicle;

        // Unsubscribe hotkeys
        for (int i = 0; i < truckHotkeyActions.Count; i++)
        {
            int slotIndex = i;
            truckHotkeyActions[slotIndex].started -= ctx => OnSelectVehicleSlot(Map.Fleet.Vehicle.VehicleType.Truck, slotIndex);
        }

        for (int i = 0; i < freighterHotkeyActions.Count; i++)
        {
            int slotIndex = i;
            freighterHotkeyActions[slotIndex].started -= ctx => OnSelectVehicleSlot(Map.Fleet.Vehicle.VehicleType.Freighter, slotIndex);
        }
        chooseFastestRoute.started -= OnChooseFastestRoute;
        chooseCheapestRoute.started -= OnChooseCheapestRoute;
    }

    private void OnBuildRoad(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Road;
    private void OnBuildCanal(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Canal;
    private void OnBuildPort(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Port;
    private void OnBuyTruck(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Truck;
    private void OnBuyFreighter(InputAction.CallbackContext ctx) => constructionControls.Type = ConstructionControls.ConstructionType.Freighter;
    private void OnConfirm(InputAction.CallbackContext ctx) => constructionControls.ConfirmConstruction();
    private void OnHide(InputAction.CallbackContext ctx) => constructionControls.ToggleHide();
    private void OnTabPressed(InputAction.CallbackContext ctx) => IngameUI.Instance.ShowTabMenu(true);
    private void OnTabReleased(InputAction.CallbackContext ctx) => IngameUI.Instance.ShowTabMenu(false);
    private void OnChangeVehicle(InputAction.CallbackContext ctx) => IngameUI.Instance.SelectNextVehicle();

    // Sends hotkey slot selections directly down to the UI Layer
    private void OnSelectVehicleSlot(Map.Fleet.Vehicle.VehicleType type, int slotIndex)
    {
        IngameUI.Instance.SelectVehicleBySlot(type, slotIndex);
    }
    private void OnChooseFastestRoute(InputAction.CallbackContext ctx) => IngameUI.Instance.VehicleControls.ChooseFastestRoute();
    private void OnChooseCheapestRoute(InputAction.CallbackContext ctx) => IngameUI.Instance.VehicleControls.ChooseCheapestRoute();
}