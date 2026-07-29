using GeoAppCore;
using GeoCadPlugin.Drawers;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace GeoCadPlugin
{
    public class WebServer
    {
        private HttpListener listener;

        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();
            Task.Run(() => Listen());
        }

        private async Task Listen()
        {
            while (true)
            {
                var context =await listener.GetContextAsync();
                await ProcessRequest(context);
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            string url = context.Request.Url.AbsolutePath;

            switch (url.ToLower())
            {
                case "/check":
                    {
                        await WriteResponse(context, "OK");
                        return;
                    }

                case "/export-sections":
                case "/export-plan":
                    {
                        await ExportProject(context);
                        return;
                    }

                default:
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                        return;
                    }
            }
        }

        private static async Task WriteResponse(HttpListenerContext context, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = data.Length;

            await context.Response.OutputStream.WriteAsync(data);
            context.Response.Close();
        }

        private async Task ExportProject(HttpListenerContext context)
        {
            string url = context.Request.Url.AbsolutePath;
            
            using StreamReader reader =
                            new StreamReader(
                                context.Request.InputStream,
                                context.Request.ContentEncoding);

            string json = await reader.ReadToEndAsync();

            CommandQueue.Enqueue(() =>
            {
                switch (url.ToLower())
                {
                    case "/export-sections":
                        ExportSections(json);
                        break;
                    case "/export-plan":
                        ExportPlan(json);
                        break;
                }
            });

            await WriteResponse(context, "Imported");
        }

        private void ExportSections(string json)
        {
            GeoDoc project = JsonConvert.DeserializeObject<GeoDoc>(json);

            var doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;

            using (doc.LockDocument())
            {
                GeoSectionDrawer.Draw(project);
            }
        }
        private void ExportPlan(string json)
        {
            GeoDoc project = JsonConvert.DeserializeObject<GeoDoc>(json);

            var doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;

            using (doc.LockDocument())
            {
                GeoPlanDrawer.Draw(project);
            }
        }
    }
}
