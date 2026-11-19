using System.Collections.Generic;

#if UNITY_EDITOR
namespace FishXSortPlay
{
    public class EditorRevertStack
    {
        private Stack<IEditorCommand> undoStack = new Stack<IEditorCommand>();
        private Stack<IEditorCommand> redoStack = new Stack<IEditorCommand>();

        public void ExecuteCommand(IEditorCommand command)
        {
            command.Execute();
            undoStack.Push(command);
            redoStack.Clear();
        }

        public void RevertLast()
        {
            if (undoStack.Count > 0)
            {
                var command = undoStack.Pop();
                command.RevertLast();
                redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                var command = redoStack.Pop();
                command.Execute();
                undoStack.Push(command);
            }
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
} 

#endif