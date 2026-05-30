using Map;

public static class PathfindingRules
{

    /// <summary>
    /// Strict Rule: Only allow movement if an actual connected road edge exists.
    /// </summary>
    public static bool BlockNoRoad(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge == null || edge.Type != Edge.EdgeType.Road;
    }

    public static bool BlockRoad(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge != null && edge.Type == Edge.EdgeType.Road;
    }

    public static bool BlockCanal(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge != null && edge.Type == Edge.EdgeType.Canal;
    }

    public static bool BlockRail(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge != null && edge.Type == Edge.EdgeType.Rail;
    }

    /// <summary>
    /// Strict Rule: Prevents vehicles from stepping into deep water zones completely.
    /// </summary>
    public static bool BlockWater(Tile current, Tile neighbor)
    {
        return neighbor.Type == Tile.TileType.Water;
    }

    /// <summary>
    /// Strict Rule: Prevents vehicles from stepping into deep water zones completely.
    /// </summary>
    public static bool BlockMountains(Tile current, Tile neighbor)
    {
        return neighbor.Type == Tile.TileType.Mountain;
    }

    /// <summary>
    /// Base Metric: Measures raw physical traversal distance (Shortest Path baseline).
    /// </summary>
    public static void MinimizeDistance(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        score[slot] += Constants.ROAD_MOVEMENT_DISTANCE; // 1 standard physical tile step
    }

    public static void MinimizeCost(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        PlayerId self = PlayerManager.Instance.SelfId;
        PlayerId owner = current.FindEdgeTo(neighbor).Owner;
        score[slot] += self == owner ? Constants.OWN_ROAD_MOVEMENT_COST : owner == PlayerId.NONE ? Constants.PUBLIC_ROAD_MOVEMENT_COST : Constants.ENEMY_ROAD_MOVEMENT_COST; // 1 standard physical tile step
    }


    /// <summary>
    /// Terrain Soft Penalty: Strongly discourages traveling through water without hard blocking it.
    /// </summary>
    public static void AvoidWater(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        if (neighbor.Type == Tile.TileType.Water)
        {
            score[slot] += 1;
        }
    }

    public static void AvoidForest(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        if (neighbor.Type == Tile.TileType.Forest)
        {
            score[slot] += 1;
        }
    }

    /// <summary>
    /// Terrain Filtering Rule: Prioritizes driving on smooth plains over uneven terrain variants.
    /// </summary>
    public static void MaximizePlains(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        // Assuming BiomeType or TileType properties exist on your globe tiles
        if (neighbor.Type != Tile.TileType.Plain)
        {
            score[slot] += 1; // Penalty point if the terrain variant isn't flat plain
        }
    }
}