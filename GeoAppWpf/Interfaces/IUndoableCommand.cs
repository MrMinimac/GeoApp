namespace GeoAppWpf.Interfaces
{
    public interface IUndoableCommand
    {
        void Execute();
        void Undo();
    }
}
