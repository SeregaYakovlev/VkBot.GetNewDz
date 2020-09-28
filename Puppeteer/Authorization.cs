using Newtonsoft.Json;
using PuppeteerSharp;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using static ClassLibrary.Global;

namespace Puppeteer
{
    static class Authorization
    {
        internal static int DefaultTimeout;
        internal static async Task<Cookie> GetCookieByAuthorizationAsync(string pathToCookieFile)
        {
            /* ОЧЕНЬ ВАЖНО!
             * В DefaultViewPort стоит параметр null, при котором в развернутом виде КОМПЬЮТЕРНАЯ ВЕРСИЯ,
             * а в окне достаточно маленького размера у некоторых сайтов МОБИЛЬНАЯ ВЕРСИЯ!
             * Следовательно, испольнование CSS-селекторов может быть удачным, а может и нет,
             * в зависимости от размера браузера.
             * В headless-режиме окно маленького размера. */
            using (var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                DefaultViewport = null,
                //Args = PuppeteerSharpLaunchArgs.args
            }))
            {
                Stopwatch timer = new Stopwatch();
                var p = await GetPage(browser, "about:blank");
                /* 10 попыток подключения нужны, если сервер плохой, или интернета вдруг нету.*/

                bool success = false;
                int connectCount = 0;
                timer.Start();
                while (connectCount < 10 && !success)
                {
                    try
                    {
                        await p.GoToAsync("https://dnevnik2.petersburgedu.ru");
                        success = true;
                        Log.Information("Подключение к сайту {Site}: успешно!", "https://dnevnik2.petersburgedu.ru");
                        Log.Information($"{timer.ElapsedMilliseconds}");
                        timer.Restart();
                    }
                    catch (PuppeteerException e)
                    {
                        Log.Error(e, "GoToAsync({Site} failed", "https://dnevnik2.petersburgedu.ru");
                        Log.Information("Подключение к сайту {Site}: Ошибка, повторяю...", "https://dnevnik2.petersburgedu.ru");
                        Log.Information("Попытка № {attempt}", connectCount + 1);
                        await System.Threading.Tasks.Task.Delay(3000);
                        connectCount++;
                    }
                }


                WaitForSelectorOptions WaitForSelectorTimeout = new WaitForSelectorOptions { Timeout = DefaultTimeout };

                Log.Information($"DefaultTimeout: {p.DefaultTimeout}");
                Log.Information($"DefaultNavigationTimeout: {p.DefaultNavigationTimeout}");

                const string button = "body > app-root > n3-grid > app-login > div > div.notice > div > app-login-form > div > button";
                await p.WaitForSelectorAsync(button, WaitForSelectorTimeout);
                await System.Threading.Tasks.Task.Delay(10000);
                await p.ClickAsync(button);

                Log.Information("Первый клик {Button}: успешно!", "Войти с ЕСИА");
                Log.Information($"{timer.ElapsedMilliseconds}");
                timer.Restart();

                p = await GetPage(browser, "https://esia.gosuslugi.ru");
                Log.Information($"DefaultTimeout: {p.DefaultTimeout}");
                Log.Information($"DefaultNavigationTimeout: {p.DefaultNavigationTimeout}");

                // Авторизация
                await p.WaitForSelectorAsync("#mobileOrEmail", WaitForSelectorTimeout);
                await p.FocusAsync("#mobileOrEmail");
                await p.Keyboard.TypeAsync(ConfigJson.Login);
                Log.Information($"Login: {timer.ElapsedMilliseconds}");
                timer.Restart();

                await p.WaitForSelectorAsync("#password", WaitForSelectorTimeout);
                await p.FocusAsync("#password");
                await p.Keyboard.TypeAsync(ConfigJson.Password);
                Log.Information($"Password: {timer.ElapsedMilliseconds}");
                timer.Restart();

                await p.WaitForSelectorAsync("#loginByPwdButton > span", WaitForSelectorTimeout);
                await p.ClickAsync("#loginByPwdButton > span");
                Log.Information($"ClickAuthorizationButton: {timer.ElapsedMilliseconds}");

                Log.Information("Авторизация: успешно!");
                timer.Stop();
                /* Куки нужны для того, чтобы сайт меня опознал
                 * при отправке http-запроса на сервер эл. дневника */

                // 10 попыток получения cookie.
                Cookie cookie;
                int count = 0;
                int attempts = (DefaultTimeout / 1000);
                do
                {
                    if (count > attempts) throw new Exception("Cookie X-JMT-Token is not present.");
                    await System.Threading.Tasks.Task.Delay(1000);
                    var cookies = await p.GetCookiesAsync();
                    cookie = cookies.Where(c => c.Name == "X-JWT-Token").Select(c => new Cookie(c.Name, c.Value)).SingleOrDefault();
                    count++;
                }
                while (cookie == null || cookie.Value == "");

                //Здесь и далее безголовый браузер уже не нужен
                await browser.CloseAsync();

                var cookieAsJson = JsonConvert.SerializeObject(cookie);
                //await File.WriteAllTextAsync(pathToCookieFile, cookieAsJson);
                var fm = new ClassLibrary.File_Manager();
                fm.OpenFile(pathToCookieFile, "Write", cookieAsJson);
                return cookie;
            }
        }

        private static void P_FrameAttached(object sender, FrameEventArgs e)
        {
            //Log.Debug("P_FrameAttached sender {@Sender}, e {@e}", sender, e);
        }

        private static void P_Close(string method)
        {
            Log.Debug(method);
            /*p.Close += (s, e) => P_Close("Close");
            p.Console += (s, e) => P_Close("Console");
            p.Dialog += (s, e) => P_Close("Dialog");
            p.DOMContentLoaded += (s, e) => P_Close("DOMContentLoaded");
            p.Error += (s, e) => P_Close("Error");
            p.Load += (s, e) => P_Close("Load");
            p.Metrics += (s, e) => P_Close("Metrics");
            p.PageError += (s, e) => P_Close("PageError");
            p.Popup += (s, e) => P_Close("Popup");
            p.Request += (s, e) => P_Close("Request");
            p.RequestFailed += (s, e) => P_Close("RequestFailed");
            p.RequestFinished += (s, e) => P_Close("RequestFinished");
            p.Response += (s, e) => P_Close("Response");
            p.WorkerCreated += (s, e) => P_Close("WorkerCreated");
            p.WorkerDestroyed += (s, e) => P_Close("WorkerDestroyed");
            p.FrameAttached += P_FrameAttached;
            p.FrameDetached += P_FrameAttached;
            p.FrameNavigated += P_FrameAttached;
            p.FrameNavigated += P_FrameAttached;*/
        }

        private static async Task<Page> GetPage(Browser browser, string url)
        {
            Page[] pages;
            do
            {
                pages = await browser.PagesAsync();
                await System.Threading.Tasks.Task.Delay(1000);
            } while (!pages.Any(p2 => p2.Url.StartsWith(url)));
            var page = pages.Single(p2 => p2.Url.StartsWith(url));
            page.DefaultTimeout = DefaultTimeout;
            page.DefaultNavigationTimeout = DefaultTimeout;

            await page.SetRequestInterceptionAsync(true);
            page.Request += (sender, e) =>
            {
                var cur_type = e.Request.ResourceType;
                bool isAllowed = true;
                var disabled_types = PuppeteerSharpLaunchArgs.types;
                foreach (var d_type in disabled_types)
                {
                    if (cur_type == d_type) isAllowed = false;
                }
                if (isAllowed)
                    e.Request.ContinueAsync();
                else
                    e.Request.AbortAsync();
            };
            return page;
        }
    }
}
