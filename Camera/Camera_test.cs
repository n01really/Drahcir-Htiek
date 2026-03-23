using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Drahcir_Htiek.Camera
{
    internal class Camera_test
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }
        public Matrix Transform { get; private set; }

        private float minZoom = 0.5f;
        private float maxZoom = 30.0f;
        private float zoomSpeed = 0.1f;
        private int previousScrollValue;

        public Camera_test()
        {
            Position = Vector2.Zero;
            Zoom = 15.0f;
            previousScrollValue = Mouse.GetState().ScrollWheelValue;
        }

        public void Update()
        {
            MouseState mouseState = Mouse.GetState();
            int scrollDiffrence = mouseState.ScrollWheelValue - previousScrollValue;

            if (scrollDiffrence != 0)
            {
                Zoom += scrollDiffrence * zoomSpeed * 0.001f * Zoom; // Zoom
                Zoom = MathHelper.Clamp(Zoom, minZoom, maxZoom);
            }

            previousScrollValue = mouseState.ScrollWheelValue;
        }

        public void Follow(Rectangle target, Viewport viewport)
        {
            var centering = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            Position = new Vector2(target.X + target.Width / 2f, target.Y + target.Height / 2f);

            Transform = Matrix.CreateTranslation(new Vector3(-Position, 0)) *
                        Matrix.CreateScale(Zoom, Zoom, 1) *
                        Matrix.CreateTranslation(new Vector3(centering, 0));
        }

    }
}
