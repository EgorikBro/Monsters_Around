namespace Monsters_Around
{
    public enum TileType
    {
        Floor,
        Wall,
        StairUp,
        StairDown
    }

    public class Tile
    {
        public TileType Type { get; set; }

        public Tile(TileType type)
        {
            Type = type;
        }

        public bool IsWalkable =>
            Type == TileType.Floor ||
            Type == TileType.StairUp ||
            Type == TileType.StairDown;
    }
}