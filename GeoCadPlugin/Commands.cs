using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Microsoft.Win32;
using ACDOC = Autodesk.AutoCAD.ApplicationServices.Document;

namespace GeoCadPlugin
{
    public class Commands
    {
        [CommandMethod("RANDOMCIRCLES", CommandFlags.UsePickSet)]
        public void RandomCircles()
        {
            ACDOC doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptSelectionResult selection = ed.SelectImplied();

            var filter = new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "CIRCLE")
            });

            if (selection.Status != PromptStatus.OK)
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите окружности: "
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
                AllowNone = true
            };

            PromptDoubleResult distanceResult = ed.GetDouble(distanceOptions);

            if (distanceResult.Status != PromptStatus.OK &&
                distanceResult.Status != PromptStatus.None)
                return;

            double maxDistance = distanceResult.Status == PromptStatus.None
                ? 3
                : distanceResult.Value;

            Random random = new Random();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MoveCircles(selection, maxDistance, random, tr);
                tr.Commit();
            }

            ed.SetImpliedSelection(Array.Empty<ObjectId>());

            ed.WriteMessage($"\nОкружности смещены. Максимальное расстояние: {maxDistance}.");
        }

        [CommandMethod("IMPORTCOORDS")]
        public void ImportCoordinates()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите Excel-файл",
                Filter = "Excel файлы (*.xlsx;*.xlsm)|*.xlsx;*.xlsm",
                Multiselect = false,
                CheckFileExists = true,
            };


            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;
            var coords = ExcelCoordinateReader.Read(filePath);
            var convertedCoords = CoorditateHelper.ConvertToGaussKruger(coords);

            if (convertedCoords == null)
            {
                Application.DocumentManager.MdiActiveDocument.Editor
                    .WriteMessage("\nНе удалось получить координаты.");

                return;
            }

            DrawCircles(convertedCoords);
        }

        [CommandMethod("DIVIDEPOLYLINE", CommandFlags.UsePickSet)]
        public void DividePolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. Выбор полилиний (поддерживается предварительный и текущий выбор)
            PromptSelectionResult selection = ed.SelectImplied();

            var filter = new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE")
            });

            if (selection.Status != PromptStatus.OK)
            {
                var selectionOptions = new PromptSelectionOptions
                {
                    MessageForAdding = "\nВыберите полилинии: "
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
                AllowNone = true
            };

            PromptDoubleResult lengthResult = ed.GetDouble(lengthOptions);
            if (lengthResult.Status != PromptStatus.OK && lengthResult.Status != PromptStatus.None)
                return;

            double sectionLength = lengthResult.Status == PromptStatus.None ? 100.0 : lengthResult.Value;

            // 3. Запрос радиуса окружностей
            var radiusOptions = new PromptDoubleOptions("\nВведите радиус окружностей <10>: ")
            {
                DefaultValue = 10.0,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true
            };

            PromptDoubleResult radiusResult = ed.GetDouble(radiusOptions);
            if (radiusResult.Status != PromptStatus.OK && radiusResult.Status != PromptStatus.None)
                return;

            double radius = radiusResult.Status == PromptStatus.None ? 10.0 : radiusResult.Value;

            // 4. Основная транзакция для создания окружностей
            int totalCirclesCreated = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject selObj in selection.Value)
                {
                    if (selObj == null)
                        continue;

                    if (tr.GetObject(selObj.ObjectId, OpenMode.ForRead) is Curve curve)
                    {
                        double totalLength = curve.GetDistanceAtParameter(curve.EndParam);

                        // Проходим вдоль полилинии с заданным шагом
                        for (double currentDist = 0; currentDist <= totalLength; currentDist += sectionLength)
                        {
                            Point3d point = curve.GetPointAtDist(currentDist);

                            using (Circle circle = new Circle())
                            {
                                circle.Center = point;
                                circle.Radius = radius;

                                btr.AppendEntity(circle);
                                tr.AddNewlyCreatedDBObject(circle, true);
                                totalCirclesCreated++;
                            }
                        }
                    }
                }

                tr.Commit();
            }

            // Очищаем выделение и выводим сообщение
            ed.SetImpliedSelection(Array.Empty<ObjectId>());
            ed.WriteMessage($"\nПолилинии разделены секциями длиной {sectionLength}. Добавлено окружностей: {totalCirclesCreated}.");
        }

        private static void DrawCircles(Coordinates coordinates)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            var radiusOptions = new PromptDoubleOptions("\nВведите радиус для окружностей <10.0>: ")
            {
                DefaultValue = 10.0,
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true
            };

            PromptDoubleResult radiusResult = ed.GetDouble(radiusOptions);

            if (radiusResult.Status != PromptStatus.OK && radiusResult.Status != PromptStatus.None)
                return;

            double radius = radiusResult.Status == PromptStatus.None ? 10.0 : radiusResult.Value;

            // Начинаем транзакцию для добавления объектов в чертеж
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Получаем текущее пространство (Модель или Лист) для записи
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                int count = 0;

                foreach (var pt in coordinates.Points)
                {
                    // Создаем точку центра (Z = 0)
                    Point3d center = new Point3d(pt.X, pt.Y, 0.0);

                    // Создаем новую окружность
                    using (Circle circle = new Circle())
                    {
                        circle.Center = center;
                        circle.Radius = radius;

                        // Добавляем окружность в таблицу блоков пространства
                        btr.AppendEntity(circle);

                        // Сообщаем транзакции о новом объекте
                        tr.AddNewlyCreatedDBObject(circle, true);

                        count++;
                    }
                }

                // Обязательно подтверждаем транзакцию
                tr.Commit();

                ed.WriteMessage($"\nУспешно импортировано и отрисовано {count} окружностей.");
            }
        }

        private static void MoveCircles(PromptSelectionResult selection, double maxDistance, Random random, Transaction tr)
        {
            foreach (SelectedObject selectedObject in selection.Value)
            {
                if (selectedObject == null)
                    continue;

                Entity entity = tr.GetObject(selectedObject.ObjectId, OpenMode.ForWrite) as Entity;

                if (entity is not Circle circle)
                    continue;

                double angle = random.NextDouble() * 2 * Math.PI;

                // Случайное расстояние от 1 до maxDistance
                double distance = 1 + random.NextDouble() * (maxDistance - 1);

                Vector3d displacement = new Vector3d(
                    Math.Cos(angle) * distance,
                    Math.Sin(angle) * distance,
                    0);

                circle.TransformBy(Matrix3d.Displacement(displacement));
            }
        }
    }
}
