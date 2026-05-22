using UnityEngine;

namespace Map.GeometryGeneration
{
    public class TileGeometryGenerator
    {
        private static readonly float TanPI3 = Mathf.Tan(Mathf.PI / 3);

        private static readonly Vector2[] HexagonCoordinates = new Vector2[]
        {
            new(-256, 0),
            new(-128, 128 * TanPI3),
            new(128, 128 * TanPI3),
            new(256, 0),
            new(128, -128 * TanPI3),
            new(-128, -128 * TanPI3),
        };

        private static readonly Vector2 WaterCenter = new(256, 128 * TanPI3);
        private static readonly Vector2 MountainCenter = new(256, 256 + 128 * TanPI3);
        private static readonly Vector2 PlainCenter = new(512 + 256, 128 * TanPI3);
        
        
        
    }
}