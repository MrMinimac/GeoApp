using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore.Services;

namespace GeoCadPlugin.Topography
{
    internal static class ContourDrawer
    {
        private const string LayerName =
            "Горизонтали";

        public static int Draw(Database db, List<ContourLine> contours)
        {
            using Transaction tr = db.TransactionManager.StartTransaction();

            BlockTable bt =
                (BlockTable)tr.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead);

            BlockTableRecord modelSpace =
                (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite);

            LayerTable layerTable =
                (LayerTable)tr.GetObject(
                    db.LayerTableId,
                    OpenMode.ForRead);

            ObjectId layerId;

            if (layerTable.Has(LayerName))
            {
                layerId =
                    layerTable[LayerName];
            }
            else
            {
                layerTable.UpgradeOpen();

                var layer =
                    new LayerTableRecord
                    {
                        Name = LayerName
                    };

                layerId =
                    layerTable.Add(layer);

                tr.AddNewlyCreatedDBObject(
                    layer,
                    true);
            }

            int count = 0;

            foreach (ContourLine contour
                     in contours)
            {
                if (contour.Points.Count < 2)
                    continue;

                var polyline =
                    new Polyline();

                for (int i = 0;
                     i < contour.Points.Count;
                     i++)
                {
                    ContourPoint point =
                        contour.Points[i];

                    var gk =
                        GaussKrugerConverter
                            .GeodeticToGK(
                                point.Latitude,
                                point.Longitude);

                    // В твоей системе:
                    // AutoCAD X = GK Easting = gk.Y
                    // AutoCAD Y = GK Northing = gk.X

                    polyline.AddVertexAt(
                        i,
                        new Point2d(
                            gk.Y,
                            gk.X),
                        0,
                        0,
                        0);
                }

                polyline.LayerId =
                    layerId;

                // Высота самой горизонтали.
                polyline.Elevation =
                    contour.Elevation;

                polyline.Closed =
                    contour.IsClosed;

                modelSpace.AppendEntity(
                    polyline);

                tr.AddNewlyCreatedDBObject(
                    polyline,
                    true);

                count++;
            }

            tr.Commit();

            return count;
        }
    }
}