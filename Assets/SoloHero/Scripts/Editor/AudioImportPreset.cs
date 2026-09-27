using UnityEditor;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// E8-13: audio under Assets/SoloHero/Audio. Music (bgm_*) streams as Vorbis so it never sits decoded in memory;
    /// effects (sfx_*) are mono ADPCM decoded on load so they start without latency. Change this file, not importers.
    /// </summary>
    public sealed class AudioImportPreset : AssetPostprocessor
    {
        private const string AudioRoot = "Assets/SoloHero/Audio/";

        private void OnPreprocessAudio()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(AudioRoot)) return;

            var importer = (AudioImporter)assetImporter;
            bool music = System.IO.Path.GetFileName(path).StartsWith("bgm_");
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (music)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.5f;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.forceToMono = true;
            }

            importer.defaultSampleSettings = settings;
        }
    }
}
