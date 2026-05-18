using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Views
{
    public class CombatLogView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        public CombatLogView(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void Update(float dt, CombatLog log)
        {
            var baseY = GetBaseY();
            while (log.TryDequeuePending(out var text, out var color))
            {
                for (var i = 0; i < log.Entries.Count; i++)
                {
                    var e = log.Entries[i];
                    if (e.IsExiting) continue;
                    e.TargetY -= GameConstants.CombatLogLineHeight;
                    if (e.TargetY < baseY - (GameConstants.CombatLogVisibleCount - 1) * GameConstants.CombatLogLineHeight - 0.5f)
                    {
                        e.IsExiting = true;
                        e.TargetY = baseY - GameConstants.CombatLogVisibleCount * GameConstants.CombatLogLineHeight;
                    }
                }
                log.Entries.Add(new CombatLogEntry
                {
                    Text = text, Color = color,
                    Y = baseY + 10f, TargetY = baseY,
                    Alpha = 0f, IsExiting = false
                });
            }

            for (var i = log.Entries.Count - 1; i >= 0; i--)
            {
                var e = log.Entries[i];
                e.Y = MathHelper.Lerp(e.Y, e.TargetY, Math.Clamp(dt * GameConstants.CombatLogMoveSpeed, 0f, 1f));
                if (e.IsExiting)
                {
                    e.Alpha = Math.Max(0f, e.Alpha - dt * GameConstants.CombatLogFadeSpeed);
                    if (e.Alpha <= 0.001f) log.Entries.RemoveAt(i);
                }
                else
                {
                    e.Alpha = Math.Min(1f, e.Alpha + dt * 6f);
                }
            }
        }

        public void UpdateScrollInput(MouseState mouse, MouseState prevMouse, int lastScrollWheel, CombatLog log, out int newScrollWheel)
        {
            newScrollWheel = mouse.ScrollWheelValue;
            if (!log.IsExpanded) { log.ScrollbarDragging = false; return; }

            var wheelDelta = mouse.ScrollWheelValue - lastScrollWheel;
            if (wheelDelta != 0) { log.ScrollOffset -= Math.Sign(wheelDelta); log.ClampScroll(); }

            var panel = GetExpandedPanel();
            var thumb = GetScrollbarThumbRect(panel, log.History.Count, GameConstants.CombatLogExpandedVisibleCount, log.ScrollOffset);

            var leftNow  = mouse.LeftButton == ButtonState.Pressed;
            var leftPrev = prevMouse.LeftButton == ButtonState.Pressed;

            if (leftNow && !leftPrev && thumb.Contains(mouse.X, mouse.Y))
            {
                log.ScrollbarDragging = true;
                log.ScrollbarGrabOffset = mouse.Y - thumb.Y;
            }
            else if (!leftNow) log.ScrollbarDragging = false;

            if (log.ScrollbarDragging && leftNow)
            {
                var maxOffset = Math.Max(0, log.History.Count - GameConstants.CombatLogExpandedVisibleCount);
                if (maxOffset > 0)
                {
                    var track = GetScrollTrackRect(panel);
                    var minY = track.Y;
                    var maxY = track.Bottom - thumb.Height;
                    var newY = (int)Math.Clamp(mouse.Y - log.ScrollbarGrabOffset, minY, maxY);
                    var t = (newY - minY) / (float)Math.Max(1, maxY - minY);
                    log.ScrollOffset = (int)Math.Round(t * maxOffset);
                    log.ClampScroll();
                }
            }
        }

        public void Draw(CombatLog log)
        {
            if (_font == null) return;

            if (log.IsExpanded) { DrawExpanded(log); return; }
            if (log.Entries.Count == 0) return;

            const float x = 16f;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            foreach (var e in log.Entries)
            {
                if (e.Alpha <= 0.001f) continue;
                var pos = new Vector2(x, e.Y);
                _spriteBatch.DrawString(_font, e.Text, pos + Vector2.One, Color.Black * (0.45f * e.Alpha),
                    0f, Vector2.Zero, GameConstants.CombatLogTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, e.Text, pos, e.Color * e.Alpha,
                    0f, Vector2.Zero, GameConstants.CombatLogTextScale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();
        }

        // Must be called inside an active world-space SpriteBatch.Begin() block.
        public void DrawDamagePopups(CombatLog log)
        {
            if (_font == null || log.DamagePopups.Count == 0) return;

            for (var i = 0; i < log.DamagePopups.Count; i++)
            {
                var p = log.DamagePopups[i];
                var alpha = MathHelper.Clamp(p.TimeLeft / p.Lifetime, 0f, 1f);
                var text = p.Value.ToString();
                var color = p.IsCrit ? Color.Gold : new Color(255, 110, 110);
                var scale = p.IsCrit ? 1.05f : 0.95f;

                _spriteBatch.DrawString(_font, text, p.WorldPosition + Vector2.One,
                    Color.Black * (0.45f * alpha), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, text, p.WorldPosition, color * alpha,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawExpanded(CombatLog log)
        {
            var panel = GetExpandedPanel();
            var contentRect = new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 30, panel.Height - 16);
            var total = log.History.Count;
            var start = Math.Clamp(log.ScrollOffset, 0, Math.Max(0, total - GameConstants.CombatLogExpandedVisibleCount));
            var end = Math.Min(total, start + GameConstants.CombatLogExpandedVisibleCount);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, panel, Color.Black * 0.5f);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.White * 0.2f);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), Color.White * 0.2f);

            for (var i = start; i < end; i++)
            {
                var row = i - start;
                var y = contentRect.Y + row * GameConstants.CombatLogLineHeight;
                var item = log.History[i];
                var pos = new Vector2(contentRect.X, y);
                _spriteBatch.DrawString(_font, item.Text, pos + Vector2.One, Color.Black * 0.45f,
                    0f, Vector2.Zero, GameConstants.CombatLogTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, item.Text, pos, item.Color,
                    0f, Vector2.Zero, GameConstants.CombatLogTextScale, SpriteEffects.None, 0f);
            }

            var track = GetScrollTrackRect(panel);
            _spriteBatch.Draw(_pixel, track, Color.Black * 0.45f);
            if (total > 0)
            {
                var thumb = GetScrollbarThumbRect(panel, total, GameConstants.CombatLogExpandedVisibleCount, log.ScrollOffset);
                _spriteBatch.Draw(_pixel, thumb, Color.White * 0.55f);
            }

            _spriteBatch.End();
        }

        private float GetBaseY()
        {
            return _graphicsDevice.Viewport.Height - 230f - 16f - 42f;
        }

        private Rectangle GetExpandedPanel()
        {
            var x = 8;
            var y = (int)(GetBaseY() - 180f);
            var h = (int)(GameConstants.CombatLogExpandedVisibleCount * GameConstants.CombatLogLineHeight + 16f);
            return new Rectangle(x, y, 500, h);
        }

        private static Rectangle GetScrollTrackRect(Rectangle panel)
        {
            return new Rectangle(panel.Right - 14, panel.Y + 8, 8, panel.Height - 16);
        }

        private static Rectangle GetScrollbarThumbRect(Rectangle panel, int total, int visibleCount, int scrollOffset)
        {
            var track = GetScrollTrackRect(panel);
            if (total <= 0) return track;

            var maxOffset = Math.Max(0, total - visibleCount);
            var thumbH = Math.Max(14, (int)Math.Round(track.Height * (visibleCount / (float)Math.Max(visibleCount, total))));
            if (maxOffset == 0) return new Rectangle(track.X, track.Y, track.Width, thumbH);

            var t = scrollOffset / (float)maxOffset;
            var y = track.Y + (int)Math.Round((track.Height - thumbH) * t);
            return new Rectangle(track.X, y, track.Width, thumbH);
        }
    }
}
