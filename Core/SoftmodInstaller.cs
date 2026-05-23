using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XboxImageExtractor.Core
{
    public static class SoftmodInstaller
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ZipUrl = "https://github.com/dromex1/xboxsoftmod/releases/download/soft/SOFT.zip";

        public static async Task DownloadAndInstallAsync(string targetDriveLetter, IProgress<ExtractionProgress> progress)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "SOFT.zip");
            
            try
            {
                var progData = new ExtractionProgress { CurrentFileName = "Downloading SOFT.zip..." };
                progress.Report(progData);
                
                using (var response = await _httpClient.GetAsync(ZipUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    long? totalBytes = response.Content.Headers.ContentLength;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;
                        int lastReportTick = Environment.TickCount;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                            
                            if (totalBytes.HasValue && Environment.TickCount - lastReportTick > 50)
                            {
                                progData.OverallTotalBytes = totalBytes.Value;
                                progData.OverallExtractedBytes = totalRead;
                                progress.Report(progData);
                                lastReportTick = Environment.TickCount;
                            }
                        }
                    }
                }

                progData.CurrentFileName = "Extracting RGH Softmod files to USB drive...";
                progData.OverallExtractedBytes = 0;
                progress.Report(progData);
                
                // Extract directly to Pendrive
                if (!targetDriveLetter.EndsWith("\\"))
                    targetDriveLetter += "\\";

                ZipFile.ExtractToDirectory(tempZipPath, targetDriveLetter, overwriteFiles: true);

                progData.CurrentFileName = "Success! Mod injection complete.";
                progress.Report(progData);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempZipPath))
                        File.Delete(tempZipPath);
                }
                catch { } // ignore cleanup fails
            }
        }

        public static async Task<bool> FormatDriveToFat32Async(string driveLetter, IProgress<ExtractionProgress> progress)
        {
            return await Task.Run(() => 
            {
                try
                {
                    var progData = new ExtractionProgress { CurrentFileName = $"Formatting {driveLetter} to FAT32. This may take a few minutes..." };
                    progress.Report(progData);
                    
                    var formattedDrive = driveLetter.Replace("\\", "");
                    
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "format.com",
                        Arguments = $"{formattedDrive} /FS:FAT32 /Q /V:XBOX_RGH /Y",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        return p.ExitCode == 0;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }
    }
}
