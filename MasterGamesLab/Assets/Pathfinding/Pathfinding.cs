using System;
using System.Collections.Generic;
using Map;


public static class Pathfinding
{

    // --- HIGH PERFORMANCE PRE-ALLOCATED RUNTIME BUFFERS ---
    private static NodeState[] nodeStatesBuffer;
    private static bool[] visitedTilesBuffer;
    private static PriorityQueue<Tile, long> tileQueue = new PriorityQueue<Tile, long>();
    private struct NodeState
    {
        public long RealCost;
        public TileId CameFromId;
    }

    public enum RoutePriorityMode { Shortest, Cheapest }


    public static void InitBuffers(int tileCount)
    {
        nodeStatesBuffer = new NodeState[tileCount];
        visitedTilesBuffer = new bool[tileCount];
    }


    public static TileId[] FindPath(Tile start, Tile target, Func<Tile, Tile, long> costFunction, Func<Tile, Tile, long> heuristicFunction = null)
    {
        heuristicFunction ??= (_, _) => 0;

        if (start == null || target == null)
            return null;
        Array.Clear(visitedTilesBuffer, 0, visitedTilesBuffer.Length);
        tileQueue.Clear();

        TileId startId = start.Id;
        nodeStatesBuffer[startId] = new NodeState { RealCost = 0, CameFromId = TileId.NONE };
        visitedTilesBuffer[startId] = true;
        tileQueue.Enqueue(start, 0);


        while (tileQueue.Count > 0)
        {
            Tile current = tileQueue.Dequeue();
            TileId currentId = current.Id;

            if (currentId == target.Id)
            {
                return ReconstructPathArray(startId, target.Id);
            }

            long currentRealCost = nodeStatesBuffer[currentId].RealCost;

            foreach (Tile neighbortile in current.Neighbors)
            {
                long stotcost = costFunction(current, neighbortile);
                if (stotcost < 0) continue;
                TileId neighborId = neighbortile.Id;

                long newRealCost = currentRealCost + stotcost;

                bool hasState = visitedTilesBuffer[neighborId];

                if (!hasState || newRealCost < nodeStatesBuffer[neighborId].RealCost)
                {
                    visitedTilesBuffer[neighborId] = true;
                    nodeStatesBuffer[neighborId] = new NodeState
                    {
                        RealCost = newRealCost,
                        CameFromId = currentId
                    };
                    tileQueue.Enqueue(neighbortile, newRealCost + heuristicFunction(neighbortile, target));
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
