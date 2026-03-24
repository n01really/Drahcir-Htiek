using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Drahcir_Htiek.Menues
{
    internal class Main_menu
    {
        private Rectangle _startGameButton;
        private Rectangle _createMapButton;
        private Rectangle _quitGameButton;

        private Color _startGameColor;
        private Color _createMapColor;
        private Color _quitGameColor;

        private Color _normalColor = new Color(70, 70, 70);
        private Color _hoverColor = new Color(100, 100, 100);

        private MouseState _previousMouseState;
        private SpriteFont _font;
        private Texture2D _pixel;

        public bool StartGameClicked { get; private set; }
        public bool QuitGameClicked { get; private set; }

        public Main_menu(int screenWidth, int screenHeight)
        {
            // Centrera knapparna på skärmen
            int buttonWidth = 300;
            int buttonHeight = 60;
            int buttonSpacing = 20;
            int centerX = (screenWidth - buttonWidth) / 2;
            int startY = (screenHeight - (buttonHeight * 3 + buttonSpacing * 2)) / 2;

            _startGameButton = new Rectangle(centerX, startY, buttonWidth, buttonHeight);
            _createMapButton = new Rectangle(centerX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight);
            _quitGameButton = new Rectangle(centerX, startY + (buttonHeight + buttonSpacing) * 2, buttonWidth, buttonHeight);

            _startGameColor = _normalColor;
            _createMapColor = _normalColor;
            _quitGameColor = _normalColor;

            _previousMouseState = Mouse.GetState();
        }

        public void LoadContent(SpriteFont font, Texture2D pixel)
        {
            _font = font;
            _pixel = pixel;
        }

        public void Update()
        {
            MouseState currentMouseState = Mouse.GetState();
            Point mousePosition = new Point(currentMouseState.X, currentMouseState.Y);

            // Uppdatera färger baserat på hover
            _startGameColor = _startGameButton.Contains(mousePosition) ? _hoverColor : _normalColor;
            _createMapColor = _createMapButton.Contains(mousePosition) ? _hoverColor : _normalColor;
            _quitGameColor = _quitGameButton.Contains(mousePosition) ? _hoverColor : _normalColor;

            // Kontrollera klick
            if (currentMouseState.LeftButton == ButtonState.Released && 
                _previousMouseState.LeftButton == ButtonState.Pressed)
            {
                if (_startGameButton.Contains(mousePosition))
                {
                    StartGameClicked = true;
                }
                else if (_quitGameButton.Contains(mousePosition))
                {
                    QuitGameClicked = true;
                }
                // createMapButton klick kan läggas till senare
            }

            _previousMouseState = currentMouseState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Rita Start Game knapp
            spriteBatch.Draw(_pixel, _startGameButton, _startGameColor);
            DrawCenteredText(spriteBatch, "Start Game", _startGameButton);

            // Rita Create Map knapp
            spriteBatch.Draw(_pixel, _createMapButton, _createMapColor);
            DrawCenteredText(spriteBatch, "Create Map", _createMapButton);

            // Rita Quit Game knapp
            spriteBatch.Draw(_pixel, _quitGameButton, _quitGameColor);
            DrawCenteredText(spriteBatch, "Quit Game", _quitGameButton);
        }

        private void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle button)
        {
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                button.X + (button.Width - textSize.X) / 2,
                button.Y + (button.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(_font, text, textPosition, Color.White);
        }

        public void Reset()
        {
            StartGameClicked = false;
            QuitGameClicked = false;
        }
    }
}
