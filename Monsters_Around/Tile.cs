namespace Monsters_Around
{
    public enum TileType
    {
        Floor,
        Wall
    }

    public class Tile
    {
        public TileType Type { get; set; }

        public Tile(TileType type)
        {
            Type = type;
        }

        public bool IsWalkable => Type == TileType.Floor;
    }
}
