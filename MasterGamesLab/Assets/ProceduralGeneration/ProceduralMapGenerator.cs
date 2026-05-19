using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map;

public class ProceduralMapGenerator : MonoBehaviour
{
    private List<IGenerationPass> generationPasses;

    private IEnumerator Start()
    {
        //wait for map.cs to finish building the sphere
        yield return null;

        //singleton instance 
        if (Map.Map.Instance == null || Map.Map.Instance.Tiles == null || Map.Map.Instance.Tiles.Count == 0)
        {
            Debug.LogError("Map ist nicht bereit oder leer!");
            yield break;
        }

        MapData data = new MapData(Map.Map.Instance);

        //pipeline
        generationPasses = new List<IGenerationPass>
        {
            new GraphSetupPass(),
            new ContinentPass()
            
            // new BiomePass(),
            // new CellularAutomataPass()
        };

        //Debug.Log("start pg");
        foreach (var pass in generationPasses)
        {
            pass.Execute(data);
        }
        //Debug.Log("finish pg");
    }
}
