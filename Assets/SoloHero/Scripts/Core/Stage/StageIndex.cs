using System;

namespace SoloHero.Core.Stage
{
    public static class StageIndex
    {
        public static int ToGlobal(int chapter, int stageNumber, int stagesPerChapter)
        {
            if (stagesPerChapter < 1) throw new ArgumentOutOfRangeException(nameof(stagesPerChapter));
            if (chapter < 1) chapter = 1;
            if (stageNumber < 1) stageNumber = 1;
            return (chapter - 1) * stagesPerChapter + stageNumber;
        }

        public static void FromGlobal(int g, int stagesPerChapter, out int chapter, out int stageNumber)
        {
            if (stagesPerChapter < 1) throw new ArgumentOutOfRangeException(nameof(stagesPerChapter));
            if (g < 1) g = 1;
            chapter = (g - 1) / stagesPerChapter + 1;
            stageNumber = (g - 1) % stagesPerChapter + 1;
        }

        public static bool IsBoss(int g, int stagesPerChapter)
        {
            if (stagesPerChapter < 1) return false;
            if (g < 1) return false;
            return g % stagesPerChapter == 0;
        }

        public static int ThemeIndex(int chapter, int themeCount)
        {
            if (themeCount < 1) throw new ArgumentOutOfRangeException(nameof(themeCount));
            if (chapter < 1) chapter = 1;
            return (chapter - 1) % themeCount;
        }

        public static int PreviousNormalStage(int bossG)
        {
            if (bossG < 2) return 1;
            return bossG - 1;
        }
    }
}
