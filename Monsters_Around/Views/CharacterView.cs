using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Views
{
    public class CharacterView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        private static readonly Color PanelBg       = new(22, 33, 62);
        private static readonly Color PanelBorder    = new(233, 69, 96);
        private static readonly Color TitleColor     = new(238, 238, 238);
        private static readonly Color Accent         = new(233, 69, 96);
        private static readonly Color Overlay        = Color.Black * 0.55f;
        private static readonly Color BtnIdle        = new(15, 52, 96);
        private static readonly Color BtnHover       = new(26, 76, 130);
        private static readonly Color BtnLvUp        = new(20, 80, 25);
        private static readonly Color BtnLvUpHover   = new(35, 120, 40);
        private static readonly Color StatLabel      = new(160, 175, 200);
        private static readonly Color StatValue      = new(235, 245, 255);

        private const int PanelPad = 26;
        private const int BtnW     = 290;
        private const int BtnH     = 44;

        public CharacterView(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch    = spriteBatch;
            _font           = font;
            _pixel          = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void Update(GameState state, MouseState mouse, MouseState prevMouse)
        {
            if (!state.IsCharacterScreenOpen) return;
            if (state.ShowLevelUpOverlay) return;   // пока открыт оверлей прокачки, ввод не перехватываем

            if (InputHandler.IsKeyPressed(Keys.Escape) || InputHandler.IsKeyPressed(Keys.Tab))
            {
                CloseScreen(state);
                return;
            }

            var vp = _graphicsDevice.Viewport;
            ComputeLayout(state, vp, out _, out var closeRect, out var lvUpRect);

            var click = mouse.LeftButton == ButtonState.Released &&
                        prevMouse.LeftButton == ButtonState.Pressed;

            if (state.PendingLevelUp && lvUpRect.HasValue && click && lvUpRect.Value.Contains(mouse.X, mouse.Y))
            {
                state.ShowLevelUpOverlay = true;
                return;
            }

            if ((click && closeRect.Contains(mouse.X, mouse.Y)) || InputHandler.IsKeyPressed(Keys.Enter))
                CloseScreen(state);
        }

        public void Draw(GameState state, MouseState mouse)
        {
            if (!state.IsCharacterScreenOpen || _font == null || _pixel == null) return;

            var vp = _graphicsDevice.Viewport;
            ComputeLayout(state, vp, out var panel, out var closeRect, out var lvUpRect);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Overlay);
                DrawPanelFrame(panel);

                var cx = panel.X + PanelPad;
                var cy = panel.Y + PanelPad;

                const string title = "ПЕРСОНАЖ";
                _spriteBatch.DrawString(_font, title, new Vector2(cx, cy) + Vector2.One * 2, Color.Black * 0.45f);
                _spriteBatch.DrawString(_font, title, new Vector2(cx, cy), TitleColor);
                var titleW = (int)_font.MeasureString(title).X;
                _spriteBatch.Draw(_pixel, new Rectangle(cx, cy + _font.LineSpacing + 4, titleW, 3), Accent);
                cy += _font.LineSpacing + 16;

                var nextLevel = state.HeroLevel < GameConstants.MaxLevel
                    ? $"{state.HeroLevel + 1}"
                    : "MAX";
                DrawStat(cx, ref cy, "Уровень",
                    state.HeroLevel >= GameConstants.MaxLevel
                        ? $"{state.HeroLevel}  (MAX)"
                        : $"{state.HeroLevel}  →  {nextLevel}");

                DrawStat(cx, ref cy, "Здоровье",
                    $"♥  {state.HeroHealth} / {state.EffectiveMaxHealth}");

                var dmgMin  = GameConstants.HeroDamageMin  + state.HeroDamageBonus;
                var dmgMax  = GameConstants.HeroDamageMax  + state.HeroDamageBonus;
                var critDmg = GameConstants.HeroCritDamage + state.HeroDamageBonus;
                DrawStat(cx, ref cy, "Урон",
                    $"{dmgMin}-{dmgMax}   (крит: {critDmg})");

                var critPct = (int)Math.Round(
                    (GameConstants.HeroCritChance + state.HeroCritChanceBonus) * 100f);
                DrawStat(cx, ref cy, "Шанс крита", $"{critPct}%");

                DrawStat(cx, ref cy, "Защита", $"{GameConstants.HeroDefense}%");

                if (state.HeroLevel < GameConstants.MaxLevel && !state.PendingLevelUp)
                {
                    var needed = GameConstants.XpNeeded(state.HeroLevel);
                    DrawStat(cx, ref cy, "Опыт", $"{state.HeroXp} / {needed}");
                }
                else
                {
                    DrawStat(cx, ref cy, "Опыт",
                        state.HeroLevel >= GameConstants.MaxLevel ? "-" : "↑  Готово к повышению");
                }

                cy += 14;

                if (state.PendingLevelUp)
                {
                    var pulse = 0.5f + 0.5f * MathF.Sin(
                        state.LevelUpBlinkAccumulator * GameConstants.LevelUpBlinkSpeed * MathF.PI * 2f);
                    var noticeColor = Color.Lerp(new Color(80, 200, 80), Color.LimeGreen, pulse);
                    const string notice = "↑  Доступно повышение уровня!";
                    const float nScale  = 0.88f;
                    var noticeSz = _font.MeasureString(notice) * nScale;
                    var nx = panel.X + (panel.Width - noticeSz.X) * 0.5f;
                    _spriteBatch.DrawString(_font, notice, new Vector2(nx, cy) + Vector2.One,
                        Color.Black * 0.4f, 0f, Vector2.Zero, new Vector2(nScale), SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, notice, new Vector2(nx, cy),
                        noticeColor,        0f, Vector2.Zero, new Vector2(nScale), SpriteEffects.None, 0f);
                    cy += (int)(noticeSz.Y) + 8;
                }

                if (lvUpRect.HasValue)
                {
                    var r   = lvUpRect.Value;
                    var hov = r.Contains(mouse.X, mouse.Y);
                    _spriteBatch.Draw(_pixel, r, hov ? BtnLvUpHover : BtnLvUp);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4),
                        Color.Black * 0.2f);
                    DrawButtonLabel(r, "↑  Повысить уровень", Color.LimeGreen);
                }

                {
                    var r   = closeRect;
                    var hov = r.Contains(mouse.X, mouse.Y);
                    _spriteBatch.Draw(_pixel, r, hov ? BtnHover : BtnIdle);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4),
                        Color.Black * 0.25f);
                    DrawButtonLabel(r, "Закрыть", Color.White);
                }

                const string hint  = "TAB / ESC - закрыть";
                const float hScale = 0.65f;
                var hintSz  = _font.MeasureString(hint) * hScale;
                var hintPos = new Vector2(
                    panel.X + (panel.Width - hintSz.X) * 0.5f,
                    panel.Bottom - PanelPad / 2 - hintSz.Y);
                _spriteBatch.DrawString(_font, hint, hintPos,
                    new Color(130, 140, 160) * 0.7f,
                    0f, Vector2.Zero, new Vector2(hScale), SpriteEffects.None, 0f);
            }
            finally { _spriteBatch.End(); }
        }

        public static void CloseScreen(GameState state)
        {
            state.IsCharacterScreenOpen = false;
            state.ShowLevelUpOverlay    = false;
        }

        private void DrawStat(int x, ref int y, string label, string value)
        {
            var lh = _font.LineSpacing + 4;
            _spriteBatch.DrawString(_font, $"{label}:", new Vector2(x, y) + Vector2.One, Color.Black * 0.3f);
            _spriteBatch.DrawString(_font, $"{label}:", new Vector2(x, y), StatLabel);
            _spriteBatch.DrawString(_font, value,       new Vector2(x + 155, y) + Vector2.One, Color.Black * 0.3f);
            _spriteBatch.DrawString(_font, value,       new Vector2(x + 155, y), StatValue);
            y += lh;
        }

        private void DrawButtonLabel(Rectangle rect, string text, Color color)
        {
            var sz  = _font.MeasureString(text);
            var pos = new Vector2(
                rect.X + (rect.Width  - sz.X) * 0.5f,
                rect.Y + (rect.Height - sz.Y) * 0.5f);
            _spriteBatch.DrawString(_font, text, pos + Vector2.One, Color.Black * 0.5f);
            _spriteBatch.DrawString(_font, text, pos, color);
        }

        private void ComputeLayout(GameState state, Viewport vp,
            out Rectangle panel, out Rectangle closeRect, out Rectangle? lvUpRect)
        {
            var lineH      = _font?.LineSpacing + 4 ?? 30;
            var titleH     = lineH + 16;
            var statsH     = 6 * lineH + 14;    // 6 строк + разделитель
            var noticeH    = state.PendingLevelUp ? (int)((lineH * 0.88f) + 8) : 0;
            var lvUpBtnH   = state.PendingLevelUp ? BtnH + 8 : 0;
            const int hintH = 22;
            var panelH     = PanelPad + titleH + statsH + noticeH + lvUpBtnH + BtnH + hintH + PanelPad;
            const int panelW = 400;

            panel = new Rectangle(
                (vp.Width  - panelW) / 2,
                Math.Clamp((vp.Height - panelH) / 2, 4, vp.Height - panelH - 4),
                panelW, panelH);

            var btnX  = panel.X + (panelW - BtnW) / 2;
            var closeY = panel.Bottom - PanelPad - hintH - BtnH;
            closeRect  = new Rectangle(btnX, closeY, BtnW, BtnH);

            if (state.PendingLevelUp)
                lvUpRect = new Rectangle(btnX, closeY - 8 - BtnH, BtnW, BtnH);
            else
                lvUpRect = null;
        }

        private void DrawPanelFrame(Rectangle panel)
        {
            _spriteBatch.Draw(_pixel, panel, PanelBg);
            const int t = 3;
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X,         panel.Y,          panel.Width,  t),            PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X,         panel.Bottom - t, panel.Width,  t),            PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X,         panel.Y,          t,            panel.Height), PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.Right - t, panel.Y,          t,            panel.Height), PanelBorder);
        }
    }
}
