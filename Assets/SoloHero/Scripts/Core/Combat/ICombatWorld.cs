namespace SoloHero.Core.Combat
{
    public interface ICombatWorld
    {
        double HeroX { get; }

        int SlotCount { get; }

        int AliveCount { get; }

        EnemyBrain GetSlot(int index);

        bool HasEnemyInRange(double range);

        double NearestEnemyDistance();

        EnemyBrain NearestEnemyInRange(double range);

        EnemyBrain NearestEnemyToTheRight();

        int CountEnemiesInRange(double range);

        /// <summary>Called by whoever damaged an enemy so views can show damage text. No game state changes here.</summary>
        void ReportHit(EnemyBrain target, double amount, HitKind kind);
    }
}
