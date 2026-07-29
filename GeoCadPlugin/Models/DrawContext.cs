using Autodesk.AutoCAD.DatabaseServices;

namespace GeoCadPlugin
{
    public class DrawContext
    {
        public Database Database;
        public Transaction Transaction;
        public BlockTableRecord ModelSpace;

        public DrawContext(Database database, Transaction transaction, BlockTableRecord modelSpace)
        {
            Database = database;
            Transaction = transaction;
            ModelSpace = modelSpace;
        }
    }
}
