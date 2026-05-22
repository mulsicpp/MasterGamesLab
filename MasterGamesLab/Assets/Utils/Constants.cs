using System.Drawing;
using UnityEngine.UIElements;

public static class Constants
{
    public const int MIN_PLAYER_COUNT = 2;
    public const int MAX_PLAYER_COUNT = 4;

    public static readonly Color[] PLAYER_COLORS = { Color.Red, Color.Blue, Color.Yellow, Color.Green };
    public const uint PLAYER_START_MONEY = 100;

    public const uint MAX_TRUCKS_PER_PLAYER = 64;
    public const uint MAX_FREIGHTERS_PER_PLAYER = 16;


    public const int MAX_EDGES_PER_RPC = 32;
}
