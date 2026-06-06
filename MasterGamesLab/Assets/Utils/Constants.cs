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

    public static OutlineData HOVER_OUTLINE = new OutlineData()
    {
        OutlineColor = new Color(1f, 0.92f, 0.016f, 0.6f),
        InnerColor = new Color(0, 0, 0, 0),
        TextureId = 0,
    };

    public static OutlineData HOVER_OUTLINE_FILLED_IN = new OutlineData()
    {
        OutlineColor = HOVER_OUTLINE.OutlineColor,
        InnerColor = new Color(1f, 0.92f, 0.016f, 0.5f),
        TextureId = 1,
    };

    public static OutlineData ROAD_BLUEPRINT_VALID_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.black,
        InnerColor = new Color(0.5f, 0.5f, 1f, 0.2f),
        TextureId = 1,
    };

    public static OutlineData CANAL_BLUEPRINT_VALID_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.blue,
        InnerColor = new Color(0, 1, 1, 0.5f),
        TextureId = 1,
    };

    public static OutlineData ROAD_BLUEPRINT_PREVIEW_OVERLAPPING_OUTLINE = new OutlineData()
    {
        OutlineColor = Color.grey,
        InnerColor = new Color(0, 0, 0, 0.2f),
        TextureId = 2,
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

    public const int PLAYER_START_MONEY = 100;

    public const byte MAX_TRUCKS_PER_PLAYER = 32;
    public const byte MAX_FREIGHTERS_PER_PLAYER = 8;

    public const byte MAX_TRUCK_COUNT = MAX_TRUCKS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_FREIGHTER_COUNT = MAX_FREIGHTERS_PER_PLAYER * MAX_PLAYER_COUNT;

    public const float TRUCK_SPEED_TPS = 1.0f;
    public const float FREIGHTER_SPEED_TPS = 1.0f;


    public const byte MAX_PRODUCER_COUNT = 64;
    public const byte MAX_CONSUMER_COUNT = 128;

    public const byte MAX_PORTS_PER_PLAYER = 12;
    public const byte MAX_GARAGES_PER_PLAYER = 1;

    public const byte MAX_PORT_COUNT = MAX_PORTS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_GARAGE_COUNT = MAX_GARAGES_PER_PLAYER * MAX_PLAYER_COUNT;


    public const int MAX_NETTO_BYTES_PER_RPC = 1000;


    public const float PLAIN_BUILD_COST_FACTOR = 1f;
    public const float FOREST_BUILD_COST_FACTOR = 2f;

    public const int ROAD_BUILD_COST = 20;
    public const int BASE_CANAL_BUILD_COST = 50;
    public const int PORT_BUILD_COST = 300;


    public const int ENEMY_ROAD_MOVEMENT_COST = 1;
    public const int OWN_ROAD_MOVEMENT_COST = 0;
    public const int PUBLIC_ROAD_MOVEMENT_COST = 0;
    public const int ROAD_MOVEMENT_DISTANCE = 1;

    public const int MAX_PRIORITYS_FOR_PATHFINDING = 4;
}