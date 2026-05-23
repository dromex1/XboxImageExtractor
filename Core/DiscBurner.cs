using System;
using System.IO;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public class DiscBurner
    {
        public static async Task BurnFolderToDiscAsync(string sourceFolder, IProgress<string> statusReporter)
        {
            await Task.Run(() =>
            {
                statusReporter?.Report("Szukanie dostępnego napędu płyt...");
                Type discMasterType = Type.GetTypeFromProgID("IMAPI2.MsftDiscMaster2");
                if (discMasterType == null) throw new Exception("W systemie brak biblioteki IMAPI2 do nagrywania płyt.");
                
                dynamic discMaster = Activator.CreateInstance(discMasterType);
                if (discMaster.Count == 0)
                {
                    throw new Exception("Nie znaleziono napędów optycznych zdolnych do nagrywania płyt.");
                }

                string activeRecorderId = "";
                foreach (string id in discMaster)
                {
                    activeRecorderId = id;
                    break;
                }

                statusReporter?.Report("Inicjalizacja nagrywarki...");
                Type recorderType = Type.GetTypeFromProgID("IMAPI2.MsftDiscRecorder2");
                dynamic recorder = Activator.CreateInstance(recorderType);
                recorder.InitializeDiscRecorder(activeRecorderId);

                statusReporter?.Report("Tworzenie struktury systemu plików UDF na płytę (może chwilę zająć)...");
                Type fsiType = Type.GetTypeFromProgID("IMAPI2FS.MsftFileSystemImage");
                dynamic fsi = Activator.CreateInstance(fsiType);
                
                // UDF (4) | ISO9660 (1) | Joliet (2). Domyślnie użyjmy UDF dla konsol RGH.
                fsi.FileSystemsToCreate = 4 | 1; 
                fsi.VolumeName = "XBOX_RGH_GAME";
                fsi.ChooseImageDefaults(recorder);

                // Add source folder content to the image root
                fsi.Root.AddTree(sourceFolder, false);
                
                dynamic resultImage = fsi.CreateResultImage();
                dynamic stream = resultImage.ImageStream;

                statusReporter?.Report($"Wypalanie {sourceFolder} na płytę. Proszę czekać...");
                Type formatType = Type.GetTypeFromProgID("IMAPI2.MsftDiscFormat2Data");
                dynamic format2Data = Activator.CreateInstance(formatType);
                format2Data.Recorder = recorder;
                format2Data.ClientName = "XboxImageExtractor";
                
                // Proces nagrywania jest synchroniczny w stosunku do strumienia COM
                format2Data.Write(stream);
                statusReporter?.Report("Wypalanie zakończone pomyślnie. Wysuwanie płyty...");

                try { recorder.EjectMedia(); } catch { }
            });
        }
    }
}
