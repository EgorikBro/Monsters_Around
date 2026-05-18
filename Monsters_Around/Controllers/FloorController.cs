using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monsters_Around.Models;
using System;
using System.Collections.Generic;

namespace Monsters_Around.Controllers
{
    public class FloorController
    {
        private readonly Dictionary<int, Map> _generatedFloors = new();
        private readonly Random _random;

        public FloorController(Random random)
        {
            _random = random;
        }

        public Map GetOrCreate(int floorIndex, Texture2D tilesetTexture)
        {
            if (_generatedFloors.TryGetValue(floorIndex, out var existing))
                return existing;

            var map = new Map(GameConstants.MapWidth, GameConstants.MapHeight, GameConstants.TileSize, floorIndex, _random);
            if (tilesetTexture != null)
                map.LoadContent(tilesetTexture);

            _generatedFloors[floorIndex] = map;
            return map;
        }

        public void LoadAllContent(Texture2D tilesetTexture)
        {
            foreach (var floor in _generatedFloors.Values)
                floor.LoadContent(tilesetTexture);
        }

        public void ClearAll()
        {
            _generatedFloors.Clear();
        }

        // Returns the new current map if a transition occurred, null otherwise.
        public Map HandleStairTransitions(Map currentMap, GameState state, Player player,
            EnemyController enemies, CombatLog log, Texture2D tilesetTexture)
        {
            var tile = currentMap.GetTileType(player.Position.X, player.Position.Y);
            if (tile != TileType.StairDown && tile != TileType.StairUp)
            {
                state.StairTransitionLock = false;
                return null;
            }

            if (state.StairTransitionLock) return null;

            Map newMap = null;
            if (tile == TileType.StairDown)
                newMap = MoveToFloor(state.CurrentFloorIndex + 1, true, state, player, enemies, log, tilesetTexture);
            else if (tile == TileType.StairUp && state.CurrentFloorIndex > 0)
                newMap = MoveToFloor(state.CurrentFloorIndex - 1, false, state, player, enemies, log, tilesetTexture);

            state.StairTransitionLock = true;
            return newMap;
        }

        public Map MoveToFloor(int targetIndex, bool comingFromAbove, GameState state, Player player,
            EnemyController enemies, CombatLog log, Texture2D tilesetTexture)
        {
            state.CurrentFloorIndex = targetIndex;
            var map = GetOrCreate(targetIndex, tilesetTexture);

            var spawnPoint = comingFromAbove ? map.StairUpPoint : map.StairDownPoint;
            player.SetMapAndPosition(map, spawnPoint);
            map.UpdateExploration(player.Position);
            enemies.SpawnEnemies(map, player, state);

            var floorNumber = targetIndex + 1;
            log.AddMessage(
                comingFromAbove ? $"Спуск на этаж {floorNumber}" : $"Подъем на этаж {floorNumber}",
                new Color(170, 205, 255));

            return map;
        }
    }
}
