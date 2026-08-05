using GeoAppWpf.Interfaces;

namespace GeoAppWpf.Services
{
    public class UndoManager
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();

        public bool CanUndo => _undoStack.Count != 0;
        public bool CanRedo => _redoStack.Count != 0;

        public event Action? StateChanged;

        public void Execute(IUndoableCommand command)
        {
            command.Execute();

            _undoStack.Push(command);
            _redoStack.Clear();

            StateChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo)
                return;

            var command = _undoStack.Pop();
            command.Undo();

            _redoStack.Push(command);

            StateChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo)
                return;

            var command = _redoStack.Pop();
            command.Execute();

            _undoStack.Push(command);

            StateChanged?.Invoke();
        }
    }
}
