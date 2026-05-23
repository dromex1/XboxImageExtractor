using System.Collections.Generic;

namespace XboxImageExtractor.Core
{
    public class GdfxEntry
    {
        public ushort LeftIndex { get; set; }
        public ushort RightIndex { get; set; }
        public uint StartSector { get; set; }
        public uint Size { get; set; }
        public byte Attributes { get; set; }
        public byte NameLength { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public bool IsDirectory => (Attributes & 0x10) != 0;
        
        public List<GdfxEntry> Children { get; set; } = new List<GdfxEntry>();
        public long AbsoluteOffset { get; set; }
    }
}
