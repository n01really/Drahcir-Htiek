using System.Collections.Generic;
using System.Linq;
using Drahcir_Htiek.Test_map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

        public Map_Maker()
        {
            _camera = new Camera.Map_Maker_Camera();
        }

        public void SetFont(SpriteFont font)
        {
            _font = font;
        }

        public void SetPixelTexture(Texture2D pixel)
        {
            _pixel = pixel;
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

            switch (tool)
            {
                case EditorTool.HorizontalWall:
                    foreach (var wall in HorWalls)
                    {
                        int rightEdge = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.X - rightEdge) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            return bestSnapPos;
                        }
                    }

                    foreach (var wall in CornerWalls)
                    {
                        int rightEdge = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.X - rightEdge) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            return bestSnapPos;
                        }

                        if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < snapDistance &&
                            System.Math.Abs(worldPos.X - rightEdge) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            return bestSnapPos;
                        }
                    }

                    foreach (var wall in VertWalls)
                    {
                        int rightEdge = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.X - rightEdge) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            return bestSnapPos;
                        }

                        if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < snapDistance &&
                            System.Math.Abs(worldPos.X - rightEdge) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            return bestSnapPos;
                        }
                    }
                    break;

                case EditorTool.VerticalWall:
                    foreach (var wall in CornerWalls)
                    {
                        int belowCorner = wall.Bounds.Y + 11;
                        if (System.Math.Abs(worldPos.Y - belowCorner) < snapDistance &&
                            System.Math.Abs(worldPos.X - wall.Bounds.X) < snapDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowCorner);
                            return bestSnapPos;
                        }
                    }

                    foreach (var wall in VertWalls)
                    {
                        int belowWall = wall.Bounds.Bottom - 11;
                        if (System.Math.Abs(worldPos.Y - belowWall) < snapDistance &&
                            System.Math.Abs(worldPos.X - wall.Bounds.X) < snapDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            return bestSnapPos;
                        }
                    }

                    foreach (var wall in HorWalls)
                    {
                        int belowWall = wall.Bounds.Y + 11;
                        if (System.Math.Abs(worldPos.Y - belowWall) < snapDistance &&
                            System.Math.Abs(worldPos.X - wall.Bounds.X) < snapDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            return bestSnapPos;
                        }

                        int rightEdge = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.Y - belowWall) < snapDistance &&
                            System.Math.Abs(worldPos.X - rightEdge) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, belowWall);
                            return bestSnapPos;
                        }
                    }
                    break;

                case EditorTool.CornerWall:
                    foreach (var wall in HorWalls)
                    {
                        int leftEdge = wall.Bounds.X - 15;
                        if (System.Math.Abs(worldPos.X - leftEdge) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge, wall.Bounds.Y);
                            return bestSnapPos;
                        }
                        
                        int rightEdge = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.X - rightEdge) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Y);
                            return bestSnapPos;
                        }
                        
                        if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < snapDistance &&
                            System.Math.Abs(worldPos.X - rightEdge) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge, wall.Bounds.Bottom);
                            return bestSnapPos;
                        }
                        
                        int leftEdge2 = wall.Bounds.X - 16;
                        if (System.Math.Abs(worldPos.Y - wall.Bounds.Bottom) < snapDistance &&
                            System.Math.Abs(worldPos.X - leftEdge2) < snapDistance)
                        {
                            bestSnapPos = new Vector2(leftEdge2, wall.Bounds.Bottom);
                            return bestSnapPos;
                        }
                    }
                    
                    foreach (var wall in VertWalls)
                    {
                        int belowWall = wall.Bounds.Bottom - 11;
                        if (System.Math.Abs(worldPos.Y - belowWall) < snapDistance &&
                            System.Math.Abs(worldPos.X - wall.Bounds.X) < snapDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowWall);
                            return bestSnapPos;
                        }
                    }
                    
                    foreach (var wall in CornerWalls)
                    {
                        int belowCorner = wall.Bounds.Bottom - 11;
                        if (System.Math.Abs(worldPos.Y - belowCorner) < snapDistance &&
                            System.Math.Abs(worldPos.X - wall.Bounds.X) < snapDistance)
                        {
                            bestSnapPos = new Vector2(wall.Bounds.X, belowCorner);
                            return bestSnapPos;
                        }
                        
                        int rightEdge2 = wall.Bounds.Right - 1;
                        if (System.Math.Abs(worldPos.X - rightEdge2) < snapDistance &&
                            System.Math.Abs(worldPos.Y - wall.Bounds.Y) < snapDistance)
                        {
                            bestSnapPos = new Vector2(rightEdge2, wall.Bounds.Y);
                            return bestSnapPos;
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
                }));
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
                }));
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
                }));
            }

            foreach (var door in Doors)
            {
                var doorToCapture = door;
                drawableObjects.Add((doorToCapture.Layer, () => 
                {
                    spriteBatch.Draw(_pixel, doorToCapture.Bounds, Color.Brown);
                }));
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

            DrawPreview(spriteBatch, mouseState);

            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawUI(spriteBatch);
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

            // Rita snap-linjer om smart snapping är aktivt
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
                            int rightEdge = wall.Bounds.Right - 1;
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
    }
}
