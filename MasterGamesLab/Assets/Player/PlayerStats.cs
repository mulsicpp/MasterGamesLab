namespace Player
{
    public class PlayerStats
    {
        public PlayerId Id;
        public string Name;
        public int Cash;
        public int MarketCap
        { 
            get
            {
                var marketCap = Cash;
                marketCap += RoadCount * Constants.ROAD_MARKET_CAP;
                marketCap += CanalCount * Constants.CANAL_MARKET_CAP;
                marketCap += PortCount * Constants.PORT_MARKET_CAP;
                marketCap += TruckCount * Constants.TRUCK_MARKET_CAP;
                marketCap += FreighterCount * Constants.FREIGHTER_MARKET_CAP;
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