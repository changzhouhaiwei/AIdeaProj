namespace FishXSortPlay
{
    public struct LevelElement 
    {
        private XSortActor xSort;
        public readonly XSortActor XSort => xSort;
        public readonly bool State => xSort != null;

        public static implicit operator LevelElement(XSortActor actor) => new() { xSort = actor };
        public static implicit operator XSortActor(LevelElement element) => element.XSort;

        public void LinkXSort(XSortActor actor)
        {
            xSort = actor;
        }
    }
}
