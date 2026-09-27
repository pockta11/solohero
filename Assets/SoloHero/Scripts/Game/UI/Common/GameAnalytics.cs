using SoloHero.Core.Analytics;
using SoloHero.Game.UI.Panels;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Presenter-side analytics events (E6-15): player decisions that do not pass through the stage flow (gacha,
    /// upgrades, skills, ads, offline claim). Silent when analytics is not registered (editor, tests).
    /// </summary>
    public static class GameAnalytics
    {
        public static void Log(string eventName, params AnalyticsParam[] parameters)
        {
            IAnalytics analytics = PanelServices.TryGet<IAnalytics>();
            if (analytics != null) analytics.Log(eventName, parameters);
        }
    }
}
