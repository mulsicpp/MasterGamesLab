namespace Player
{
    public class PlayerStats
    {
        PlayerId Id;
        string Name;
        int Money;
        int MarketCap { get; }
        int Revenue;

        int RoadCount;
        int CanalCount;

        int PortCount;

        int TruckCount;
        int FreighterCount;
    }
}