using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drahcir_Htiek.Test_map
{
    public class Chests
    {
        public Rectangle Bounds;
        //public Color Color = Color.Green;
        public Texture2D Texture;


        public Chests(int x, int y)
        {
            //Bounds = new Rectangle(x, y, 16, 16);
        }

        public void Draw(SpriteBatch spriteBatch /*Texture2D pixel*/)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }
}
