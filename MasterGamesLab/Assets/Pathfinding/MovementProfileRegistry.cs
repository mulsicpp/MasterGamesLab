using Map;

public static class MovementProfileRegistry
{
    public static MovementProfile TruckFastestRoute { get; private set; }
    public static MovementProfile TruckCheapestRoute { get; private set; }

    public static MovementProfile FindRoadBuildPath { get; private set; }
    public static MovementProfile FindCanalBuildPath { get; private set; }


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
        FindRoadBuildPath.IsHardBlocked = (s, t) => PathfindingRules.BlockMountains(s, t) || PathfindingRules.BlockCanal(s, t);
        FindRoadBuildPath.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        FindRoadBuildPath.AddPriorityRule(1, PathfindingRules.AvoidWater);
        FindRoadBuildPath.AddPriorityRule(2, PathfindingRules.AvoidForest);

        FindCanalBuildPath = new MovementProfile();
        FindCanalBuildPath.IsHardBlocked = (s, t) => PathfindingRules.BlockMountains(s, t) || PathfindingRules.BlockRoad(s, t);
        FindCanalBuildPath.AddPriorityRule(0, PathfindingRules.MinimizeDistance);
        FindCanalBuildPath.AddPriorityRule(2, PathfindingRules.AvoidForest);

    }
}