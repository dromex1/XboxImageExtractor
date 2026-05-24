using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace XboxImageExtractor.Core
{
    public class DownloadedGameInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public DateTime DateModified { get; set; }
    }

    public static class MyDownloadsManager
    {
        private static string GetTrackerPath()
        {
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            return Path.Combine(downloadsFolder, "XboxExtractor_Downloads.txt");
        }

        public static void RegisterDownload(string fullPath)
        {
            try
            {
                File.AppendAllText(GetTrackerPath(), fullPath + Environment.NewLine);
            }
            catch { }
        }

        public static List<DownloadedGameInfo> GetDownloadedGames()
        {
            var tracker = GetTrackerPath();
            if (!File.Exists(tracker)) return new List<DownloadedGameInfo>();

            var result = new List<DownloadedGameInfo>();
            foreach (var line in File.ReadAllLines(tracker))
            {
                var path = line.Trim();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                var fi = new FileInfo(path);
                result.Add(new DownloadedGameInfo
                {
                    FileName = fi.Name,
                    FullPath = fi.FullName,
                    FileSize = FormatBytes(fi.Length),
                    DateModified = fi.LastWriteTime
                });
            }
            return result;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }

        public static async Task ExtractAndLoadArchiveAsync(
            string archivePath,
            IProgress<int> progress,
            Action<string, bool> onIsoReady)
        {
            await Task.Run(() =>
            {
                using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var archive = ArchiveFactory.Open(archiveStream))
                {
                    var isoEntry = archive.Entries.FirstOrDefault(e =>
                        !e.IsDirectory && e.Key.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));

                    if (isoEntry == null)
                        throw new Exception("No .iso file found inside this archive!");

                    string extractFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads", "_xbox_extracted");
                    Directory.CreateDirectory(extractFolder);

                    string destinationPath = Path.Combine(extractFolder, Path.GetFileName(isoEntry.Key));

                    if (!File.Exists(destinationPath) || new FileInfo(destinationPath).Length != isoEntry.Size)
                    {
                        using (var entryStream = isoEntry.OpenEntryStream())
                        using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[81920];
                            int read;
                            long totalRead = 0;
                            long totalSize = isoEntry.Size;
                            int lastTick = Environment.TickCount;

                            while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fs.Write(buffer, 0, read);
                                totalRead += read;

                                if (Environment.TickCount - lastTick > 200)
                                {
                                    int pct = totalSize > 0 ? (int)((double)totalRead / totalSize * 100) : 0;
                                    progress.Report(pct);
                                    lastTick = Environment.TickCount;
                                }
                            }
                        }
                    }

                    progress.Report(100);

                    bool isClassic = DetermineIfClassic(destinationPath);
                    onIsoReady?.Invoke(destinationPath, isClassic);
                }
            });
        }

        private static bool DetermineIfClassic(string isoPath)
        {
            try
            {
                using (var fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buf = new byte[20];
                    fs.Seek(0x10000, SeekOrigin.Begin);
                    fs.Read(buf, 0, 20);
                    return System.Text.Encoding.ASCII.GetString(buf) == "MICROSOFT*XBOX*MEDIA";
                }
            }
            catch { return false; }
        }
    }
}
