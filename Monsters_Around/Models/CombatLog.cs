using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Monsters_Around.Models
{
    public class CombatLogEntry
    {
        public string Text;
        public Color Color;
        public float Y;
        public float TargetY;
        public float Alpha;
        public bool IsExiting;
    }

    public struct CombatLogHistoryItem
    {
        public string Text;
        public Color Color;
    }

    public struct DamagePopup
    {
        public Vector2 WorldPosition;
        public int Value;
        public bool IsCrit;
        public float TimeLeft;
        public float Lifetime;
    }

    public class CombatLog
    {
        public List<CombatLogHistoryItem> History { get; } = new();
        public List<DamagePopup> DamagePopups { get; } = new();
        public List<CombatLogEntry> Entries { get; } = new();

        public int ScrollOffset { get; set; }
        public bool IsExpanded { get; set; }
        public bool ScrollbarDragging { get; set; }
        public float ScrollbarGrabOffset { get; set; }

        private readonly Queue<(string Text, Color Color)> _pending = new();

        public void AddMessage(string text, Color color)
        {
            History.Add(new CombatLogHistoryItem { Text = text, Color = color });
            if (History.Count > 400)
                History.RemoveRange(0, 100);

            _pending.Enqueue((text, color));
            ClampScroll();
            ScrollOffset = Math.Max(0, History.Count - GameConstants.CombatLogExpandedVisibleCount);
        }

        public bool TryDequeuePending(out string text, out Color color)
        {
            if (_pending.Count > 0)
            {
                var item = _pending.Dequeue();
                text = item.Text;
                color = item.Color;
                return true;
            }
            text = null;
            color = default;
            return false;
        }

        public void AddDamagePopup(Vector2 worldPosition, int damage, bool isCrit, int tileSize)
        {
            DamagePopups.Add(new DamagePopup
            {
                WorldPosition = worldPosition + new Vector2(tileSize * 0.35f, -4f),
                Value = damage,
                IsCrit = isCrit,
                Lifetime = 0.70f,
                TimeLeft = 0.70f
            });
        }

        public void UpdateDamagePopups(float dt)
        {
            for (var i = DamagePopups.Count - 1; i >= 0; i--)
            {
                var p = DamagePopups[i];
                p.TimeLeft -= dt;
                p.WorldPosition.Y -= (p.IsCrit ? 34f : 26f) * dt;
                if (p.TimeLeft <= 0f)
                    DamagePopups.RemoveAt(i);
                else
                    DamagePopups[i] = p;
            }
        }

        public void ClampScroll()
        {
            var max = Math.Max(0, History.Count - GameConstants.CombatLogExpandedVisibleCount);
            ScrollOffset = Math.Clamp(ScrollOffset, 0, max);
        }
    }
}
