using UnityEngine;

public static class Constants
{
    public const byte MIN_PLAYER_COUNT = 2;
    public const byte MAX_PLAYER_COUNT = 4;

    public static readonly Color ROAD_BLUEPRINT_COLOR = Color.darkCyan;
    public static readonly Color ROAD_BLUEPRINT_PREVIEW_COLOR = Color.mediumAquamarine;

    public struct OutlineData
    {
        public Color OutlineColor;
        public Color InnerColor;
        public int TextureId;
    }

    public static OutlineData ROAD_BLUEPRINT_VALID_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.black,
        InnerColor = new Color(0.5f, 0.5f, 1f, 0.2f),
        TextureId = 1,
    };

    public static OutlineData CANAL_BLUEPRINT_VALID_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.blue,
        InnerColor = new Color(0, 1, 1, 0.2f),
        TextureId = 1,
    };

    public static OutlineData ROAD_BLUEPRINT_OVERLAPPING_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.black,
        InnerColor = new Color(0, 0, 0, 1f),
        TextureId = 1,
    };

    public static readonly Color ROAD_BLUEPRINT_INVALID_COLOR = Color.orange;

    public static OutlineData ROAD_BLUEPRINT_INVALID_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.red,
        InnerColor = new Color(255, 0, 0, 0.5f),
        TextureId = 1,
    };

    public static readonly Color[] PLAYER_COLORS = { Color.red, Color.blue, Color.yellow, Color.green };

    public const uint PLAYER_START_MONEY = 100;

    public const byte MAX_TRUCKS_PER_PLAYER = 32;
    public const byte MAX_FREIGHTERS_PER_PLAYER = 8;

    public const byte MAX_TRUCK_COUNT = MAX_TRUCKS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_FREIGHTER_COUNT = MAX_FREIGHTERS_PER_PLAYER * MAX_PLAYER_COUNT;

    public const float TRUCK_SPEED_TPS = 1.0f;
    public const float FREIGHTER_SPEED_TPS = 1.0f;


    public const byte MAX_PRODUCER_COUNT = 64;
    public const byte MAX_CONSUMER_COUNT = 128;

    public const byte MAX_PORTS_PER_PLAYER = 4;
    public const byte MAX_GARAGES_PER_PLAYER = 1;

    public const byte MAX_PORT_COUNT = MAX_PORTS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_GARAGE_COUNT = MAX_GARAGES_PER_PLAYER * MAX_PLAYER_COUNT;


    public const int MAX_NETTO_BYTES_PER_RPC = 1000;


    public const int ENEMY_ROAD_MOVEMENT_COST = 1;
    public const int OWN_ROAD_MOVEMENT_COST = 0;
    public const int PUBLIC_ROAD_MOVEMENT_COST = 0;
    public const int ROAD_MOVEMENT_DISTANCE = 1;

    public const int MAX_PRIORITYS_FOR_PATHFINDING = 4;
}