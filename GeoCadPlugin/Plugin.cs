using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace GeoCadPlugin
{
    public class Plugin : IExtensionApplication
    {
        private static WebServer server;

        public void Initialize()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                .MdiActiveDocument?
                .Editor
                .WriteMessage("\nGeoAppPlugin загружен!");

            Application.Idle += OnIdle;
            server = new WebServer();
            server.Start();
        }

        public void Terminate()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                .MdiActiveDocument?
                .Editor
                .WriteMessage("\nLGeoAppPlugin выгружен!");
        }

        private void OnIdle(object sender, EventArgs e)
        {
            CommandQueue.Execute();
        }
    }
}
