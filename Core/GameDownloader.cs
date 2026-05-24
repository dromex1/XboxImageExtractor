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
        public static bool IsPaused { get; set; } = false;
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

                // Find the mediaId and download URL from the vault page
                var dlInfo = ExtractDownloadInfo(vaultPageHtml);
                if (string.IsNullOrEmpty(dlInfo.MediaId) || string.IsNullOrEmpty(dlInfo.DownloadUrl))
                {
                    prog.HasError = true;
                    prog.ErrorMessage = "Could not find download form on vault page.";
                    progress.Report(prog);
                    return;
                }

                // Step 2: GET to Vimm's dynamic download endpoint
                string finalDownloadUrl = dlInfo.DownloadUrl.Contains("?") 
                    ? $"{dlInfo.DownloadUrl}&mediaId={dlInfo.MediaId}"
                    : $"{dlInfo.DownloadUrl}?mediaId={dlInfo.MediaId}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, finalDownloadUrl))
                {
                    request.Headers.Referrer = new Uri(game.Url);

                    using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                                throw new Exception("Vimm's Lair rejected the download. You can only download ONE game at a time. Please wait for the current download to finish, or your IP limit is reached.");
                            
                            response.EnsureSuccessStatusCode();
                        }

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
                                while (IsPaused && !ct.IsCancellationRequested)
                                    await Task.Delay(200, ct);

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

                        // Register in tracker
                        MyDownloadsManager.RegisterDownload(destPath);
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

        private static (string MediaId, string DownloadUrl) ExtractDownloadInfo(string html)
        {
            string mediaId = null;
            string downloadUrl = null;

            // Find mediaId
            var inputRegex = new Regex(@"name=""mediaId""[^>]*value=""(\d+)""", RegexOptions.IgnoreCase);
            var match = inputRegex.Match(html);
            if (match.Success) 
                mediaId = match.Groups[1].Value;

            // Find form action
            var formActionRegex = new Regex(@"<form[^>]*action=""([^""]+)""[^>]*id=""dl_form""", RegexOptions.IgnoreCase);
            var actionMatch = formActionRegex.Match(html);
            if (actionMatch.Success)
            {
                downloadUrl = actionMatch.Groups[1].Value;
                if (downloadUrl.StartsWith("//"))
                    downloadUrl = "https:" + downloadUrl;
                else if (downloadUrl.StartsWith("/"))
                    downloadUrl = "https://vimm.net" + downloadUrl;
            }

            return (mediaId, downloadUrl);
        }
    }
}
