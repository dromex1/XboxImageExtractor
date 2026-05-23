using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XboxImageExtractor.Core
{
    public class GdfxArchive : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly BinaryReader _reader;
        private const int SectorSize = 2048;
        private const string MagicString = "MICROSOFT*XBOX*MEDIA";
        
        public string ImagePath { get; }
        public GdfxEntry RootDirectory { get; private set; }
        public long MainSectorOffset { get; private set; }
        public long PartitionBaseOffset { get; private set; }
        public long TotalImageSize { get; private set; }
        public int FileCount { get; private set; }
        public int DirectoryCount { get; private set; }
        
        public GdfxArchive(string path)
        {
            ImagePath = path;
            // BARDZO WAŻNE: użycie useAsync: false i dużego bufora 4MB by małe odczyty nie alokowały tysięcy Tasków pod spodem (.NET overhead).
            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024 * 4, false);
            _reader = new BinaryReader(_fileStream, Encoding.ASCII, true);
            TotalImageSize = _fileStream.Length;
        }

        public async Task LoadAsync()
        {
            await Task.Run(() => 
            {
                MainSectorOffset = FindMagicOffset();
                if (MainSectorOffset < 0)
                    throw new InvalidDataException("Valid GDFX header could not be found in the provided ISO.");

                PartitionBaseOffset = MainSectorOffset - 0x10000;

                _fileStream.Position = MainSectorOffset;
                // Ominięcie magic string z nagłówka
                _fileStream.Position += 20;

                uint rootDirSector = _reader.ReadUInt32();
                uint rootDirSize = _reader.ReadUInt32();

                RootDirectory = new GdfxEntry
                {
                    Name = "/",
                    Attributes = 0x10, // Folder
                    StartSector = rootDirSector,
                    Size = rootDirSize,
                    AbsoluteOffset = PartitionBaseOffset + ((long)rootDirSector * SectorSize)
                };

                ReadDirectory(RootDirectory);
            });
        }
        
        private long FindMagicOffset()
        {
            byte[] magicBytes = Encoding.ASCII.GetBytes(MagicString);
            
            // Popularne offsety gdzie nagłówek pojawia się od razu (XISO, XGD2, XGD3, itp.)
            long[] commonOffsets = { 0x10000, 0xFDA0000, 0x1FB30000, 0x2090000, 0x40B0000, 0x2080000, 0x2020000, 0x0 };
            foreach(var offset in commonOffsets)
            {
                if (CheckMagicAtOffset(offset, magicBytes))
                    return offset;
            }
            
            // Ultra-szybkie przeszukiwanie blokowe jeśli plik ma niestandardowy układ struktury GDFX
            _fileStream.Position = 0;
            long endPosition = Math.Min(_fileStream.Length, 1024L * 1024L * 1000L); // Szukaj w pierwszym 1GB
            int chunkSize = 1024 * 1024 * 4; // Bufor 4MB
            byte[] searchBuffer = new byte[chunkSize];
            
            long currentPos = 0;
            while(currentPos < endPosition)
            {
                _fileStream.Position = currentPos;
                int bytesRead = _fileStream.Read(searchBuffer, 0, chunkSize);
                if (bytesRead < magicBytes.Length) break;
                
                // GDFX header musi znajdować się na początku jakiegoś sektora (skok co 2048 bajtów)
                for(int i = 0; i <= bytesRead - magicBytes.Length; i += SectorSize)
                {
                    bool match = true;
                    for(int j = 0; j < magicBytes.Length; j++)
                    {
                        if (searchBuffer[i + j] != magicBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    
                    if (match) 
                        return currentPos + i;
                }
                
                // Kontynuuj poszukiwania
                currentPos += bytesRead;
            }
            
            return -1;
        }

        private bool CheckMagicAtOffset(long offset, byte[] magic)
        {
            if (offset + magic.Length > _fileStream.Length) return false;
            
            _fileStream.Position = offset;
            byte[] buffer = _reader.ReadBytes(magic.Length);
            
            for(int i = 0; i < magic.Length; i++)
            {
                if (buffer[i] != magic[i]) return false;
            }
            
            return true;
        }

        private void ReadDirectory(GdfxEntry directory)
        {
            if (directory.Size <= 0 || directory.Size > 1024 * 1024 * 10) return; // Zabezpieczenie przed śmieciami w ISO (Katalogi są zawsze małe)
            
            // Parsuj strukturę drzewa binarnego bieżącego folderu z wykorzystaniem przeliczonego offsetu absolutnego
            List<GdfxEntry> entries = ParseDirectoryTree(directory.AbsoluteOffset);
            
            foreach(var entry in entries)
            {
                if (entry.IsDirectory && entry.StartSector != directory.StartSector && entry.Size > 0)
                {
                    ReadDirectory(entry);
                    DirectoryCount++;
                }
                else
                {
                    FileCount++;
                }
            }
            
            directory.Children = entries;
        }

        private List<GdfxEntry> ParseDirectoryTree(long directoryBaseOffset)
        {
            var entries = new List<GdfxEntry>();
            var queue = new Queue<long>();
            var visited = new HashSet<long>(); // Zabezpieczenie przed pętlami w uszkodzonym ISO
            
            queue.Enqueue(0);
            
            while (queue.Count > 0)
            {
                long relativeOffset = queue.Dequeue();
                
                if (visited.Contains(relativeOffset))
                    continue;
                    
                visited.Add(relativeOffset);
                
                _fileStream.Position = directoryBaseOffset + relativeOffset;
                
                ushort leftIndex = _reader.ReadUInt16();
                ushort rightIndex = _reader.ReadUInt16();
                uint startSector = _reader.ReadUInt32();
                uint size = _reader.ReadUInt32();
                byte attributes = _reader.ReadByte();
                byte nameLength = _reader.ReadByte();
                
                if (nameLength == 0 || nameLength == 255) continue; // Garbage / EOF
                if (startSector == 0 && size == 0 && nameLength == 0) continue;
                
                byte[] nameBytes = _reader.ReadBytes(nameLength);
                
                var entry = new GdfxEntry
                {
                    LeftIndex = leftIndex,
                    RightIndex = rightIndex,
                    StartSector = startSector,
                    Size = size,
                    Attributes = attributes,
                    NameLength = nameLength,
                    Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0'),
                    AbsoluteOffset = PartitionBaseOffset + ((long)startSector * SectorSize)
                };
                
                entries.Add(entry);
                
                if (leftIndex > 0) queue.Enqueue(leftIndex * 4L);
                if (rightIndex > 0) queue.Enqueue(rightIndex * 4L);
            }
            
            // Sortowanie po opcjonalnej nazwie
            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _fileStream?.Dispose();
        }
    }
}
