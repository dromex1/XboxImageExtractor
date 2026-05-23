using System;
using System.IO;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public class ExtractionProgress
    {
        public string CurrentFileName { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int ExtractedFiles { get; set; }
        public long CurrentFileTotalBytes { get; set; }
        public long CurrentFileExtractedBytes { get; set; }
        public long OverallTotalBytes { get; set; }
        public long OverallExtractedBytes { get; set; }
    }

    public class GdfxExtractor
    {
        private const int BufferSize = 1024 * 1024 * 4; // Bufor 4MB!
        private const int SectorSize = 2048;

        public async Task ExtractAsync(
            string imagePath, 
            GdfxEntry rootEntryToExtract, 
            string destinationFolder, 
            IProgress<ExtractionProgress> progress)
        {
            var progressData = new ExtractionProgress();
            
            await Task.Run(() => CalculateTotals(rootEntryToExtract, progressData));
            
            using (var isoStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await ExtractInternalAsync(isoStream, rootEntryToExtract, destinationFolder, progressData, progress);
            }
        }

        private void CalculateTotals(GdfxEntry entry, ExtractionProgress data)
        {
            if (entry.IsDirectory && entry.Name != ".." && entry.Name != ".")
            {
                foreach (var child in entry.Children)
                {
                    CalculateTotals(child, data);
                }
            }
            else if (!entry.IsDirectory)
            {
                data.TotalFiles++;
                data.OverallTotalBytes += entry.Size;
            }
        }

        private async Task ExtractInternalAsync(
            FileStream isoStream, 
            GdfxEntry entry, 
            string destPath, 
            ExtractionProgress data, 
            IProgress<ExtractionProgress> progress)
        {
            if (entry.Name == "/" || entry.Name == "")
            {
                 // Głębsza struktura root
                 foreach(var child in entry.Children)
                 {
                     await ExtractInternalAsync(isoStream, child, destPath, data, progress);
                 }
                 return;
            }
            
            string currentDestPath = Path.Combine(destPath, entry.Name);
            
            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(currentDestPath);
                foreach(var child in entry.Children)
                {
                    await ExtractInternalAsync(isoStream, child, currentDestPath, data, progress);
                }
            }
            else
            {
                data.CurrentFileName = entry.Name;
                data.CurrentFileTotalBytes = entry.Size;
                data.CurrentFileExtractedBytes = 0;
                progress?.Report(data);

                long startOffset = entry.AbsoluteOffset;
                isoStream.Position = startOffset;
                
                using (var destStream = new FileStream(currentDestPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
                {
                    byte[] buffer = new byte[BufferSize];
                    long bytesRemaining = entry.Size;

                    int lastReportTime = Environment.TickCount;

                    while (bytesRemaining > 0)
                    {
                        int bytesToRead = (int)Math.Min(buffer.Length, bytesRemaining);
                        int bytesRead = await isoStream.ReadAsync(buffer, 0, bytesToRead);
                        if (bytesRead == 0) break; // Zakończenie strumienia
                        
                        await destStream.WriteAsync(buffer, 0, bytesRead);
                        
                        bytesRemaining -= bytesRead;
                        data.CurrentFileExtractedBytes += bytesRead;
                        data.OverallExtractedBytes += bytesRead;
                        
                        // Limitujemy aktualizacje UI np. do 25 fps by nie zablokować wątku glownego zdarzeniami
                        if (Environment.TickCount - lastReportTime > 40)
                        {
                            progress?.Report(data);
                            lastReportTime = Environment.TickCount;
                        }
                    }
                    
                    // Finalny report dla ukończonego pliku
                    progress?.Report(data);
                }
                
                data.ExtractedFiles++;
                progress?.Report(data);
            }
        }
    }
}
