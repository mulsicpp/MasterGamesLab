using System;
using System.Collections.Generic;
using Map;
using Unity.VisualScripting;

public static class Pathfinding
{
    public delegate void PathfindingRule(Tile current, Tile neighbor, ref PathScore stepScore, int targetSlot);

    public struct RuleMapping
    {
        public PathfindingRule RuleExecutable;
        public int TargetSlot;
    }

    private static NodeState[] nodeStatesBuffer;
    private static bool[] visitedTilesBuffer;
    private static PriorityQueue<Tile, PathScore> tileQueue = new PriorityQueue<Tile, PathScore>();

    private struct NodeState
    {
        public PathScore RealCost;
        public TileId CameFromId;
    }

    /// <summary>
    /// Allocates the static search buffers once at startup based on map sizes.
    /// </summary>
    public static void InitBuffers(int tileCount)
    {
        nodeStatesBuffer = new NodeState[tileCount];
        visitedTilesBuffer = new bool[tileCount];
    }

    public static TileId[] FindPath(Tile start, Tile target, MovementProfile profile, Func<Tile, PathScore> heuristicFunction = null)
        => FindPath(start, t => t.Id == target.Id, profile, heuristicFunction);

    /// <summary>
    /// Evaluates the optimal path through your tiled world based on your movement profiles.
    /// Leaving heuristicFunction null transforms this automatically into pure Dijkstra.
    /// </summary>
    public static TileId[] FindPath(
        Tile start, 
        Predicate<Tile> targetCondition, 
        MovementProfile profile, 
        Func<Tile, PathScore> heuristicFunction = null)
    {
        if (start == null || targetCondition == null || profile == null) return null;

        // Instant hard block fast-exit check
        // if (profile.IsHardBlocked != null && profile.IsHardBlocked(start, target)) return null;

        // Reset tracking layers without allocating new memory objects
        Array.Clear(visitedTilesBuffer, 0, visitedTilesBuffer.Length);
        tileQueue.Clear();

        TileId startId = start.Id;
        nodeStatesBuffer[startId] = new NodeState { RealCost = new PathScore(), CameFromId = TileId.NONE };
        visitedTilesBuffer[startId] = true;
        
        tileQueue.Enqueue(start, new PathScore());

        var activeRules = profile.GetRules();

        while (tileQueue.Count > 0)
        {
            Tile current = tileQueue.Dequeue();
            TileId currentId = current.Id;

            if (targetCondition(current))
            {
                return ReconstructPathArray(startId, currentId);
            }

            PathScore currentRealCost = nodeStatesBuffer[currentId].RealCost;

            foreach (Tile neighborTile in current.Neighbors)
            {
                if (profile.IsHardBlocked != null && profile.IsHardBlocked(current, neighborTile)) continue;

                TileId neighborId = neighborTile.Id;

                PathScore stepScore = new PathScore();
                for (int i = 0; i < activeRules.Count; i++)
                {
                    RuleMapping mapping = activeRules[i];
                    
                    mapping.RuleExecutable(current, neighborTile, ref stepScore, mapping.TargetSlot);
                }

                PathScore newRealCost = currentRealCost + stepScore;
                bool hasState = visitedTilesBuffer[neighborId];

                if (!hasState || newRealCost.CompareTo(nodeStatesBuffer[neighborId].RealCost) < 0)
                {
                    visitedTilesBuffer[neighborId] = true;
                    nodeStatesBuffer[neighborId] = new NodeState
                    {
                        RealCost = newRealCost,
                        CameFromId = currentId
                    };

                    PathScore priorityScore = newRealCost;
                    
                    if (heuristicFunction != null)
                    {
                        priorityScore += heuristicFunction(neighborTile);
                    }

                    tileQueue.Enqueue(neighborTile, priorityScore);
                }
            }
        }
        return null;
    }

    private static TileId[] ReconstructPathArray(TileId startId, TileId targetId)
    {
        var result = new List<TileId>();
        TileId currId = targetId;
        while (currId != TileId.NONE)
        {
            result.Add(currId);
            currId = nodeStatesBuffer[currId].CameFromId;
        }
        result.Reverse();
        return result.ToArray();
    }
}