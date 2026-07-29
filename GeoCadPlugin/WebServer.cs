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

                case "/import":
                case "/import2":
                    {
                        using StreamReader reader =
                            new StreamReader(
                                context.Request.InputStream,
                                context.Request.ContentEncoding);

                        string json = await reader.ReadToEndAsync();

                        CommandQueue.Enqueue(() =>
                        {
                            int.TryParse(url.ToLower().Replace("/import", ""), out int id);
                            ImportProject(json, id != 0 ? id : 1);
                        });

                        await WriteResponse(context, "Imported");
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

        private void ImportProject(string json, int id)
        {
            GeoDoc project = JsonConvert.DeserializeObject<GeoDoc>(json);

            var doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;

            using (doc.LockDocument())
            {
                if (id == 1)
                {
                    GeoPlanDrawer.Draw(project);
                }
                if (id == 2)
                {
                    GeoSectionDrawer.Draw(project);
                }
            }
        }
    }
}
