using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace ClassLibrary
{
    public class Config
    {
        public readonly static string SETTINGS_FILE =
            //Path.Combine(@"C:\Users\Serega\Desktop\VkBotDzRelease", "config.json");
            Path.Combine(Directory.GetCurrentDirectory(), "config.json");

        private static Config instance;

        public static Config Instance
        {
            get { return instance ?? ReadConfig(); }
            private set { instance = value; }
        }

        private static Config ReadConfig()
        {
            var context = File.ReadAllText(SETTINGS_FILE);
            return JsonConvert.DeserializeObject<Config>(context);
        }

        public ConfigJson ConfigJson { get; set; }
    }

    public class ConfigJson
    {
        public string Author { get; set; }
        public string SiteInfo { get; set; }
        public string HowOftenDataUpdates { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int HowManyWeeksToDownload { get; set; }
        public int HowManyWeeksToSave { get; set; }
        public int DefaultTimeout { get; set; }
        public string Seq { get; set; }
        public string serverFileName { get; set; }
        public string diffsFileName { get; set; }
        public string UsersCounterFileName { get; set; }
        public string JavaScriptErrorsFileName { get; set; }
        public string AuthorizationCookieFileName { get; set; }
        public DateTimesFormats DateTimesFormats { get; set; }
        public Pathes Pathes { get; set; }
        public WebServer WebServer { get; set; }
    }

    public class WebServer
    {
        public int HTTP_Port { get; set; }
        public int HTTPS_Port { get; set; }
        public string WebRoot { get; set; }
        public HTTPS HTTPS { get; set; }
    }

    public class HTTPS
    {
        public string PathForPfxFile { get; set; }
    }

    public class DateTimesFormats
    {
        public string FullDateTime { get; set; }
        public string No_seconds { get; set; }
        public string No_year { get; set; }
    }

    public class Pathes
    {
        public string pathToDataDirectory { get; set; }
        public string pathToAuthorizationDataDirectory { get; set; }
        public string pathToReports { get; set; }
    }
}
