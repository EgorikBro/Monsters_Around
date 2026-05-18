using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Views
{
    public class MinimapView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        private const int MinimapDiameter = 230;
        private const int MinimapPadding = 16;
        private const int MinimapVisibleTiles = 16;

        public MinimapView(SpriteBatch spriteBatch, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _pixel = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void DrawMinimap(Map map, Player player)
        {
            var vp = _graphicsDevice.Viewport;
            var miniRect = new Rectangle(
                MinimapPadding,
                vp.Height - MinimapDiameter - MinimapPadding,
                MinimapDiameter,
                MinimapDiameter);

            var viewW = Math.Min(MinimapVisibleTiles, map.Width);
            var viewH = Math.Min(MinimapVisibleTiles, map.Height);
            var playerMapX = player.WorldPosition.X / GameConstants.TileSize;
            var playerMapY = player.WorldPosition.Y / GameConstants.TileSize;
            var originX = Math.Clamp(playerMapX - viewW / 2f, 0f, map.Width - viewW);
            var originY = Math.Clamp(playerMapY - viewH / 2f, 0f, map.Height - viewH);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawCircularBackdrop(miniRect, Color.Black * 0.6f);
            DrawMapTiles(miniRect, map, player, true, originX, originY, viewW, viewH);
            _spriteBatch.End();
        }

        public void DrawFullMapOverlay(Map map, Player player)
        {
            var vp = _graphicsDevice.Viewport;
            var size = Math.Max(220, Math.Min(vp.Width, vp.Height) - 120);
            var mapRect = new Rectangle((vp.Width - size) / 2, (vp.Height - size) / 2, size, size);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, mapRect, Color.Black * 0.65f);
            DrawMapTiles(mapRect, map, player, false, 0f, 0f, map.Width, map.Height);
            _spriteBatch.End();
        }

        private void DrawMapTiles(Rectangle targetRect, Map map, Player player,
            bool circularMask, float startX, float startY, int visibleW, int visibleH)
        {
            var cellW = targetRect.Width / (float)visibleW;
            var cellH = targetRect.Height / (float)visibleH;
            var centerX = targetRect.X + targetRect.Width * 0.5f;
            var centerY = targetRect.Y + targetRect.Height * 0.5f;
            var radius = targetRect.Width * 0.5f;
            var radiusSq = radius * radius;

            var tileStartX = Math.Max(0, (int)Math.Floor(startX));
            var tileStartY = Math.Max(0, (int)Math.Floor(startY));
            var tileEndX = Math.Min(map.Width, (int)Math.Ceiling(startX + visibleW));
            var tileEndY = Math.Min(map.Height, (int)Math.Ceiling(startY + visibleH));

            for (var x = tileStartX; x < tileEndX; x++)
            {
                for (var y = tileStartY; y < tileEndY; y++)
                {
                    if (!map.IsExplored(x, y)) continue;

                    var cellRect = new Rectangle(
                        targetRect.X + (int)MathF.Floor((x - startX) * cellW),
                        targetRect.Y + (int)MathF.Floor((y - startY) * cellH),
                        Math.Max(1, (int)Math.Ceiling(cellW)),
                        Math.Max(1, (int)Math.Ceiling(cellH)));

                    if (circularMask)
                    {
                        var ddx = cellRect.Center.X - centerX;
                        var ddy = cellRect.Center.Y - centerY;
                        if (ddx * ddx + ddy * ddy > radiusSq) continue;
                    }

                    var type = map.GetTileType(x, y);
                    Color color;
                    if (type == TileType.StairDown)     color = Color.OrangeRed;
                    else if (type == TileType.StairUp)  color = Color.CornflowerBlue;
                    else if (type == TileType.Wall)     color = new Color(70, 70, 80, 190);
                    else                                color = new Color(190, 190, 200, 230);

                    _spriteBatch.Draw(_pixel, cellRect, color);
                }
            }

            DrawPlayerDot(targetRect, map, player, circularMask, startX, startY, visibleW, visibleH,
                centerX, centerY, radius * radius);
        }

        private void DrawPlayerDot(Rectangle targetRect, Map map, Player player,
            bool circularMask, float startX, float startY, int visibleW, int visibleH,
            float centerX, float centerY, float radiusSq)
        {
            var cellW = targetRect.Width / (float)visibleW;
            var cellH = targetRect.Height / (float)visibleH;
            var playerMapX = player.WorldPosition.X / GameConstants.TileSize;
            var playerMapY = player.WorldPosition.Y / GameConstants.TileSize;
            var playerRect = new Rectangle(
                targetRect.X + (int)((playerMapX - startX) * cellW),
                targetRect.Y + (int)((playerMapY - startY) * cellH),
                Math.Max(2, (int)Math.Ceiling(cellW)),
                Math.Max(2, (int)Math.Ceiling(cellH)));

            if (circularMask)
            {
                var ddx = playerRect.Center.X - centerX;
                var ddy = playerRect.Center.Y - centerY;
                if (ddx * ddx + ddy * ddy > radiusSq) return;
            }

            _spriteBatch.Draw(_pixel, playerRect, Color.LimeGreen);
        }

        private void DrawCircularBackdrop(Rectangle rect, Color color)
        {
            var centerX = rect.X + rect.Width * 0.5f;
            var centerY = rect.Y + rect.Height * 0.5f;
            var radius = rect.Width * 0.5f;
            var radiusSq = radius * radius;

            for (var x = rect.Left; x < rect.Right; x++)
            {
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSq)
                        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 1, 1), color);
                }
            }
        }
    }
}
