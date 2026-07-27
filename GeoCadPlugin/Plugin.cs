using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace GeoCadPlugin
{
    public class Plugin : IExtensionApplication
    {
        private static WebServer server;

        public void Initialize()
        {
            Application.Idle += OnIdle;

            server = new WebServer();

            server.Start();
        }

        public void Terminate()
        {

        }

        private void OnIdle(object sender, EventArgs e)
        {
            CommandQueue.Execute();
        }
    }
}
