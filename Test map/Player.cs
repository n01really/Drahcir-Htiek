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
        public Color Color = Color.Blue;

        public Player(int startX, int startY) 
        { 
            Bounds = new Rectangle(startX, startY, 16, 32);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Bounds, Color);
        }
    }
}
