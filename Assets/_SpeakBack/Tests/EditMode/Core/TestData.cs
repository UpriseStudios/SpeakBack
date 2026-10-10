using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Uprise.SpeakBack.Core.Tests
{
    /// <summary>
    /// Reads source-of-truth files from the repo's <c>data/</c> folder. Unity runs tests from the project
    /// root and dotnet from <c>Tools/DotnetTests/bin/…</c>, so the root is found by walking up from
    /// several starting points until <c>data/golden</c> appears.
    /// </summary>
    internal static class TestData
    {
        private static string s_root;

        public static string RepoRoot => s_root ?? (s_root = FindRoot());

        public static string ReadText(string relativePath) =>
            File.ReadAllText(Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)), Encoding.UTF8);

        private static string FindRoot()
        {
            var starts = new[]
            {
                TestContext.CurrentContext.TestDirectory,
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
            };

            foreach (var start in starts.Where(s => !string.IsNullOrEmpty(s)))
            {
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "data", "golden"))) return dir.FullName;
                }
            }

            throw new DirectoryNotFoundException("Could not find the repo root (a folder containing data/golden).");
        }
    }
}
