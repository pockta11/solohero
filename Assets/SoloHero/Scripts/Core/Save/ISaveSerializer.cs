namespace SoloHero.Core.Save
{
    public interface ISaveSerializer
    {
        string ToJson(SaveDataV2 data);

        SaveDataV2 FromV2Json(string json);

        PlayerDataV1 FromV1Json(string json);
    }
}
