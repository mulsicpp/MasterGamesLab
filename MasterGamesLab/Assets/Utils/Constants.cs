using Map.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

public static class Constants
{
    // ------------------- Player -------------------

    public const byte MIN_PLAYER_COUNT = 2;
    public const byte MAX_PLAYER_COUNT = 4;

    public static readonly Color[] PLAYER_COLORS = { Color.red, Color.blue, Color.yellow, Color.green };

    public const int PLAYER_INITIAL_CASH = 300;
    public const int WINNING_MARKET_CAP = 10000;

    // ------------------- Vehicles -------------------

    public const byte MAX_TRUCKS_PER_PLAYER = 32;
    public const byte MAX_TRUCK_COUNT = MAX_TRUCKS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const float TRUCK_BASE_SPEED_TPS = 1.0f;

    public const byte MAX_FREIGHTERS_PER_PLAYER = 8;
    public const byte MAX_FREIGHTER_COUNT = MAX_FREIGHTERS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const float FREIGHTER_BASE_SPEED_TPS = 0.4f;

    public const int TRUCK_LOADING_COST_ENEMY = 40;
    public const int TRUCK_UNLOADING_COST_ENEMY = 80;


    // ------------------- Structures -------------------

    public const byte MAX_PRODUCER_COUNT = 64;
    public const byte MAX_CONSUMER_COUNT = 128;

    public const byte MAX_PORTS_PER_PLAYER = 12;
    public const byte MAX_GARAGES_PER_PLAYER = 1;

    public const byte MAX_PORT_COUNT = MAX_PORTS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_GARAGE_COUNT = MAX_GARAGES_PER_PLAYER * MAX_PLAYER_COUNT;


    // ------------------- Producer & Consumer Logic -------------------

    public const int GOOD_COMMON_BASE_PAYOUT = 100;
    public const int GOOD_UNCOMMON_BASE_PAYOUT = 150;
    public const int GOOD_RARE_BASE_PAYOUT = 200;
    public const int GOOD_EPIC_BASE_PAYOUT = 300;
    public const int GOOD_LEGENDARY_BASE_PAYOUT = 500;

    public static readonly float[,] GOOD_SPAWN_CHANCE_PER_CONTINENT = new float[4, 5]
    {
        { 0.0f, 0.7f, 0.3f, 0.0f, 0.0f },
        { 0.0f, 0.3f, 0.5f, 0.2f, 0.0f },
        { 0.0f, 0.0f, 0.2f, 0.5f, 0.3f },
        { 0.0f, 0.0f, 0.0f, 0.2f, 0.8f },
    };

    public const int BASE_CONSUMER_COUNT = 10;
    public const int CONSUMER_COUNT_PER_PLAYER = 10;

    public static int TotalConsumerCount =>
        BASE_CONSUMER_COUNT + (Map.Map.Instance?.Players?.Count ?? 0) * CONSUMER_COUNT_PER_PLAYER;

    public static int StartConsumerCount => TotalConsumerCount / 6;

    public const float MIN_CONSUMER_REQUEST_COOLDOWN = 15f;
    public const float MAX_CONSUMER_REQUEST_COOLDOWN = 40f;

    public const float MIN_CONSUMER_PAYOUT_INCREASE_COOLDOWN = 20f;
    public const float MAX_CONSUMER_PAYOUT_INCREASE_COOLDOWN = 50f;

    public const float MIN_CONSUMER_PAYOUT_INCREASE_FACTOR = 1.1f;
    public const float MAX_CONSUMER_PAYOUT_INCREASE_FACTOR = 1.3f;


    public const float MIN_CONSUMER_SPAWN_COOLDOWN = 5f;
    public const float MAX_CONSUMER_SPAWN_COOLDOWN = 30f;

    public const float MIN_PRODUCER_SPAWN_COOLDOWN = 30f;
    public const float MAX_PRODUCER_SPAWN_COOLDOWN = 60f;


    // ------------------- Build Costs -------------------

    public const float PLAIN_BUILD_COST_FACTOR = 1f;
    public const float FOREST_BUILD_COST_FACTOR = 2f;

    public const int ROAD_BUILD_COST = 10;
    public const int BASE_CANAL_BUILD_COST = 20;

    public const int PORT_BUILD_COST = 500;

    public const int TRUCK_BUILD_COST = 150;
    public const int FREIGHTER_BUILD_COST = 300;


    // ------------------- Market Cap -------------------

    public const int ROAD_MARKET_CAP = (int)(0.7f * ROAD_BUILD_COST);
    public const int CANAL_MARKET_CAP = (int)(1.0f * BASE_CANAL_BUILD_COST);

    public const int PORT_MARKET_CAP = (int)(0.9f * PORT_BUILD_COST);

    public const int TRUCK_MARKET_CAP = (int)(0.7f * TRUCK_BUILD_COST);
    public const int FREIGHTER_MARKET_CAP = (int)(0.7f * FREIGHTER_BUILD_COST);


    // ------------------- Pathfinding -------------------

    public const int MAX_PRIORITYS_FOR_PATHFINDING = 4;

    public const int ROAD_TRAVERSAL_COST_PUBLIC = 0;
    public const int ROAD_TRAVERSAL_COST_OWN = 0;
    public const int ROAD_TRAVERSAL_COST_ENEMY = 1;

    public const float ROAD_SPEED_MULTIPLIER = 1.0f;

    public const int CANAL_TRAVERSAL_COST_PUBLIC = 0;
    public const int CANAL_TRAVERSAL_COST_OWN = 0;
    public const int CANAL_TRAVERSAL_COST_ENEMY = 1;

    public const float CANAL_SPEED_MULTIPLIER = 1.0f;


    // ------------------- Networking -------------------

    public const int MAX_NETTO_BYTES_PER_RPC = 1000;


    // ------------------- Rendering -------------------

    public static readonly Color ROAD_BLUEPRINT_COLOR = Color.orange;
    public static readonly Color ROAD_BLUEPRINT_PREVIEW_COLOR = Color.yellow;

    public struct OutlineData
    {
        public Color OutlineColor;
        public Color InnerColor;
        public int TextureId;
    }

    public static OutlineData TRANSPARENT_OUTLINE = new OutlineData()
    {
        OutlineColor = new Color(0, 0, 0, 0),
        InnerColor = new Color(0, 0, 0, 0),
        TextureId = 0,
    };

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

    public static OutlineData SELECTED_OUTLINE = new OutlineData()
    {
        OutlineColor = new Color(0f, 0.5f, 1.0f, 1f),
        InnerColor = new Color(0, 0, 0, 0),
        TextureId = 0,
    };

    public static OutlineData SELECTED_OUTLINE_FILLED_IN = new OutlineData()
    {
        OutlineColor = SELECTED_OUTLINE.OutlineColor,
        InnerColor = new Color(0f, 0.5f, 1.0f, 0.5f),
        TextureId = 0,
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

    public static OutlineData FASTEST_ROAD_OUTLINE = new OutlineData()
    {
        OutlineColor = new Color(0, 0, 0, 0),
        InnerColor = Color.orange,
        TextureId = 1,
    };

    public static OutlineData FASTEST_ROAD_OUTLINE_HOVERED = new OutlineData()
    {
        OutlineColor = Color.orange,
        InnerColor = Color.orange,
        TextureId = 1,
    };

    public static OutlineData CHEAPEST_ROAD_OUTLINE = new OutlineData()
    {
        OutlineColor = new Color(0, 0, 0, 0),
        InnerColor = Color.green,
        TextureId = 1,
    };

    public static OutlineData CHEAPEST_ROAD_OUTLINE_HOVERED = new OutlineData()
    {
        OutlineColor = Color.green,
        InnerColor = Color.green,
        TextureId = 1,
    };
}