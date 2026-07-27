using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using ACDOC = Autodesk.AutoCAD.ApplicationServices.Document;

namespace GeoCadPlugin
{
    public enum TextPlacement
    {
        Left, Right, Forward, Backward
    }

    public class GeoSectionDrawer
    {
        public static void Draw(GeoDoc project)
        {
            foreach (var line in project.BoreholeLines)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    $"BHsCount: {line.Boreholes.Count},\n" +
                    $"MinMaxZ: {line.MinZ}, {line.MaxZ}\n----\n"
                    );
               
                foreach (var bh in line.Boreholes)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"SamplesCount: {bh.SamplesCount},\n" +
                        $"Deapth: {bh.Deapth},\n" +
                        $"Z: {bh.Z}\n----\n"
                        );
                }

                DrawVertRuler(-20, line.MinZ, line.MaxZ, project.VerticalScale);
                DrawLine(line.BuildSections(), project.VerticalScale);
            }
        }

        private static void DrawVertRuler(double startX, double minZ, double maxZ, int scale, double step = 5, double tickStep = 1, double width = 1)
        {
            ACDOC doc =
               Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                maxZ = Math.Ceiling(maxZ / step) * step;
                minZ = Math.Floor(minZ / step) * step;

                doc.Editor.WriteMessage($"\nMinZ={minZ}, MaxZ={maxZ}");

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                LayerManager.CreateLayer(db, tr, LayerManager.GetLayerName(LayerManager.GeoLayers.Ruler));

                Line axis = new Line(new Point3d(startX, minZ * scale, 0), new Point3d(startX, maxZ * scale, 0));
                axis.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Ruler);

                ms.AppendEntity(axis);
                tr.AddNewlyCreatedDBObject(axis, true);

                Line axis2 = new Line(new Point3d(startX + width, minZ * scale, 0), new Point3d(startX + width, maxZ * scale, 0));
                axis2.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Ruler);

                ms.AppendEntity(axis2);
                tr.AddNewlyCreatedDBObject(axis2, true);

                for (double z = minZ; z <= maxZ; z += tickStep)
                {
                    Line tick = new Line(
                        new Point3d(startX + width, z * scale, 0),
                        new Point3d(startX, z * scale, 0));

                    tick.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Ruler);

                    ms.AppendEntity(tick);
                    tr.AddNewlyCreatedDBObject(tick, true);
                }

                for (double z = minZ; z <= maxZ; z += step)
                {
                    DBText text = new DBText
                    {
                        TextString = z.ToString("0"),
                        Height = 2,
                        Position = new Point3d(startX - 7, z * scale, 0)
                    };

                    text.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Ruler);

                    ms.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }

                tr.Commit();
            }
        }

        private static void DrawLine(List<SectionBorehole> line, int vertScale)
        {
            // рисуем скважины
            foreach (var cbh in line)
            {
                DrawBorehole(cbh, vertScale);
            }
        }

        public static void DrawBorehole(SectionBorehole bh, int Scale)
        {
            ACDOC doc =
                Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                LayerManager.CreateLayers(db, tr);

                Line bhLine = new Line(
                    new Point3d(
                        bh.X,
                        bh.Top * Scale,
                        0),

                    new Point3d(
                        bh.X,
                        bh.Bottom * Scale,
                        0));

                bhLine.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Boreholes);

                ms.AppendEntity(bhLine);
                tr.AddNewlyCreatedDBObject(bhLine, true);

                tr.Commit();
            }
        }
    }

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
                line.Number,
                first,
                GetTextAngle(line.Azimuth));


            // рисуем скважины
            foreach (var bh in boreholes)
                DrawBorehole(bh, GetTextAngle(line.Azimuth));
        }

        private static void DrawLineNumber(int number, Borehole first, double angle)
        {
            ACDOC doc =
                Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;


            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {

                BlockTable bt =
                    (BlockTable)tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);


                BlockTableRecord ms =
                    (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);



                double offset = 10;


                // смещение от первой скважины
                Point3d position = new Point3d(
                    first.X - Math.Sin(angle) * offset,
                    first.Y + Math.Cos(angle) * offset,
                    first.Z);



                DBText text = new DBText
                {
                    Height = 3,

                    TextString = $"БЛ-{number}",

                    Rotation = angle,


                    HorizontalMode =
                        TextHorizontalMode.TextCenter,

                    VerticalMode =
                        TextVerticalMode.TextVerticalMid
                };


                // теперь это центр текста
                text.AlignmentPoint = position;


                ms.AppendEntity(text);

                tr.AddNewlyCreatedDBObject(
                    text,
                    true);


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

                LayerManager.CreateLayers(db, tr);

                Point3d pt = new Point3d(bh.X, bh.Y, bh.Z);
                Circle circle = new Circle(pt, Vector3d.ZAxis, 1.5);
                circle.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Boreholes);

                ms.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                var idText = BuildText(bh.Id.ToString(), bh.X, bh.Y, bh.Z, angle);
                ms.AppendEntity(idText);
                tr.AddNewlyCreatedDBObject(idText, true);
                idText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.BoreholeNumbers);

                var zText = BuildText($"{bh.Z:f1}", bh.X, bh.Y, bh.Z, angle, TextPlacement.Left, verticalOffset: 1.5, offset: 3);
                ms.AppendEntity(zText);
                tr.AddNewlyCreatedDBObject(zText, true);
                zText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.AbsoluteElevations);

                var deapthText = BuildText($"{bh.Deapth:f1}", bh.X, bh.Y, bh.Z, angle, TextPlacement.Left, verticalOffset: -1.5, offset: 3);
                ms.AppendEntity(deapthText);
                tr.AddNewlyCreatedDBObject(deapthText, true);
                deapthText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.Deapths);

                var peatText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: 1.5, offset: 3);
                ms.AppendEntity(peatText);
                tr.AddNewlyCreatedDBObject(peatText, true);
                peatText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.PeatThinckness);

                var sandText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: -1.5, offset: 3);
                ms.AppendEntity(sandText);
                tr.AddNewlyCreatedDBObject(sandText, true);
                sandText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.SandThickness);

                var avgText = BuildText($"0,0", bh.X, bh.Y, bh.Z, angle, TextPlacement.Right, verticalOffset: 0, offset: 10);
                ms.AppendEntity(avgText);
                tr.AddNewlyCreatedDBObject(avgText, true);
                avgText.Layer = LayerManager.GetLayerName(LayerManager.GeoLayers.GoldAvgs);

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

                AlignmentPoint = position
            };


            return text;
        }
    }

    public static class LayerManager
    {
        public enum GeoLayers
        {
            Boreholes,
            BoreholeNumbers,
            PeatThinckness,
            SandThickness,
            AbsoluteElevations,
            Deapths,
            GoldAvgs,
            Ruler,
        }

        public static string GetLayerName(GeoLayers layer)
        {
            return layer switch
            {
                GeoLayers.Boreholes => "GEO_Boreholes",
                GeoLayers.BoreholeNumbers => "GEO_Borehole_Numbers",
                GeoLayers.PeatThinckness => "GEO_Peat_Thinckness",
                GeoLayers.SandThickness => "GEO_Sand_Thickness",
                GeoLayers.AbsoluteElevations => "GEO_Absolute_Elevations",
                GeoLayers.Deapths => "GEO_Deapths",
                GeoLayers.GoldAvgs => "GEO_GoldAvgs",
                GeoLayers.Ruler => "GEO_Rulers",
            };
        }

        public static void CreateLayers(Database db, Transaction tr)
        {
            foreach (var lay in Enum.GetValues<GeoLayers>())
                CreateLayer(db, tr, GetLayerName(lay));
        }

        public static void CreateLayer(
            Database db,
            Transaction tr,
            string layerName)
        {
            LayerTable lt =
                (LayerTable)tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead);


            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();

                LayerTableRecord layer =
                    new LayerTableRecord();

                layer.Name = layerName;

                lt.Add(layer);

                tr.AddNewlyCreatedDBObject(
                    layer,
                    true);
            }
        }
    }
}
