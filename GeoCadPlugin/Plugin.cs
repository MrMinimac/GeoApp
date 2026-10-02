using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace GeoCadPlugin
{
    public class Plugin : IExtensionApplication
    {
        private WebServer? _server;

        public void Initialize()
        {
            var editor = Application.DocumentManager
                .MdiActiveDocument?
                .Editor;

            editor?.WriteMessage("\nGeoAppPlugin загружен!");

            Application.Idle += OnIdle;

            _server = new WebServer();
            _server.Start();
        }

        public void Terminate()
        {
            Application.Idle -= OnIdle;

            _server?.Stop();
            _server = null;

            Application.DocumentManager
                .MdiActiveDocument?
                .Editor
                .WriteMessage("\nGeoAppPlugin выгружен!");
        }

        private void OnIdle(object? sender, EventArgs e)
        {
            CommandQueue.Execute();
        }
    }
}
