using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monsters_Around.Models;
using System;
using System.Text;

namespace Monsters_Around.Views
{
    public class MenuView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        private static readonly Color PanelBg     = new(22, 33, 62);
        private static readonly Color PanelBorder  = new(233, 69, 96);
        private static readonly Color BtnIdle      = new(15, 52, 96);
        private static readonly Color BtnHover     = new(26, 76, 130);
        private static readonly Color BtnDisabled  = new(40, 45, 60);
        private static readonly Color TitleColor   = new(238, 238, 238);
        private static readonly Color Accent       = new(233, 69, 96);
        private static readonly Color Overlay      = Color.Black * 0.55f;

        private const int BtnW = 320;
        private const int BtnH = 44;
        private const int Gap  = 10;
        private const int PanelPad = 28;

        public MenuView(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void UpdatePause(GameState state, MouseState mouse, MouseState prevMouse)
        {
            if (InputHandler.IsKeyPressed(Keys.Escape))
            {
                if (state.PauseScreenState == PauseScreen.Controls)
                {
                    state.PauseScreenState = PauseScreen.Main;
                    state.PauseKeyboardIndex = 3;
                    return;
                }
                ClosePause(state);
                return;
            }

            var vp = _graphicsDevice.Viewport;
            if (state.PauseScreenState == PauseScreen.Main)
            {
                UpdatePauseMain(state, mouse, prevMouse, vp);
            }
            else
            {
                ComputeControlsLayout(vp, out _, out var backBtn, out _, out _, out _);
                var backHover = backBtn.Contains(mouse.X, mouse.Y);
                var backClick = mouse.LeftButton == ButtonState.Released &&
                                prevMouse.LeftButton == ButtonState.Pressed && backHover;
                if (backClick || InputHandler.IsKeyPressed(Keys.Enter) || InputHandler.IsKeyPressed(Keys.Back))
                {
                    state.PauseScreenState = PauseScreen.Main;
                    state.PauseKeyboardIndex = 3;
                }
            }
        }

        private void UpdatePauseMain(GameState state, MouseState mouse, MouseState prevMouse, Viewport vp)
        {
            const int btnCount = 5;
            var panelH = PanelPad * 2 + 52 + btnCount * BtnH + (btnCount - 1) * Gap;
            var panel = new Rectangle((vp.Width - BtnW - PanelPad * 2) / 2, (vp.Height - panelH) / 2,
                                      BtnW + PanelPad * 2, panelH);
            var btnLeft = panel.X + PanelPad;
            var btnTop  = panel.Y + PanelPad + 52;

            if (InputHandler.IsKeyPressed(Keys.Down))
                state.PauseKeyboardIndex = (state.PauseKeyboardIndex + 1) % btnCount;
            if (InputHandler.IsKeyPressed(Keys.Up))
                state.PauseKeyboardIndex = (state.PauseKeyboardIndex - 1 + btnCount) % btnCount;

            for (var i = 0; i < btnCount; i++)
            {
                var r = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                if (r.Contains(mouse.X, mouse.Y)) state.PauseKeyboardIndex = i;

                var click = mouse.LeftButton == ButtonState.Released &&
                            prevMouse.LeftButton == ButtonState.Pressed && r.Contains(mouse.X, mouse.Y);
                var activate = click || (InputHandler.IsKeyPressed(Keys.Enter) && state.PauseKeyboardIndex == i);
                if (!activate) continue;

                switch (i)
                {
                    case 0: ClosePause(state); return;
                    case 3: state.PauseScreenState = PauseScreen.Controls; state.PauseKeyboardIndex = 0; return;
                    case 4: state.VoluntaryExitRequested = true; return;
                }
            }
        }

        public void DrawPause(GameState state, MouseState mouse)
        {
            if (_font == null || _pixel == null) return;
            var vp = _graphicsDevice.Viewport;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Overlay);

                if (state.PauseScreenState == PauseScreen.Main)
                    DrawPauseMain(state, mouse, vp);
                else
                    DrawPauseControls(state, mouse, vp);
            }
            finally { _spriteBatch.End(); }
        }

        private void DrawPauseMain(GameState state, MouseState mouse, Viewport vp)
        {
            const int btnCount = 5;
            var panelH = PanelPad * 2 + 52 + btnCount * BtnH + (btnCount - 1) * Gap;
            var panel = new Rectangle((vp.Width - BtnW - PanelPad * 2) / 2, (vp.Height - panelH) / 2,
                                      BtnW + PanelPad * 2, panelH);
            DrawPanelFrame(panel);
            DrawTitle("ПАУЗА", new Vector2(panel.X + PanelPad, panel.Y + PanelPad));

            var btnLeft = panel.X + PanelPad;
            var btnTop  = panel.Y + PanelPad + 52;
            var labels   = new[] { "Продолжить", "Сохранить", "Загрузить", "Управление", "Выйти из игры" };
            var disabled = new[] { false, true, true, false, false };

            for (var i = 0; i < labels.Length; i++)
            {
                var r = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                var hover    = !disabled[i] && r.Contains(mouse.X, mouse.Y);
                var selected = state.PauseKeyboardIndex == i;
                var fill     = disabled[i] ? BtnDisabled : (hover || selected ? BtnHover : BtnIdle);
                DrawButton(r, fill, labels[i], disabled[i]);
            }
        }

        private void DrawPauseControls(GameState state, MouseState mouse, Viewport vp)
        {
            ComputeControlsLayout(vp, out var panel, out var backBtn, out var textOrigin, out var body, out var textScale);
            DrawPanelFrame(panel);
            DrawTitle("Управление", new Vector2(panel.X + PanelPad, panel.Y + PanelPad));

            var shadow = Vector2.One * Math.Max(1f, textScale);
            _spriteBatch.DrawString(_font, body, textOrigin + shadow, Color.Black * 0.35f,
                0f, Vector2.Zero, new Vector2(textScale), SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, body, textOrigin, new Color(220, 225, 235),
                0f, Vector2.Zero, new Vector2(textScale), SpriteEffects.None, 0f);

            var backHover = backBtn.Contains(mouse.X, mouse.Y);
            DrawButton(backBtn, backHover ? BtnHover : BtnIdle, "Назад", false);
        }

        public void UpdateGameOver(GameState state, MouseState mouse, MouseState prevMouse)
        {
            var vp = _graphicsDevice.Viewport;
            const int panelH = 220;
            var btnLeft = (vp.Width - BtnW) / 2;
            var btnTop  = (vp.Height - panelH) / 2 + 120;

            var labels = new[] { "Играть снова", "Выход" };
            for (var i = 0; i < labels.Length; i++)
            {
                var r = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                if (r.Contains(mouse.X, mouse.Y)) state.GameOverSelectedIndex = i;

                var click = mouse.LeftButton == ButtonState.Released &&
                            prevMouse.LeftButton == ButtonState.Pressed && r.Contains(mouse.X, mouse.Y);
                if (click || (InputHandler.IsKeyPressed(Keys.Enter) && state.GameOverSelectedIndex == i))
                {
                    if (i == 0) { state.IsGameOver = false; }
                    else        state.VoluntaryExitRequested = true;
                    return;
                }
            }

            if (InputHandler.IsKeyPressed(Keys.Down))
                state.GameOverSelectedIndex = (state.GameOverSelectedIndex + 1) % labels.Length;
            else if (InputHandler.IsKeyPressed(Keys.Up))
                state.GameOverSelectedIndex = (state.GameOverSelectedIndex - 1 + labels.Length) % labels.Length;
        }

        public void DrawGameOver(GameState state, MouseState mouse)
        {
            if (_font == null || _pixel == null) return;
            var vp = _graphicsDevice.Viewport;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.65f);

                const int panelW = 520;
                const int panelH = 220;
                var panel = new Rectangle((vp.Width - panelW) / 2, (vp.Height - panelH) / 2, panelW, panelH);
                DrawPanelFrame(panel);
                DrawTitle("ИГРА ОКОНЧЕНА", new Vector2(panel.X + 28, panel.Y + 28));

                var btnLeft = panel.X + (panel.Width - BtnW) / 2;
                var btnTop  = panel.Y + 120;
                var labels  = new[] { "Играть снова", "Выход" };
                for (var i = 0; i < labels.Length; i++)
                {
                    var r = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                    var selected = state.GameOverSelectedIndex == i;
                    var hover = r.Contains(mouse.X, mouse.Y);
                    DrawButton(r, hover || selected ? BtnHover : BtnIdle, labels[i], false);
                }
            }
            finally { _spriteBatch.End(); }
        }

        // ──────────────────────────────── Экран победы ────────────────────────────────

        private static readonly string[] _killTypeLabels =
            { "Зомби", "Скелеты", "Слизни", "Маги", "Пауки" };

        public void UpdateVictory(GameState state, MouseState mouse, MouseState prevMouse)
        {
            var vp = _graphicsDevice.Viewport;
            ComputeVictoryLayout(state, vp, out _, out var btnLeft, out var btnTop);

            var labels = new[] { "Играть снова", "Выход" };
            for (var i = 0; i < labels.Length; i++)
            {
                var r = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                if (r.Contains(mouse.X, mouse.Y)) state.VictorySelectedIndex = i;

                var click = mouse.LeftButton  == ButtonState.Released &&
                            prevMouse.LeftButton == ButtonState.Pressed && r.Contains(mouse.X, mouse.Y);
                if (click || (InputHandler.IsKeyPressed(Keys.Enter) && state.VictorySelectedIndex == i))
                {
                    if (i == 0) state.IsVictory = false;
                    else        state.VoluntaryExitRequested = true;
                    return;
                }
            }

            if (InputHandler.IsKeyPressed(Keys.Down))
                state.VictorySelectedIndex = (state.VictorySelectedIndex + 1) % labels.Length;
            else if (InputHandler.IsKeyPressed(Keys.Up))
                state.VictorySelectedIndex = (state.VictorySelectedIndex - 1 + labels.Length) % labels.Length;
        }

        public void DrawVictory(GameState state, MouseState mouse)
        {
            if (_font == null || _pixel == null) return;
            var vp = _graphicsDevice.Viewport;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.70f);

                ComputeVictoryLayout(state, vp, out var panel, out var btnLeft, out var btnTop);
                DrawPanelFrame(panel);

                var cx = panel.X + PanelPad;
                var cy = panel.Y + PanelPad;

                // Заголовок
                const string title = "ПОБЕДА!";
                var titleColor = new Color(255, 220, 60);
                _spriteBatch.DrawString(_font, title, new Vector2(cx, cy) + Vector2.One * 2, Color.Black * 0.5f);
                _spriteBatch.DrawString(_font, title, new Vector2(cx, cy), titleColor);
                var titleW = (int)_font.MeasureString(title).X;
                _spriteBatch.Draw(_pixel, new Rectangle(cx, cy + _font.LineSpacing + 4, titleW, 3), titleColor);
                cy += _font.LineSpacing + 18;

                // Подзаголовок
                const string sub = "Подземелье успешно пройдено!";
                _spriteBatch.DrawString(_font, sub, new Vector2(cx, cy) + Vector2.One, Color.Black * 0.4f);
                _spriteBatch.DrawString(_font, sub, new Vector2(cx, cy), new Color(200, 235, 200));
                cy += _font.LineSpacing + 14;

                // Статистика
                var lh = _font.LineSpacing + 3;
                DrawVictoryStat(cx, panel.Right - PanelPad, ref cy, lh,
                    "Уровень героя", state.HeroLevel.ToString());
                DrawVictoryStat(cx, panel.Right - PanelPad, ref cy, lh,
                    "Этажей пройдено", $"{GameConstants.MaxFloor} / {GameConstants.MaxFloor}");
                DrawVictoryStat(cx, panel.Right - PanelPad, ref cy, lh,
                    "Монстров убито", state.TotalKills.ToString());
                DrawVictoryStat(cx, panel.Right - PanelPad, ref cy, lh,
                    "Опыт получен", state.TotalXpGained.ToString());

                cy += 6;

                // Убийства по типам (два столбца)
                var col1X = cx + 20;
                var col2X = cx + 20 + (panel.Width - PanelPad * 2) / 2;
                var killColor = new Color(210, 215, 230);
                for (var i = 0; i < _killTypeLabels.Length; i++)
                {
                    var kx    = i % 2 == 0 ? col1X : col2X;
                    var ky    = cy + (i / 2) * lh;
                    var kText = $"{_killTypeLabels[i]}: {state.KillsByType[i]}";
                    _spriteBatch.DrawString(_font, kText, new Vector2(kx, ky) + Vector2.One, Color.Black * 0.3f);
                    _spriteBatch.DrawString(_font, kText, new Vector2(kx, ky), killColor);
                }

                // Кнопки
                var labels = new[] { "Играть снова", "Выход" };
                for (var i = 0; i < labels.Length; i++)
                {
                    var r        = new Rectangle(btnLeft, btnTop + i * (BtnH + Gap), BtnW, BtnH);
                    var selected = state.VictorySelectedIndex == i;
                    var hover    = r.Contains(mouse.X, mouse.Y);
                    DrawButton(r, hover || selected ? BtnHover : BtnIdle, labels[i], false);
                }
            }
            finally { _spriteBatch.End(); }
        }

        private void DrawVictoryStat(int leftX, int rightEdge, ref int cy, int lh,
            string label, string value)
        {
            var labelColor = new Color(160, 175, 200);
            var valueColor = new Color(235, 245, 255);
            _spriteBatch.DrawString(_font, $"{label}:", new Vector2(leftX, cy) + Vector2.One, Color.Black * 0.3f);
            _spriteBatch.DrawString(_font, $"{label}:", new Vector2(leftX, cy), labelColor);
            var vsz = _font.MeasureString(value);
            var vx  = rightEdge - (int)vsz.X;
            _spriteBatch.DrawString(_font, value, new Vector2(vx, cy) + Vector2.One, Color.Black * 0.3f);
            _spriteBatch.DrawString(_font, value, new Vector2(vx, cy), valueColor);
            cy += lh;
        }

        private void ComputeVictoryLayout(GameState state, Viewport vp,
            out Rectangle panel, out int btnLeft, out int btnTop)
        {
            if (_font == null)
            {
                panel   = new Rectangle(vp.Width / 2 - 200, vp.Height / 2 - 150, 400, 300);
                btnLeft = panel.X + PanelPad;
                btnTop  = panel.Bottom - PanelPad - 2 * (BtnH + Gap);
                return;
            }

            var lh        = _font.LineSpacing + 3;
            var titleH    = _font.LineSpacing + 18;    // ПОБЕДА!
            var subH      = _font.LineSpacing + 14;    // подзаголовок
            const int statRows  = 4;                   // 4 строки статистики
            const int killRows  = 3;                   // 5 типов в 2 колонки = 3 строки
            var statsH    = statRows * lh + 6 + killRows * lh;
            var btnsH     = 2 * BtnH + Gap + 14;      // 2 кнопки + отступ сверху
            const int panelW = 520;
            var panelH    = PanelPad + titleH + subH + statsH + btnsH + PanelPad;

            panel   = new Rectangle((vp.Width - panelW) / 2, (vp.Height - panelH) / 2, panelW, panelH);
            btnLeft = panel.X + (panelW - BtnW) / 2;
            btnTop  = panel.Bottom - PanelPad - 2 * BtnH - Gap;
        }

        // ──────────────────────────────────────────────────────────────────────────────

        public static void ClosePause(GameState state)
        {
            state.IsPaused = false;
            state.PauseScreenState = PauseScreen.Main;
        }

        private void DrawPanelFrame(Rectangle panel)
        {
            _spriteBatch.Draw(_pixel, panel, PanelBg);
            const int t = 3;
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, panel.Width, t), PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Bottom - t, panel.Width, t), PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, t, panel.Height), PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.Right - t, panel.Y, t, panel.Height), PanelBorder);
        }

        private void DrawTitle(string title, Vector2 pos)
        {
            _spriteBatch.DrawString(_font, title, pos + Vector2.One * 2, Color.Black * 0.45f);
            _spriteBatch.DrawString(_font, title, pos, TitleColor);
            var w = _font.MeasureString(title).X;
            _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X, (int)(pos.Y + _font.LineSpacing + 6), (int)w, 3), Accent);
        }

        private void DrawButton(Rectangle rect, Color fill, string text, bool disabled)
        {
            _spriteBatch.Draw(_pixel, rect, fill);
            _spriteBatch.Draw(_pixel,
                new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4),
                Color.Black * 0.25f);

            var label = disabled ? $"{text}  (скоро)" : text;
            var size  = _font.MeasureString(label);
            var pos   = new Vector2(rect.X + (rect.Width - size.X) * 0.5f, rect.Y + (rect.Height - size.Y) * 0.5f);
            var color = disabled ? new Color(140, 145, 160) : Color.White;
            _spriteBatch.DrawString(_font, label, pos + Vector2.One, Color.Black * 0.5f);
            _spriteBatch.DrawString(_font, label, pos, color);
        }

        private void ComputeControlsLayout(Viewport vp,
            out Rectangle panel, out Rectangle backBtn,
            out Vector2 textOrigin, out string wrappedBody, out float textScale)
        {
            const int contentTopOffset = 52;
            const int maxPanelOuterW = 560;
            const int marginV = 16;

            var panelOuterW = Math.Min(maxPanelOuterW, Math.Max(376, vp.Width - 48));
            var innerW = panelOuterW - PanelPad * 2;

            wrappedBody = WrapText(GetControlsHelpText(), innerW);
            var sz = _font.MeasureString(wrappedBody);

            var reservedTop    = PanelPad + contentTopOffset;
            var reservedBottom = Gap + BtnH + PanelPad;
            var maxPanelH  = Math.Max(BtnH + reservedTop + 8, vp.Height - marginV);
            var maxTextH   = Math.Max(24, maxPanelH - reservedTop - reservedBottom);

            textScale = sz.Y <= 0 ? 1f : Math.Min(1f, maxTextH / sz.Y);
            var scaledTextH = (int)Math.Ceiling(sz.Y * textScale);
            if (scaledTextH > maxTextH && sz.Y > 0)
            {
                textScale   = maxTextH / sz.Y;
                scaledTextH = maxTextH;
            }

            var panelH = Math.Min(reservedTop + scaledTextH + reservedBottom, maxPanelH);

            panel = new Rectangle((vp.Width - panelOuterW) / 2, (vp.Height - panelH) / 2, panelOuterW, panelH);
            textOrigin = new Vector2(panel.X + PanelPad, panel.Y + reservedTop);
            backBtn = new Rectangle(panel.X + PanelPad, panel.Bottom - PanelPad - BtnH, innerW, BtnH);
        }

        private string WrapText(string text, float maxWidth)
        {
            if (_font == null || maxWidth < 48f) return text;
            var sb = new StringBuilder();
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0) { sb.AppendLine(); continue; }

                var indent = line.StartsWith("  ", StringComparison.Ordinal) ? "  " : "";
                var rest   = indent.Length > 0 ? line.Substring(2).TrimStart() : line.TrimStart();
                if (rest.Length == 0) { sb.AppendLine(); continue; }

                var indentW  = _font.MeasureString(indent).X;
                var budget   = Math.Max(48f, maxWidth - indentW);
                var words    = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var lineSb   = new StringBuilder();
                foreach (var word in words)
                {
                    var candidate = lineSb.Length == 0 ? word : lineSb + " " + word;
                    if (_font.MeasureString(candidate).X > budget && lineSb.Length > 0)
                    {
                        sb.Append(indent).AppendLine(lineSb.ToString());
                        lineSb.Clear().Append(word);
                    }
                    else
                    {
                        if (lineSb.Length > 0) lineSb.Append(' ');
                        lineSb.Append(word);
                    }
                }
                if (lineSb.Length > 0) sb.Append(indent).AppendLine(lineSb.ToString());
            }
            return sb.ToString();
        }

        private static string GetControlsHelpText() =>
            "Движение по клеткам\n" +
            "  WASD или стрелки - шаг по сетке\n" +
            "  Пробел - пропустить ход\n\n" +
            "Предметы\n" +
            "  E - использовать сердечко (+50 HP)\n\n" +
            "Камера и экран\n" +
            "  F11 - полный экран / окно\n\n" +
            "Интерфейс\n" +
            "  F1 - счётчик FPS\n" +
            "  M - большая карта этажа\n" +
            "  T - раскрыть историю событий\n" +
            "  Мини-карта - всегда слева снизу\n\n" +
            "Подземелье\n" +
            "  Лестница вниз / вверх - переход между этажами\n\n" +
            "Пауза\n" +
            "  Esc - меню паузы";
    }
}
