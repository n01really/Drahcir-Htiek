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

        // Collision box size and vertical offset (negative moves the box up)
        //private int _collisionWidth = 12;
        //private int _collisionHeight = 24;
        //private int _collisionYOffset = -8; // change this to move box up/down

        public Player(int startX, int startY) 
        { 
            Bounds = new Rectangle(startX, startY, _frameWidth, _frameHeight);
            Layer = 5; // Standard layer
            SetFrame(0, 0); // Default to the first frame of the sprite sheet
        }

        //public Rectangle CollisionBounds
        //{
        //    get
        //    {
        //        int cx = Bounds.Center.X;
        //        int cy = Bounds.Center.Y + _collisionYOffset;
        //        return new Rectangle(
        //            cx - (_collisionWidth / 2),
        //            cy - (_collisionHeight / 2),
        //            _collisionWidth,
        //            _collisionHeight
        //        );
        //    }
        //}

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
