namespace FishFramework
{
    public interface IUIOpen
    {
    }

    public interface IUIOpen<P1> : IUIOpen
    {
        void OnOpen(P1 p1);
    }

    public interface IUIOpen<P1, P2> : IUIOpen
    {
        void OnOpen(P1 p1, P2 p2);
    }

    public interface IUIOpen<P1, P2, P3> : IUIOpen
    {
        void OnOpen(P1 p1, P2 p2, P3 p3);
    }

    public interface IUIOpen<P1, P2, P3, P4> : IUIOpen
    {
        void OnOpen(P1 p1, P2 p2, P3 p3, P4 p4);
    }

    public interface IUIOpen<P1, P2, P3, P4, P5> : IUIOpen
    {
        void OnOpen(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);
    }
}