using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monsters_Around
{
    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; }

        private Tile[,] _tiles;

        private Texture2D _floorTexture;
        private Texture2D _wallTexture;

        public Map(int width, int height, int tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            _tiles = new Tile[width, height];

            GenerateBasicMap();
        }

        public void LoadContent(Texture2D floorTex, Texture2D wallTex)
        {
            _floorTexture = floorTex;
            _wallTexture = wallTex;
        }

        private void GenerateBasicMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    {
                        _tiles[x, y] = new Tile(TileType.Wall);
                    }
                    else
                    {
                        if (x % 5 == 0 && y % 5 == 0)
                        {
                            _tiles[x, y] = new Tile(TileType.Wall);
                        }
                        else
                        {
                            _tiles[x, y] = new Tile(TileType.Floor);
                        }
                    }
                }
            }
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            
            return _tiles[x, y].IsWalkable;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Texture2D tex = _tiles[x, y].Type == TileType.Wall ? _wallTexture : _floorTexture;
                    if (tex != null)
                    {
                        spriteBatch.Draw(
                            tex, 
                            new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), 
                            Color.White
                        );
                    }
                }
            }
        }
    }
}
