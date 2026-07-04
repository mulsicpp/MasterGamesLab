using UnityEngine;

namespace Player
{
    public class PlayerStats
    {
        public PlayerId Id;
        public string Name;
        public Color Color;

        public int Cash;
        public int MarketCap
        { 
            get
            {
                var marketCap = Cash;

                for (var i = 0; i < RoadCount; i++) marketCap += Constants.RoadMarketCap(i);
                for (var i = 0; i < CanalCount; i++) marketCap += Constants.CanalMarketCap(i);
                for (var i = 0; i < PortCount; i++) marketCap += Constants.PortMarketCap(i);
                for (var i = 0; i < TruckCount; i++) marketCap += Constants.TruckMarketCap(i);
                for (var i = 0; i < FreighterCount; i++) marketCap += Constants.FreighterMarketCap(i);
                return marketCap;
            }
        }

        public int Revenue;

        public int RoadCount;
        public int CanalCount;

        public int PortCount;

        public int TruckCount;
        public int FreighterCount;
    }
}