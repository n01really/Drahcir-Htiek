using Drahcir_Htiek.Menues;
using Drahcir_Htiek.Test_map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Drahcir_Htiek.Logic
{
    public enum EditorTool
    {
        HorizontalWall,
        VerticalWall,
        CornerWall,
        Door,
        Player,
        Chest
    }

    internal class Map_Maker
    {
        private KeyboardState _previousKeyState;
        private MouseState _previousMouseState;
        private SpriteFont _font;
        private Texture2D _pixel;
        private bool _isInitialized = false;

        private EditorTool _currentTool = EditorTool.HorizontalWall;
        private int _currentLayer = 0;

        public List<Hor_Wall> HorWalls { get; private set; } = new List<Hor_Wall>();
        public List<Vert_Wall> VertWalls { get; private set; } = new List<Vert_Wall>();
        public List<Corner_Wall> CornerWalls { get; private set; } = new List<Corner_Wall>();
        public List<Door> Doors { get; private set; } = new List<Door>();
        public List<Test_Chest> Chests { get; private set; } = new List<Test_Chest>();
        public Vector2? PlayerStartPosition { get; private set; } = null;

        private int _gridSize = 16;
        private bool _smartSnapping = true;
        private Camera.Map_Maker_Camera _camera;

        private Save_Map_Menu _saveMapMenu;
        private bool _lastActionWasSave = false;

        // Konstanter för väggstorlekar
        private const int DefaultHeight = 48;
        private const int DefaultThickness = 16;

        public Map_Maker()
        {
            _camera = new Camera.Map_Maker_Camera();
            _saveMapMenu = new Save_Map_Menu();
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

            _camera.Update(viewport);

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
                return; // Avsluta Update tidigt
            }

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

            if (currentKeyState.IsKeyDown(Keys.Q) && !_previousKeyState.IsKeyDown(Keys.Q))
                _currentLayer = System.Math.Max(0, _currentLayer - 1);
            if (currentKeyState.IsKeyDown(Keys.E) && !_previousKeyState.IsKeyDown(Keys.E))
                _currentLayer++;

            if (currentKeyState.IsKeyDown(Keys.T) && !_previousKeyState.IsKeyDown(Keys.T))
            {
                _smartSnapping = !_smartSnapping;
                System.Diagnostics.Debug.WriteLine($"Smart snapping: {_smartSnapping}");
            }

            if (currentKeyState.IsKeyDown(Keys.R) && !_previousKeyState.IsKeyDown(Keys.R))
            {
                _camera.Reset();
                System.Diagnostics.Debug.WriteLine("Camera reset");
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

            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released &&
                !_camera.IsDragging &&
                !currentKeyState.IsKeyDown(Keys.Space))
            {
                PlaceObject(currentMouseState);
            }

            if (currentMouseState.RightButton == ButtonState.Pressed &&
                _previousMouseState.RightButton == ButtonState.Released)
            {
                RemoveObject(currentMouseState);
            }

            _previousKeyState = currentKeyState;
            _previousMouseState = currentMouseState;
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
                        int belowCorner = wall.Bounds.Bottom;
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
                        // Exakt samma position (överlappande) - INGEN offset, de ska överlappa helt
                        float distSamePos = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, wall.Bounds.Y));
                        if (distSamePos < snapDistance && distSamePos < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, wall.Bounds.Y);
                            closestDistance = distSamePos;
                        }

                        // Under wall (direkt efter) - ingen offset
                        int belowWall = wall.Bounds.Bottom;
                        float distBelow = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, belowWall));
                        if (distBelow < snapDistance && distBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            closestDistance = distBelow;
                        }

                        // Över wall (direkt innan) - använd samma offset som mot corner wall
                        int aboveWall = wall.Bounds.Y - DefaultHeight - 37;
                        float distAbove = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, aboveWall));
                        if (distAbove < snapDistance && distAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, aboveWall);
                            closestDistance = distAbove;
                        }

                        // Till höger av wall, kompensera för visuell offset (samma som corner wall)
                        int rightX = wall.Bounds.Right;
                        float distRightSame = Vector2.Distance(worldPos, new Vector2(rightX, wall.Bounds.Y - 37));
                        if (distRightSame < snapDistance && distRightSame < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, wall.Bounds.Y - 37);
                            closestDistance = distRightSame;
                        }

                        // Till höger av wall, under
                        float distRightBelow = Vector2.Distance(worldPos, new Vector2(rightX, belowWall));
                        if (distRightBelow < snapDistance && distRightBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, belowWall);
                            closestDistance = distRightBelow;
                        }

                        // Till höger av wall, över - använd samma aboveWall
                        float distRightAbove = Vector2.Distance(worldPos, new Vector2(rightX, aboveWall));
                        if (distRightAbove < snapDistance && distRightAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, aboveWall);
                            closestDistance = distRightAbove;
                        }

                        // Till vänster av wall, kompensera för visuell offset (samma som corner wall)
                        int leftX = wall.Bounds.X - DefaultThickness;
                        float distLeftSame = Vector2.Distance(worldPos, new Vector2(leftX, wall.Bounds.Y - 37));
                        if (distLeftSame < snapDistance && distLeftSame < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, wall.Bounds.Y - 37);
                            closestDistance = distLeftSame;
                        }

                        // Till vänster av wall, under
                        float distLeftBelow = Vector2.Distance(worldPos, new Vector2(leftX, belowWall));
                        if (distLeftBelow < snapDistance && distLeftBelow < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, belowWall);
                            closestDistance = distLeftBelow;
                        }

                        // Till vänster av wall, över - använd samma aboveWall
                        float distLeftAbove = Vector2.Distance(worldPos, new Vector2(leftX, aboveWall));
                        if (distLeftAbove < snapDistance && distLeftAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(leftX, aboveWall);
                            closestDistance = distLeftAbove;
                        }
                    }

                    // Snappa till alla HorWalls (alla fyra hörn/kanter)
                    foreach (var wall in HorWalls)
                    {
                        // Under wall, vänster kant
                        int belowWall = wall.Bounds.Bottom;
                        float distLeft = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, belowWall));
                        if (distLeft < snapDistance && distLeft < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            closestDistance = distLeft;
                        }

                        // Under wall, höger kant
                        int rightX = wall.Bounds.Right - DefaultThickness;
                        float distRight = Vector2.Distance(worldPos, new Vector2(rightX, belowWall));
                        if (distRight < snapDistance && distRight < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, belowWall);
                            closestDistance = distRight;
                        }

                        // Över wall, vänster kant
                        int aboveWall = wall.Bounds.Y - DefaultHeight;
                        float distLeftAbove = Vector2.Distance(worldPos, new Vector2(wall.Bounds.X, aboveWall));
                        if (distLeftAbove < snapDistance && distLeftAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, aboveWall);
                            closestDistance = distLeftAbove;
                        }

                        // Över wall, höger kant
                        float distRightAbove = Vector2.Distance(worldPos, new Vector2(rightX, aboveWall));
                        if (distRightAbove < snapDistance && distRightAbove < closestDistance)
                        {
                            bestSnapPos = new Vector2(rightX, aboveWall);
                            closestDistance = distRightAbove;
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
                    Chests.Add(new Test_Chest(snappedX, snappedY));
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

            if (PlayerStartPosition.HasValue)
            {
                Rectangle playerRect = new Rectangle((int)PlayerStartPosition.Value.X, (int)PlayerStartPosition.Value.Y, 16, 32);
                if (playerRect.Intersects(mouseRect))
                {
                    PlayerStartPosition = null;
                    System.Diagnostics.Debug.WriteLine("Player start position removed");
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, MouseState mouseState,
                        Texture2D horWallTexture, Texture2D vertWallTexture, Texture2D cornerWallTexture,
                        Texture2D chestTexture)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);

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

            DrawPreview(spriteBatch, mouseState);

            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawUI(spriteBatch);
            _saveMapMenu.Draw(spriteBatch, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            spriteBatch.End();
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
            if (_pixel == null || _camera.IsDragging)
                return;

            Vector2 worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Vector2 snappedPos = GetSmartSnappedPosition(worldPos, _currentTool);
            int snappedX = (int)snappedPos.X;
            int snappedY = (int)snappedPos.Y;

            if (_smartSnapping)
            {
                switch (_currentTool)
                {
                    case EditorTool.HorizontalWall:
                        foreach (var wall in HorWalls)
                        {
                            int rightEdge = wall.Bounds.Right - 1;
                            if (System.Math.Abs(worldPos.X - rightEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int leftEdge = wall.Bounds.X - 47;
                            if (System.Math.Abs(worldPos.X - leftEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(leftEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in CornerWalls)
                        {
                            int rightEdge = wall.Bounds.Right - 1;
                            if (System.Math.Abs(worldPos.X - rightEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                            if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < 64 &&
                                System.Math.Abs(worldPos.X - rightEdge) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge - 10, wall.Bounds.Bottom, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int leftEdge = wall.Bounds.X - 47;
                            if (System.Math.Abs(worldPos.X - leftEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(leftEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in VertWalls)
                        {
                            int rightEdge = wall.Bounds.Right - 1;
                            if (System.Math.Abs(worldPos.X - rightEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                            if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < 64 &&
                                System.Math.Abs(worldPos.X - rightEdge) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge - 10, wall.Bounds.Bottom, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int leftEdge = wall.Bounds.X - 47;
                            if (System.Math.Abs(worldPos.X - leftEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(leftEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        break;

                    case EditorTool.VerticalWall:
                        foreach (var wall in CornerWalls)
                        {
                            int belowCorner = wall.Bounds.Y + 11;
                            if (System.Math.Abs(worldPos.Y - belowCorner) < 64 &&
                                System.Math.Abs(worldPos.X - wall.Bounds.X) < 64)
                            {
                                Rectangle snapLine = new Rectangle(wall.Bounds.X - 10, belowCorner, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in VertWalls)
                        {
                            int belowWall = wall.Bounds.Bottom - 11;
                            if (System.Math.Abs(worldPos.Y - belowWall) < 64 &&
                                System.Math.Abs(worldPos.X - wall.Bounds.X) < 64)
                            {
                                Rectangle snapLine = new Rectangle(wall.Bounds.X - 10, belowWall, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in HorWalls)
                        {
                            int belowWall = wall.Bounds.Y + 11;
                            if (System.Math.Abs(worldPos.Y - belowWall) < 64 &&
                                System.Math.Abs(worldPos.X - wall.Bounds.X) < 64)
                            {
                                Rectangle snapLine = new Rectangle(wall.Bounds.X - 10, belowWall, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                            int rightEdge = wall.Bounds.Right - 16;
                            if (System.Math.Abs(worldPos.Y - belowWall) < 64 &&
                                System.Math.Abs(worldPos.X - rightEdge) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge - 10, belowWall, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        break;

                    case EditorTool.CornerWall:
                        foreach (var wall in HorWalls)
                        {
                            int leftEdge = wall.Bounds.X - 15;
                            if (System.Math.Abs(worldPos.X - leftEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(leftEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int rightEdge = wall.Bounds.Right - 1;
                            if (System.Math.Abs(worldPos.X - rightEdge) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < 64 &&
                                System.Math.Abs(worldPos.X - rightEdge) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge - 10, wall.Bounds.Bottom, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int leftEdge2 = wall.Bounds.X - 16;
                            if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < 64 &&
                                System.Math.Abs(worldPos.X - leftEdge2) < 64)
                            {
                                Rectangle snapLine = new Rectangle(leftEdge2 - 10, wall.Bounds.Bottom, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in VertWalls)
                        {
                            int belowWall = wall.Bounds.Bottom - 11;
                            if (System.Math.Abs(worldPos.Y - belowWall) < 64 &&
                                System.Math.Abs(worldPos.X - wall.Bounds.X) < 64)
                            {
                                Rectangle snapLine = new Rectangle(wall.Bounds.X - 10, belowWall, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        foreach (var wall in CornerWalls)
                        {
                            int belowCorner = wall.Bounds.Bottom - 11;
                            if (System.Math.Abs(worldPos.Y - belowCorner) < 64 &&
                                System.Math.Abs(worldPos.X - wall.Bounds.X) < 64)
                            {
                                Rectangle snapLine = new Rectangle(wall.Bounds.X - 10, belowCorner, 36, 2);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }

                            int rightEdge2 = wall.Bounds.Right - 1;
                            if (System.Math.Abs(worldPos.X - rightEdge2) < 64 &&
                                System.Math.Abs(worldPos.Y - wall.Bounds.Y) < 64)
                            {
                                Rectangle snapLine = new Rectangle(rightEdge2, wall.Bounds.Y - 10, 2, 68);
                                spriteBatch.Draw(_pixel, snapLine, Color.Cyan * 0.5f);
                            }
                        }
                        break;
                }
            }

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
                $"Current Tool: {_currentTool} (1-6 to switch)",
                $"Layer: {_currentLayer} (Q/E to change)",
                $"Zoom: {_camera.Zoom:F2}x (Scroll Wheel)",
                $"Smart Snap: {(_smartSnapping ? "ON" : "OFF")} (T to toggle)",
                "Left Click: Place | Right Click: Remove",
                "Middle Mouse / Space+Drag: Pan Camera",
                "Arrow Keys: Move Camera | R: Reset Camera",
                "ESC: Return to Menu",
                "",
                "Tools:",
                "1: Horizontal Wall | 2: Vertical Wall | 3: Corner Wall",
                "4: Door | 5: Player Start | 6: Chest",
                "Ctrl+S: Save Map | Ctrl+L: Load Map",
                "",
                $"Objects: Walls={HorWalls.Count + VertWalls.Count + CornerWalls.Count}, Chests={Chests.Count}",
                $"Camera: X={_camera.Position.X:F0}, Y={_camera.Position.Y:F0}"
            };

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
                spriteBatch.DrawString(_font, line, position, Color.White);
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
                Chests.Add(new Test_Chest(chestData.X, chestData.Y));
            }

            PlayerStartPosition = mapData.PlayerStartPosition;

            System.Diagnostics.Debug.WriteLine($"Map loaded from: {filepath}");
        }
    }
}