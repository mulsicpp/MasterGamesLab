
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class Distribution<T>
    {
        (T, float)[] cutoffs;
        public Distribution(IReadOnlyList<(T, float)> probabilities)
        {
            cutoffs = new (T, float)[probabilities.Count];

            float accum = 0;

            for (int i = 0; i < cutoffs.Length; i++)
            {
                accum += probabilities[i].Item2;
                cutoffs[i] = (probabilities[i].Item1, accum);
            }

            for (int i = 0; i < cutoffs.Length; i++)
            {
                cutoffs[i].Item2 *= accum;
            }

        }

        public T Get()
        {
            var value = Random.value;

            foreach(var (val, cutoff) in cutoffs)
            {
                if (value < cutoff) return val;
            }
            return cutoffs[^1].Item1;
        }
    }
}