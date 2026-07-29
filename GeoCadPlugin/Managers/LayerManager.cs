using Autodesk.AutoCAD.DatabaseServices;

namespace GeoCadPlugin.Managers
{
    public static class LayerManager
    {
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
                GeoLayers.Rulers => "GEO_Rulers",
                GeoLayers.Tables => "GEO_Tables",
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
