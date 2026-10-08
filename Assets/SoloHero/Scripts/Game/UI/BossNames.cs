namespace SoloHero.Game.UI
{
    /// <summary>
    /// Chapter boss names (D-131: ten chapter looks). Chapters past the tenth reuse the names cyclically like the
    /// themes (StageIndex.ThemeIndex).
    /// </summary>
    public static class BossNames
    {
        public const int Count = 10;

        public static string KeyFor(int chapter)
        {
            int index = ((chapter - 1) % Count + Count) % Count;
            return "boss.name." + (index + 1);
        }
    }
}
