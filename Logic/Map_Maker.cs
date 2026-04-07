using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System;
using Drahcir_Htiek.Test_map;
using Drahcir_Htiek.Menues;
using Drahcir_Htiek.Entities;

namespace Drahcir_Htiek.Logic
{
    public enum EditorTool
    {
        HorizontalWall,
        VerticalWall,
        CornerWall,
        Door,
        Player,
        Chest,
        Enemy,
        Floor,
        SelectOnly  // Nytt verktyg
    }

    public interface IPlacedObject
    {
        Rectangle Bounds { get; set; }
        int Layer { get; set; }
        string GetTypeName();
    }

    internal class Map_Maker
    {
        private KeyboardState _previousKeyState;
        private MouseState _previousMouseState;
        private SpriteFont _font;
        private Texture2D _pixel;
        private bool _isInitialized = false;

        private EditorTool _currentTool = EditorTool.SelectOnly;
        private int _currentLayer = 0;
        private bool _selectOnlyMode = true;  // Nytt fält för att spåra select-only läge

        public List<Hor_Wall> HorWalls { get; private set; } = new List<Hor_Wall>();
        public List<Vert_Wall> VertWalls { get; private set; } = new List<Vert_Wall>();
        public List<Corner_Wall> CornerWalls { get; private set; } = new List<Corner_Wall>();
        public List<Door> Doors { get; private set; } = new List<Door>();
        public List<Chests> Chests { get; private set; } = new List<Chests>();
        public List<Enemy_test> Enemies { get; private set; } = new List<Enemy_test>();
        public List<Dundgeon_Floor> FloorTiles { get; private set; } = new List<Dundgeon_Floor>();
        public Vector2? PlayerStartPosition { get; private set; } = null;

        private int _gridSize = 16;
        private bool _smartSnapping = true;
        private Camera.Map_Maker_Camera _camera;

        private Save_Map_Menu _saveMapMenu;
        private bool _lastActionWasSave = false;

        private EditorMenues _textureMenu;

        // Selection and movement
        private object _selectedObject = null;
        private string _selectedObjectType = "";
        private int _moveSpeed = 1; // Pixels per keypress

        // Konstanter för väggstorlekar
        private const int DefaultHeight = 48;
        private const int DefaultThickness = 16;

        public Map_Maker()
        {
            _camera = new Camera.Map_Maker_Camera();
            _saveMapMenu = new Save_Map_Menu();
            _textureMenu = new EditorMenues();
        }

        public void SetFont(SpriteFont font)
        {
            _font = font;
            _saveMapMenu.SetFont(font);
        }

        public void SetPixelTexture(Texture2D pixel)
        {
            _pixel = pixel;
            _saveMapMenu.SetPixelTexture(pixel);
        }

        public void InitializeTextureMenu(GraphicsDevice graphicsDevice,
            Texture2D horWallTex, Texture2D vertWallTex, Texture2D cornerWallTex,
            Texture2D doorTex, Texture2D floorTex, Texture2D chestTex, Texture2D enemyTex)
        {
            _textureMenu.Initialize(graphicsDevice, _font, _pixel);
            _textureMenu.SetupDefaultItems(horWallTex, vertWallTex, cornerWallTex,
                doorTex, floorTex, chestTex, enemyTex);
        }

        public void Update(Viewport viewport)
        {
            KeyboardState currentKeyState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            if (!_isInitialized)
            {
                _previousKeyState = currentKeyState;
                _previousMouseState = currentMouseState;
                _isInitialized = true;
                return;
            }

            // Only update camera if no object is selected
            if (_selectedObject == null)
            {
                _camera.Update(viewport);
            }

            // Om save/load-menyn är aktiv, hantera bara den
            if (_saveMapMenu.IsActive)
            {
                _saveMapMenu.Update();

                if (_saveMapMenu.MapSelected)
                {
                    string mapName = _saveMapMenu.SelectedMapName;

                    if (_lastActionWasSave)
                    {
                        SaveMap(mapName);
                        System.Diagnostics.Debug.WriteLine($"Map saved as: {mapName}");
                    }
                    else
                    {
                        LoadMap(mapName);
                        System.Diagnostics.Debug.WriteLine($"Map loaded: {mapName}");
                    }

                    _saveMapMenu.Close();
                }

                _previousKeyState = currentKeyState;
                _previousMouseState = currentMouseState;
                return;
            }

            // Update texture menu
            _textureMenu.Update();

            // Update current tool based on texture menu selection (om inte i select-only läge)
            if (_textureMenu.SelectedItem != null && !_selectOnlyMode)
            {
                _currentTool = _textureMenu.SelectedItem.Tool;
            }

            // Toggle select-only mode med P
            if (currentKeyState.IsKeyDown(Keys.P) && !_previousKeyState.IsKeyDown(Keys.P))
            {
                _selectOnlyMode = !_selectOnlyMode;
                if (_selectOnlyMode)
                {
                    _currentTool = EditorTool.SelectOnly;
                    System.Diagnostics.Debug.WriteLine("Select-Only Mode: ON (ingen textur vald, kan endast markera objekt)");
                }
                else
                {
                    // Återgå till det verktyg som är valt i texture menu
                    if (_textureMenu.SelectedItem != null)
                    {
                        _currentTool = _textureMenu.SelectedItem.Tool;
                    }
                    else
                    {
                        _currentTool = EditorTool.HorizontalWall;
                    }
                    System.Diagnostics.Debug.WriteLine($"Select-Only Mode: OFF (nuvarande verktyg: {_currentTool})");
                }
            }

            // Toggle texture menu with Tab key
            if (currentKeyState.IsKeyDown(Keys.Tab) && !_previousKeyState.IsKeyDown(Keys.Tab))
            {
                _textureMenu.Toggle();
            }

            // Toggle Smart Snapping with T
            if (currentKeyState.IsKeyDown(Keys.T) && !_previousKeyState.IsKeyDown(Keys.T))
            {
                _smartSnapping = !_smartSnapping;
                System.Diagnostics.Debug.WriteLine($"Smart snapping: {_smartSnapping}");
            }

            // Handle object selection and movement BEFORE other keyboard shortcuts
            if (_selectedObject != null)
            {
                HandleSelectedObjectMovement(currentKeyState);
                HandleSelectedObjectLayerChange(currentKeyState);
            }

            // Fallback keyboard shortcuts (fungerar inte i select-only läge)
            if (!_selectOnlyMode)
            {
                if (currentKeyState.IsKeyDown(Keys.D1) && !_previousKeyState.IsKeyDown(Keys.D1))
                    _currentTool = EditorTool.HorizontalWall;
                if (currentKeyState.IsKeyDown(Keys.D2) && !_previousKeyState.IsKeyDown(Keys.D2))
                    _currentTool = EditorTool.VerticalWall;
                if (currentKeyState.IsKeyDown(Keys.D3) && !_previousKeyState.IsKeyDown(Keys.D3))
                    _currentTool = EditorTool.CornerWall;
                if (currentKeyState.IsKeyDown(Keys.D4) && !_previousKeyState.IsKeyDown(Keys.D4))
                    _currentTool = EditorTool.Door;
                if (currentKeyState.IsKeyDown(Keys.D5) && !_previousKeyState.IsKeyDown(Keys.D5))
                    _currentTool = EditorTool.Player;
                if (currentKeyState.IsKeyDown(Keys.D6) && !_previousKeyState.IsKeyDown(Keys.D6))
                    _currentTool = EditorTool.Chest;
                if (currentKeyState.IsKeyDown(Keys.D7) && !_previousKeyState.IsKeyDown(Keys.D7))
                    _currentTool = EditorTool.Floor;
                if (currentKeyState.IsKeyDown(Keys.D8) && !_previousKeyState.IsKeyDown(Keys.D8))
                    _currentTool = EditorTool.Enemy;
            }

            if (currentKeyState.IsKeyDown(Keys.Q) && !_previousKeyState.IsKeyDown(Keys.Q))
                _currentLayer = System.Math.Max(0, _currentLayer - 1);
            if (currentKeyState.IsKeyDown(Keys.E) && !_previousKeyState.IsKeyDown(Keys.E))
                _currentLayer++;

            if (currentKeyState.IsKeyDown(Keys.R) && !_previousKeyState.IsKeyDown(Keys.R))
            {
                _camera.Reset();
                System.Diagnostics.Debug.WriteLine("Camera reset");
            }

            // Deselect object with Escape
            if (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))
            {
                if (_selectedObject != null)
                {
                    _selectedObject = null;
                    _selectedObjectType = "";
                    System.Diagnostics.Debug.WriteLine("Object deselected");
                }
            }

            // Deselect object with X
            if (currentKeyState.IsKeyDown(Keys.X) && !_previousKeyState.IsKeyDown(Keys.X))
            {
                if (_selectedObject != null)
                {
                    _selectedObject = null;
                    _selectedObjectType = "";
                    System.Diagnostics.Debug.WriteLine("Object deselected");
                }
            }

            // Visa save-meny med Ctrl+S
            if (currentKeyState.IsKeyDown(Keys.LeftControl) &&
                currentKeyState.IsKeyDown(Keys.S) &&
                !_previousKeyState.IsKeyDown(Keys.S))
            {
                _saveMapMenu.ShowSaveMenu();
                _lastActionWasSave = true;
            }

            // Visa load-meny med Ctrl+L
            if (currentKeyState.IsKeyDown(Keys.LeftControl) &&
                currentKeyState.IsKeyDown(Keys.L) &&
                !_previousKeyState.IsKeyDown(Keys.L))
            {
                _saveMapMenu.ShowLoadMenu();
                _lastActionWasSave = false;
            }

            // Update hover info
            Vector2 worldPos = _camera.ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
            Rectangle mouseRect = new Rectangle((int)worldPos.X, (int)worldPos.Y, 1, 1);
            UpdateHoverInfo(mouseRect);

            // Don't place objects if clicking on menu
            bool isMouseOverMenu = _textureMenu.IsMouseOverMenu();

            // Left click: Place object or select existing
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released &&
                !_camera.IsDragging &&
                !currentKeyState.IsKeyDown(Keys.Space) &&
                !isMouseOverMenu)
            {
                // Try to select an object first
                if (!SelectObjectAtPosition(mouseRect))
                {
                    // If no object selected AND not in select-only mode, place new one
                    if (!_selectOnlyMode)
                    {
                        PlaceObject(currentMouseState);
                    }
                }
            }

            if (currentMouseState.RightButton == ButtonState.Pressed &&
                _previousMouseState.RightButton == ButtonState.Released &&
                !isMouseOverMenu)
            {
                RemoveObject(currentMouseState);
            }

            _previousKeyState = currentKeyState;
            _previousMouseState = currentMouseState;
        }

        private void HandleSelectedObjectMovement(KeyboardState currentKeyState)
        {
            bool moved = false;
            int deltaX = 0;
            int deltaY = 0;

            if (currentKeyState.IsKeyDown(Keys.Left) && !_previousKeyState.IsKeyDown(Keys.Left))
            {
                deltaX = -_moveSpeed;
                moved = true;
            }
            if (currentKeyState.IsKeyDown(Keys.Right) && !_previousKeyState.IsKeyDown(Keys.Right))
            {
                deltaX = _moveSpeed;
                moved = true;
            }
            if (currentKeyState.IsKeyDown(Keys.Up) && !_previousKeyState.IsKeyDown(Keys.Up))
            {
                deltaY = -_moveSpeed;
                moved = true;
            }
            if (currentKeyState.IsKeyDown(Keys.Down) && !_previousKeyState.IsKeyDown(Keys.Down))
            {
                deltaY = _moveSpeed;
                moved = true;
            }

            if (moved)
            {
                MoveSelectedObject(deltaX, deltaY);
            }
        }

        private void HandleSelectedObjectLayerChange(KeyboardState currentKeyState)
        {
            if (currentKeyState.IsKeyDown(Keys.OemPlus) && !_previousKeyState.IsKeyDown(Keys.OemPlus))
            {
                ChangeSelectedObjectLayer(1);
            }
            if (currentKeyState.IsKeyDown(Keys.OemMinus) && !_previousKeyState.IsKeyDown(Keys.OemMinus))
            {
                ChangeSelectedObjectLayer(-1);
            }
        }

        private void MoveSelectedObject(int deltaX, int deltaY)
        {
            if (_selectedObject == null) return;

            switch (_selectedObjectType)
            {
                case "HorizontalWall":
                    var hWall = (Hor_Wall)_selectedObject;
                    hWall.Bounds = new Rectangle(hWall.Bounds.X + deltaX, hWall.Bounds.Y + deltaY, hWall.Bounds.Width, hWall.Bounds.Height);
                    break;
                case "VerticalWall":
                    var vWall = (Vert_Wall)_selectedObject;
                    vWall.Bounds = new Rectangle(vWall.Bounds.X + deltaX, vWall.Bounds.Y + deltaY, vWall.Bounds.Width, vWall.Bounds.Height);
                    break;
                case "CornerWall":
                    var cWall = (Corner_Wall)_selectedObject;
                    cWall.Bounds = new Rectangle(cWall.Bounds.X + deltaX, cWall.Bounds.Y + deltaY, cWall.Bounds.Width, cWall.Bounds.Height);
                    break;
                case "Door":
                    var door = (Door)_selectedObject;
                    door.Bounds = new Rectangle(door.Bounds.X + deltaX, door.Bounds.Y + deltaY, door.Bounds.Width, door.Bounds.Height);
                    break;
                case "Chest":
                    var chest = (Chests)_selectedObject;
                    chest.Bounds = new Rectangle(chest.Bounds.X + deltaX, chest.Bounds.Y + deltaY, chest.Bounds.Width, chest.Bounds.Height);
                    break;
                case "Enemy":
                    var enemy = (Enemy_test)_selectedObject;
                    enemy.Bounds = new Rectangle(enemy.Bounds.X + deltaX, enemy.Bounds.Y + deltaY, enemy.Bounds.Width, enemy.Bounds.Height);
                    break;
                case "Floor":
                    var floor = (Dundgeon_Floor)_selectedObject;
                    floor.Bounds = new Rectangle(floor.Bounds.X + deltaX, floor.Bounds.Y + deltaY, floor.Bounds.Width, floor.Bounds.Height);
                    break;
                case "Player":
                    if (PlayerStartPosition.HasValue)
                    {
                        PlayerStartPosition = new Vector2(PlayerStartPosition.Value.X + deltaX, PlayerStartPosition.Value.Y + deltaY);
                    }
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Moved {_selectedObjectType} by ({deltaX}, {deltaY})");
        }

        private void ChangeSelectedObjectLayer(int delta)
        {
            if (_selectedObject == null) return;

            switch (_selectedObjectType)
            {
                case "HorizontalWall":
                    var hWall = (Hor_Wall)_selectedObject;
                    hWall.Layer = System.Math.Max(0, hWall.Layer + delta);
                    break;
                case "VerticalWall":
                    var vWall = (Vert_Wall)_selectedObject;
                    vWall.Layer = System.Math.Max(0, vWall.Layer + delta);
                    break;
                case "CornerWall":
                    var cWall = (Corner_Wall)_selectedObject;
                    cWall.Layer = System.Math.Max(0, cWall.Layer + delta);
                    break;
                case "Door":
                    var door = (Door)_selectedObject;
                    door.Layer = System.Math.Max(0, door.Layer + delta);
                    break;
                case "Enemy":
                    var enemy = (Enemy_test)_selectedObject;
                    enemy.Layer = System.Math.Max(0, enemy.Layer + delta);
                    break;
                case "Floor":
                    var floor = (Dundgeon_Floor)_selectedObject;
                    floor.Layer = System.Math.Max(0, floor.Layer + delta);
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Changed {_selectedObjectType} layer by {delta}");
        }

        private bool SelectObjectAtPosition(Rectangle mouseRect)
        {
            // Try to select horizontal walls
            foreach (var wall in HorWalls)
            {
                if (wall.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = wall;
                    _selectedObjectType = "HorizontalWall";
                    System.Diagnostics.Debug.WriteLine($"Selected Horizontal Wall at {wall.Bounds.X}, {wall.Bounds.Y}");
                    return true;
                }
            }

            // Try to select vertical walls
            foreach (var wall in VertWalls)
            {
                if (wall.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = wall;
                    _selectedObjectType = "VerticalWall";
                    System.Diagnostics.Debug.WriteLine($"Selected Vertical Wall at {wall.Bounds.X}, {wall.Bounds.Y}");
                    return true;
                }
            }

            // Try to select corner walls
            foreach (var wall in CornerWalls)
            {
                if (wall.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = wall;
                    _selectedObjectType = "CornerWall";
                    System.Diagnostics.Debug.WriteLine($"Selected Corner Wall at {wall.Bounds.X}, {wall.Bounds.Y}");
                    return true;
                }
            }

            // Try to select doors
            foreach (var door in Doors)
            {
                if (door.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = door;
                    _selectedObjectType = "Door";
                    System.Diagnostics.Debug.WriteLine($"Selected Door at {door.Bounds.X}, {door.Bounds.Y}");
                    return true;
                }
            }

            // Try to select chests
            foreach (var chest in Chests)
            {
                if (chest.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = chest;
                    _selectedObjectType = "Chest";
                    System.Diagnostics.Debug.WriteLine($"Selected Chest at {chest.Bounds.X}, {chest.Bounds.Y}");
                    return true;
                }
            }

            // Try to select enemies
            foreach (var enemy in Enemies)
            {
                if (enemy.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = enemy;
                    _selectedObjectType = "Enemy";
                    System.Diagnostics.Debug.WriteLine($"Selected Enemy at {enemy.Bounds.X}, {enemy.Bounds.Y}");
                    return true;
                }
            }

            // Try to select floor tiles
            foreach (var floor in FloorTiles)
            {
                if (floor.Bounds.Intersects(mouseRect))
                {
                    _selectedObject = floor;
                    _selectedObjectType = "Floor";
                    System.Diagnostics.Debug.WriteLine($"Selected Floor at {floor.Bounds.X}, {floor.Bounds.Y}");
                    return true;
                }
            }

            // Try to select player start
            if (PlayerStartPosition.HasValue)
            {
                Rectangle playerRect = new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                if (playerRect.Intersects(mouseRect))
                {
                    _selectedObject = PlayerStartPosition;
                    _selectedObjectType = "Player";
                    System.Diagnostics.Debug.WriteLine($"Selected Player Start at {PlayerStartPosition.Value.X}, {PlayerStartPosition.Value.Y}");
                    return true;
                }
            }

            return false;
        }

        private void UpdateHoverInfo(Rectangle mouseRect)
        {
            string hoverInfo = "";
            
            // Check if we're hovering over selected object
            if (_selectedObject != null)
            {
                Rectangle selectedBounds = GetSelectedObjectBounds();
                if (selectedBounds.Intersects(mouseRect))
                {
                    int layer = GetSelectedObjectLayer();
                    hoverInfo = $"SELECTED: {_selectedObjectType} | Pos: {selectedBounds.X}, {selectedBounds.Y} | Layer: {layer} | Use Arrow Keys to Move, +/- to Change Layer";
                    _textureMenu.SetHoveredObjectInfo(hoverInfo);
                    return;
                }
            }

            foreach (var wall in HorWalls)
            {
                if (wall.Bounds.Intersects(mouseRect))
                {
                    hoverInfo = $"Horizontal Wall | Pos: {wall.Bounds.X}, {wall.Bounds.Y} | Size: {wall.Bounds.Width}x{wall.Bounds.Height} | Layer: {wall.Layer}";
                    break;
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var wall in VertWalls)
                {
                    if (wall.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Vertical Wall | Pos: {wall.Bounds.X}, {wall.Bounds.Y} | Size: {wall.Bounds.Width}x{wall.Bounds.Height} | Layer: {wall.Layer}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var wall in CornerWalls)
                {
                    if (wall.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Corner Wall | Pos: {wall.Bounds.X}, {wall.Bounds.Y} | Size: {wall.Bounds.Width}x{wall.Bounds.Height} | Layer: {wall.Layer}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var door in Doors)
                {
                    if (door.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Door | Pos: {door.Bounds.X}, {door.Bounds.Y} | Size: {door.Bounds.Width}x{door.Bounds.Height} | Layer: {door.Layer}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var chest in Chests)
                {
                    if (chest.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Chest | Pos: {chest.Bounds.X}, {chest.Bounds.Y} | Size: {chest.Bounds.Width}x{chest.Bounds.Height}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var enemy in Enemies)
                {
                    if (enemy.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Enemy | Pos: {enemy.Bounds.X}, {enemy.Bounds.Y} | Size: {enemy.Bounds.Width}x{enemy.Bounds.Height} | Layer: {enemy.Layer}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                foreach (var floor in FloorTiles)
                {
                    if (floor.Bounds.Intersects(mouseRect))
                    {
                        hoverInfo = $"Floor | Pos: {floor.Bounds.X}, {floor.Bounds.Y} | Size: {floor.Bounds.Width}x{floor.Bounds.Height} | Layer: {floor.Layer}";
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(hoverInfo))
            {
                if (PlayerStartPosition.HasValue)
                {
                    Rectangle playerRect = new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                    if (playerRect.Intersects(mouseRect))
                    {
                        hoverInfo = $"Player Start | Pos: {PlayerStartPosition.Value.X}, {PlayerStartPosition.Value.Y}";
                    }
                }
            }

            _textureMenu.SetHoveredObjectInfo(hoverInfo);
        }

        private Rectangle GetSelectedObjectBounds()
        {
            if (_selectedObject == null) return Rectangle.Empty;

            switch (_selectedObjectType)
            {
                case "HorizontalWall": return ((Hor_Wall)_selectedObject).Bounds;
                case "VerticalWall": return ((Vert_Wall)_selectedObject).Bounds;
                case "CornerWall": return ((Corner_Wall)_selectedObject).Bounds;
                case "Door": return ((Door)_selectedObject).Bounds;
                case "Chest": return ((Chests)_selectedObject).Bounds;
                case "Enemy": return ((Enemy_test)_selectedObject).Bounds;
                case "Floor": return ((Dundgeon_Floor)_selectedObject).Bounds;
                case "Player":
                    if (PlayerStartPosition.HasValue)
                        return new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                    break;
            }

            return Rectangle.Empty;
        }

        private int GetSelectedObjectLayer()
        {
            if (_selectedObject == null) return 0;

            switch (_selectedObjectType)
            {
                case "HorizontalWall": return ((Hor_Wall)_selectedObject).Layer;
                case "VerticalWall": return ((Vert_Wall)_selectedObject).Layer;
                case "CornerWall": return ((Corner_Wall)_selectedObject).Layer;
                case "Door": return ((Door)_selectedObject).Layer;
                case "Enemy": return ((Enemy_test)_selectedObject).Layer;
                case "Floor": return ((Dundgeon_Floor)_selectedObject).Layer;
                default: return 0;
            }
        }

        private Vector2 GetSmartSnappedPosition(Vector2 worldPos, EditorTool tool)
        {
            if (!_smartSnapping)
            {
                int snappedX = ((int)worldPos.X / _gridSize) * _gridSize;
                int snappedY = ((int)worldPos.Y / _gridSize) * _gridSize;
                return new Vector2(snappedX, snappedY);
            }

            int snapDistance = 64;
            Vector2 bestSnapPos = new Vector2(
                ((int)worldPos.X / _gridSize) * _gridSize,
                ((int)worldPos.Y / _gridSize) * _gridSize
            );
            float closestDistance = float.MaxValue;

            switch (tool)
            {
                case EditorTool.HorizontalWall:
                    // Snappa till alla HorWalls (höger och vänster kant)
                    foreach (var wall in HorWalls)
                    {
                        // Höger kant
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Vänster kant
                        int leftEdge = wall.Bounds.X - 47;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }
                    }

                    // Snappa till alla CornerWalls (alla fyra kanter)
                    foreach (var wall in CornerWalls)
                    {
                        // Höger kant, samma Y
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Höger kant, nedre Y
                        float distRightBottom = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Bottom));
                        if (distRightBottom < snapDistance && distRightBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            closestDistance = distRightBottom;
                        }

                        // Vänster kant, samma Y
                        int leftEdge = wall.Bounds.X - 47;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }

                        // Vänster kant, nedre Y
                        float distLeftBottom = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Bottom));
                        if (distLeftBottom < snapDistance && distLeftBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Bottom);
                            closestDistance = distLeftBottom;
                        }
                    }

                    // Snappa till alla VertWalls (topp och botten)
                    foreach (var wall in VertWalls)
                    {
                        // Höger kant, övre Y
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Höger kant, nedre Y
                        float distRightBottom = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Bottom));
                        if (distRightBottom < snapDistance && distRightBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            closestDistance = distRightBottom;
                        }

                        // Vänster kant, övre Y
                        int leftEdge = wall.Bounds.X - 47;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }

                        // Vänster kant, nedre Y
                        float distLeftBottom = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Bottom));
                        if (distLeftBottom < snapDistance && distLeftBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Bottom);
                            closestDistance = distLeftBottom;
                        }
                    }
                    break;

                case EditorTool.VerticalWall:
                    // Snappa till alla CornerWalls (topp och botten)
                    foreach (var wall in CornerWalls)
                    {
                        // Ovanpå corner wall - kompensera för vertical walls visuella offset (37 pixlar upp)
                        float distSamePos = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, wall.Bounds.Y - 37));
                        if (distSamePos < snapDistance && distSamePos < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, wall.Bounds.Y - 37);
                            closestDistance = distSamePos;
                        }

                        // Under corner (direkt under hela corner wall)
                        int belowCorner = wall.Bounds.Bottom - 37;
                        float distBelow = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, belowCorner));
                        if (distBelow < snapDistance && distBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowCorner);
                            closestDistance = distBelow;
                        }

                        // Över corner (direkt över hela corner wall)
                        int aboveCorner = wall.Bounds.Y - DefaultHeight;
                        float distAbove = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, aboveCorner));
                        if (distAbove < snapDistance && distAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, aboveCorner);
                            closestDistance = distAbove;
                        }

                        // Till höger av corner, kompensera för visuell offset (37 pixlar upp)
                        int rightX = wall.Bounds.Right;
                        float distRightSame = Vector2.Distance(worldPos, new Vector2(rightX, wall.Bounds.Y - 37));
                        if (distRightSame < snapDistance && distRightSame < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, wall.Bounds.Y - 37);
                            closestDistance = distRightSame;
                        }

                        // Till höger av corner, under (bredvid till höger, under)
                        float distRightBelow = Vector2.Distance(worldPos, new Vector2(rightX, belowCorner));
                        if (distRightBelow < snapDistance && distRightBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, belowCorner);
                            closestDistance = distRightBelow;
                        }

                        // Till höger av corner, över (bredvid till höger, över)
                        float distRightAbove = Vector2.Distance(worldPos, new Vector2(rightX, aboveCorner));
                        if (distRightAbove < snapDistance && distRightAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, aboveCorner);
                            closestDistance = distRightAbove;
                        }

                        // Till vänster av corner, kompensera för visuell offset (37 pixlar upp)
                        int leftX = wall.Bounds.X - DefaultThickness;
                        float distLeftSame = Vector2.Distance(worldPos, new Vector2(leftX, wall.Bounds.Y - 37));
                        if (distLeftSame < snapDistance && distLeftSame < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, wall.Bounds.Y - 37);
                            closestDistance = distLeftSame;
                        }

                        // Till vänster av corner, under (bredvid till vänster, under)
                        float distLeftBelow = Vector2.Distance(worldPos, new Vector2(leftX, belowCorner));
                        if (distLeftBelow < snapDistance && distLeftBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, belowCorner);
                            closestDistance = distLeftBelow;
                        }

                        // Till vänster av corner, över (bredvid till vänster, över)
                        float distLeftAbove = Vector2.Distance(worldPos, new Vector2(leftX, aboveCorner));
                        if (distLeftAbove < snapDistance && distLeftAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, aboveCorner);
                            closestDistance = distLeftAbove;
                        }
                    }

                    // Snappa till alla VertWalls (topp och botten)
                    foreach (var wall in VertWalls)
                    {
                        // Höger kant, övre Y
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Höger kant, nedre Y
                        float distRightBottom = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Bottom));
                        if (distRightBottom < snapDistance && distRightBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            closestDistance = distRightBottom;
                        }

                        // Vänster kant, övre Y
                        int leftEdge = wall.Bounds.X - 47;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }

                        // Vänster kant, nedre Y
                        float distLeftBottom = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Bottom));
                        if (distLeftBottom < snapDistance && distLeftBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Bottom);
                            closestDistance = distLeftBottom;
                        }
                    }
                    break;

                case EditorTool.CornerWall:
                    // Snappa till alla HorWalls (alla fyra kanter)
                    foreach (var wall in HorWalls)
                    {
                        // Vänster kant, samma Y
                        int leftEdge = wall.Bounds.X - 15;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }

                        // Höger kant, samma Y
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Höger kant, nedre Y
                        float distRightBottom = Vector2.Distance(worldPos, new Vector2(rightEdge, wall.Bounds.Bottom));
                        if (distRightBottom < snapDistance && distRightBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            closestDistance = distRightBottom;
                        }

                        // Vänster kant, nedre Y
                        int leftEdge2 = wall.Bounds.X - 16;
                        float distLeftBottom = Vector2.Distance(worldPos, new Vector2(leftEdge2, wall.Bounds.Bottom));
                        if (distLeftBottom < snapDistance && distLeftBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge2, wall.Bounds.Bottom);
                            closestDistance = distLeftBottom;
                        }

                        // Övre Y, vänster kant
                        int aboveWall = wall.Bounds.Y - 11;
                        float distLeftTop = Vector2.Distance(worldPos, new Vector2(leftEdge, aboveWall));
                        if (distLeftTop < snapDistance && distLeftTop < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, aboveWall);
                            closestDistance = distLeftTop;
                        }

                        // Övre Y, höger kant
                        float distRightTop = Vector2.Distance(worldPos, new Vector2(rightEdge, aboveWall));
                        if (distRightTop < snapDistance && distRightTop < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, aboveWall);
                            closestDistance = distRightTop;
                        }
                    }

                    // Snappa till alla VertWalls (topp och botten, vänster och höger)
                    foreach (var wall in VertWalls)
                    {
                        // Under wall
                        int belowWall = wall.Bounds.Bottom - 11;
                        float distBelow = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, belowWall));
                        if (distBelow < snapDistance && distBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            closestDistance = distBelow;
                        }

                        // Över wall
                        int aboveWall = wall.Bounds.Y - 11;
                        float distAbove = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, aboveWall));
                        if (distAbove < snapDistance && distAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, aboveWall);
                            closestDistance = distAbove;
                        }

                        // Höger sida, under
                        int rightEdge = wall.Bounds.Right - 1;
                        float distRightBelow = Vector2.Distance(worldPos, new Vector2(rightEdge, belowWall));
                        if (distRightBelow < snapDistance && distRightBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, belowWall);
                            closestDistance = distRightBelow;
                        }

                        // Höger sida, över
                        float distRightAbove = Vector2.Distance(worldPos, new Vector2(rightEdge, aboveWall));
                        if (distRightAbove < snapDistance && distRightAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, aboveWall);
                            closestDistance = distRightAbove;
                        }
                    }

                    // Snappa till alla CornerWalls (alla fyra kanter)
                    foreach (var wall in CornerWalls)
                    {
                        // Under corner
                        int belowCorner = wall.Bounds.Bottom - 11;
                        float distBelow = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, belowCorner));
                        if (distBelow < snapDistance && distBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowCorner);
                            closestDistance = distBelow;
                        }

                        // Över corner
                        int aboveCorner = wall.Bounds.Y - 11;
                        float distAbove = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, aboveCorner));
                        if (distAbove < snapDistance && distAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, aboveCorner);
                            closestDistance = distAbove;
                        }

                        // Höger kant, samma Y
                        int rightEdge2 = wall.Bounds.Right - 1;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightEdge2, wall.Bounds.Y));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge2, wall.Bounds.Y);
                            closestDistance = distRight;
                        }

                        // Höger kant, nedre Y
                        float distRightBottom = Vector2.Distance(worldPos, new Vector2(rightEdge2, wall.Bounds.Bottom));
                        if (distRightBottom < snapDistance && distRightBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge2, wall.Bounds.Bottom);
                            closestDistance = distRightBottom;
                        }

                        // Vänster kant, samma Y
                        int leftEdge = wall.Bounds.X - 15;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Y));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            closestDistance = distLeft;
                        }

                        // Vänster kant, nedre Y
                        float distLeftBottom = Vector2.Distance(worldPos, new Vector2(leftEdge, wall.Bounds.Bottom));
                        if (distLeftBottom < snapDistance && distLeftBottom < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Bottom);
                            closestDistance = distLeftBottom;
                        }
                    }
                    break;
            }

            return bestSnapPos;
        }

        private void PlaceObject(MouseState mouseState)
        {
            Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Vector2 snappedPos = GetSmartSnappedPosition(worldPos, _currentTool);
            int snappedX = (int)snappedPos.X;
            int snappedY = (int)snappedPos.Y;

            System.Diagnostics.Debug.WriteLine($"Placing {_currentTool} at {snappedX}, {snappedY}");
            
            // Update info panel with placement position
            _textureMenu.SetLastPlacedPosition(new Vector2(snappedX, snappedY));

            switch (_currentTool)
            {
                case EditorTool.HorizontalWall:
                    HorWalls.Add(new Hor_Wall(snappedX, snappedY, _currentLayer));
                    break;

                case EditorTool.VerticalWall:
                    VertWalls.Add(new Vert_Wall(snappedX, snappedY, _currentLayer));
                    break;

                case EditorTool.CornerWall:
                    CornerWalls.Add(new Corner_Wall(snappedX, snappedY, _currentLayer));
                    break;

                case EditorTool.Door:
                    Doors.Add(new Door(snappedX, snappedY, _currentLayer));
                    break;

                case EditorTool.Player:
                    PlayerStartPosition = new Vector2(snappedX, snappedY);
                    System.Diagnostics.Debug.WriteLine($"Player start position set to: {snappedX}, {snappedY}");
                    break;

                case EditorTool.Chest:
                    Chests.Add(new Chests(snappedX, snappedY));
                    break;

                case EditorTool.Enemy:
                    var enemy = new Enemy_test(snappedX, snappedY) { Layer = _currentLayer };
                    Enemies.Add(enemy);
                    break;

                case EditorTool.Floor:
                    FloorTiles.Add(new Dundgeon_Floor(snappedX, snappedY, _currentLayer));
                    break;

                case EditorTool.SelectOnly:
                    // Gör ingenting - detta verktyg placerar inga objekt
                    System.Diagnostics.Debug.WriteLine("Select-Only Mode: Cannot place objects");
                    break;
            }
        }

        private void RemoveObject(MouseState mouseState)
        {
            Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Rectangle mouseRect = new Rectangle((int)worldPos.X, (int)worldPos.Y, 1, 1);

            HorWalls.RemoveAll(w => w.Bounds.Intersects(mouseRect));
            VertWalls.RemoveAll(w => w.Bounds.Intersects(mouseRect));
            CornerWalls.RemoveAll(w => w.Bounds.Intersects(mouseRect));
            Doors.RemoveAll(d => d.Bounds.Intersects(mouseRect));
            Chests.RemoveAll(c => c.Bounds.Intersects(mouseRect));
            Enemies.RemoveAll(e => e.Bounds.Intersects(mouseRect));
            FloorTiles.RemoveAll(f => f.Bounds.Intersects(mouseRect));

            if (PlayerStartPosition.HasValue)
            {
                Rectangle playerRect = new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                if (playerRect.Intersects(mouseRect))
                {
                    PlayerStartPosition = null;
                    System.Diagnostics.Debug.WriteLine("Player start position removed");
                }
            }

            // Deselect if removed object was selected
            if (_selectedObject != null)
            {
                Rectangle selectedBounds = GetSelectedObjectBounds();
                if (selectedBounds.Intersects(mouseRect))
                {
                    _selectedObject = null;
                    _selectedObjectType = "";
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, MouseState mouseState,
                        Texture2D horWallTexture, Texture2D vertWallTexture, Texture2D cornerWallTexture,
                        Texture2D chestTexture, Texture2D floorTexture)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);

            // Rita floor tiles först (under allt annat)
            foreach (var floor in FloorTiles)
            {
                if (floorTexture != null)
                    spriteBatch.Draw(floorTexture, floor.Bounds, Color.White);
                else
                    spriteBatch.Draw(_pixel, floor.Bounds, Color.DarkGray);
            }

            DrawGrid(spriteBatch, graphicsDevice);

            var drawableObjects = new List<(int layer, System.Action drawAction)>();

            foreach (var wall in HorWalls)
            {
                var wallToCapture = wall;
                drawableObjects.Add((wallToCapture.Layer, () =>
                {
                    if (horWallTexture != null)
                        spriteBatch.Draw(horWallTexture, wallToCapture.Bounds, Color.White);
                    else
                        spriteBatch.Draw(_pixel, wallToCapture.Bounds, Color.Gray);
                }
                ));
            }

            foreach (var wall in VertWalls)
            {
                var wallToCapture = wall;
                drawableObjects.Add((wallToCapture.Layer, () =>
                {
                    if (vertWallTexture != null)
                        spriteBatch.Draw(vertWallTexture, wallToCapture.Bounds, Color.White);
                    else
                        spriteBatch.Draw(_pixel, wallToCapture.Bounds, Color.Red);
                }
                ));
            }

            foreach (var wall in CornerWalls)
            {
                var wallToCapture = wall;
                drawableObjects.Add((wallToCapture.Layer, () =>
                {
                    if (cornerWallTexture != null)
                        spriteBatch.Draw(cornerWallTexture, wallToCapture.Bounds, Color.White);
                    else
                        spriteBatch.Draw(_pixel, wallToCapture.Bounds, Color.DarkRed);
                }
                ));
            }

            foreach (var door in Doors)
            {
                var doorToCapture = door;
                drawableObjects.Add((doorToCapture.Layer, () =>
                {
                    spriteBatch.Draw(_pixel, doorToCapture.Bounds, Color.Brown);
                }
                ));
            }

            foreach (var chest in Chests)
            {
                var chestToCapture = chest;
                drawableObjects.Add((0, () =>
                {
                    if (chestTexture != null)
                        spriteBatch.Draw(chestTexture, chestToCapture.Bounds, Color.White);
                    else
                        spriteBatch.Draw(_pixel, chestToCapture.Bounds, Color.Green);
                }
                ));
            }

            foreach (var enemy in Enemies)
            {
                var e = enemy;
                drawableObjects.Add((e.Layer, () =>
                {
                    // Draw using enemy.Draw which uses the pixel texture
                    e.Draw(spriteBatch, _pixel);
                }));
            }

            var sortedObjects = drawableObjects.OrderBy(obj => obj.layer).ToList();

            foreach (var obj in sortedObjects)
            {
                obj.drawAction();
            }

            if (PlayerStartPosition.HasValue)
            {
                Rectangle playerRect = new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                spriteBatch.Draw(_pixel, playerRect, Color.Blue);
            }

            // Draw selection highlight
            if (_selectedObject != null)
            {
                Rectangle selectedBounds = GetSelectedObjectBounds();
                DrawSelectionHighlight(spriteBatch, selectedBounds);
            }

            // Visa inte förhandsvisning i select-only läge
            if (!_selectOnlyMode)
            {
                DrawPreview(spriteBatch, mouseState);
            }

            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawUI(spriteBatch);
            _textureMenu.Draw(spriteBatch);
            _saveMapMenu.Draw(spriteBatch, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            spriteBatch.End();
        }

        private void DrawSelectionHighlight(SpriteBatch spriteBatch, Rectangle bounds)
        {
            int thickness = 2;
            Color highlightColor = Color.Cyan;

            // Top
            spriteBatch.Draw(_pixel, new Rectangle(bounds.X - thickness, bounds.Y - thickness, bounds.Width + thickness * 2, thickness), highlightColor);
            // Bottom
            spriteBatch.Draw(_pixel, new Rectangle(bounds.X - thickness, bounds.Bottom, bounds.Width + thickness * 2, thickness), highlightColor);
            // Left
            spriteBatch.Draw(_pixel, new Rectangle(bounds.X - thickness, bounds.Y, thickness, bounds.Height), highlightColor);
            // Right
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Right, bounds.Y, thickness, bounds.Height), highlightColor);
        }

        private void DrawGrid(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (_pixel == null)
                return;

            Vector2 topLeft = _camera.ScreenToWorld(Vector2.Zero);
            Vector2 bottomRight = _camera.ScreenToWorld(new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height));

            int startX = ((int)topLeft.X / _gridSize - 1) * _gridSize;
            int startY = ((int)topLeft.Y / _gridSize - 1) * _gridSize;
            int endX = (int)bottomRight.X + _gridSize;
            int endY = (int)bottomRight.Y + _gridSize;

            Color gridColor = Color.White * 0.2f;

            for (int x = startX; x < endX; x += _gridSize)
            {
                Rectangle line = new Rectangle(x, (int)topLeft.Y - _gridSize, 1, (int)(bottomRight.Y - topLeft.Y) + _gridSize * 2);
                spriteBatch.Draw(_pixel, line, gridColor);
            }

            for (int y = startY; y < endY; y += _gridSize)
            {
                Rectangle line = new Rectangle((int)topLeft.X - _gridSize, y, (int)(bottomRight.X - topLeft.X) + _gridSize * 2, 1);
                spriteBatch.Draw(_pixel, line, gridColor);
            }
        }

        private void DrawPreview(SpriteBatch spriteBatch, MouseState mouseState)
        {
            if (_pixel == null || _camera.IsDragging || _textureMenu.IsMouseOverMenu())
                return;

            Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Vector2 snappedPos = GetSmartSnappedPosition(worldPos, _currentTool);
            int snappedX = (int)snappedPos.X;
            int snappedY = (int)snappedPos.Y;

            // ... (your existing snap visualization code)

            Rectangle previewRect;
            Color previewColor = Color.Yellow * 0.5f;

            switch (_currentTool)
            {
                case EditorTool.HorizontalWall:
                    previewRect = new Rectangle(snappedX, snappedY, 48, 48);
                    break;
                case EditorTool.VerticalWall:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 48);
                    break;
                case EditorTool.CornerWall:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 48);
                    break;
                case EditorTool.Door:
                    previewRect = new Rectangle(snappedX, snappedY, 48, 48);
                    break;
                case EditorTool.Player:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 32);
                    previewColor = Color.Blue * 0.5f;
                    break;
                case EditorTool.Chest:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 16);
                    previewColor = Color.Green * 0.5f;
                    break;
                case EditorTool.Enemy:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 32);
                    previewColor = Color.Red * 0.5f;
                    break;
                case EditorTool.Floor:
                    previewRect = new Rectangle(snappedX, snappedY, 16, 16);
                    previewColor = Color.Brown * 0.5f;
                    break;
                default:
                    return;
            }

            spriteBatch.Draw(_pixel, previewRect, previewColor);
        }

        private void DrawUI(SpriteBatch spriteBatch)
        {
            if (_font == null || _pixel == null)
                return;

            List<string> uiLines = new List<string>
            {
                "MAP EDITOR",
                $"Current Tool: {(_selectOnlyMode ? "SELECT ONLY" : _currentTool.ToString())}",
                $"Layer: {_currentLayer} (Q/E to change)",
                $"Zoom: {_camera.Zoom:F2}x (Scroll Wheel)",
                $"Smart Snap: {(_smartSnapping ? "ON" : "OFF")} (T to toggle)",
                $"Select-Only Mode: {(_selectOnlyMode ? "ON" : "OFF")} (P to toggle)",
                "Tab: Toggle Texture Menu",
                "Left Click: Place/Select | Right Click: Remove",
                "Arrow Keys: Move Selected Object (1px)",
                "+/-: Change Selected Object Layer",
                "X: Deselect Object",
                "Middle Mouse / Space+Drag: Pan Camera",
                "R: Reset Camera",
                "Ctrl+S: Save | Ctrl+L: Load",
                "",
                $"Objects: Walls={HorWalls.Count + VertWalls.Count + CornerWalls.Count}, Chests={Chests.Count}, Enemies={Enemies.Count}, Floors={FloorTiles.Count}",
                $"Camera: X={_camera.Position.X:F0}, Y={_camera.Position.Y:F0}"
            };

            if (_selectedObject != null)
            {
                uiLines.Add("");
                uiLines.Add($"SELECTED: {_selectedObjectType}");
            }

            Vector2 position = new Vector2(10, 10);
            float lineHeight = _font.MeasureString("A").Y;

            Vector2 maxSize = Vector2.Zero;
            foreach (string line in uiLines)
            {
                Vector2 size = _font.MeasureString(line);
                if (size.X > maxSize.X)
                    maxSize.X = size.X;
            }
            maxSize.Y = lineHeight * uiLines.Count;

            Rectangle bgRect = new Rectangle((int)position.X - 5, (int)position.Y - 5, (int)maxSize.X + 10, (int)maxSize.Y + 10);
            spriteBatch.Draw(_pixel, bgRect, Color.Black * 0.7f);

            foreach (string line in uiLines)
            {
                Color textColor = line.StartsWith("SELECTED") ? Color.Cyan : Color.White;
                
                // Färglägg Select-Only Mode linjen
                if (line.Contains("Select-Only Mode"))
                {
                    textColor = _selectOnlyMode ? Color.Lime : Color.White;
                }
                
                // Färglägg Current Tool linjen när i select-only läge
                if (line.StartsWith("Current Tool") && _selectOnlyMode)
                {
                    textColor = Color.Lime;
                }
                
                spriteBatch.DrawString(_font, line, position, textColor);
                position.Y += lineHeight;
            }
        }

        public void SaveMap(string filename)
        {
            var mapData = new MapData
            {
                PlayerStartPosition = PlayerStartPosition
            };

            foreach (var wall in HorWalls)
            {
                mapData.HorWalls.Add(new WallData
                {
                    X = wall.Bounds.X,
                    Y = wall.Bounds.Y,
                    Layer = wall.Layer
                });
            }

            foreach (var wall in VertWalls)
            {
                mapData.VertWalls.Add(new WallData
                {
                    X = wall.Bounds.X,
                    Y = wall.Bounds.Y,
                    Layer = wall.Layer
                });
            }

            foreach (var wall in CornerWalls)
            {
                mapData.CornerWalls.Add(new WallData
                {
                    X = wall.Bounds.X,
                    Y = wall.Bounds.Y,
                    Layer = wall.Layer
                });
            }

            foreach (var door in Doors)
            {
                mapData.Doors.Add(new WallData
                {
                    X = door.Bounds.X,
                    Y = door.Bounds.Y,
                    Layer = door.Layer
                });
            }

            foreach (var chest in Chests)
            {
                mapData.Chests.Add(new ChestData
                {
                    X = chest.Bounds.X,
                    Y = chest.Bounds.Y
                });
            }

            foreach (var enemy in Enemies)
            {
                mapData.Enemies.Add(new EnemyData
                {
                    X = enemy.Bounds.X,
                    Y = enemy.Bounds.Y,
                    Layer = enemy.Layer
                });
            }

            foreach (var floor in FloorTiles)
            {
                mapData.FloorTiles.Add(new FloorTileData
                {
                    X = floor.Bounds.X,
                    Y = floor.Bounds.Y,
                    Width = floor.Bounds.Width,
                    Height = floor.Bounds.Height,
                    Layer = floor.Layer
                });
            }

            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string mapsFolder = Path.Combine(projectRoot, "Maps");
            if (!Directory.Exists(mapsFolder))
            {
                Directory.CreateDirectory(mapsFolder);
            }

            string filepath = Path.Combine(mapsFolder, filename + ".json");
            string jsonString = JsonSerializer.Serialize(mapData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filepath, jsonString);

            System.Diagnostics.Debug.WriteLine($"Map saved to: {filepath}");
        }

        public void LoadMap(string filename)
        {
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string filepath = Path.Combine(projectRoot, "Maps", filename + ".json");

            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"Map file not found: {filepath}");
                return;
            }

            string jsonString = File.ReadAllText(filepath);
            var mapData = JsonSerializer.Deserialize<MapData>(jsonString);

            HorWalls.Clear();
            VertWalls.Clear();
            CornerWalls.Clear();
            Doors.Clear();
            Chests.Clear();
            Enemies.Clear();
            FloorTiles.Clear();

            foreach (var wallData in mapData.HorWalls)
            {
                HorWalls.Add(new Hor_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.VertWalls)
            {
                VertWalls.Add(new Vert_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.CornerWalls)
            {
                CornerWalls.Add(new Corner_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.Doors)
            {
                Doors.Add(new Door(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var chestData in mapData.Chests)
            {
                Chests.Add(new Chests(chestData.X, chestData.Y));
            }

            foreach (var enemyData in mapData.Enemies)
            {
                var e = new Enemy_test(enemyData.X, enemyData.Y) { Layer = enemyData.Layer };
                Enemies.Add(e);
            }

            foreach (var floorData in mapData.FloorTiles)
            {
                FloorTiles.Add(new Dundgeon_Floor(floorData.X, floorData.Y, floorData.Layer));
            }

            PlayerStartPosition = mapData.PlayerStartPosition;

            System.Diagnostics.Debug.WriteLine($"Map loaded from: {filepath}");
        }
    }
}