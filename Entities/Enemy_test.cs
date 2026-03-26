using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drahcir_Htiek.Entities
{
    internal class Enemy_test
    {

        public Rectangle Bounds;
        public Color Color = Color.Red;
        public int Layer;

        public Enemy_test(int startX, int startY)
        {
            Bounds = new Rectangle(startX, startY, 16, 32);
            Layer = 5; // Standard layer
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Bounds, Color);
        }

    }
}
