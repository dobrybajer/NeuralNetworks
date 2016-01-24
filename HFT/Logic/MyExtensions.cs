using System;
using System.Collections.Generic;

namespace HFT.Logic
{
    public static class MyExtensions
    {
        private static int? _seed;
        private static readonly Random SeedGenerator = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            if (_seed == null)
                _seed = SeedGenerator.Next();

            var rng = new Random(_seed.Value);
            var n = list.Count;

            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ResetStableShuffle()
        {
            _seed = null;
        }
    }
}
