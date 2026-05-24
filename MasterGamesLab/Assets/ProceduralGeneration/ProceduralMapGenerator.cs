using System.Collections.Generic;
using UnityEngine;
using Map;

public static class ProceduralMapGenerator
{
    public static void GenerateMap(IMap map)
    {
        var generationPasses = new List<IGenerationPass>
        {
            new GraphSetupPass(),
            new ContinentPass()
        };

        foreach (var pass in generationPasses)
        {
            pass.Execute(map);
        }
    }
}