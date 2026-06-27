using Map;
using Map.Fleet;

public static class MovementProfileRegistry
{
    public static MovementProfile TruckFastestRoute { get; private set; }
    public static MovementProfile TruckCheapestRoute { get; private set; }

    public static MovementProfile FreighterFastestRoute { get; private set; }
    public static MovementProfile FreighterCheapestRoute { get; private set; }

    public static MovementProfile FindRoadBuildPath { get; private set; }
    public static MovementProfile FindCanalBuildPath { get; private set; }


    public static void Initialize()
    {
        TruckFastestRoute = new MovementProfile();
        TruckFastestRoute.CanPass = (s, t) => Vehicle.CanCross(s, t, Vehicle.VehicleType.Truck);
        TruckFastestRoute.AddPriorityRule(0, PathfindingRules.MinimizeDuration);
        TruckFastestRoute.AddPriorityRule(1, PathfindingRules.MinimizeCost);

        TruckCheapestRoute = new MovementProfile();
        TruckCheapestRoute.CanPass = (s, t) => Vehicle.CanCross(s, t, Vehicle.VehicleType.Truck);
        TruckCheapestRoute.AddPriorityRule(0, PathfindingRules.MinimizeCost);
        TruckCheapestRoute.AddPriorityRule(1, PathfindingRules.MinimizeDuration);

        FreighterFastestRoute = new MovementProfile();
        FreighterFastestRoute.CanPass = (s, t) => Vehicle.CanCross(s, t, Vehicle.VehicleType.Freighter);
        FreighterFastestRoute.AddPriorityRule(0, PathfindingRules.MinimizeDuration);
        FreighterFastestRoute.AddPriorityRule(1, PathfindingRules.MinimizeCost);
        
        FreighterCheapestRoute = new MovementProfile();
        FreighterCheapestRoute.CanPass = (s, t) => Vehicle.CanCross(s, t, Vehicle.VehicleType.Freighter);
        FreighterCheapestRoute.AddPriorityRule(0, PathfindingRules.MinimizeCost);
        FreighterCheapestRoute.AddPriorityRule(1, PathfindingRules.MinimizeDuration);

        FindRoadBuildPath = new MovementProfile();
        FindRoadBuildPath.CanPass = (s, t) => PathfindingRules.CanBecomeBlueprintType(s, t, Edge.EdgeType.Road);
        FindRoadBuildPath.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        FindRoadBuildPath.AddPriorityRule(1, PathfindingRules.AvoidForest);
        //FindRoadBuildPath.AddPriorityRule(1, PathfindingRules.AvoidWater);

        FindCanalBuildPath = new MovementProfile();
        FindCanalBuildPath.CanPass = (s, t) => PathfindingRules.CanBecomeBlueprintType(s, t, Edge.EdgeType.Canal);
        FindCanalBuildPath.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        FindCanalBuildPath.AddPriorityRule(1, PathfindingRules.AvoidForest);

    }
}