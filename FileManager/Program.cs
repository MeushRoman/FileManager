//using Newtonsoft.Json;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager
{
   

    class Program
    {
        static void Main(string[] args)
        {

            FileManager fm = new FileManager();
            string path = @"C:\test\";
            fm.CheckDirectoryForNewFiles(path);
            fm.start();
            //fm.ReadingFile("222.mp4");
        }
    }
}
