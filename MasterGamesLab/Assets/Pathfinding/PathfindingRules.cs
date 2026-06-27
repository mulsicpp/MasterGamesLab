using Map;
using Player;

public static class PathfindingRules
{
    public static bool CanBecomeBlueprintType(Tile current, Tile neighbor, Edge.EdgeType type)
    {
        return current.FindEdgeTo(neighbor)?.CanBecomeBlueprintType(type) ?? false;
    }

    /// <summary>
    /// Strict Rule: Only allow movement if an actual connected road edge exists.
    /// </summary>
    public static bool BlockCannotDrive(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge == null || edge.Type != Edge.EdgeType.Road;
    }

    public static bool BlockCannotSwim(Tile current, Tile neighbor)
    {
        if (current.Type == Tile.TileType.Water && neighbor.Type == Tile.TileType.Water)
            return false;
        Edge edge = current.FindEdgeTo(neighbor);
        return edge == null || edge.Type != Edge.EdgeType.Canal;
    }

    public static bool BlockIsNotCanal(Tile current, Tile neighbor)
    {
        Edge edge = current.FindEdgeTo(neighbor);
        return edge == null || edge.Type != Edge.EdgeType.Canal;
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





    public static void MinimizeSteps(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        score[slot] += 1;
    }

    /// <summary>
    /// Base Metric: Measures raw physical traversal distance (Shortest Path baseline).
    /// </summary>
    public static void MinimizeDuration(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        score[slot] += (long)(100.0f / (current.FindEdgeTo(neighbor)?.GetSpeedMultiplier() ?? 1.0f));
    }

    public static void MinimizeDistance(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        score[slot] += 1; // 1 standard physical tile step
    }

    public static void MinimizeCost(Tile current, Tile neighbor, ref PathScore score, int slot)
    {
        PlayerId self = Player.Player.SelfId;
        score[slot] += current.FindEdgeTo(neighbor)?.GetTraversalCost(Player.Player.Self) ?? 0;
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