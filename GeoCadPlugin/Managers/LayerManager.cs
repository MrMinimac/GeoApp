using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace GeoCadPlugin.Managers
{
    public static class LayerManager
    {
        public static string GetLayerName(GeoLayers layer)
        {
            return layer switch
            {
                GeoLayers.Boreholes => "Скважины",
                GeoLayers.BoreholeNumbers => "Номера скважин",
                GeoLayers.PeatThinckness => "Мощности торфов",
                GeoLayers.SandThickness => "Мощности песков",
                GeoLayers.AbsoluteElevations => "Абсолютные отметки",
                GeoLayers.Deapths => "Глубины скважин",
                GeoLayers.Avgs => "Средние содержания",
                GeoLayers.Rulers => "Линейки масштаба",
                GeoLayers.Tables => "Таблицы",
                GeoLayers.Intervals => "Интервалы проб",
                GeoLayers.EmptyAvgs => "ПС",
                GeoLayers.NotDeterminedAvgs => "ЗН",
                GeoLayers.Header => "Заголовок",
                GeoLayers.Surface => "Поверхность",
                GeoLayers.Litologies => "Литология",
                GeoLayers.OreBody => "Контур",
            };
        }

        //public static void CreateLayers(Database db, Transaction tr)
        //{
        //    foreach (var lay in Enum.GetValues<GeoLayers>())
        //        CreateLayer(db, tr, GetLayerName(lay));
        //}

        public static void CreateLayers(Database db, Transaction tr, GeoLayers[] layerNames)
        {
            foreach (var lay in layerNames)
                CreateLayer(db, tr, lay);
        }

        public static void CreateLayer(Database db, Transaction tr, GeoLayers layer)
        {
            var layerName = GetLayerName(layer);
            var color = GetLayerColor(layer);
            CreateLayer(db, tr, layerName, color);
        }

        public static void CreateLayer(Database db, Transaction tr, string layerName, Color? color = null)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();

                LayerTableRecord lay = new LayerTableRecord();

                lay.Name = layerName;

                if (color != null)
                    lay.Color = color;

                lt.Add(lay);

                tr.AddNewlyCreatedDBObject(lay, true);
            }
        }

        private static Color? GetLayerColor(GeoLayers layer)
        {
            return layer switch
            {
                GeoLayers.Avgs => Color.FromColorIndex(ColorMethod.ByAci, 1),
                GeoLayers.NotDeterminedAvgs => Color.FromColorIndex(ColorMethod.ByAci, 1),
                GeoLayers.OreBody => Color.FromColorIndex(ColorMethod.ByAci, 1),
                _ => null
            };
        }
    }
}
