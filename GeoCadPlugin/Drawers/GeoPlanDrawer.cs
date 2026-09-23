using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.Managers;
using System.Data;
using ACDOC = Autodesk.AutoCAD.ApplicationServices.Document;

namespace GeoCadPlugin.Drawers
{

    public class GeoPlanDrawer
    {
        public static void Draw(GeoDoc project)
        {
            foreach (var line in project.BoreholeLines)
            {
                DrawLine(line);
            }
        }

        private static void DrawLine(BoreholeLine line)
        {
            // сортировка по номеру скважины
            var boreholes = line.Boreholes
                .OrderBy(x => x.Id)
                .ToList();


            if (boreholes.Count < 2)
                return;


            Borehole first = boreholes.First();
            Borehole last = boreholes.Last();

            // рисуем номер буровой линии
            DrawLineNumber(
                line.Id,
                first,
                GetTextAngle(line.Azimuth));


            // рисуем скважины
            foreach (var bh in boreholes)
                DrawBorehole(bh, GetTextAngle(line.Azimuth));
        }

        private static void DrawLineNumber(string number, Borehole first, double angle)
        {
            ACDOC doc =
                Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double offset = 10;

                // смещение от первой скважины
                Point3d position = new Point3d(
                    first.X - Math.Sin(angle) * offset,
                    first.Y + Math.Cos(angle) * offset,
                    first.Z);

                DBText text = new DBText
                {
                    Height = 3,
                    TextString = number,
                    Rotation = angle,
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid
                };

                // теперь это центр текста
                text.AlignmentPoint = position;

                ms.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);
                tr.Commit();
            }
        }

        private static double GetAngle(double azimuth)
        {
            double angle = 90 - azimuth;

            if (angle < 0)
                angle += 360;

            return angle * Math.PI / 180;
        }

        private static double GetTextAngle(double azimuth)
        {
            double angle = GetAngle(azimuth) + Math.PI / 2;

            if (angle >= Math.PI * 2)
                angle -= Math.PI * 2;

            return angle;
        }

        public static void DrawBorehole(Borehole bh, double angle)
        {
            ACDOC doc =
                Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {


                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                LayerManager.CreateLayers(db, tr,
                    [
                        GeoLayers.Boreholes,
                        GeoLayers.BoreholeNumbers,
                        GeoLayers.AbsoluteElevations,
                        GeoLayers.Deapths,
                        GeoLayers.PeatThinckness,
                        GeoLayers.SandThickness,
                        GeoLayers.Avgs,
                    ]
                 );

                Point3d pt = new Point3d(bh.X, bh.Y, bh.Z);
                Circle circle = new Circle(pt, Vector3d.ZAxis, 1.5);
                circle.Layer = LayerManager.GetLayerName(GeoLayers.Boreholes);

                ms.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                var idText = BuildText(bh.Id.ToString(), bh.X, bh.Y, bh.Z, angle);
                ms.AppendEntity(idText);
                tr.AddNewlyCreatedDBObject(idText, true);
                idText.Layer = LayerManager.GetLayerName(GeoLayers.BoreholeNumbers);

                var zText = BuildText($"{bh.Z:f1}", bh.X, bh.Y, bh.Z, angle, TextPlacement.Left, verticalOffset: 1.5, offset: 3);
                ms.AppendEntity(zText);
                tr.AddNewlyCreatedDBObject(zText, true);
                zText.Layer = LayerManager.GetLayerName(GeoLayers.AbsoluteElevations);

                var deapthText = BuildText($"{bh.Deapth:f1}", bh.X, bh.Y, bh.Z, angle, TextPlacement.Left, verticalOffset: -1.5, offset: 3);
                ms.AppendEntity(deapthText);
                tr.AddNewlyCreatedDBObject(deapthText, true);
                deapthText.Layer = LayerManager.GetLayerName(GeoLayers.Deapths);

                var peatText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: 1.5, offset: 3);
                ms.AppendEntity(peatText);
                tr.AddNewlyCreatedDBObject(peatText, true);
                peatText.Layer = LayerManager.GetLayerName(GeoLayers.PeatThinckness);

                var sandText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: -1.5, offset: 3);
                ms.AppendEntity(sandText);
                tr.AddNewlyCreatedDBObject(sandText, true);
                sandText.Layer = LayerManager.GetLayerName(GeoLayers.SandThickness);

                var avgText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: 0, offset: 10);
                ms.AppendEntity(avgText);
                tr.AddNewlyCreatedDBObject(avgText, true);
                avgText.Layer = LayerManager.GetLayerName(GeoLayers.Avgs);

                tr.Commit();
            }
        }

        private static DBText BuildText(
            string content,
            double x,
            double y,
            double z,
            double angle,
            TextPlacement placement = TextPlacement.Forward,
            double offset = 3,
            double verticalOffset = 0)
        {
            // смещение от скважины в сторону
            double sideAngle = placement switch
            {
                TextPlacement.Forward => angle,
                TextPlacement.Left => angle + Math.PI / 2,
                TextPlacement.Backward => angle + Math.PI,
                TextPlacement.Right => angle - Math.PI / 2,
                _ => angle
            };

            var hMode = placement switch
            {
                TextPlacement.Left => TextHorizontalMode.TextRight,
                TextPlacement.Right => TextHorizontalMode.TextLeft,
                _ => TextHorizontalMode.TextCenter
            };


            double sideX = -Math.Sin(sideAngle);
            double sideY = Math.Cos(sideAngle);


            // вверх/вниз относительно текста
            double textX = -Math.Sin(angle);
            double textY = Math.Cos(angle);


            Point3d position = new Point3d(
                x + sideX * offset + textX * verticalOffset,
                y + sideY * offset + textY * verticalOffset,
                z);


            DBText text = new DBText
            {
                TextString = content,
                Height = 2,
                Rotation = angle,
                HorizontalMode = hMode,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                AlignmentPoint = position,
            };

            return text;
        }
    }
}
