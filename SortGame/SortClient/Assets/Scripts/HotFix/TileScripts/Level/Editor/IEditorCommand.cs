namespace FishTilePlay
{
    public interface IEditorCommand
    {
        void Execute();
        void RevertLast();
    }
} 