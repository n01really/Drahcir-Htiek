using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Drahcir_Htiek.Menues
{
    internal class Save_Map_Menu
    {
        private SpriteFont _font;
        private Texture2D _pixel;
        private KeyboardState _previousKeyState;

        // För save-meny
        private string _mapName = "";
        private bool _isActive = false;
        private bool _isSaveMode = true; // true = save, false = load
        
        // För load-meny
        private List<string> _availableMaps = new List<string>();
        private int _selectedMapIndex = 0;
        
        public bool IsActive => _isActive;
        public string SelectedMapName { get; private set; }
        public bool MapSelected { get; private set; }

        public void SetFont(SpriteFont font)
        {
            _font = font;
        }

        public void SetPixelTexture(Texture2D pixel)
        {
            _pixel = pixel;
        }

        public void ShowSaveMenu()
        {
            _isActive = true;
            _isSaveMode = true;
            _mapName = "";
            MapSelected = false;
        }

        public void ShowLoadMenu()
        {
            _isActive = true;
            _isSaveMode = false;
            _selectedMapIndex = 0;
            MapSelected = false;
            RefreshMapList();
        }

        public void Close()
        {
            _isActive = false;
            _mapName = "";
            MapSelected = false;
        }

        private void RefreshMapList()
        {
            _availableMaps.Clear();
            
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string mapsFolder = Path.Combine(projectRoot, "Maps");
            
            if (Directory.Exists(mapsFolder))
            {
                var files = Directory.GetFiles(mapsFolder, "*.json");
                foreach (var file in files)
                {
                    _availableMaps.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        public void Update()
        {
            if (!_isActive)
                return;

            KeyboardState currentKeyState = Keyboard.GetState();

            if (_isSaveMode)
            {
                UpdateSaveMode(currentKeyState);
            }
            else
            {
                UpdateLoadMode(currentKeyState);
            }

            _previousKeyState = currentKeyState;
        }

        private void UpdateSaveMode(KeyboardState currentKeyState)
        {
            // Hantera textinmatning
            var pressedKeys = currentKeyState.GetPressedKeys();
            
            foreach (var key in pressedKeys)
            {
                if (_previousKeyState.IsKeyUp(key))
                {
                    // Backspace
                    if (key == Keys.Back && _mapName.Length > 0)
                    {
                        _mapName = _mapName.Substring(0, _mapName.Length - 1);
                    }
                    // Enter för att spara
                    else if (key == Keys.Enter && _mapName.Length > 0)
                    {
                        SelectedMapName = _mapName;
                        MapSelected = true;
                        _isActive = false;
                    }
                    // Escape för att avbryta
                    else if (key == Keys.Escape)
                    {
                        _isActive = false;
                    }
                    // Tillåtna tecken: bokstäver, siffror, underscore
                    else if (_mapName.Length < 20)
                    {
                        string keyString = key.ToString();
                        
                        if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
                        {
                            bool shift = currentKeyState.IsKeyDown(Keys.LeftShift) || 
                                       currentKeyState.IsKeyDown(Keys.RightShift);
                            _mapName += shift ? keyString.ToUpper() : keyString.ToLower();
                        }
                        else if (key == Keys.OemMinus && (currentKeyState.IsKeyDown(Keys.LeftShift) || 
                                                          currentKeyState.IsKeyDown(Keys.RightShift)))
                        {
                            _mapName += "_";
                        }
                    }
                }
            }
        }

        private void UpdateLoadMode(KeyboardState currentKeyState)
        {
            if (_availableMaps.Count == 0)
            {
                if (currentKeyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
                {
                    _isActive = false;
                }
                return;
            }

            // Navigera med piltangenter
            if (currentKeyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up))
            {
                _selectedMapIndex--;
                if (_selectedMapIndex < 0)
                    _selectedMapIndex = _availableMaps.Count - 1;
            }
            
            if (currentKeyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down))
            {
                _selectedMapIndex++;
                if (_selectedMapIndex >= _availableMaps.Count)
                    _selectedMapIndex = 0;
            }

            // Enter för att välja
            if (currentKeyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter))
            {
                SelectedMapName = _availableMaps[_selectedMapIndex];
                MapSelected = true;
                _isActive = false;
            }

            // Escape för att avbryta
            if (currentKeyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
            {
                _isActive = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            if (!_isActive || _font == null || _pixel == null)
                return;

            if (_isSaveMode)
            {
                DrawSaveMenu(spriteBatch, screenWidth, screenHeight);
            }
            else
            {
                DrawLoadMenu(spriteBatch, screenWidth, screenHeight);
            }
        }

        private void DrawSaveMenu(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            // Bakgrund
            Rectangle backgroundRect = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Draw(_pixel, backgroundRect, Color.Black * 0.8f);

            // Dialog box
            int boxWidth = 600;
            int boxHeight = 200;
            int boxX = (screenWidth - boxWidth) / 2;
            int boxY = (screenHeight - boxHeight) / 2;
            
            Rectangle boxRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            spriteBatch.Draw(_pixel, boxRect, Color.DarkGray);
            
            // Border
            Rectangle borderRect = new Rectangle(boxX - 2, boxY - 2, boxWidth + 4, boxHeight + 4);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Y, borderRect.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Bottom - 2, borderRect.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Y, 2, borderRect.Height), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.Right - 2, borderRect.Y, 2, borderRect.Height), Color.White);

            // Text
            string title = "SAVE MAP";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(boxX + (boxWidth - titleSize.X) / 2, boxY + 20);
            spriteBatch.DrawString(_font, title, titlePos, Color.Yellow);

            string prompt = "Enter map name:";
            Vector2 promptPos = new Vector2(boxX + 30, boxY + 60);
            spriteBatch.DrawString(_font, prompt, promptPos, Color.White);

            // Input box
            Rectangle inputBox = new Rectangle(boxX + 30, boxY + 90, boxWidth - 60, 40);
            spriteBatch.Draw(_pixel, inputBox, Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(inputBox.X, inputBox.Y, inputBox.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(inputBox.X, inputBox.Bottom - 2, inputBox.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(inputBox.X, inputBox.Y, 2, inputBox.Height), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(inputBox.Right - 2, inputBox.Y, 2, inputBox.Height), Color.White);

            // Input text med cursor
            string displayText = _mapName + "_";
            Vector2 textPos = new Vector2(inputBox.X + 10, inputBox.Y + 10);
            spriteBatch.DrawString(_font, displayText, textPos, Color.Cyan);

            // Instructions
            string instructions = "Press ENTER to save, ESC to cancel";
            Vector2 instSize = _font.MeasureString(instructions);
            Vector2 instPos = new Vector2(boxX + (boxWidth - instSize.X) / 2, boxY + boxHeight - 40);
            spriteBatch.DrawString(_font, instructions, instPos, Color.Gray);
        }

        private void DrawLoadMenu(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            // Bakgrund
            Rectangle backgroundRect = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Draw(_pixel, backgroundRect, Color.Black * 0.8f);

            // Dialog box
            int boxWidth = 600;
            int boxHeight = 400;
            int boxX = (screenWidth - boxWidth) / 2;
            int boxY = (screenHeight - boxHeight) / 2;
            
            Rectangle boxRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            spriteBatch.Draw(_pixel, boxRect, Color.DarkGray);
            
            // Border
            Rectangle borderRect = new Rectangle(boxX - 2, boxY - 2, boxWidth + 4, boxHeight + 4);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Y, borderRect.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Bottom - 2, borderRect.Width, 2), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.X, borderRect.Y, 2, borderRect.Height), Color.White);
            spriteBatch.Draw(_pixel, new Rectangle(borderRect.Right - 2, borderRect.Y, 2, borderRect.Height), Color.White);

            // Title
            string title = "LOAD MAP";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(boxX + (boxWidth - titleSize.X) / 2, boxY + 20);
            spriteBatch.DrawString(_font, title, titlePos, Color.Yellow);

            if (_availableMaps.Count == 0)
            {
                string noMaps = "No maps found!";
                Vector2 noMapsSize = _font.MeasureString(noMaps);
                Vector2 noMapsPos = new Vector2(boxX + (boxWidth - noMapsSize.X) / 2, boxY + boxHeight / 2);
                spriteBatch.DrawString(_font, noMaps, noMapsPos, Color.Red);
            }
            else
            {
                // Map list
                float lineHeight = _font.MeasureString("A").Y + 10;
                float startY = boxY + 60;

                for (int i = 0; i < _availableMaps.Count; i++)
                {
                    string mapName = _availableMaps[i];
                    Vector2 textPos = new Vector2(boxX + 50, startY + i * lineHeight);
                    Color textColor = (i == _selectedMapIndex) ? Color.Cyan : Color.White;

                    // Selection indicator
                    if (i == _selectedMapIndex)
                    {
                        string arrow = "> ";
                        spriteBatch.DrawString(_font, arrow, new Vector2(boxX + 30, textPos.Y), Color.Yellow);
                    }

                    spriteBatch.DrawString(_font, mapName, textPos, textColor);
                }
            }

            // Instructions
            string instructions = _availableMaps.Count > 0 
                ? "UP/DOWN to select, ENTER to load, ESC to cancel"
                : "Press ESC to cancel";
            Vector2 instSize = _font.MeasureString(instructions);
            Vector2 instPos = new Vector2(boxX + (boxWidth - instSize.X) / 2, boxY + boxHeight - 40);
            spriteBatch.DrawString(_font, instructions, instPos, Color.Gray);
        }
    }
}


