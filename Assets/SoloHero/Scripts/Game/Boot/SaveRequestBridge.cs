using SoloHero.Core.Common;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Game.Boot
{
    public sealed class SaveRequestBridge : ISaveRequester
    {
        private readonly SaveService _save;

        public SaveRequestBridge(SaveService save)
        {
            _save = save;
        }

        public void RequestSave()
        {
            if (_save == null)
                return;

            SaveDataV2 data;
            try
            {
                data = Services.Get<SaveDataV2>();
            }
            catch (System.Exception)
            {
                return;
            }

            SoloHero.Core.Common.Log.Info(SoloHero.Core.Common.LogTag.Save, "request save");
            _save.RequestSave(data);
        }
    }
}
