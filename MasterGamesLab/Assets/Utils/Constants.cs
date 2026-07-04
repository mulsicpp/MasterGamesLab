using Map.Infrastructure;
using System;
using Map.GeometryGeneration;
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

    public const byte MAX_TRUCKS_PER_PLAYER = 16;
    public const byte MAX_TRUCK_COUNT = MAX_TRUCKS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const float TRUCK_BASE_SPEED_TPS = 1.0f;

    public const byte MAX_FREIGHTERS_PER_PLAYER = 8;
    public const byte MAX_FREIGHTER_COUNT = MAX_FREIGHTERS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const float FREIGHTER_BASE_SPEED_TPS = 0.6f;

    public const int TRUCK_LOADING_COST_ENEMY = 40;
    public const int TRUCK_UNLOADING_COST_ENEMY = 80;

    public const int MAX_VEHICLE_ACTION_COUNT_PER_VEHICLE = 64;


    // ------------------- Structures -------------------

    public const byte MAX_PRODUCER_COUNT = 64;
    public const byte MAX_CONSUMER_COUNT = 128;

    public const byte MAX_PORTS_PER_PLAYER = 12;
    public const byte MAX_GARAGES_PER_PLAYER = 1;

    public const byte MAX_PORT_COUNT = MAX_PORTS_PER_PLAYER * MAX_PLAYER_COUNT;
    public const byte MAX_GARAGE_COUNT = MAX_GARAGES_PER_PLAYER * MAX_PLAYER_COUNT;


    // ------------------- Producer & Consumer Logic -------------------

    public const int GOOD_COMMON_BASE_PAYOUT = 0;
    public const int GOOD_UNCOMMON_BASE_PAYOUT = 0;
    public const int GOOD_RARE_BASE_PAYOUT = 0;
    public const int GOOD_EPIC_BASE_PAYOUT = 0;
    public const int GOOD_LEGENDARY_BASE_PAYOUT = 0;
    public const int NORMAL_SHIPPING_COST = 10;
    public const int WATER_SHIPPING_COST = 30;

    public const int MIN_RANDOM_COST = 0;
    public const int MAX_RANDOM_COST = 0;

    public const int MAX_PAYOUT = 1000;

    public const float FOREIGN_GOOD_PAYOUT_FACTOR = 3.0f;

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

    public const float MIN_CONSUMER_PAYOUT_INCREASE_FACTOR = 1.02f;
    public const float MAX_CONSUMER_PAYOUT_INCREASE_FACTOR = 1.05f;

    public const int MIN_CONSUMER_PAYOUT_INCREASE_BASE = 10;
    public const int MAX_CONSUMER_PAYOUT_INCREASE_BASE = 50;


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

    public static int RoadBuildCost(int n) => 10 + (n / 40) * 5;
    public static int CanalBuildCost(int n) => 20 + (n / 10) * 5;

    public static int PortBuildCost(int n) => 200 + n * 100;

    public static int TruckBuildCost(int n) => 75 * n;
    public static int FreighterBuildCost(int n) => 100 + n * 50;


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
    public const int ROAD_TRAVERSAL_COST_ENEMY = 2;

    public const float ROAD_SPEED_MULTIPLIER = 1.0f;

    public const int CANAL_TRAVERSAL_COST_PUBLIC = 0;
    public const int CANAL_TRAVERSAL_COST_OWN = 0;
    public const int CANAL_TRAVERSAL_COST_ENEMY = 4;

    public const float CANAL_SPEED_MULTIPLIER = 2.5f;


    // ------------------- Networking -------------------

    public const int MAX_NETTO_BYTES_PER_RPC = 1000;


    // ------------------- Rendering -------------------
    [Serializable]
    public enum OutlineTextures
    {
        Clear = 0,
        Full = 1,
        Checkerboard = 2,
        HorizontalLines = 3,
        VerticalLines = 4,
        DiagonalLines = 5,
        DiagonalLinesMirrored = 6,
        Hatching = 7,
        Circles = 8,
        Waves = 9,
    }

    [Serializable]
    public struct OutlineData
    {
        public Color outlineColor;
        public Color innerColor;
        public OutlineTextures textureId;
    }

    public static OutlineData TransparentOutline = new OutlineData()
    {
        outlineColor = new Color(0, 0, 0, 0),
        innerColor = new Color(0, 0, 0, 0),
        textureId = OutlineTextures.Clear,
    };

    public static OutlineData HoverOutlineClear => new OutlineData()
    {
        outlineColor = GeometriesManager.Instance.hoverOutlineColor,
        innerColor = GeometriesManager.Instance.hoverInnerColor,
        textureId = OutlineTextures.Clear,
    };

    public static OutlineData HoverOutlineHatching => new OutlineData()
    {
        outlineColor = GeometriesManager.Instance.hoverOutlineColor,
        innerColor = GeometriesManager.Instance.hoverInnerColor,
        textureId = OutlineTextures.Hatching,
    };

    public static OutlineData TRUCK_DRIVE_TARGET_OUTLINE = new OutlineData()
    {
        outlineColor = Color.magenta,
        innerColor = new Color(0, 0, 0, 0),
        textureId = 0,
    };

    public static OutlineData FREIGHTER_DRIVE_TARGET_OUTLINE = TRUCK_DRIVE_TARGET_OUTLINE;

    public static OutlineData LOAD_TARGET_OUTLINE = new OutlineData()
    {
        outlineColor = Color.magenta,
        innerColor = new Color(0, 0, 0, 0),
        textureId = 0,
    };

    public static OutlineData UNLOAD_TARGET_OUTLINE = LOAD_TARGET_OUTLINE;
    public static OutlineData WAIT_TARGET_OUTLINE = LOAD_TARGET_OUTLINE;
}