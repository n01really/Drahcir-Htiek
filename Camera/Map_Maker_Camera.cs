using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Drahcir_Htiek.Camera
{
    internal class Map_Maker_Camera
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }
        public Matrix Transform { get; private set; }

        private float _minZoom = 0.5f;
        private float _maxZoom = 5.0f;
        private float _zoomSpeed = 0.15f;
        private int _previousScrollValue;

        // För att dra kameran med musen
        private bool _isDragging = false;
        private Vector2 _dragStartMousePos;
        private Vector2 _dragStartCameraPos;
        private MouseState _previousMouseState;

        // För kamerans hastighet
        private int _keyboardMoveSpeed = 5;

        public Map_Maker_Camera()
        {
            Position = Vector2.Zero;
            Zoom = 1.0f;
            _previousScrollValue = Mouse.GetState().ScrollWheelValue;
            _previousMouseState = Mouse.GetState();
        }

        public void Update(Viewport viewport)
        {
            MouseState currentMouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            // Hantera zoom med scroll wheel
            HandleZoom(currentMouseState);

            // Hantera kamera-drag med mellanmusknapp eller håll Space + vänsterklick
            HandleDragging(currentMouseState, keyState);

            // Alternativ: Flytta kamera med piltangenter (backup om man föredrar det)
            HandleKeyboardMovement(keyState);

            // Uppdatera transform-matrisen
            UpdateTransform(viewport);

            _previousMouseState = currentMouseState;
            _previousScrollValue = currentMouseState.ScrollWheelValue;
        }

        private void HandleZoom(MouseState currentMouseState)
        {
            int scrollDifference = currentMouseState.ScrollWheelValue - _previousScrollValue;

            if (scrollDifference != 0)
            {
                // Spara musens världsposition innan zoom
                Vector2 mouseWorldPosBefore = ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));

                // Ändra zoom
                float zoomChange = scrollDifference * _zoomSpeed * 0.001f;
                Zoom += zoomChange;
                Zoom = MathHelper.Clamp(Zoom, _minZoom, _maxZoom);

                // Justera kamerans position så att zoom sker mot muspekaren
                Vector2 mouseWorldPosAfter = ScreenToWorld(new Vector2(currentMouseState.X, currentMouseState.Y));
                Position += mouseWorldPosBefore - mouseWorldPosAfter;

                System.Diagnostics.Debug.WriteLine($"Zoom: {Zoom:F2}");
            }
        }

        private void HandleDragging(MouseState currentMouseState, KeyboardState keyState)
        {
            // Använd mellanmusknapp ELLER Space + vänsterklick för att dra
            bool dragButton = currentMouseState.MiddleButton == ButtonState.Pressed ||
                             (keyState.IsKeyDown(Keys.Space) && currentMouseState.LeftButton == ButtonState.Pressed);

            if (dragButton && !_isDragging)
            {
                // Börja dra
                _isDragging = true;
                _dragStartMousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
                _dragStartCameraPos = Position;
            }
            else if (!dragButton && _isDragging)
            {
                // Sluta dra
                _isDragging = false;
            }

            if (_isDragging)
            {
                // Beräkna hur mycket musen har rört sig
                Vector2 currentMousePos = new Vector2(currentMouseState.X, currentMouseState.Y);
                Vector2 mouseDelta = _dragStartMousePos - currentMousePos;

                // Flytta kameran (inverterad zoom-kompensation)
                Position = _dragStartCameraPos + mouseDelta / Zoom;
            }
        }

        private void HandleKeyboardMovement(KeyboardState keyState)
        {
            // Backup: Flytta med piltangenter (snabbare än musklick)
            if (keyState.IsKeyDown(Keys.Left))
                Position = new Vector2(Position.X - _keyboardMoveSpeed / Zoom, Position.Y);
            if (keyState.IsKeyDown(Keys.Right))
                Position = new Vector2(Position.X + _keyboardMoveSpeed / Zoom, Position.Y);
            if (keyState.IsKeyDown(Keys.Up))
                Position = new Vector2(Position.X, Position.Y - _keyboardMoveSpeed / Zoom);
            if (keyState.IsKeyDown(Keys.Down))
                Position = new Vector2(Position.X, Position.Y + _keyboardMoveSpeed / Zoom);
        }

        private void UpdateTransform(Viewport viewport)
        {
            // Skapa transform-matris för zooming och panning
            var centering = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

            Transform = Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                       Matrix.CreateScale(Zoom, Zoom, 1) *
                       Matrix.CreateTranslation(new Vector3(centering, 0));
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Konvertera skärmkoordinater till världskoordinater
            return Vector2.Transform(screenPosition, Matrix.Invert(Transform));
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            // Konvertera världskoordinater till skärmkoordinater
            return Vector2.Transform(worldPosition, Transform);
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Zoom = 1.0f;
        }

        public bool IsDragging => _isDragging;
    }
}
