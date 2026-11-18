namespace FishFramework
{
    public abstract partial class UIView
    {
        public void Open()
        {
            OnOpen();
        }

        public void Open<P1>(P1 p1)
        {
            if (this is IUIOpen<P1> panel)
            {
                panel.OnOpen(p1);
            }
            else
            {
                OnOpen();
            }
        }

        public void Open<P1, P2>(P1 p1, P2 p2)
        {
            if (this is IUIOpen<P1, P2> panel)
            {
                panel.OnOpen(p1, p2);
            }
            else
            {
                OnOpen();
            }
        }

        public void Open<P1, P2, P3>(P1 p1, P2 p2, P3 p3)
        {
            if (this is IUIOpen<P1, P2, P3> panel)
            {
                panel.OnOpen(p1, p2, p3);
            }
            else
            {
                OnOpen();
            }
        }

        public void Open<P1, P2, P3, P4>(P1 p1, P2 p2, P3 p3, P4 p4)
        {
            if (this is IUIOpen<P1, P2, P3, P4> panel)
            {
                panel.OnOpen(p1, p2, p3, p4);
            }
            else
            {
                OnOpen();
            }
        }

        public void Open<P1, P2, P3, P4, P5>(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            if (this is IUIOpen<P1, P2, P3, P4, P5> panel)
            {
                panel.OnOpen(p1, p2, p3, p4, p5);
            }
            else
            {
                OnOpen();
            }
        }
    }
}