using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GeoCadPlugin.InputDialogBuilders;
using GeoCadPlugin.Topography;
using ACDOC = Autodesk.AutoCAD.ApplicationServices.Document;
using Exception = System.Exception;

namespace GeoCadPlugin
{
    public class Commands
    {
        [CommandMethod("GEOMENU")]
        public void ShowGeoMenu()
        {
            GeoMenuController.Show();
        }

        [CommandMethod("RANDOMCIRCLES", CommandFlags.UsePickSet)]
        public void RandomCircles()
        {
            ACDOC doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptSelectionResult selection = ed.SelectImplied();

            var filter = new SelectionFilter(
                new TypedValue[] { new TypedValue((int)DxfCode.Start, "CIRCLE") }
            );

            if (selection.Status != PromptStatus.OK)
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите окружности: ",
                };

                selection = ed.GetSelection(options, filter);

                if (selection.Status != PromptStatus.OK)
                    return;
            }

            // Запрашиваем максимальное расстояние
            var distanceOptions = new PromptDoubleOptions("\nМаксимальное расстояние смещения: ")
            {
                DefaultValue = 3,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
            };

            PromptDoubleResult distanceResult = ed.GetDouble(distanceOptions);

            if (
                distanceResult.Status != PromptStatus.OK
                && distanceResult.Status != PromptStatus.None
            )
                return;

            double maxDistance = distanceResult.Status == PromptStatus.None
                ? 3
                : distanceResult.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Functions.MoveCircles(selection, maxDistance, tr);
                tr.Commit();
            }

            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            ed.WriteMessage($"\nОкружности смещены. Максимальное расстояние: {maxDistance}.");
        }

        [CommandMethod("IMPORTCOORDS")]
        public void ImportCoordinates()
        {
            string? filePath = Functions.ExcelFileDialog();

            if (filePath == null)
                return;

            var coords = ExcelCoordinateReader.Read(filePath);
            var convertedCoords = CoorditateHelper.ConvertToGaussKruger(coords);

            if (convertedCoords == null)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    "\nНе удалось получить координаты."
                );

                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            var radiusOptions = new PromptDoubleOptions("\nВведите радиус для окружностей <10.0>: ")
            {
                DefaultValue = 10.0,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
            };

            PromptDoubleResult radiusResult = ed.GetDouble(radiusOptions);

            if (radiusResult.Status != PromptStatus.OK && radiusResult.Status != PromptStatus.None)
                return;

            double radius = radiusResult.Status == PromptStatus.None ? 10.0 : radiusResult.Value;
            var count = Functions.DrawCircles(db, convertedCoords, radius);

            ed.WriteMessage($"\nУспешно импортировано и отрисовано {count} окружностей.");
        }

        [CommandMethod("DIVIDEPOLYLINE", CommandFlags.UsePickSet)]
        public void DividePolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. Выбор полилиний (поддерживается предварительный и текущий выбор)
            PromptSelectionResult selection = ed.SelectImplied();

            var filter = new SelectionFilter(
                new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE") }
            );

            if (selection.Status != PromptStatus.OK)
            {
                var selectionOptions = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите полилинии: ",
                };

                selection = ed.GetSelection(selectionOptions, filter);

                if (selection.Status != PromptStatus.OK)
                    return;
            }

            // 2. Запрос длины секции (шага)
            var lengthOptions = new PromptDoubleOptions("\nВведите длину секции: ")
            {
                DefaultValue = 100.0,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
            };

            PromptDoubleResult lengthResult = ed.GetDouble(lengthOptions);
            if (lengthResult.Status != PromptStatus.OK && lengthResult.Status != PromptStatus.None)
                return;

            double sectionLength =
                lengthResult.Status == PromptStatus.None ? 100.0 : lengthResult.Value;

            // 3. Запрос радиуса окружностей
            var radiusOptions = new PromptDoubleOptions("\nВведите радиус окружностей <10>: ")
            {
                DefaultValue = 10.0,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
            };

            PromptDoubleResult radiusResult = ed.GetDouble(radiusOptions);
            if (radiusResult.Status != PromptStatus.OK && radiusResult.Status != PromptStatus.None)
                return;

            double radius = radiusResult.Status == PromptStatus.None ? 10.0 : radiusResult.Value;

            // 4. Основная транзакция для создания окружностей
            var totalCirclesCreated = Functions.DrawBoreholesOnLine(db, selection, sectionLength, radius);

            // Очищаем выделение и выводим сообщение
            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            ed.WriteMessage($"\nПолилинии разделены секциями длиной {sectionLength}. Добавлено окружностей: {totalCirclesCreated}.");
        }

        [CommandMethod("FindWellElevation", CommandFlags.UsePickSet)]
        public void FindWellElevation()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptSelectionResult selection = ed.SelectImplied();

            TypedValue[] circleFilter = { new TypedValue((int)DxfCode.Start, "CIRCLE") };

            if (selection.Status != PromptStatus.OK)
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите скважины: ",
                };

                selection = ed.GetSelection(options, new SelectionFilter(circleFilter));

                if (selection.Status != PromptStatus.OK)
                    return;
            }

            ElevationFindService.FindWellElevation(selection);
        }

        [CommandMethod("GETTOPOGRAPHY")]
        public static async void DownloadDem()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptSelectionResult selection = ed.SelectImplied();

            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "CIRCLE") };

            if (selection.Status != PromptStatus.OK)
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите круги: ",
                };

                selection = ed.GetSelection(options, new SelectionFilter(filter));

                if (selection.Status != PromptStatus.OK)
                    return;
            }

            var demFilePath = await TopographyDownloader.Download(selection);

            if (demFilePath == null)
                return;

            var properties = TopographyDialogBuilder.Build(ownerHandle: Application.MainWindow.Handle);

            if (properties == null)
                return;

            try
            {
                DemGrid dem = DemReader.Read(demFilePath);

                if (properties.SmoothDem)
                {
                    dem = DemSmoother.Smooth(dem, iterations: properties.SmoothDemIterations);
                }

                List<TopographyLine> lines = TopographyBuilder.Build(dem, properties.Interval);

                if (properties.SimplifyLines)
                {
                    lines = ContourSimplifier.Simplify(lines, toleranceMeters: properties.SimplifyTolerance);
                }

                if (properties.SmoothLines)
                {
                    lines = ContourSmoother.Smooth(lines, stepMeters: properties.SmoothStep);
                }

                if (properties.JoinLines)
                {
                    lines = ContourJoiner.Join(lines, maxGapMeters: properties.JoinMaxGap, maxTailMeters: properties.JoinMaxTail);
                }

                using (doc.LockDocument())
                {
                    TopographyDrawer.Draw(doc.Database, lines);
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage("\nОшибка обработки DEM:");
                ed.WriteMessage($"\n{ex}");
            }
        }
    }
}
