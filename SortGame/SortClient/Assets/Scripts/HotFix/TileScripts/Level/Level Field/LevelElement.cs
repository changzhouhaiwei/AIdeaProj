namespace FishTilePlay
{
    public struct LevelElement 
    {
        private TileActor tile;
        public readonly TileActor Tile => tile;
        public readonly bool State => tile != null;

        public static implicit operator LevelElement(TileActor tile) => new() { tile = tile };
        public static implicit operator TileActor(LevelElement element) => element.tile;

        public void LinkTile(TileActor tile)
        {
            this.tile = tile;
        }
    }
}
