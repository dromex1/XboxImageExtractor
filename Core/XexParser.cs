using System;
using System.IO;
using System.Text;

namespace XboxImageExtractor.Core
{
    public class XexInfo
    {
        public string TitleId { get; set; }
        public string MediaId { get; set; }
        public string TitleName { get; set; } 
    }

    public static class XexParser
    {
        public static XexInfo Parse(byte[] xexData)
        {
            if (xexData == null || xexData.Length < 0x24)
                throw new Exception("XEX file is too short.");

            using (var ms = new MemoryStream(xexData))
            using (var reader = new BinaryReader(ms))
            {
                // Magic "XEX2" (Big Endian)
                byte[] magic = reader.ReadBytes(4);
                if (Encoding.ASCII.GetString(magic) != "XEX2")
                {
                    throw new Exception("Invalid header. Expected XEX2.");
                }

                // Odczyt flag
                reader.ReadUInt32(); // module flags
                reader.ReadUInt32(); // pe offset
                reader.ReadUInt32(); // reserved
                uint certOffset = ReverseBytes(reader.ReadUInt32()); // Certificate offset

                int numHeaders = (int)ReverseBytes(reader.ReadUInt32());
                
                string titleId = "00000000";
                string mediaId = "00000000";
                long executionInfoOffset = -1;

                // Przeszukiwanie nagłówków opcjonalnych po wskaźnik wykonawczy (Execution Info)
                for (int i = 0; i < numHeaders; i++)
                {
                    uint key = ReverseBytes(reader.ReadUInt32());
                    uint val = ReverseBytes(reader.ReadUInt32());

                    // Execution Info Structure ID to 0x00040006
                    if (key == 0x00040006)
                    {
                        executionInfoOffset = val; // Pointer do Execution Info
                        break;
                    }
                }

                if (executionInfoOffset > 0 && executionInfoOffset < xexData.Length)
                {
                    ms.Position = executionInfoOffset;
                    
                    reader.ReadUInt32(); // Media ID (część 1) 
                    
                    // Właściwy TitleID znajduje się po pierwszych kilku polach execution info
                    ms.Position = executionInfoOffset + 0x04;
                    uint ver = ReverseBytes(reader.ReadUInt32()); 
                    uint tId = ReverseBytes(reader.ReadUInt32()); // Title ID (przesunięty o 0x08)
                    uint mId = ReverseBytes(reader.ReadUInt32()); // Media ID (przesunięty o 0x0C) zależy od formatu

                    ms.Position = executionInfoOffset;
                    reader.ReadUInt32(); 
                    reader.ReadUInt32();
                    tId = ReverseBytes(reader.ReadUInt32());
                    mId = ReverseBytes(reader.ReadUInt32()); // Real Media ID
                    
                    titleId = tId.ToString("X8");
                    mediaId = mId.ToString("X8");
                }

                // Rozszerzone Title ID z Certyfikatu Xbox360 (Jeżeli opcjonalny nagłówek zawiódł)
                if (titleId == "00000000" && certOffset > 0 && certOffset < xexData.Length)
                {
                    ms.Position = certOffset;
                    // Certyfikat XEX2 to skomplikowana struktura. Omijamy nagłówek klucza RSA (0x100) itp.
                    // Omijamy pola by dotrzeć do Title ID (zazwyczaj offset 0x14 lub 0x10 wewnątrz certyfikatu)
                    ms.Position = certOffset + 0x08;
                    uint certTitle = ReverseBytes(reader.ReadUInt32());
                    if (certTitle != 0) titleId = certTitle.ToString("X8");
                }

                return new XexInfo 
                { 
                    TitleId = titleId, 
                    MediaId = mediaId,
                    TitleName = "Unknown (Requires SPA Parse)" 
                };
            }
        }

        private static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
    }
}
