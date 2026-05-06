using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Monsters_Around
{
    public class Map
    {
        private readonly struct RoomEdge
        {
            public int A { get; }
            public int B { get; }
            public int Weight { get; }

            public RoomEdge(int a, int b, int weight)
            {
                A = a;
                B = b;
                Weight = weight;
            }
        }

        public int Width { get; }
        public int Height { get; }
        public int TileSize { get; }

        private readonly Tile[,] _tiles;
        private readonly bool[,] _explored;
        private readonly List<Rectangle> _rooms = new List<Rectangle>();
        private readonly Random _random;

        private Texture2D _tilesetTexture;
        private Texture2D _markerTexture;
        private Rectangle _floorSourceRect;
        private Rectangle _wallSourceRect;

        private const int WallTileId = 27;
        private const int FloorTileId = 241;
        private const int MinRoomSize = 5;
        private const int MaxRoomSize = 15;
        private const int RoomBuffer = 3;

        public Point PlayerSpawnPoint { get; private set; }
        public Point StairUpPoint { get; private set; }
        public Point StairDownPoint { get; private set; }
        public int FloorIndex { get; }
        public IReadOnlyList<Rectangle> Rooms => _rooms;
        public int StartingRoomIndex => 0;

        public Map(int width, int height, int tileSize, int floorIndex, Random random)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            FloorIndex = floorIndex;
            _random = random;
            _tiles = new Tile[width, height];
            _explored = new bool[width, height];

            GenerateDungeonMap();
        }

        public void LoadContent(Texture2D tilesetTexture)
        {
            _tilesetTexture = tilesetTexture;
            _wallSourceRect = GetSourceRectangleByTileId(WallTileId);
            _floorSourceRect = GetSourceRectangleByTileId(FloorTileId);

            _markerTexture = new Texture2D(_tilesetTexture.GraphicsDevice, 1, 1);
            _markerTexture.SetData(new[] { Color.White });
        }

        public TileType GetTileType(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return TileType.Wall;
            }

            return _tiles[x, y].Type;
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return false;
            }

            return _tiles[x, y].IsWalkable;
        }

        public bool IsInsidePlayerSafeZone(Point p, int radius = 2)
        {
            return Math.Abs(p.X - PlayerSpawnPoint.X) <= radius &&
                   Math.Abs(p.Y - PlayerSpawnPoint.Y) <= radius;
        }

        public bool IsExplored(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return false;
            }

            return _explored[x, y];
        }

        public void UpdateExploration(Point playerPosition)
        {
            if (playerPosition.X < 0 || playerPosition.X >= Width || playerPosition.Y < 0 || playerPosition.Y >= Height)
            {
                return;
            }

            RevealAround(playerPosition, 1);

            var roomIndex = FindRoomContaining(playerPosition);
            if (roomIndex >= 0)
            {
                RevealRoom(_rooms[roomIndex]);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_tilesetTexture == null)
            {
                return;
            }

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var type = _tiles[x, y].Type;
                    var sourceRect = type == TileType.Wall ? _wallSourceRect : _floorSourceRect;
                    var destRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);

                    spriteBatch.Draw(_tilesetTexture, destRect, sourceRect, Color.White);

                    if (_markerTexture != null)
                    {
                        if (type == TileType.StairDown)
                        {
                            DrawStairMarker(spriteBatch, destRect, Color.OrangeRed);
                        }
                        else if (type == TileType.StairUp)
                        {
                            DrawStairMarker(spriteBatch, destRect, Color.CornflowerBlue);
                        }
                    }
                }
            }
        }

        private void DrawStairMarker(SpriteBatch spriteBatch, Rectangle tileRect, Color color)
        {
            var margin = Math.Max(2, TileSize / 4);
            var markerRect = new Rectangle(
                tileRect.X + margin,
                tileRect.Y + margin,
                TileSize - margin * 2,
                TileSize - margin * 2
            );
            spriteBatch.Draw(_markerTexture, markerRect, color);
        }

        private Rectangle GetSourceRectangleByTileId(int tileId)
        {
            var tilesPerRow = _tilesetTexture.Width / TileSize;
            var sourceX = (tileId % tilesPerRow) * TileSize;
            var sourceY = (tileId / tilesPerRow) * TileSize;
            return new Rectangle(sourceX, sourceY, TileSize, TileSize);
        }

        private void GenerateDungeonMap()
        {
            FillWithWalls();
            _rooms.Clear();

            var targetRoomCount = Math.Clamp((Width * Height) / 250, 10, 32);
            var maxPlacementAttempts = targetRoomCount * 15;

            for (var attempt = 0; attempt < maxPlacementAttempts && _rooms.Count < targetRoomCount; attempt++)
            {
                var roomWidth = _random.Next(MinRoomSize, MaxRoomSize + 1);
                var roomHeight = _random.Next(MinRoomSize, MaxRoomSize + 1);
                var roomX = _random.Next(1, Width - roomWidth - 1);
                var roomY = _random.Next(1, Height - roomHeight - 1);
                var candidate = new Rectangle(roomX, roomY, roomWidth, roomHeight);

                if (IntersectsExistingRoomsWithPadding(candidate, RoomBuffer))
                {
                    continue;
                }

                CarveRoom(candidate);
                _rooms.Add(candidate);
            }

            if (_rooms.Count == 0)
            {
                var fallback = new Rectangle(Width / 2 - 2, Height / 2 - 2, 5, 5);
                CarveRoom(fallback);
                _rooms.Add(fallback);
            }

            ConnectRoomsWithMstAndLoops();
            PlaceStairsAndSpawn();
        }

        private void FillWithWalls()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    _tiles[x, y] = new Tile(TileType.Wall);
                    _explored[x, y] = false;
                }
            }
        }

        private bool IntersectsExistingRoomsWithPadding(Rectangle candidate, int padding)
        {
            var candidateWithPadding = new Rectangle(
                candidate.X - padding,
                candidate.Y - padding,
                candidate.Width + padding * 2,
                candidate.Height + padding * 2
            );

            foreach (var room in _rooms)
            {
                var existingWithPadding = new Rectangle(
                    room.X - padding,
                    room.Y - padding,
                    room.Width + padding * 2,
                    room.Height + padding * 2
                );

                if (candidateWithPadding.Intersects(existingWithPadding))
                {
                    return true;
                }
            }

            return false;
        }

        private void CarveRoom(Rectangle room)
        {
            for (var x = room.Left; x < room.Right; x++)
            {
                for (var y = room.Top; y < room.Bottom; y++)
                {
                    SetTileType(x, y, TileType.Floor);
                }
            }
        }

        private void ConnectRoomsWithMstAndLoops()
        {
            var allEdges = BuildAllPotentialRoomEdges();
            if (allEdges.Count == 0)
            {
                return;
            }

            var mstEdges = BuildMstWithPrim(allEdges);
            var mstKeys = new HashSet<(int, int)>();
            foreach (var edge in mstEdges)
            {
                mstKeys.Add(GetEdgeKey(edge.A, edge.B));
            }

            var rejectedEdges = new List<RoomEdge>();
            foreach (var edge in allEdges)
            {
                if (!mstKeys.Contains(GetEdgeKey(edge.A, edge.B)))
                {
                    rejectedEdges.Add(edge);
                }
            }

            var extraLoopEdges = SelectExtraLoopEdges(rejectedEdges);
            var carvedConnections = new HashSet<(int, int)>();

            foreach (var edge in mstEdges)
            {
                TryCarveUniqueConnection(edge, carvedConnections);
            }

            foreach (var edge in extraLoopEdges)
            {
                TryCarveUniqueConnection(edge, carvedConnections);
            }
        }

        private void TryCarveUniqueConnection(RoomEdge edge, HashSet<(int, int)> carvedConnections)
        {
            var key = GetEdgeKey(edge.A, edge.B);
            if (carvedConnections.Contains(key))
            {
                return;
            }

            CarvePathAStar(GetRoomCenter(_rooms[edge.A]), GetRoomCenter(_rooms[edge.B]));
            carvedConnections.Add(key);
        }

        private List<RoomEdge> BuildAllPotentialRoomEdges()
        {
            var edges = new List<RoomEdge>();
            for (var i = 0; i < _rooms.Count; i++)
            {
                var a = GetRoomCenter(_rooms[i]);
                for (var j = i + 1; j < _rooms.Count; j++)
                {
                    var b = GetRoomCenter(_rooms[j]);
                    var manhattan = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
                    edges.Add(new RoomEdge(i, j, manhattan));
                }
            }

            return edges;
        }

        private List<RoomEdge> BuildMstWithPrim(List<RoomEdge> allEdges)
        {
            var mst = new List<RoomEdge>();
            if (_rooms.Count <= 1)
            {
                return mst;
            }

            var connected = new HashSet<int> { 0 };
            while (connected.Count < _rooms.Count)
            {
                RoomEdge? best = null;
                foreach (var edge in allEdges)
                {
                    var aConnected = connected.Contains(edge.A);
                    var bConnected = connected.Contains(edge.B);
                    if (aConnected == bConnected)
                    {
                        continue;
                    }

                    if (!best.HasValue || edge.Weight < best.Value.Weight)
                    {
                        best = edge;
                    }
                }

                if (!best.HasValue)
                {
                    break;
                }

                mst.Add(best.Value);
                connected.Add(best.Value.A);
                connected.Add(best.Value.B);
            }

            return mst;
        }

        private List<RoomEdge> SelectExtraLoopEdges(List<RoomEdge> rejectedEdges)
        {
            var selected = new List<RoomEdge>();
            if (rejectedEdges.Count == 0)
            {
                return selected;
            }

            // Добавляем немного лишних связей, чтобы карта была не слишком прямой.
            var percent = _random.Next(10, 16);
            var count = (int)Math.Round(rejectedEdges.Count * (percent / 100f));
            var clampedCount = Math.Clamp(count, 1, rejectedEdges.Count);

            for (var i = rejectedEdges.Count - 1; i > 0; i--)
            {
                var swap = _random.Next(i + 1);
                (rejectedEdges[i], rejectedEdges[swap]) = (rejectedEdges[swap], rejectedEdges[i]);
            }

            for (var i = 0; i < clampedCount; i++)
            {
                selected.Add(rejectedEdges[i]);
            }

            return selected;
        }

        private static (int, int) GetEdgeKey(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }

        private void CarvePathAStar(Point start, Point end)
        {
            var openSet = new PriorityQueue<Point, int>();
            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, int>();

            openSet.Enqueue(start, 0);
            gScore[start] = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current == end)
                {
                    break;
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    var moveCost = _tiles[neighbor.X, neighbor.Y].Type == TileType.Floor ? 1 : 10;
                    var turnPenalty = 0;
                    var proximityPenalty = 0;

                    if (_tiles[neighbor.X, neighbor.Y].Type == TileType.Wall)
                    {
                        var adjacentFloors = 0;
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            for (var dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                var nx = neighbor.X + dx;
                                var ny = neighbor.Y + dy;
                                if (nx > 0 && nx < Width - 1 && ny > 0 && ny < Height - 1)
                                {
                                    if ((nx != current.X || ny != current.Y) && _tiles[nx, ny].Type != TileType.Wall)
                                    {
                                        adjacentFloors++;
                                    }
                                }
                            }
                        }
                        proximityPenalty = adjacentFloors * 100;
                    }

                    if (cameFrom.TryGetValue(current, out var prev))
                    {
                        var wasHorizontal = prev.Y == current.Y;
                        var isHorizontal = current.Y == neighbor.Y;
                        if (wasHorizontal != isHorizontal)
                        {
                            turnPenalty = 5;
                        }
                    }

                    var tentative_gScore = gScore[current] + moveCost + turnPenalty + proximityPenalty;

                    if (!gScore.TryGetValue(neighbor, out var currentG) || tentative_gScore < currentG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative_gScore;

                        var h = (Math.Abs(neighbor.X - end.X) + Math.Abs(neighbor.Y - end.Y)) * 5;
                        openSet.Enqueue(neighbor, tentative_gScore + h);
                    }
                }
            }

            var curr = end;
            while (cameFrom.ContainsKey(curr))
            {
                SetTileType(curr.X, curr.Y, TileType.Floor);
                curr = cameFrom[curr];
            }
        }

        private IEnumerable<Point> GetNeighbors(Point p)
        {
            if (p.X > 1) yield return new Point(p.X - 1, p.Y);
            if (p.X < Width - 2) yield return new Point(p.X + 1, p.Y);
            if (p.Y > 1) yield return new Point(p.X, p.Y - 1);
            if (p.Y < Height - 2) yield return new Point(p.X, p.Y + 1);
        }

        private void PlaceStairsAndSpawn()
        {
            PlayerSpawnPoint = GetRoomCenter(_rooms[0]);

            var farthestIndex = 0;
            var maxDistance = -1;
            for (var i = 0; i < _rooms.Count; i++)
            {
                var center = GetRoomCenter(_rooms[i]);
                var distance = Math.Abs(center.X - PlayerSpawnPoint.X) + Math.Abs(center.Y - PlayerSpawnPoint.Y);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestIndex = i;
                }
            }

            StairDownPoint = GetRoomCenter(_rooms[farthestIndex]);
            StairUpPoint = PlayerSpawnPoint;

            SetTileType(StairDownPoint.X, StairDownPoint.Y, TileType.StairDown);
            SetTileType(StairUpPoint.X, StairUpPoint.Y, TileType.StairUp);
        }

        private void SetTileType(int x, int y, TileType type)
        {
            // Край карты всегда стена, чтобы персонаж не уходил за границы.
            if (x <= 0 || x >= Width - 1 || y <= 0 || y >= Height - 1)
            {
                return;
            }

            _tiles[x, y].Type = type;
        }

        private static Point GetRoomCenter(Rectangle room)
        {
            return new Point(room.X + room.Width / 2, room.Y + room.Height / 2);
        }

        private int FindRoomContaining(Point p)
        {
            for (var i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].Contains(p))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RevealRoom(Rectangle room)
        {
            for (var x = room.Left; x < room.Right; x++)
            {
                for (var y = room.Top; y < room.Bottom; y++)
                {
                    _explored[x, y] = true;
                }
            }
        }

        private void RevealAround(Point center, int radius)
        {
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                for (var y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    if (x >= 0 && x < Width && y >= 0 && y < Height)
                    {
                        _explored[x, y] = true;
                    }
                }
            }
        }
    }
}
