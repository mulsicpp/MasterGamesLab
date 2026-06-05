using System.Collections.Generic;

namespace Map.Blueprint
{
    public class BlueprintDetails
    {
        public class ObjectInfo
        {
            public int Count;
            public int Cost;
        }

        public readonly SortedList<ConstructibleType, ObjectInfo> ObjectInfos;

        public readonly int TotalCost;

        public BlueprintDetails(SortedList<ConstructibleType, ObjectInfo> objectInfos, int totalCost)
        {
            ObjectInfos = objectInfos;
            TotalCost = totalCost;
        }
    }
}