using UnityEngine;

using System;
using System.Collections.Generic;
using Map;



public struct PathScore : IComparable<PathScore>
{
    private unsafe fixed long slots[Constants.MAX_PRIORITYS_FOR_PATHFINDING];

    public long this[int index]
    {
        get
        {
            if (index < 0 || index >= Constants.MAX_PRIORITYS_FOR_PATHFINDING) throw new IndexOutOfRangeException();
            unsafe { return slots[index]; }
        }
        set
        {
            if (index < 0 || index >= Constants.MAX_PRIORITYS_FOR_PATHFINDING) throw new IndexOutOfRangeException();
            unsafe { slots[index] = value; }
        }
    }

    public int CompareTo(PathScore other)
    {
        for (int i = 0; i < Constants.MAX_PRIORITYS_FOR_PATHFINDING; i++)
        {
            long mine = this[i];
            long theirs = other[i];
            if (mine < theirs) return -1;
            if (mine > theirs) return 1;
        }
        return 0;
    }

    public static PathScore operator +(PathScore a, PathScore b)
    {
        PathScore result = new PathScore();
        for (int i = 0; i < Constants.MAX_PRIORITYS_FOR_PATHFINDING; i++)
        {
            result[i] = a[i] + b[i];
        }
        return result;
    }
}


public class MovementProfile
{
    private readonly List<Pathfinding.RuleMapping> _rules = new();

    public Func<Tile, Tile, bool> IsHardBlocked { get; set; }

    public List<Pathfinding.RuleMapping> GetRules() => _rules;

    public void AddPriorityRule(int lane, Pathfinding.PathfindingRule rule)
    {
        _rules.Add(new Pathfinding.RuleMapping
        {
            RuleExecutable = rule,
            TargetSlot = lane
        });
    }
}


