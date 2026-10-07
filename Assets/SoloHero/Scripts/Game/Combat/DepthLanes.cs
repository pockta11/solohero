using SoloHero.Core.Combat;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// 2.5D battle view (D-091), view only: enemies stand on depth lanes spread over the floor plane (Core stays 1-D
    /// on X), and every character sorts by its lane so the one nearer the viewer (lower on screen) draws in front.
    /// A wave of six (D-107) takes six different lanes; the hero and bosses keep the middle lane.
    /// </summary>
    public static class DepthLanes
    {
        /// <summary>
        /// World Y of each lane relative to the hero's ground line (positive = farther back). D-109: eight lanes for
        /// the pack of eight; neighbours in a wave sit far apart so the tight line stays readable.
        /// </summary>
        private static readonly float[] Lanes = { 0f, 0.5f, -0.4f, 0.25f, -0.2f, 0.55f, -0.45f, 0.12f };

        private const int OrderBase = 100;
        private const float OrderPerUnit = 20f;

        public static float For(EnemyBrain enemy)
        {
            if (enemy == null || enemy.IsBoss) return 0f;
            return ForIndex(enemy.SpawnIndex);
        }

        /// <summary>The lane of the n-th enemy of a stage (D-110 shots fly at their shooter's lane).</summary>
        public static float ForIndex(int spawnIndex)
        {
            int i = spawnIndex < 0 ? 0 : spawnIndex;
            return Lanes[i % Lanes.Length];
        }

        /// <summary>
        /// Size on screen by lane: the back lane is drawn a little smaller, the front lane a little larger. The
        /// camera renders into the 270x480 pixel target first, so a scaled sprite still lands on the pixel grid.
        /// </summary>
        public static float Scale(float y) => 1f - y * ScalePerUnit;

        private const float ScalePerUnit = 0.2f;

        /// <summary>Sorting order for a character standing at world Y: lower on screen draws on top.</summary>
        public static int Order(float y) => OrderBase - Mathf.RoundToInt(y * OrderPerUnit);
    }
}
