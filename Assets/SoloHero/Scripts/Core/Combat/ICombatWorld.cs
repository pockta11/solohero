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
    }
}
