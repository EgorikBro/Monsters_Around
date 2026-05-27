using Microsoft.Xna.Framework.Input;

namespace Monsters_Around
{
    public static class InputHandler
    {
        private static KeyboardState _currentKeyState;
        private static KeyboardState _previousKeyState;

        public static void Update()
        {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();
        }

        public static bool IsKeyPressed(Keys key)
        {
            return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
        }

        public static bool IsKeyDown(Keys key)
        {
            return _currentKeyState.IsKeyDown(key);
        }
    }
}