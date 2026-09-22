namespace SoloHero.Core.Combat
{
    public static class Targeting
    {
        public static EnemyBrain FindNearestToTheRight(ICombatWorld world, double fromX)
        {
            EnemyBrain best = null;
            double bestDist = double.MaxValue;
            int slots = world.SlotCount;
            for (int i = 0; i < slots; i++)
            {
                EnemyBrain e = world.GetSlot(i);
                if (e == null || !e.IsAlive) continue;
                if (e.X < fromX) continue;
                double dist = e.X - fromX;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = e;
                }
            }

            return best;
        }

        public static EnemyBrain FindNearestInRange(ICombatWorld world, double fromX, double range)
        {
            EnemyBrain nearest = FindNearestToTheRight(world, fromX);
            if (nearest == null) return null;
            if (nearest.X - fromX > range) return null;
            return nearest;
        }

        public static int CountInRange(ICombatWorld world, double fromX, double range)
        {
            int count = 0;
            int slots = world.SlotCount;
            for (int i = 0; i < slots; i++)
            {
                EnemyBrain e = world.GetSlot(i);
                if (e == null || !e.IsAlive) continue;
                if (e.X < fromX) continue;
                if (e.X - fromX <= range) count++;
            }

            return count;
        }
    }
}
