using Map;

public static class MovementProfileRegistry
{
    public static MovementProfile TruckFastestRoute { get; private set; }
    public static MovementProfile TruckCheapestRoute { get; private set; }

    public static MovementProfile FindRoadBuildPath { get; private set; }



    public static void Initialize()
    {
        TruckFastestRoute = new MovementProfile();
        TruckFastestRoute.IsHardBlocked = PathfindingRules.BlockNoRoad;
        TruckFastestRoute.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        TruckFastestRoute.AddPriorityRule(1, PathfindingRules.MinimizeCost);

        TruckCheapestRoute = new MovementProfile();
        TruckCheapestRoute.IsHardBlocked = PathfindingRules.BlockNoRoad;
        TruckCheapestRoute.AddPriorityRule(0, PathfindingRules.MinimizeCost);
        TruckCheapestRoute.AddPriorityRule(1, PathfindingRules.MinimizeDistance);

        FindRoadBuildPath = new MovementProfile();
        FindRoadBuildPath.IsHardBlocked = PathfindingRules.BlockMountains;
        FindRoadBuildPath.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        FindRoadBuildPath.AddPriorityRule(1, PathfindingRules.AvoidWater);
        FindRoadBuildPath.AddPriorityRule(2, PathfindingRules.AvoidForest);

    }
}