using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager
{
    public class FileManager
    {
        public List<SendsLog> Logs { get; set; } = new List<SendsLog>();

        public List<FileChunk> fileChunks { get; set; } = new List<FileChunk>();
        public string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public void CheckDirectoryForNewFiles(string pathToDirectory)
        {           
            var task = Task.Run(() =>
            {
                string fName;
                while (true)
                {
                    var files = Directory.GetFiles(pathToDirectory);

                    for (int i = 0; i < files.Length; i++)
                    {
                        fName = Path.GetFileName(files[i]);
                        var lg = Logs.Find(f => f.fileName == fName);
                        if (lg == null)
                        {
                            var log = new SendsLog()
                            {
                                fileName = fName
                            };

                            Logs.Add(log);
                        }
                    }
                }
            });
        }

        public void LogsSave()
        {
            var jsonObj = JsonConvert.SerializeObject(Logs);
            File.WriteAllText(@"Logs.json", jsonObj);            
        }

        public void LogsRead()
        {
            if (File.Exists(@"Logs.json")){
                var json = File.ReadAllText(@"Logs.json");
                var logs = JsonConvert.DeserializeObject<List<SendsLog>>(json);

                if (logs != null)
                    Logs = logs;
            } 
            else File.Create(@"Logs.json");
        }

        public void ReadingFile(SendsLog log)
        {
            try
            {
                string path = @"C:\test\";
                int partitionsCount = 0;
                int size = 1024 * 1024;

                Guid fileGuid = Guid.NewGuid();

                string sha = GetChecksum(path + log.fileName);


                using (var fs = new FileStream(path + log.fileName, FileMode.Open))
                {
                    int countChunks = (int)Math.Ceiling((double)fs.Length / size);

                    FileInfo fileInfo = new FileInfo()
                    {
                        Name = log.fileName,
                        Size = (int)fs.Length,
                        CountChunks = countChunks,
                        FileGuid = fileGuid,
                        FileSHA256 = sha
                    };

                    if(log.ChanksInfo == null)
                        log.ChanksInfo = new bool[countChunks];

                    if (!log.SendingStarted)
                    {
                        SendFileInfo(fileInfo);
                        log.SendingStarted = true;
                    }                   

                    var bytes = new byte[size];

                    while (countChunks > 0)
                    {
                        bytes = (fs.Length - fs.Position >= size) ? new byte[size] : new byte[fs.Length - fs.Position];

                        int position = (int)fs.Position;
                        fs.Read(bytes, 0, bytes.Length);

                        FileChunk fc = new FileChunk()
                        {
                            FileName = log.fileName,
                            FileGuid = fileGuid,
                            Content = bytes,
                            ChunkN = partitionsCount,
                            StartPosition = position
                        };

                        if (!log.ChanksInfo[partitionsCount])
                        {
                            SendFileChunk(fc, log);
                            log.ChanksInfo[partitionsCount] = true;
                        }
                        countChunks--;
                        partitionsCount++;
                    }
                    log.Send = true;
                    LogsSave();
                }               
            }
            catch (Exception) 
            {
                ReadingFile(log);
            }
        }

        public void start()
        {
            LogsRead();

            while (true)
            {
                for (int i = 0; i < Logs.Count; i++)
                {
                    var log = Logs[i];
                    if (!log.Send)
                    {
                        ReadingFile(log);
                    }
                }

                Thread.Sleep(1000);
            }
        }

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

        public void SendFileChunk(FileChunk fileChunk, SendsLog log)
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

                log.ChanksInfo[fileChunk.ChunkN] = true;
                LogsSave();
            }

            
        }
    }
}
