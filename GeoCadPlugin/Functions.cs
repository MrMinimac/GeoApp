using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Microsoft.Win32;

namespace GeoCadPlugin
{
    public static class Functions
    {
        public static void MoveCircles(PromptSelectionResult selection, double maxDistance, Transaction tr)
        {
            Random random = new Random();

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
                    0
                );

                circle.TransformBy(Matrix3d.Displacement(displacement));
            }
        }

        public static int DrawCircles(Database db, Coordinates coordinates, double radius)
        {
            int count = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)
                    tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (var pt in coordinates.Points)
                {
                    Point3d center = new Point3d(pt.X, pt.Y, 0.0);

                    using (Circle circle = new Circle())
                    {
                        circle.Center = center;
                        circle.Radius = radius;

                        btr.AppendEntity(circle);

                        tr.AddNewlyCreatedDBObject(circle, true);

                        count++;
                    }
                }

                tr.Commit();
            }

            return count;
        }

        public static int DrawBoreholesOnLine(Database db,  PromptSelectionResult selection, double sectionLength, double radius)
        {
            int totalCirclesCreated = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)
                    tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject selObj in selection.Value)
                {
                    if (selObj == null)
                        continue;

                    if (tr.GetObject(selObj.ObjectId, OpenMode.ForRead) is Curve curve)
                    {
                        double totalLength = curve.GetDistanceAtParameter(curve.EndParam);

                        // Проходим вдоль полилинии с заданным шагом
                        for (
                            double currentDist = 0;
                            currentDist <= totalLength;
                            currentDist += sectionLength
                        )
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

            return totalCirclesCreated;
        }

        public static string? ExcelFileDialog()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите Excel-файл",
                Filter = "Excel файлы (*.xlsx;*.xlsm)|*.xlsx;*.xlsm",
                Multiselect = false,
                CheckFileExists = true,
            };

            if (dialog.ShowDialog() != true)
                return null;

            return dialog.FileName;
        }
    }
}
