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

        /// <summary>
        /// UI 9-slice sprites: ui9_{name}_{border px}.png, and the D-108 smooth skin hd9_{name}_{border px}.png or
        /// hd9_{name}_{left/right}x{top/bottom}.png.
        /// </summary>
        private static readonly Regex NineSlice = new Regex(@"^(?:ui9|hd9)_[a-z0-9]+_(\d+)(?:x(\d+))?$");

        /// <summary>UI art is drawn at 1/4 of the 1080 reference width: 1 art pixel = 4 canvas units (100 / 25).</summary>
        private const int UiPpu = 25;

        /// <summary>D-108 smooth skin (Art/UI/Hd, tools/art/uigen3.py): drawn at 1 px = 1 canvas unit, filtered.</summary>
        private const int HdPpu = 100;

        /// <summary>
        /// Foot pivot per entity in frame pixels from the bottom-left, for sheets whose feet are not on the bottom-centre.
        /// The D-082 character sheets are cropped with the feet on the bottom row, centred, so none need an entry.
        /// </summary>
        private static readonly Dictionary<string, Vector2> FootPivotPx = new Dictionary<string, Vector2>();

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
                if (ui)
                {
                    bool hd = path.Contains("/UI/Hd/");
                    int ppu = hd ? HdPpu : UiPpu;
                    importer.spritePixelsPerUnit = ppu;
                    settings.spritePixelsPerUnit = ppu;
                    if (hd)
                    {
                        // The settings copy was read with Point above and is written back last, so set both.
                        // Icons (128 px, shown at 40-112 units) get mipmaps so the small sizes stay smooth.
                        bool icon = path.Contains("/UI/Hd/Icons/") || path.Contains("/UI/Hd/Equipment/") || path.Contains("/UI/Hd/Skills/");
                        FilterMode filter = icon ? FilterMode.Trilinear : FilterMode.Bilinear;
                        importer.filterMode = filter;
                        settings.filterMode = filter;
                        importer.mipmapEnabled = icon;
                        settings.mipmapEnabled = icon;
                    }
                    Match nine = NineSlice.Match(name);
                    float side = nine.Success ? int.Parse(nine.Groups[1].Value) : 0f;
                    float cap = nine.Success && nine.Groups[2].Success ? int.Parse(nine.Groups[2].Value) : side;
                    settings.spriteBorder = new Vector4(side, cap, side, cap);
                }

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
