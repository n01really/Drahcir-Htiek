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
        public Texture2D Texture;

        private Rectangle _sourceRectangle;
        private int _frameWidth = 64;
        private int _frameHeight = 80;

        public Player(int startX, int startY) 
        { 
            Bounds = new Rectangle(startX, startY, 32, 64);
            Layer = 5; // Standard layer
            SetFrame(0, 0); // Default to the first frame of the sprite sheet
        }

        public Rectangle CollisionBounds
        {
            get
            {
                // Centrerad horisontellt och vertikalt i Bounds
                return new Rectangle(
                    Bounds.X + (Bounds.Width / 2) - 8 + 4, // 8 = 16/2
                    Bounds.Y + (Bounds.Height / 2) - 16, // 16 = 32/2
                    16,
                    32
                );
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (Texture != null)
            {
                spriteBatch.Draw(Texture, Bounds, _sourceRectangle, Color);
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
