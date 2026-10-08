using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using GeoAppCore;
using GeoAppCore.Models;
using GeoCadPlugin.Drawers;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;

namespace GeoCadPlugin
{
    public class WebServer
    {
        private HttpListener _listener;

        private bool _running = false;

        public void Start()
        {
            _running = true;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5050/");
            _listener.Start();
            Task.Run(() => Listen());
        }

        private async Task Listen()
        {
            while (_running)
            {
                try
                {
                    var context = await _listener.GetContextAsync();

                    // Не ждём завершения обработки этого запроса.
                    _ = ProcessRequestSafe(context);
                }
                catch (HttpListenerException)
                {
                    // Нормально при Stop()
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Нормально при Stop()
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"WebServer Listen error: {ex}");
                }
            }
        }

        private async Task ProcessRequestSafe(HttpListenerContext context)
        {
            try
            {
                await ProcessRequest(context);
            }
            catch (Exception ex)
            {
                try
                {
                    await SendJson(context, new
                    {
                        Success = false,
                        Error = ex.ToString()
                    });
                }
                catch
                {
                    // Соединение уже могло быть закрыто.
                }
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
                case "/export":
                    {
                        await ExportProject(context);
                        return;
                    }
                case "/get-boundary":
                    {
                        await GetBoundary(context);
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

            using StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);

            string json = await reader.ReadToEndAsync();

            CommandQueue.Enqueue(() =>
            {
                Export(json);
            });

            await WriteResponse(context, "Imported");
        }

        private void Export(string json)
        {
            GeoDoc? project = JsonConvert.DeserializeObject<GeoDoc>(json);

            var doc =
                Autodesk.AutoCAD.ApplicationServices.Application
                .DocumentManager
                .MdiActiveDocument;

            if (project == null)
                return;

            using (doc.LockDocument())
            {
                switch (project.ExportType)
                {
                    case AcadExportType.Plan:
                        GeoPlanDrawer.Draw(project);
                        break;
                    case AcadExportType.Sections:
                        GeoSectionDrawer.Draw(project);
                        break;
                }
            }
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
        }

        private async Task GetBoundary(HttpListenerContext context)
        {
            var tcs = new TaskCompletionSource<GetBoundaryResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            CommandQueue.Enqueue(() =>
            {
                try
                {
                    var result = SelectBoundaries();

                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(new GetBoundaryResult
                    {
                        Success = false,
                        Error = ex.ToString()
                    });
                }
            });

            // Ждём, пока AutoCAD выполнит команду
            var result = await tcs.Task;

            await SendJson(context, result);
        }

        private GetBoundaryResult SelectBoundaries()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc == null)
            {
                return new GetBoundaryResult
                {
                    Success = false,
                    Error = "Активный документ AutoCAD отсутствует."
                };
            }

            var ed = doc.Editor;

            var options = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите контур(ы) и нажмите Enter: "
            };

            var filter = new SelectionFilter(
                new[]
                {
            new TypedValue(
                (int)DxfCode.Start,
                "LWPOLYLINE,POLYLINE,CIRCLE")
                });

            var result = ed.GetSelection(options, filter);

            if (result.Status != PromptStatus.OK)
            {
                return new GetBoundaryResult
                {
                    Success = false,
                    Cancelled = true
                };
            }

            var boundaries = new List<BoundaryData>();

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selected in result.Value)
                {
                    if (selected == null)
                        continue;

                    var entity = tr.GetObject(
                        selected.ObjectId,
                        OpenMode.ForRead) as Entity;

                    if (entity is Polyline polyline)
                    {
                        var points = new List<Point2D>();

                        for (int i = 0; i < polyline.NumberOfVertices; i++)
                        {
                            var p = polyline.GetPoint2dAt(i);

                            points.Add(new Point2D
                            {
                                X = p.X,
                                Y = p.Y
                            });
                        }

                        boundaries.Add(new BoundaryData
                        {
                            Closed = polyline.Closed,
                            Points = points
                        });
                    }
                }

                tr.Commit();
            }

            return new GetBoundaryResult
            {
                Success = true,
                Boundaries = boundaries
            };
        }

        private static async Task SendJson(HttpListenerContext context, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }
}
