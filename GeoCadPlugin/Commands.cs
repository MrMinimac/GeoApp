using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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
            var distanceOptions = new PromptDoubleOptions("\nМаксимальное расстояние смещения <3>: ")
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
