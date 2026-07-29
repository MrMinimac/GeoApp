using System.Diagnostics;
using System.IO;

namespace GeoAppWpf.Services
{
    public static class RestartHelper
    {
        public static void Restart()
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

            if (exePath == "")
                return;

            string exeName = Path.GetFileName(exePath);

            Cmd($"taskkill /f /im \"{exeName}\" && timeout /t 1 && \"{exePath}\"");
        }

        private static void Cmd(string line)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + line)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
