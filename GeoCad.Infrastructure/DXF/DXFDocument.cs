using GeoAppCore.Abstractions.Document;
using GeoAppCore.Geometry;
using netDxf;
using netDxf.Entities;
using System.IO;

namespace GeoCad.Infrastructure.DXF
{
    //public class DXFDocument : IDocument
    //{
    //    public string Name { get; set; }

    //    public string? FilePath { get; set; }

    //    public DxfDocument? Data { get; private set; }

    //    public static DXFDocument Load(string path)
    //    {
    //        var doc = DxfDocument.Load(path);
    //        doc.Name = Path.GetFileNameWithoutExtension(path);

    //        return new DXFDocument
    //        {
    //            Name = Path.GetFileNameWithoutExtension(path),
    //            FilePath = path,
    //            Data = doc
    //        };
    //    }

    //    public IEnumerable<WorkspaceObject> GetObjects()
    //    {
    //        if (Data == null)
    //            yield break;

    //        foreach (var entity in Data.Entities.All)
    //        {
    //            switch (entity)
    //            {
    //                case Polyline3D:
    //                    yield return ConvertPolyline((Polyline3D)entity);
    //                    break;
    //            }
    //        }
    //    }

    //    public PolylineObject ConvertPolyline(Polyline3D polyline)
    //    {
    //        var vertexes = polyline.Vertexes.Select(v => new GeoVector3(v.X, v.Y, v.Z));

    //        return new PolylineObject(vertexes)
    //        {
    //            LayerName = polyline.Layer.Name,
    //            Color =  polyline.Color.ToColor(),
    //        };
    //    }
    //}
}
