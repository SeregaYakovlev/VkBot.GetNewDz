using Newtonsoft.Json;
using PuppeteerSharp;
using Serilog;
using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using static ClassLibrary.Global;
using System.Threading.Tasks;
using System.Text;

namespace Puppeteer
{
    class Program
    {
        private static Cookie cookie;
        private static async System.Threading.Tasks.Task Main()
        {
            string pathToCookieFile = Path.Combine(Pathes.pathToAuthorizationDataDirectory, ConfigJson.AuthorizationCookieFileName);
            ConfigureLogger();
            await InstallBrowserAsync();

            string cookieFile;
            if (File.Exists(pathToCookieFile))
            {
                var fileManager = new ClassLibrary.File_Manager();
                var result = fileManager.OpenFile(pathToCookieFile, "Read", null);
                cookieFile = result.fileData;
                var json = JObject.Parse(cookieFile);
                var cookieName = json["Name"].ToString();
                var cookieValue = json["Value"].ToString();
                cookie = new Cookie(cookieName, cookieValue);
            }
            else
            {
                Log.Information("Файл cookie отсутствует. Авторизация");
                Authorization.DefaultTimeout = ConfigJson.DefaultTimeout;
                cookie = await Authorization.GetCookieByAuthorizationAsync(pathToCookieFile);
            }

            // Соединяемся с электронным дневником и получаем JSON

            var monday_sunday = GetMondaySunday();
            var mondayDate = monday_sunday.monday.Date;
            var sundayDate = monday_sunday.sunday.Date;

            int howManyWeeksToDownload = ConfigJson.HowManyWeeksToDownload;

            var dataFromServerList = new SortedList<DateTime, string>();
            var dataFromFileSystemList = new SortedList<DateTime, string>();
            var serverDataObj = new JObject();

            EnsureDirectoryExists(Pathes.pathToDataDirectory);
            var lastFile = new DirectoryInfo(Pathes.pathToDataDirectory)
                                    .GetFiles()
                                    .OrderByDescending(fi => fi.CreationTime)
                                    .Where(file => file.Name == ConfigJson.serverFileName)
                                    .FirstOrDefault();
            if (lastFile != null)
            {
                var fileManager = new ClassLibrary.File_Manager();
                var path2 = Path.Combine(Pathes.pathToDataDirectory, ConfigJson.serverFileName);
                var result = fileManager.OpenFile(path2, "Read", null);
                var json = result.fileData;
                var p = JObject.Parse(json);
                for (int i = 0; i < p.Count; i++)
                {
                    var date = DateTime.ParseExact(p[i.ToString()]["data"]["Monday"].ToString(), ConfigJson.DateTimesFormats.FullDateTime, null).Date;
                    var jsonWeek = JsonConvert.SerializeObject(p[i.ToString()]);
                    dataFromFileSystemList.Add(date, jsonWeek);
                }
            }

            for (int i = 0; i < howManyWeeksToDownload; i++)
            {
                Log.Information(i.ToString());
                if (i != 0)
                {
                    mondayDate = mondayDate.AddDays(-7);
                    sundayDate = sundayDate.AddDays(-7);
                }

                string jsonContentAsString = GetDataFromServer(mondayDate, sundayDate, pathToCookieFile).Result;
                var strAsJson = JsonConvert.DeserializeObject<JObject>(jsonContentAsString);
                var date = DateTime.ParseExact(strAsJson["data"]["Monday"].ToString(), "dd.MM.yyyy H:mm:ss", null).Date;
                dataFromServerList.Add(date, jsonContentAsString);
                serverDataObj.Add(new JProperty(i.ToString(), strAsJson));
            }
            var path = Path.Combine(Pathes.pathToDataDirectory, ConfigJson.serverFileName);
            var content = JsonConvert.SerializeObject(serverDataObj, Formatting.Indented);
            var file_manager = new ClassLibrary.File_Manager();
            file_manager.OpenFile(path, "Write", content);

            var orderedDates = dataFromFileSystemList.Keys.Concat(dataFromServerList.Keys).Distinct().OrderByDescending(d => d);
            var howManyWeeksToSaveInDiffsJson = ConfigJson.HowManyWeeksToSave;

            foreach (var date in orderedDates)
            {
                bool fileExists = dataFromFileSystemList.TryGetValue(date, out var fileJson);
                bool serverContains = dataFromServerList.TryGetValue(date, out var serverJson);
                if (fileExists && serverContains)
                {
                    string newDz;
                    newDz = await GetDiffsContent(fileJson, serverJson, howManyWeeksToSaveInDiffsJson);
                    if (newDz != null)
                    {
                        string dzMessage = "";
                        var json = JArray.Parse(newDz);
                        IEnumerable<JToken> items = json.AsEnumerable().OrderBy(it => DateTime.ParseExact(it["Item1" ?? "Item2"]["datetime_from"].ToString(), DateTimesFormats.FullDateTime, null));
                        foreach (var item in items)
                        {
                            var item1 = item["Item1"];
                            var item2 = item["Item2"];

                            /*if (item1.Any())
                            {
                                var updateTime = DateTime.ParseExact(item1["updateTime"].ToString(), DateTimesFormats.FullDateTime, null).ToString(DateTimesFormats.No_seconds);
                                var subjectStatus = item1["SubjectStatus"].ToString();
                                var homeworkStatus = item1["HomeworkStatus"].ToString();

                                if (subjectStatus == "deleted" && item1["tasks"].Any())
                                {
                                    //< p class="dzDeleted">У(пбу) @updateTime</p>
                                }
                            }*/

                            if (item2.Any())
                            {
                                //var updateTime = DateTime.ParseExact(item2["updateTime"].ToString(), DateTimesFormats.FullDateTime, null).ToString(DateTimesFormats.No_seconds);
                                var subjectStatus = item2["SubjectStatus"].ToString();
                                var homeworkStatus = item2["HomeworkStatus"].ToString();

                                if ((subjectStatus == "new" && item2["tasks"].Any())
                                    || (homeworkStatus == "changed" && item1.Any() && !item1["tasks"].Any() && item2["tasks"].Any()))
                                {
                                    var subject = item2["subject_name"].ToString();
                                    var subject_date = DateTime.ParseExact(item2["datetime_from"].ToString(), ConfigJson.DateTimesFormats.FullDateTime, null).Date;
                                    string dayOfWeek = "";
                                    string subjectDateStr = "";
                                    if (subject_date != DateTime.Now.Date)
                                    {
                                        dayOfWeek = DayOfWeekExtention.ToRussianString(subject_date.DayOfWeek);
                                    }
                                    var monday_sunday2 = GetMondaySunday();
                                    var mondayDate2 = monday_sunday.monday.Date;
                                    if ((subject_date - mondayDate2).TotalDays < 0)
                                    {
                                        subjectDateStr = subject_date.ToString(ConfigJson.DateTimesFormats.No_year);
                                    }
                                    string itog = "";
                                    if (dayOfWeek != "")
                                    {
                                        itog += ($"{dayOfWeek},\n");
                                        if (subjectDateStr != "")
                                        {
                                            itog += ($"{subjectDateStr}:\n");

                                        }
                                    }
                                    itog += ($"{subject}:\n");

                                    dzMessage += itog;
                                    foreach (var task in item2["tasks"])
                                    {
                                        dzMessage += ($"Появилось: {task["task_name"].ToString()}\n");
                                    }
                                    //<p class="dzAdded">П @updateTime</p>
                                }
                                else if (item1.Any() && item1["tasks"].Any() && item2["tasks"].Any())
                                {
                                    var subject = item2["subject_name"].ToString();
                                    var subject_date = DateTime.ParseExact(item2["datetime_from"].ToString(), ConfigJson.DateTimesFormats.FullDateTime, null).Date;
                                    string dayOfWeek = "";
                                    string subjectDateStr = "";
                                    if (subject_date != DateTime.Now.Date)
                                    {
                                        dayOfWeek = DayOfWeekExtention.ToRussianString(subject_date.DayOfWeek);
                                    }
                                    var monday_sunday2 = GetMondaySunday();
                                    var mondayDate2 = monday_sunday.monday.Date;
                                    if ((subject_date - mondayDate2).TotalDays < 0)
                                    {
                                        subjectDateStr = subject_date.ToString(ConfigJson.DateTimesFormats.No_year);
                                    }
                                    string itog = "";
                                    if (dayOfWeek != "")
                                    {
                                        itog += ($"{dayOfWeek},\n");
                                        if (subjectDateStr != "")
                                        {
                                            itog += ($"{subjectDateStr}:\n");

                                        }
                                    }
                                    itog += ($"{subject}:\n");

                                    dzMessage += itog;
                                    foreach (var task in item2["tasks"])
                                    {
                                        dzMessage += ($"Изменилось: {task["task_name"].ToString()}\n");
                                    }
                                }

                                /*else if (homeworkStatus == "changed")
                                {

                                    if (item1.Any() && item1["tasks"].Any() && item2["tasks"].Any())
                                    {
                                        //<p class="dzChanged">И @updateTime</p>
                                    }
                                    else if (item1.Any() && !item1["tasks"].Any() && item2["tasks"].Any())
                                    {
                                        //<p class="dzAdded">П(прб) @updateTime</p>
                                    }
                                    else if (item1.Any() && !item2["tasks"].Any() && item1["tasks"].Any())
                                    {
                                        //<p class="dzDeleted">У @updateTime</p>
                                    }
                                }*/
                            }
                            await SendNewDzToServer(dzMessage);
                            dzMessage = "";
                        }
                    }
                }
            }

            Log.Information("Скрипт выполнен успешно!");
            Log.CloseAndFlush(); /*отправляем логи на сервер логов*/
        }

        private static async System.Threading.Tasks.Task SendNewDzToServer(string dzMessage)
        {
            string url = "http://f0470600.xsph.ru/program.php";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            //ЭТО СТИРАТЬ НЕЛЬЗЯ, ИНАЧЕ ЗАБАНЯТ!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
            request.Referer = "no-referrer-when-downgrade";
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] bytes = Encoding.UTF8.GetBytes(dzMessage);
            request.ContentLength = bytes.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);

            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);

            var result = reader.ReadToEnd();
            Log.Information(result);
            stream.Dispose();
            reader.Dispose();

        }

        private static async System.Threading.Tasks.Task<string> GetDiffsContent(string old, string @new, int howManyWeeksToSave)
        {
            if (old != null && @new != null)
            {
                var oldTree = JsonConvert.DeserializeObject<Rootobject>(old);
                var newTree = JsonConvert.DeserializeObject<Rootobject>(@new);
                var result = oldTree.GetDiffs(newTree);

                if (result.Any())
                {
                    var file = new DirectoryInfo(Pathes.pathToDataDirectory).GetFiles().Where(fi => fi.Name == ConfigJson.diffsFileName).SingleOrDefault();
                    if (file == null)
                    {
                        var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                        var path = Path.Combine(Pathes.pathToDataDirectory, ConfigJson.diffsFileName);
                        var file_manager = new ClassLibrary.File_Manager();
                        file_manager.OpenFile(path, "Append", json);
                    }
                    else
                    {
                        var fileManager = new ClassLibrary.File_Manager();
                        var fileDataObj = fileManager.OpenFile(file.FullName, "Read", null);
                        string readedDiffsFile = fileDataObj.fileData;

                        var parsedFile = JsonConvert.DeserializeObject<IEnumerable<(Item old, Item @new)>>(readedDiffsFile);

                        var concatedObj = parsedFile.Concat(result);

                        var dateTimeList = new List<DateTime>();
                        foreach (var homework in concatedObj)
                        {
                            string dateTime;
                            DateTime dateTimeAsDateTime;
                            var notEmptyItem = (homework.old ?? homework.@new);
                            if ((notEmptyItem) != null)
                            {
                                dateTime = notEmptyItem.datetime_from;
                                dateTimeAsDateTime = DateTime.ParseExact(dateTime, DateTimesFormats.FullDateTime, null);
                                dateTimeList.Add(dateTimeAsDateTime);
                            }
                        }
                        var maxDateTimeSaved = dateTimeList.Max().AddDays(-7 * howManyWeeksToSave);

                        var recentItemsOnly = concatedObj.Where(changedHomework =>
                        {
                            var timeAsDateTime = DateTime.ParseExact((changedHomework.old ?? changedHomework.@new).datetime_from, DateTimesFormats.FullDateTime, null);
                            return timeAsDateTime >= maxDateTimeSaved;
                        });

                        var newData = JsonConvert.SerializeObject(recentItemsOnly, Formatting.Indented);

                        if (newData.Any())
                        {
                            var file_manager = new ClassLibrary.File_Manager();
                            file_manager.OpenFile(file.FullName, "Write", newData);

                        }
                    }
                }
                if (result.Any()) return JsonConvert.SerializeObject(result);
            }
            return null;
        }
        private static async Task<string> GetDataFromServer(DateTime last, DateTime next, string pathToCookieFile)
        {
            string jsonContentAsString = null;
            string lastStr = last.ToString("dd.MM.yyyy");
            string nextStr = next.ToString("dd.MM.yyyy");
            HttpResponseMessage response;
            int connectionCount = 0;
            bool success = false;
            string link = $"/api/journal/lesson/list-by-education?p_limit=100&p_page=1&p_datetime_from={lastStr}&p_datetime_to={nextStr}&p_groups%5B%5D=5881&p_educations%5B%5D=15622";
            var baseAddress = new Uri("https://dnevnik2.petersburgedu.ru");
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(baseAddress, cookie);
            while (!success && connectionCount < 10)
            {
                try
                {
                    using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                    using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                    {
                        response = await client.GetAsync(link);
                        response.EnsureSuccessStatusCode();
                        jsonContentAsString = await response.Content.ReadAsStringAsync();
                    }
                    success = true;
                    jsonContentAsString = AddJsonTimeSet(jsonContentAsString, last);
                    return jsonContentAsString;
                }
                catch (HttpRequestException err) when (err.Message.IndexOf("401") > -1)
                {

                    connectionCount++;
                    Log.Information("Файл cookie устарел. Авторизация.");
                    Authorization.DefaultTimeout = ConfigJson.DefaultTimeout;
                    cookie = await Authorization.GetCookieByAuthorizationAsync(pathToCookieFile);
                    cookieContainer = new CookieContainer();
                    cookieContainer.Add(baseAddress, cookie);
                }
            }
            return jsonContentAsString;
        }
        private static async System.Threading.Tasks.Task InstallBrowserAsync()
        {
            // Установка и обновление браузера chromium
            var browserFetcher = new BrowserFetcher();
            var localVersions = browserFetcher.LocalRevisions();

            if (!localVersions.Any() || BrowserFetcher.DefaultRevision != localVersions.Max())
            {
                Log.Information("Downloading chromium...");
                browserFetcher.DownloadProgressChanged += (_, e) => { Console.Write("\r" + e.ProgressPercentage + "%"); };
                await browserFetcher.DownloadAsync(BrowserFetcher.DefaultRevision);
                Console.WriteLine(); // Перевод курсора на следующую строку(чтобы небыло "100%Успешно")
            }
        }
        private static MondaySunday GetMondaySunday()
        {
            DateTime next = DateTime.MinValue;
            DateTime last = DateTime.MinValue;

            DateTime currentNext = DateTime.Now;
            while (currentNext.DayOfWeek != DayOfWeek.Sunday)
            {
                currentNext = currentNext.AddDays(1);
            }
            next = currentNext;

            DateTime currentLast = DateTime.Now;
            while (currentLast.DayOfWeek != DayOfWeek.Monday)
            {
                currentLast = currentLast.AddDays(-1);
            }
            last = currentLast;

            var MondaySundayClass = new MondaySunday();
            MondaySundayClass.monday = last;
            MondaySundayClass.sunday = next;
            return MondaySundayClass;
        }
        private static string AddJsonTimeSet(string jsonContentAsStr, DateTime time)
        {
            var jobj = JObject.Parse(jsonContentAsStr);
            string datetime = ConvertToDate(time).ToString(ConfigJson.DateTimesFormats.FullDateTime);
            jobj["data"][$"{time.DayOfWeek}"] = datetime;
            var jsonAsStr = JsonConvert.SerializeObject(jobj);
            return jsonAsStr;
        }
        private static DateTime ConvertToDate(DateTime dateTime)
        {
            return dateTime.Date;
        }
        private static void ConfigureLogger()
        {
            var logConfig = new LoggerConfiguration()
                   .MinimumLevel.Debug()
                   .WriteTo.Console()
                   .WriteTo.Seq(ConfigJson.Seq);
            Log.Logger = logConfig.CreateLogger();
        }
    }
    public class MondaySunday
    {
        public DateTime monday;
        public DateTime sunday;
    }
}
