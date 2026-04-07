using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using Drahcir_Htiek.Logic;

namespace Drahcir_Htiek.Menues
{
    public enum TextureCategory
    {
        Walls,
        Floors,
        Doors,
        NPCs,
        Furniture
    }

    public class TextureMenuItem
    {
        public string Name { get; set; }
        public Rectangle SourceRect { get; set; }
        public EditorTool Tool { get; set; }
        public Texture2D Texture { get; set; }
        public Rectangle DisplayBounds { get; set; }
    }

    internal class EditorMenues
    {
        private Texture2D _pixelTexture;
        private SpriteFont _font;
        private Rectangle _menuBounds;
        private Rectangle _infoPanelBounds;
        private List<Rectangle> _tabBounds;
        private TextureCategory _currentTab;
        private Dictionary<TextureCategory, List<TextureMenuItem>> _menuItems;
        private TextureMenuItem _selectedItem;
        private MouseState _previousMouseState;
        private bool _isVisible;

        // Info panel properties
        private string _hoveredObjectInfo;
        private Vector2 _lastPlacedPosition;

        private const int MenuWidth = 300;
        private const int TabHeight = 50;
        private const int InfoPanelHeight = 120;
        private const int ItemSize = 64;
        private const int Padding = 12;
        private const int ItemsPerColumn = 2;
        private const float ItemTextScale = 1.0f;
        private const float TabTextScale = 0.65f;
        private const float InfoTextScale = 0.8f;

        public bool IsVisible => _isVisible;
        public TextureMenuItem SelectedItem => _selectedItem;

        public EditorMenues()
        {
            _tabBounds = new List<Rectangle>();
            _menuItems = new Dictionary<TextureCategory, List<TextureMenuItem>>();
            _currentTab = TextureCategory.Walls;
            _isVisible = true;
            _previousMouseState = Mouse.GetState();
            _hoveredObjectInfo = "";
            _lastPlacedPosition = Vector2.Zero;

            foreach (TextureCategory category in Enum.GetValues(typeof(TextureCategory)))
            {
                _menuItems[category] = new List<TextureMenuItem>();
            }
        }

        public void Initialize(GraphicsDevice graphicsDevice, SpriteFont font, Texture2D pixel)
        {
            _font = font;
            _pixelTexture = pixel;

            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;
            
            // Meny på höger sida
            _menuBounds = new Rectangle(
                screenWidth - MenuWidth,
                0,
                MenuWidth,
                screenHeight - InfoPanelHeight
            );

            // Info panel längst ner
            _infoPanelBounds = new Rectangle(
                0,
                screenHeight - InfoPanelHeight,
                screenWidth,
                InfoPanelHeight
            );

            CalculateTabBounds();
        }

        public void SetHoveredObjectInfo(string info)
        {
            _hoveredObjectInfo = info;
        }

        public void SetLastPlacedPosition(Vector2 position)
        {
            _lastPlacedPosition = position;
        }

        public void AddTextureItem(TextureCategory category, string name, Rectangle sourceRect,
            EditorTool tool, Texture2D texture)
        {
            var item = new TextureMenuItem
            {
                Name = name,
                SourceRect = sourceRect,
                Tool = tool,
                Texture = texture
            };

            _menuItems[category].Add(item);
        }

        public void SetupDefaultItems(Texture2D horWallTex, Texture2D vertWallTex, Texture2D cornerWallTex,
            Texture2D doorTex, Texture2D floorTex, Texture2D chestTex, Texture2D enemyTex)
        {
            // Walls
            if (horWallTex != null)
            {
                AddTextureItem(TextureCategory.Walls, "Horizontal Wall",
                    new Rectangle(0, 0, 48, 48), EditorTool.HorizontalWall, horWallTex);
            }
            if (vertWallTex != null)
            {
                AddTextureItem(TextureCategory.Walls, "Vertical Wall",
                    new Rectangle(0, 0, 16, 48), EditorTool.VerticalWall, vertWallTex);
            }
            if (cornerWallTex != null)
            {
                AddTextureItem(TextureCategory.Walls, "Corner Wall",
                    new Rectangle(0, 0, 16, 48), EditorTool.CornerWall, cornerWallTex);
            }

            // Floors
            if (floorTex != null)
            {
                AddTextureItem(TextureCategory.Floors, "Floor Tile",
                    new Rectangle(0, 0, 16, 16), EditorTool.Floor, floorTex);
            }

            // Doors
            if (doorTex != null)
            {
                AddTextureItem(TextureCategory.Doors, "Door",
                    new Rectangle(0, 0, 48, 48), EditorTool.Door, doorTex);
            }

            // NPCs
            if (enemyTex != null)
            {
                AddTextureItem(TextureCategory.NPCs, "Enemy",
                    new Rectangle(0, 0, 16, 32), EditorTool.Enemy, enemyTex);
            }

            // Add Player Start Position tool
            AddTextureItem(TextureCategory.NPCs, "Player Start",
                new Rectangle(0, 0, 16, 32), EditorTool.Player, _pixelTexture);

            // Furniture
            if (chestTex != null)
            {
                AddTextureItem(TextureCategory.Furniture, "Chest",
                    new Rectangle(0, 0, 16, 16), EditorTool.Chest, chestTex);
            }
        }

        private void CalculateTabBounds()
        {
            _tabBounds.Clear();
            int tabCount = Enum.GetValues(typeof(TextureCategory)).Length;
            int tabWidth = _menuBounds.Width / tabCount;

            for (int i = 0; i < tabCount; i++)
            {
                _tabBounds.Add(new Rectangle(
                    _menuBounds.X + (i * tabWidth),
                    _menuBounds.Y,
                    tabWidth,
                    TabHeight
                ));
            }
        }

        public void Update()
        {
            if (!_isVisible) return;

            MouseState mouseState = Mouse.GetState();
            Point mousePos = new Point(mouseState.X, mouseState.Y);

            // Check tab clicks
            if (mouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                for (int i = 0; i < _tabBounds.Count; i++)
                {
                    if (_tabBounds[i].Contains(mousePos))
                    {
                        _currentTab = (TextureCategory)i;
                        break;
                    }
                }

                // Check item clicks
                var items = _menuItems[_currentTab];
                foreach (var item in items)
                {
                    if (item.DisplayBounds.Contains(mousePos))
                    {
                        _selectedItem = item;
                        break;
                    }
                }
            }

            _previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible) return;

            // Draw menu background
            spriteBatch.Draw(_pixelTexture, _menuBounds, Color.Black * 0.85f);

            // Draw tabs (horisontellt högst upp i menyn)
            int tabIndex = 0;
            foreach (TextureCategory category in Enum.GetValues(typeof(TextureCategory)))
            {
                Rectangle tabRect = _tabBounds[tabIndex];
                Color tabColor = _currentTab == category ? new Color(80, 80, 80) : new Color(50, 50, 50);

                spriteBatch.Draw(_pixelTexture, tabRect, tabColor);

                // Draw tab border
                DrawBorder(spriteBatch, tabRect, Color.White * 0.8f, 2);

                // Draw tab text (horisontell) med större text och bättre kontrast
                string tabName = category.ToString();
                Vector2 textSize = _font.MeasureString(tabName) * TabTextScale;
                
                Vector2 textPos = new Vector2(
                    tabRect.X + (tabRect.Width - textSize.X) / 2,
                    tabRect.Y + (tabRect.Height - textSize.Y) / 2
                );
                
                // Rita text med skugga för bättre läsbarhet
                Vector2 shadowOffset = new Vector2(2, 2);
                spriteBatch.DrawString(_font, tabName, textPos + shadowOffset, Color.Black * 0.8f,
                    0f, Vector2.Zero, TabTextScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, tabName, textPos, Color.White,
                    0f, Vector2.Zero, TabTextScale, SpriteEffects.None, 0f);

                tabIndex++;
            }

            // Draw content area (börjar under tabbarna)
            Rectangle contentArea = new Rectangle(
                _menuBounds.X + Padding,
                _menuBounds.Y + TabHeight + Padding,
                _menuBounds.Width - Padding * 2,
                _menuBounds.Height - TabHeight - Padding * 2
            );

            // Draw items in current tab (vertikalt, 2 kolumner)
            var items = _menuItems[_currentTab];
            int itemIndex = 0;

            foreach (var item in items)
            {
                int col = itemIndex % ItemsPerColumn;
                int row = itemIndex / ItemsPerColumn;

                int x = contentArea.X + col * (ItemSize + Padding);
                int y = contentArea.Y + row * (ItemSize + Padding + 40);

                item.DisplayBounds = new Rectangle(x, y, ItemSize, ItemSize);

                // Draw item background
                Color bgColor = _selectedItem == item ? new Color(200, 180, 0) : new Color(70, 70, 70);
                spriteBatch.Draw(_pixelTexture, item.DisplayBounds, bgColor);

                // Draw texture preview
                if (item.Texture != null)
                {
                    // Special handling for Player Start tool
                    if (item.Tool == EditorTool.Player)
                    {
                        Rectangle playerRect = new Rectangle(
                            x + (ItemSize - 16) / 2,
                            y + (ItemSize - 32) / 2,
                            16,
                            32
                        );
                        spriteBatch.Draw(_pixelTexture, playerRect, Color.Blue);
                    }
                    else
                    {
                        // Calculate scale to fit within ItemSize
                        float scaleX = (float)ItemSize / item.SourceRect.Width;
                        float scaleY = (float)ItemSize / item.SourceRect.Height;
                        float scale = Math.Min(scaleX, scaleY) * 0.8f;

                        int drawWidth = (int)(item.SourceRect.Width * scale);
                        int drawHeight = (int)(item.SourceRect.Height * scale);

                        Rectangle drawRect = new Rectangle(
                            x + (ItemSize - drawWidth) / 2,
                            y + (ItemSize - drawHeight) / 2,
                            drawWidth,
                            drawHeight
                        );

                        spriteBatch.Draw(item.Texture, drawRect, item.SourceRect, Color.White);
                    }
                }

                // Draw item border
                Color borderColor = _selectedItem == item ? Color.Yellow : Color.White * 0.7f;
                DrawBorder(spriteBatch, item.DisplayBounds, borderColor, 2);

                // Draw item name below med större text och skugga
                if (_font != null && item.Name != null)
                {
                    int maxWidth = ItemSize * ItemsPerColumn + Padding * (ItemsPerColumn - 1);
                    
                    // Dela upp texten i flera rader om den är för lång
                    string[] words = item.Name.Split(' ');
                    List<string> lines = new List<string>();
                    string currentLine = "";

                    foreach (string word in words)
                    {
                        string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                        Vector2 testSize = _font.MeasureString(testLine) * ItemTextScale;
                        
                        if (testSize.X > maxWidth && currentLine.Length > 0)
                        {
                            lines.Add(currentLine);
                            currentLine = word;
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }
                    if (currentLine.Length > 0)
                        lines.Add(currentLine);

                    // Rita varje rad med skugga
                    float yOffset = 0;
                    foreach (string line in lines)
                    {
                        Vector2 lineSize = _font.MeasureString(line) * ItemTextScale;
                        Vector2 namePos = new Vector2(
                            x + (ItemSize - lineSize.X) / 2,
                            y + ItemSize + 6 + yOffset
                        );
                        
                        // Rita text med skugga
                        Vector2 shadowOffset = new Vector2(1, 1);
                        spriteBatch.DrawString(_font, line, namePos + shadowOffset, Color.Black,
                            0f, Vector2.Zero, ItemTextScale, SpriteEffects.None, 0f);
                        spriteBatch.DrawString(_font, line, namePos, Color.White,
                            0f, Vector2.Zero, ItemTextScale, SpriteEffects.None, 0f);
                        
                        yOffset += lineSize.Y + 2;
                    }
                }

                itemIndex++;
            }

            // Draw border för hela menyn
            DrawBorder(spriteBatch, _menuBounds, Color.White * 0.8f, 3);

            // Draw info panel
            DrawInfoPanel(spriteBatch);
        }

        private void DrawInfoPanel(SpriteBatch spriteBatch)
        {
            // Draw background
            spriteBatch.Draw(_pixelTexture, _infoPanelBounds, Color.Black * 0.9f);
            DrawBorder(spriteBatch, _infoPanelBounds, Color.White * 0.8f, 3);

            if (_selectedItem == null) return;

            Vector2 startPos = new Vector2(_infoPanelBounds.X + 20, _infoPanelBounds.Y + 15);
            float lineHeight = _font.MeasureString("A").Y * InfoTextScale + 5;

            // Selected Tool Info
            List<string> infoLines = new List<string>
            {
                $"SELECTED TOOL: {_selectedItem.Tool}",
                $"Name: {_selectedItem.Name}",
                $"Texture Size: {_selectedItem.SourceRect.Width}x{_selectedItem.SourceRect.Height}",
                $"Last Placed: X={_lastPlacedPosition.X:F0}, Y={_lastPlacedPosition.Y:F0}"
            };

            // Add hovered object info if available
            if (!string.IsNullOrEmpty(_hoveredObjectInfo))
            {
                infoLines.Add("");
                infoLines.Add("HOVERED OBJECT:");
                infoLines.Add(_hoveredObjectInfo);
            }

            Vector2 currentPos = startPos;
            foreach (string line in infoLines)
            {
                // Draw shadow
                Vector2 shadowOffset = new Vector2(1, 1);
                spriteBatch.DrawString(_font, line, currentPos + shadowOffset, Color.Black,
                    0f, Vector2.Zero, InfoTextScale, SpriteEffects.None, 0f);
                
                // Draw text
                Color textColor = line.StartsWith("SELECTED") || line.StartsWith("HOVERED") ? Color.Yellow : Color.White;
                spriteBatch.DrawString(_font, line, currentPos, textColor,
                    0f, Vector2.Zero, InfoTextScale, SpriteEffects.None, 0f);
                
                currentPos.Y += lineHeight;
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        public void Toggle()
        {
            _isVisible = !_isVisible;
        }

        public void Show()
        {
            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
        }

        public bool IsMouseOverMenu()
        {
            if (!_isVisible) return false;

            MouseState mouseState = Mouse.GetState();
            Point mousePos = new Point(mouseState.X, mouseState.Y);
            
            // Inkludera både menyn och info panelen
            return _menuBounds.Contains(mousePos) || _infoPanelBounds.Contains(mousePos);
        }
    }
}
