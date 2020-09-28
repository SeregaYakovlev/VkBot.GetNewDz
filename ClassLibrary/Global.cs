using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ClassLibrary
{
    public static class Global
    {
        public static Mutex mutex;

        public static ConfigJson ConfigJson = Config.Instance.ConfigJson;
        public static Pathes Pathes = ConfigJson.Pathes;
        public static DateTimesFormats DateTimesFormats = ConfigJson.DateTimesFormats;

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static Global()
        {
            mutex = new Mutex(false, "mutex", out bool createdNew);
            Log.Information($"mutex {(createdNew? "created":"existed")}");
        }
    }

    public class File_Manager
    {
        public FileData OpenFile(string path, string type, string content)
        {
            if(!Global.mutex.WaitOne(TimeSpan.FromSeconds(Debugger.IsAttached ? 60 : 10)))
            {
                throw new InvalidProgramException("Impossible to aquire mutex");
            }
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var file = new FileInfo(path);

                if (type == "Write")
                {
                    File.WriteAllText(path, content);
                }
                else if (type == "Read")
                {
                    string readedFile;
                    using (var reader = new StreamReader(file.FullName))
                    {
                        readedFile = reader.ReadToEnd();
                    }
                    var fileData = new FileData();
                    fileData.fileData = readedFile;
                    return fileData;
                }
                else if (type == "Append")
                {
                    File.AppendAllText(path, content);
                }
                return null;
            }
            finally
            {
                Global.mutex.ReleaseMutex();
            }
        }
    }

    public class FileData
    {
        public string fileData;
    }
}
