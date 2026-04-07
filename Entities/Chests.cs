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

        public Chests(int x, int y)
        {
            //Bounds = new Rectangle(x, y, 16, 16);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Drahcir_Htiek.TextureLoader.ChestTexture, Bounds, Color.White);
        }
    }
}
