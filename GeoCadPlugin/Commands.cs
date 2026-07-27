using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GeoAppCore;
using ACDOC = Autodesk.AutoCAD.ApplicationServices.Document;

namespace GeoCadPlugin
{
    public class Commands
    {
        [CommandMethod("GEOIMPORT")]
        public void GeoImport()
        {
            //ACDOC acadDoc = Application.DocumentManager.MdiActiveDocument;

            //Editor ed = acadDoc.Editor;

            //ed.WriteMessage("\nGEOIMPORT...");

            //string path = @"C:\GeoAppTemp\project.json";

            //var doc = GeoDoc.Load(path);

            //foreach (var line in doc.BoreholeLines)
            //{
            //    GeoPlanDrawer.DrawLine(line);
            //}
        }
    }
}
