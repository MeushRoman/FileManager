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
    [Serializable]
    public class FileChunk
    {
        public Guid FileGuid { get; set; }
        public byte[] Content { get; set; }
        public int ChunkN { get; set; }
        public int StartPosition { get; set; }
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public string FileType { get; set; }
        public int Size { get; set; }
        public string FileSHA256 { get; set; }
        public Guid FileGuid { get; set; }
        public int CountChunks { get; set; }
    }

    public class FileManager
    {
        public string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
        //public void Demo(string pathToDirectory)
        //{            
        //    var filesOld = Directory.GetFiles(pathToDirectory);

        //    var task = Task.Run(() =>
        //    {
        //        while (true)
        //        {
        //            var filesNew = Directory.GetFiles(pathToDirectory);
        //            if (filesOld.Length != filesNew.Length)
        //            {
        //                Console.WriteLine("Smth happened!");
        //            }
        //            Thread.Sleep(TimeSpan.FromSeconds(5));
        //        }
        //    });
        //}        

        public void ReadingFile(string path)
        {
            int partitionsCount = 0;
            int size = 1024 * 1024;
            
            Guid fileGuid = Guid.NewGuid();        

            string sha = GetChecksum(path);

            using (var fs = new FileStream(path, FileMode.Open))
            {
                int countChunks = (int)Math.Ceiling((double)fs.Length / size);

                FileInfo fileInfo = new FileInfo()
                {
                    Name = fs.Name,
                    Size = (int)fs.Length,
                    CountChunks = countChunks,
                    FileGuid = fileGuid,
                    FileSHA256 = sha
                };

                SendFileInfo(fileInfo);

                var bytes = new byte[size];               

                while (countChunks>0)
                {
                    if (fs.Length - fs.Position >= size)
                        bytes = new byte[size];
                    else
                        bytes = new byte[fs.Length - fs.Position];
                    
                    fs.Read(bytes, 0, bytes.Length);

                    FileChunk fc = new FileChunk()
                    {
                        FileGuid = fileGuid,
                        Content = bytes,
                        ChunkN = partitionsCount,
                        StartPosition = (int) fs.Position
                    };

                    SendFileChunk(fc);
                    countChunks--;
                    partitionsCount++;
                }
            }
        }

        //byte[] ObjectToByteArray(object obj)
        //{
        //    if (obj == null)
        //        return null;
        //    BinaryFormatter bf = new BinaryFormatter();

        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        bf.Serialize(ms, obj);
        //        return ms.ToArray();
        //    }
        //}

        public void SendFileInfo(FileInfo fileInfo)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; //, UserName = "step-devs", Password = "step-devs"
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "files_info_test",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var jsonObj = JsonConvert.SerializeObject(fileInfo);
                var body = Encoding.UTF8.GetBytes(jsonObj);

                channel.BasicPublish(exchange: "",
                                     routingKey: "files_info_test",
                                     basicProperties: null,
                                     body: body);
            }
        }

        public void SendFileChunk(FileChunk fileChunk)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; //, UserName = "step-devs", Password = "step-devs"
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "files_test",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var jsonObj = JsonConvert.SerializeObject(fileChunk);
                var body = Encoding.UTF8.GetBytes(jsonObj);

                channel.BasicPublish(exchange: "",
                                     routingKey: "files_test",
                                     basicProperties: null,
                                     body: body);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            FileManager fm = new FileManager();
            fm.ReadingFile(@"C:\test\1.jpg");
        }
    }
}
