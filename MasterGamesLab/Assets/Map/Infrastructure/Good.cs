using System.Collections.Generic;
using UnityEngine;

namespace Map.Infrastructure
{
    // TODO change temporary goods
    public enum Good : byte
    {
        None,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class GoodUtils
    {
        public static readonly Good[] Goods = new Good[] { Good.Common, Good.Uncommon, Good.Rare, Good.Epic, Good.Legendary };

        public static readonly Dictionary<Good, Color> GoodColors = new() {
            { Good.Common, Color.darkGray },
            { Good.Uncommon, Color.dodgerBlue },
            { Good.Rare, Color.seaGreen },
            { Good.Epic, Color.gold },
            { Good.Legendary, Color.magenta },
        };

        public static readonly Dictionary<Good, int> GoodBasePayout = new() {
            { Good.Common, Constants.GOOD_COMMON_BASE_PAYOUT },
            { Good.Uncommon, Constants.GOOD_UNCOMMON_BASE_PAYOUT },
            { Good.Rare, Constants.GOOD_RARE_BASE_PAYOUT },
            { Good.Epic, Constants.GOOD_EPIC_BASE_PAYOUT },
            { Good.Legendary, Constants.GOOD_LEGENDARY_BASE_PAYOUT },
        };
    }
}