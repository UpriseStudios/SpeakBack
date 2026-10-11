using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Facts read from a built APK, so build checks verify the artefact rather than the build inputs:
    /// the number of packaged scenes (assets/bin/Data/levelN), and whether IL2CPP metadata contains the
    /// SpeakBack.Dev assembly (it must not in release builds).
    /// </summary>
    public sealed class ApkContents
    {
        private const string MetadataEntry = "assets/bin/Data/Managed/Metadata/global-metadata.dat";
        private static readonly Regex LevelEntry = new Regex(@"^assets/bin/Data/level\d+$");
        private static readonly byte[] DevMarker = Encoding.ASCII.GetBytes("SpeakBack.Dev");

        private ApkContents(int sceneCount, bool containsDevCode)
        {
            SceneCount = sceneCount;
            ContainsDevCode = containsDevCode;
        }

        /// <summary>Scenes packaged in the APK.</summary>
        public int SceneCount { get; }

        /// <summary>Whether the SpeakBack.Dev assembly or namespace appears in IL2CPP metadata.</summary>
        public bool ContainsDevCode { get; }

        /// <summary>Opens <paramref name="apkPath"/> and inspects it.</summary>
        public static ApkContents Read(string apkPath)
        {
            using (var file = File.OpenRead(apkPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                var scenes = archive.Entries.Count(e => LevelEntry.IsMatch(e.FullName));
                var metadata = archive.GetEntry(MetadataEntry)
                               ?? throw new InvalidDataException($"{MetadataEntry} not found; is this an IL2CPP build?");
                using (var stream = metadata.Open())
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return new ApkContents(scenes, IndexOf(memory.ToArray(), DevMarker) >= 0);
                }
            }
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (var i = 0; i <= haystack.Length - needle.Length; i++)
            {
                var match = true;
                for (var j = 0; j < needle.Length && match; j++) match = haystack[i + j] == needle[j];
                if (match) return i;
            }

            return -1;
        }
    }
}
