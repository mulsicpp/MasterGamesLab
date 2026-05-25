using UnityEngine;

public static class Constants
{
    public const byte MIN_PLAYER_COUNT = 2;
    public const byte MAX_PLAYER_COUNT = 4;

    public static readonly Color[] PLAYER_COLORS = { Color.red, Color.blue, Color.yellow, Color.green };
    public const uint PLAYER_START_MONEY = 100;

    public const byte MAX_TRUCKS_PER_PLAYER = 32;
    public const byte MAX_FREIGHTERS_PER_PLAYER = 8;

    public const byte MAX_PRODUCER_COUNT = 64;
    public const byte MAX_CONSUMER_COUNT = 128;

    public const byte MAX_TRUCK_COUNT = MAX_TRUCKS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_FREIGHTER_COUNT = MAX_FREIGHTERS_PER_PLAYER * MAX_PLAYER_COUNT;


    public const int MAX_EDGES_PER_RPC = 32;
    public const int MAX_PRODUCERS_PER_RPC = 32;
    public const int MAX_CONSUMERS_PER_RPC = 32;

    public const int MAX_TRUCKS_PER_RPC = 8;


    public const int ROAD_MOVEMENT_COST = 1;
    public const int ROAD_MOVEMENT_DISTANCE = 1;
}
