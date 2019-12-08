using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public class SendsLog
    {
        public string fileName { get; set; }        
        public bool[] ChanksInfo { get; set; }
        public bool SendingStarted { get; set; }
        public bool Send { get; set; }
    }
}
