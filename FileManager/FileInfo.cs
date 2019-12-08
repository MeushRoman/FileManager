using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public class FileInfo
    {
        public string Name { get; set; }
        public string FileType { get; set; }
        public int Size { get; set; }
        public string FileSHA256 { get; set; }
        public Guid FileGuid { get; set; }
        public int CountChunks { get; set; }
    }
}
