using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public class FileChunk
    {
        public string FileName { get; set; }
        public Guid FileGuid { get; set; }
        public byte[] Content { get; set; }
        public int ChunkN { get; set; }
        public int StartPosition { get; set; }
    }
}
