using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public class DownloadProgress
    {
        public string FileName { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public int PercentComplete => TotalBytes > 0 ? (int)((double)DownloadedBytes / TotalBytes * 100) : 0;
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class GameDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static GameDownloader()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromMinutes(60); // Large ISO downloads
        }

        public static async Task DownloadGameAsync(
            VimmGame game, 
            string destinationFolder, 
            IProgress<DownloadProgress> progress,
            CancellationToken ct = default)
        {
            var prog = new DownloadProgress { FileName = $"{game.Title}.7z" };
            
            try
            {
                // Step 1: Navigate to vault page to find the actual download link/mediaId
                progress.Report(prog);

                string vaultPageHtml = await _httpClient.GetStringAsync(game.Url, ct);

                // Find the mediaId from the vault page
                string mediaId = ExtractMediaId(vaultPageHtml);
                if (string.IsNullOrEmpty(mediaId))
                {
                    prog.HasError = true;
                    prog.ErrorMessage = "Could not find download link on vault page.";
                    progress.Report(prog);
                    return;
                }

                // Step 2: POST to Vimm's download endpoint
                string downloadUrl = $"https://download2.vimm.net/download/?mediaId={mediaId}";
                
                using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
                {
                    request.Headers.Referrer = new Uri(game.Url);

                    using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        response.EnsureSuccessStatusCode();

                        long? totalBytes = response.Content.Headers.ContentLength;
                        prog.TotalBytes = totalBytes ?? 0;

                        // Determine filename from Content-Disposition or fallback
                        string fileName = prog.FileName;
                        if (response.Content.Headers.ContentDisposition?.FileName != null)
                        {
                            fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
                        }
                        prog.FileName = fileName;

                        string destPath = Path.Combine(destinationFolder, fileName);

                        using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
                        using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[65536]; // 64KB chunks
                            int bytesRead;
                            int lastReportTick = Environment.TickCount;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) != 0)
                            {
                                await fs.WriteAsync(buffer, 0, bytesRead, ct);
                                prog.DownloadedBytes += bytesRead;

                                if (Environment.TickCount - lastReportTick > 100)
                                {
                                    progress.Report(prog);
                                    lastReportTick = Environment.TickCount;
                                }
                            }
                        }

                        prog.IsComplete = true;
                        progress.Report(prog);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                prog.HasError = true;
                prog.ErrorMessage = "Download was cancelled.";
                progress.Report(prog);
            }
            catch (Exception ex)
            {
                prog.HasError = true;
                prog.ErrorMessage = ex.Message;
                progress.Report(prog);
            }
        }

        private static string ExtractMediaId(string html)
        {
            // Look for mediaId in hidden input or download form
            // Pattern: <input ... name="mediaId" ... value="12345" />
            var inputRegex = new Regex(@"name=""mediaId""[^>]*value=""(\d+)""", RegexOptions.IgnoreCase);
            var match = inputRegex.Match(html);
            if (match.Success) return match.Groups[1].Value;

            // Alternative: value first then name
            var altRegex = new Regex(@"value=""(\d+)""[^>]*name=""mediaId""", RegexOptions.IgnoreCase);
            match = altRegex.Match(html);
            if (match.Success) return match.Groups[1].Value;

            // Alternative: direct download link pattern
            var linkRegex = new Regex(@"download/?.*?mediaId=(\d+)", RegexOptions.IgnoreCase);
            match = linkRegex.Match(html);
            if (match.Success) return match.Groups[1].Value;

            return null;
        }
    }
}
