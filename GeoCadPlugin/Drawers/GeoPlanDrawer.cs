using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.Managers;
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
            var boreholes = line.Boreholes.OrderBy(x => x.Id).ToList();

            if (boreholes.Count < 2)
                return;

            Borehole first = boreholes.First();

            // Базовый физический угол линии (до разворота текста)
            double baseAngle = NormalizeAngleRadians(GetAngle(line.Azimuth) + Math.PI / 2);

            // Угол для читаемости текста (может быть развернут на 180°)
            double textAngle = GetTextAngle(line.Azimuth);

            // Отрисовка номера линии с учетом обоих углов
            DrawLineNumber(line.Id, first, textAngle, baseAngle);

            // Скважины
            var sections = line.BuildSections();

            foreach (var cbh in sections)
            {
                DrawBorehole(cbh, textAngle);
            }
        }

        private static void DrawLineNumber(string number, Borehole first, double textAngle, double baseAngle)
        {
            ACDOC doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double offset = 10;

                // Позицию вычисляем строго по БАЗОВОМУ углу, 
                // чтобы точка всегда оставалась перед первой скважиной
                Point3d position = new Point3d(
                    first.X - Math.Sin(baseAngle) * offset,
                    first.Y + Math.Cos(baseAngle) * offset,
                    first.Z);

                DBText text = new DBText
                {
                    Height = 3,
                    TextString = number,
                    Rotation = textAngle, // Сам текст поворачиваем для читаемости
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid
                };

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

            return angle * Math.PI / 180.0;
        }

        private static double GetTextAngle(double azimuth)
        {
            // Основное направление
            double angle = GetAngle(azimuth) + Math.PI / 2;

            // Нормализуем в диапазон 0..2π
            angle = NormalizeAngleRadians(angle);

            // Если направление текста находится в верхней
            // "перевёрнутой" половине, разворачиваем на 180°.
            //
            // 90° ... 270° => текст будет перевёрнут.
            if (angle > Math.PI / 2 && angle < 3 * Math.PI / 2)
            {
                angle += Math.PI;
            }

            return NormalizeAngleRadians(angle);
        }

        public static void DrawBorehole(SectionBorehole cbh, double angle)
        {
            ACDOC doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var bh = cbh.Source;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                LayerManager.CreateLayers(
                    db,
                    tr,
                    [
                        GeoLayers.Boreholes,
                        GeoLayers.BoreholeNumbers,
                        GeoLayers.AbsoluteElevations,
                        GeoLayers.Deapths,
                        GeoLayers.PeatThinckness,
                        GeoLayers.SandThickness,
                        GeoLayers.GradeAvgs,
                        GeoLayers.EmptyAvgs,
                        GeoLayers.NotDeterminedAvgs,
                    ]);

                // -------------------------------------------------
                // Скважина
                // -------------------------------------------------

                Point3d pt = new Point3d(bh.X, bh.Y, bh.Z);
                Circle circle = new Circle(pt, Vector3d.ZAxis, 1.5);
                circle.Layer = LayerManager.GetLayerName(GeoLayers.Boreholes);

                ms.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                // -------------------------------------------------
                // Номер скважины
                // -------------------------------------------------

                var idText = BuildText(bh.Id.ToString(), bh.X, bh.Y, bh.Z, angle);

                ms.AppendEntity(idText);
                tr.AddNewlyCreatedDBObject(idText, true);

                idText.Layer = LayerManager.GetLayerName(GeoLayers.BoreholeNumbers);

                // -------------------------------------------------
                // Абсолютная отметка
                // -------------------------------------------------

                var zText = BuildText(
                    $"{bh.Z:f1}",
                    bh.X,
                    bh.Y,
                    bh.Z,
                    angle,
                    TextPlacement.Left,
                    verticalOffset: 1.5,
                    offset: 3);

                ms.AppendEntity(zText);
                tr.AddNewlyCreatedDBObject(zText, true);

                zText.Layer = LayerManager.GetLayerName(GeoLayers.AbsoluteElevations);

                // -------------------------------------------------
                // Глубина
                // -------------------------------------------------

                var deapthText = BuildText(
                    $"{bh.Deapth:f1}",
                    bh.X,
                    bh.Y,
                    bh.Z,
                    angle,
                    TextPlacement.Left,
                    verticalOffset: -1.5,
                    offset: 3);

                ms.AppendEntity(deapthText);
                tr.AddNewlyCreatedDBObject(deapthText, true);

                deapthText.Layer = LayerManager.GetLayerName(GeoLayers.Deapths);

                // Мощность торфов
                var peatText = BuildText(
                    cbh.OreInterval?.From.ToString("F1") ?? "-",
                    bh.X,
                    bh.Y,
                    bh.Z,
                    angle,
                    TextPlacement.Right,
                    verticalOffset: 1.5,
                    offset: 3);

                ms.AppendEntity(peatText);
                tr.AddNewlyCreatedDBObject(peatText, true);

                peatText.Layer = LayerManager.GetLayerName(GeoLayers.PeatThinckness);

                // Мощность песков
                var sandText = BuildText(
                    cbh.OreInterval?.Thickness.ToString("F1") ?? "-",
                    bh.X,
                    bh.Y,
                    bh.Z,
                    angle,
                    TextPlacement.Right,
                    verticalOffset: -1.5,
                    offset: 3);

                ms.AppendEntity(sandText);
                tr.AddNewlyCreatedDBObject(sandText, true);

                sandText.Layer = LayerManager.GetLayerName(GeoLayers.SandThickness);

                // -------------------------------------------------
                // Содержание
                // -------------------------------------------------

                var pureAvgGrade = cbh.OreInterval?.PureAvgGrade ?? 0;

                var layer = pureAvgGrade switch
                {
                    0 => GeoLayers.EmptyAvgs,
                    -1 => GeoLayers.NotDeterminedAvgs,
                    _ => GeoLayers.GradeAvgs,
                };

                var pureAvgGradeText = pureAvgGrade switch
                {
                    0 => "пс",
                    -1 => "зн",
                    _ => pureAvgGrade.ToString("F3"),
                };

                var avgText = BuildText(
                    pureAvgGradeText,
                    bh.X,
                    bh.Y,
                    bh.Z,
                    angle,
                    TextPlacement.Right,
                    verticalOffset: 0,
                    offset: 10);

                ms.AppendEntity(avgText);
                tr.AddNewlyCreatedDBObject(avgText, true);

                avgText.Layer = LayerManager.GetLayerName(layer);

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
            // -------------------------------------------------
            // Локальное направление относительно скважины
            // -------------------------------------------------

            double sideAngle = placement switch
            {
                TextPlacement.Forward => angle,
                TextPlacement.Left => angle + Math.PI / 2,
                TextPlacement.Backward => angle + Math.PI,
                TextPlacement.Right => angle - Math.PI / 2,
                _ => angle
            };

            // -------------------------------------------------
            // Выравнивание текста
            // -------------------------------------------------

            var hMode = placement switch
            {
                TextPlacement.Left => TextHorizontalMode.TextRight,
                TextPlacement.Right => TextHorizontalMode.TextLeft,
                _ => TextHorizontalMode.TextCenter
            };

            // -------------------------------------------------
            // Вектор бокового смещения
            // -------------------------------------------------

            double sideX = -Math.Sin(sideAngle);
            double sideY = Math.Cos(sideAngle);

            // -------------------------------------------------
            // Вектор "вверх/вниз" относительно текста
            // -------------------------------------------------

            double textX = -Math.Sin(angle);
            double textY = Math.Cos(angle);

            // -------------------------------------------------
            // Итоговая позиция
            //
            // ВАЖНО:
            // angle уже может быть развернут на 180°.
            // Поэтому ВСЕ offset автоматически вращаются
            // вокруг исходной точки скважины.
            // -------------------------------------------------

            Point3d position = new Point3d(
                x + sideX * offset + textX * verticalOffset,
                y + sideY * offset + textY * verticalOffset,
                z);

            DBText text = new DBText
            {
                TextString = content,
                Height = 2,

                // Сам текст тоже использует тот же угол
                Rotation = angle,

                HorizontalMode = hMode,
                VerticalMode = TextVerticalMode.TextVerticalMid,

                AlignmentPoint = position
            };

            return text;
        }

        private static double NormalizeAngleRadians(double angle)
        {
            angle %= 2 * Math.PI;

            if (angle < 0)
                angle += 2 * Math.PI;

            return angle;
        }
    }
}