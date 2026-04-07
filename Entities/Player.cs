using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drahcir_Htiek.Test_map
{
    public class Player
    {
        public Rectangle Bounds;
        public Color Color = Color.White;
        public int Layer;

        private Rectangle _sourceRectangle;
        private int _frameWidth = 16;
        private int _frameHeight = 32;

        private float _animationTimer = 0f;
        private int _currentFrame = 0;
        private const float _frameTime = 0.15f; // Time per frame in seconds
        private const int _framesPerDirection = 3; // Number of frames for each direction in the sprite sheet
        private int _currentDirection = 0; // 0 = down, 1 = left, 2 = right, 3 = up

        public Player(int startX, int startY) 
        { 
            Bounds = new Rectangle(startX, startY, _frameWidth, _frameHeight);
            Layer = 5; // Standard layer
            SetFrame(0, 0); // Default to the first frame of the sprite sheet
        }


        public void Update(GameTime gameTime, bool isMoving, Vector2 movementDirection)
        {
            int previousDirection = _currentDirection;

            // Uppdatera riktning baserat på rörelse
            if (movementDirection.Y > 0) _currentDirection = 0; // Ner (S)
            else if (movementDirection.Y < 0) _currentDirection = 3; // Upp (W)
            else if (movementDirection.X > 0) _currentDirection = 1; // Höger (D)
            else if (movementDirection.X < 0) _currentDirection = 2; // Vänster (A)

            // Återställ animationen om riktningen ändras
            if (previousDirection != _currentDirection)
            {
                _currentFrame = 0;
                _animationTimer = 0f;
            }

            // Animera endast om spelaren rör sig
            if (isMoving)
            {
                _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_animationTimer >= _frameTime)
                {
                    _currentFrame = (_currentFrame + 1) % _framesPerDirection;
                    _animationTimer = 0f;
                }
            }
            else
            {
                _currentFrame = 0; // Visa idle frame
                _animationTimer = 0f;
            }

            SetFrame(_currentFrame, _currentDirection);
        }


        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (Drahcir_Htiek.TextureLoader.PlayerTexture != null)
            {
                spriteBatch.Draw(Drahcir_Htiek.TextureLoader.PlayerTexture, Bounds, _sourceRectangle, Color);
            }
            else
            {
                spriteBatch.Draw(pixel, Bounds, Color);
            }

            // Debug: rita kollisionen (CollisionBounds) i rött, halvgenomskinligt
            //spriteBatch.Draw(pixel, CollisionBounds, Color.Red * 0.5f);
        }

        public void SetFrame(int frameX, int frameY)
        {
            _sourceRectangle = new Rectangle(frameX * _frameWidth, frameY * _frameHeight, _frameWidth, _frameHeight);
        }
    }
}
