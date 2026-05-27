using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monsters_Around
{
    public class Camera2D
    {
        private Vector2 _position;
        private readonly float _zoom;

        public Camera2D(float zoom = 1f)
        {
            _zoom = MathHelper.Max(zoom, 0.1f);
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-_position, 0f))
                   * Matrix.CreateScale(_zoom, _zoom, 1f);
        }

        public void Follow(Vector2 targetWorldPosition, Viewport viewport, Point worldSize)
        {
            var visibleWorldWidth = viewport.Width / _zoom;
            var visibleWorldHeight = viewport.Height / _zoom;

            var desiredX = targetWorldPosition.X - visibleWorldWidth * 0.5f;
            var desiredY = targetWorldPosition.Y - visibleWorldHeight * 0.5f;

            var maxX = MathHelper.Max(0f, worldSize.X - visibleWorldWidth);
            var maxY = MathHelper.Max(0f, worldSize.Y - visibleWorldHeight);

            _position.X = MathHelper.Clamp(desiredX, 0f, maxX);
            _position.Y = MathHelper.Clamp(desiredY, 0f, maxY);
        }
    }
}