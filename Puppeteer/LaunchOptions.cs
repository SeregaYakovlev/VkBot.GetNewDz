using PuppeteerSharp;

namespace Puppeteer
{
    public static class PuppeteerSharpLaunchArgs
    {
        public static string[] args = new string[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-plugins",
                "--disable-sync",
                "--disable-gpu",
                "--disable-speech-api",
                "--disable-remote-fonts",
                "--disable-shared-workers",
                "--disable-webgl",
                "--no-experiments",
                "--no-first-run",
                "--no-default-browser-check",
                "--no-wifi",
                "--no-pings",
                "--no-service-autorun",
                "--disable-databases",
                "--disable-default-apps",
                "--disable-demo-mode",
                "--disable-notifications",
                "--disable-permissions-api",
                "--disable-background-networking",
                "--disable-3d-apis",
                "--disable-bundled-ppapi-flash",
                "--disable-extensions",
                "--disable-gl-drawing-for-tests",
                "--disable-breakpad",
                "--disable-infobars",
                "--hide-scrollbars",
                "--disable-canvas-aa",
                "--disable-2d-canvas-clip-aa",
                "--disable-dev-shm-usage",
                "--no-zygote",
                "--use-gl=swiftshader",
                "--mute-audio",
                "--proxy-server='direct://",
                "--proxy-bypass-list=*",
            };

        public static ResourceType[] types = new ResourceType[]
        {
            ResourceType.Image,
            ResourceType.Font,
            ResourceType.StyleSheet
        };
    }
}

