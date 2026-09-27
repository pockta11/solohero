using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// E8-16: every texture under Assets/SoloHero/Art gets pixel-art settings (PPU 32, Point, no compression,
    /// no mipmaps). Sheets named "{entity}_{clip}_{frames}.png" are sliced into equal frames with the entity's
    /// foot pivot (VFX sheets pivot at their centre); backgrounds and tiles become single repeatable sprites pivoted
    /// at the bottom centre, and UI sprites single sprites pivoted at the centre.
    /// Do not hand-edit importer settings under Art/ - change this file and reimport.
    /// </summary>
    public sealed class SpriteImportPreset : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/SoloHero/Art/";
        private const int Ppu = 32;

        private static readonly Regex SheetName = new Regex(@"^([a-z0-9]+)_([a-z0-9]+)_(\d+)$");

        /// <summary>Foot pivot per entity in frame pixels from the bottom-left (measured from the idle frame).</summary>
        private static readonly Dictionary<string, Vector2> FootPivotPx = new Dictionary<string, Vector2>
        {
            { "king", new Vector2(32f, 14f) },
            { "pig", new Vector2(20f, 0f) },
            { "pigr", new Vector2(20f, 0f) },
            { "boxpig", new Vector2(15f, 0f) },
            { "boxpigv", new Vector2(15f, 0f) },
            { "bombpig", new Vector2(11f, 0f) },
            { "bombpigr", new Vector2(11f, 0f) },
            { "hidepig", new Vector2(13f, 0f) },
            { "matchpig", new Vector2(14f, 0f) },
            { "kingpig", new Vector2(20f, 0f) },
            { "kingpigb", new Vector2(20f, 0f) },
            { "kingpigd", new Vector2(20f, 0f) },
            { "kingpign", new Vector2(20f, 0f) },
            { "kingpigr", new Vector2(20f, 0f) },
        };

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(ArtRoot) || path.StartsWith(ArtRoot + "Fonts/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;

            string name = Path.GetFileNameWithoutExtension(path);
            Match sheet = SheetName.Match(name);
            bool repeating = path.Contains("/Backgrounds/") || path.Contains("/Tiles/");
            bool ui = path.Contains("/UI/");
            if (repeating || ui || !sheet.Success)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                settings.spriteAlignment = (int)(ui ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
                importer.wrapMode = repeating ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.SetTextureSettings(settings);
                return;
            }

            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.spriteImportMode = SpriteImportMode.Multiple;

            int frames = int.Parse(sheet.Groups[3].Value);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int frameWidth = frames > 0 ? width / frames : width;
            bool vfx = path.Contains("/Vfx/");
            Vector2 pivotPx = vfx ? new Vector2(frameWidth / 2f, height / 2f)
                : FootPivotPx.TryGetValue(sheet.Groups[1].Value, out Vector2 p) ? p : new Vector2(frameWidth / 2f, 0f);
            var pivot = new Vector2(pivotPx.x / frameWidth, pivotPx.y / height);

            var metas = new SpriteMetaData[frames];
            for (int i = 0; i < frames; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = name + "_" + i,
                    rect = new Rect(i * frameWidth, 0, frameWidth, height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot
                };
            }

#pragma warning disable 618
            importer.spritesheet = metas;
#pragma warning restore 618
        }
    }
}
