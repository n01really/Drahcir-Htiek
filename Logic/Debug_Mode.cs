using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Drahcir_Htiek.Logic
{
    public class Debug_Mode
    {
        private bool _isDebugActive = false;
        private KeyboardState _previousKeyState;
        private SpriteFont _font;
        private Texture2D _pixel;
        private bool _isInitialized = false;

        public bool IsActive => _isDebugActive;

        public Debug_Mode()
        {
        }

        public void SetFont(SpriteFont font)
        {
            _font = font;
        }

        public void SetPixelTexture(Texture2D pixel)
        {
            _pixel = pixel;
        }

        public void Update()
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            // Initialisera vid första anropet för att undvika falska knapptryck
            if (!_isInitialized)
            {
                _previousKeyState = currentKeyState;
                _isInitialized = true;
                return;
            }

            // F1 för att togga debug mode
            if (currentKeyState.IsKeyDown(Keys.F1) && !_previousKeyState.IsKeyDown(Keys.F1))
            {
                _isDebugActive = !_isDebugActive;
                System.Diagnostics.Debug.WriteLine($"Debug Mode: {(_isDebugActive ? "ON" : "OFF")}");
            }

            _previousKeyState = currentKeyState;
        }

        public void DrawPlayerPosition(SpriteBatch spriteBatch, Rectangle playerBounds, GraphicsDevice graphicsDevice)
        {
            if (!_isDebugActive)
                return;

            string positionText = $"X: {playerBounds.X}, Y: {playerBounds.Y}";

            // Om vi har en font, använd den
            if (_font != null)
            {
                Vector2 textSize = _font.MeasureString(positionText);
                
                // Högra hörnet - justera för textens bredd
                Vector2 position = new Vector2(
                    graphicsDevice.Viewport.Width - textSize.X - 10,
                    10
                );

                // Rita en halvgenomskinlig bakgrund för bättre läsbarhet
                if (_pixel != null)
                {
                    spriteBatch.Draw(
                        _pixel,
                        new Rectangle((int)position.X - 5, (int)position.Y - 5, (int)textSize.X + 10, (int)textSize.Y + 10),
                        Color.Black * 0.7f
                    );
                }

                // Rita texten
                spriteBatch.DrawString(_font, positionText, position, Color.Yellow);
            }
            else
            {
                // Fallback: Skriv till Output-fönstret istället
                System.Diagnostics.Debug.WriteLine($"Player Position - {positionText}");
            }
        }
    }
}
