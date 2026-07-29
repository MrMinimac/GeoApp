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

            LayerTable lt =
                (LayerTable)tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead);


            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();

                LayerTableRecord lay =
                    new LayerTableRecord();

                lay.Name = layerName;

                lt.Add(lay);

                tr.AddNewlyCreatedDBObject(
                    lay,
                    true);
            }
        }
    }
}
