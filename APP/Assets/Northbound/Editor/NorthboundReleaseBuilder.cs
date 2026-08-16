using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;

namespace Northbound.Editor
{
    /// <summary>Deterministic release entry point used from Unity batch mode.</summary>
    public static class NorthboundReleaseBuilder
    {
        private const string AppIconPath = "Assets/Northbound/Art/Brand/NorthboundAppIcon.icns";

        private static readonly string[] ReleaseScenes =
        {
            "Assets/Northbound/Scenes/Bootstrap.unity",
            "Assets/Northbound/Scenes/Greybridge.unity"
        };

        public static void BuildMacOS()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = ReleaseScenes,
                locationPathName = "Builds/macOS/Northbound.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });

            if (report.summary.totalErrors > 0)
            {
                throw new InvalidOperationException($"Northbound macOS build failed with {report.summary.totalErrors} errors.");
            }

            InstallMacOSApplicationIcon(report.summary.outputPath);
            UnityEngine.Debug.Log($"Northbound macOS build ready at {report.summary.outputPath} ({report.summary.totalSize} bytes).");
        }

        private static void InstallMacOSApplicationIcon(string applicationPath)
        {
            var sourcePath = Path.GetFullPath(AppIconPath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Northbound application icon is missing at {AppIconPath}.");
            }

            var destinationPath = Path.Combine(applicationPath, "Contents", "Resources", "PlayerIcon.icns");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, true);

            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/codesign",
                Arguments = $"--force --deep --sign - \"{applicationPath.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Northbound could not start codesign for the macOS build.");
                }

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Northbound macOS signing failed with exit code {process.ExitCode}.");
                }
            }
        }
    }
}
