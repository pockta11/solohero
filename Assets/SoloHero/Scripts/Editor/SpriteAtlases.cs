using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// D-149 sprite atlases (Sprite Atlas V2). Every UI sprite used to be its own texture, so uGUI broke a batch at
    /// nearly every element and the HUD alone drew ~115 of the battle's ~130 draw calls (E9-19 budget 120). Three UI
    /// atlases, split by how the sprites are filtered: UiHd (the smooth skin v3 frames and HUD icons), UiItems
    /// (equipment and skill icons) and UiPixel (the old pixel UI and the 16 px icons, which the world coins use too).
    /// Vfx holds every effect sheet, so a skill burst (D-146: clips, glows, circles, pillars, sparks) draws from one
    /// texture per material instead of one per sheet. The source sprites stay as they are; the atlases only change
    /// which texture they are drawn from. ArtBuilder rebuilds them; Tools > Setup > Build Sprite Atlases does it alone.
    /// </summary>
    public static class SpriteAtlases
    {
        private const string Dir = "Assets/SoloHero/Art/Atlases";
        private const string Ui = "Assets/SoloHero/Art/UI";

        private sealed class Spec
        {
            public string Name;
            public string[] Folders;
            public FilterMode Filter;
            public bool MipMaps;
            public int MaxSize;
        }

        private static readonly Spec[] Specs =
        {
            // The icons were mip-mapped (they shrink on buttons); the frames show at 1:1 and are unaffected by mips.
            new Spec { Name = "UiHd", Folders = new[] { Ui + "/Hd", Ui + "/Hd/Icons" }, Filter = FilterMode.Trilinear, MipMaps = true, MaxSize = 2048 },
            new Spec { Name = "UiItems", Folders = new[] { Ui + "/Hd/Equipment", Ui + "/Hd/Skills" }, Filter = FilterMode.Trilinear, MipMaps = true, MaxSize = 2048 },
            new Spec { Name = "UiPixel", Folders = new[] { Ui, Ui + "/Icons" }, Filter = FilterMode.Point, MipMaps = false, MaxSize = 1024 },
            new Spec { Name = "Vfx", Folders = new[] { "Assets/SoloHero/Art/Vfx" }, Filter = FilterMode.Point, MipMaps = false, MaxSize = 4096 },
        };

        [MenuItem("Tools/Setup/Build Sprite Atlases")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/SoloHero/Art", "Atlases");
            foreach (Spec spec in Specs) BuildOne(spec);
            AssetDatabase.SaveAssets();
            Debug.Log("[Art] sprite atlases built");
        }

        private static void BuildOne(Spec spec)
        {
            string path = Dir + "/" + spec.Name + ".spriteatlasv2";
            var asset = new SpriteAtlasAsset();
            asset.Add(Packables(spec.Folders).ToArray());
            // Saving over the same path keeps the .meta (and the GUID); the packables are rewritten every build.
            SpriteAtlasAsset.Save(asset, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            importer.includeInBuild = true;
            // No rotation or tight packing: uGUI draws sprite rects (9-slices included). Padding 4 keeps the bilinear
            // and mip-mapped samples from bleeding into neighbours.
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1, padding = 4, enableRotation = false, enableTightPacking = false, enableAlphaDilation = true
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false, generateMipMaps = spec.MipMaps, sRGB = true, filterMode = spec.Filter, anisoLevel = 1
            };
            // Uncompressed like the source sprites, so the UI looks exactly as before.
            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = spec.MaxSize;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();
        }

        /// <summary>The sprite textures directly in each folder (not its subfolders), in path order.</summary>
        private static List<Object> Packables(string[] folders)
        {
            var list = new List<Object>();
            foreach (string folder in folders)
            {
                var files = new List<string>(Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly));
                files.Sort(System.StringComparer.Ordinal);
                foreach (string file in files)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\', '/'));
                    if (texture != null) list.Add(texture);
                }
            }

            return list;
        }
    }
}
