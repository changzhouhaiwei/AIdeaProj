namespace FishXSortPlay
{
    public interface IEditorCommand
    {
        void Execute();
        void RevertLast();
    }
} 