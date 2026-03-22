using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drahcir_Htiek.Test_map
{
    public class Hor_Wall
    {
        public Rectangle Bounds;
        public Texture2D Texture;
        public int Layer;

        public Hor_Wall(int x, double y, int width, int thickness = 48, int layer = 0)
        {
            Bounds = new Rectangle(x, (int)y, width, thickness);
            Layer = layer;
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }

    public class Vert_Wall
    {
        public Rectangle Bounds;
        //public Color Color = Color.Red;
        public Texture2D Texture;
        public int Layer;

        public Vert_Wall(int x, int y, int height, int thickness = 16, int layer = 0)
        {
            Bounds = new Rectangle(x, y, thickness, height);
            Layer = layer;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }
    
    public class Corner_Wall
    {
        public Rectangle Bounds;
        public Texture2D Texture;
        public int Layer;
        
        public Corner_Wall(int x, int y, int height, int thickness = 16, int layer = 0)
        {
            Bounds = new Rectangle(x, y, thickness, height);
            Layer = layer;
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }

    public class Test_Map
    {
        public List<Hor_Wall> HorWalls;
        public List<Vert_Wall> VertWalls;
        public List<Corner_Wall> CornerWalls;
        public Texture2D HorWallTexture;
        public Texture2D VertWallTexture;
        public Texture2D CornerWallTexture;

        public Test_Map()
        {
            HorWalls = new List<Hor_Wall>();
            VertWalls = new List<Vert_Wall>();
            CornerWalls = new List<Corner_Wall>();

            // --- RUM 1 (Startrummet uppe till vänster) ---
            HorWalls.Add(new Hor_Wall(15, 0, 48, 48, 1)); // Toppvägg
            HorWalls.Add(new Hor_Wall(62, 0, 48, 48, 1)); // Toppvägg
            VertWalls.Add(new Vert_Wall(0, 11, 48, 16, 3)); // Högervägg
            CornerWalls.Add(new Corner_Wall(0, 0, 48, 16, 2));
            VertWalls.Add(new Vert_Wall(0, 48, 48, 16, 3));
            CornerWalls.Add(new Corner_Wall(0, 85, 48, 16, 4));

            // Bottenväggen i rum 1 delas i två för att skapa en dörröppning till korridoren
            HorWalls.Add(new Hor_Wall(50, 150, 48)); // Botten (vänster om dörr)
            HorWalls.Add(new Hor_Wall(120, 150, 48)); // Botten (höger om dörr)
            // --- KORRIDOR (Går neråt) ---
            VertWalls.Add(new Vert_Wall(80, 150, 48)); // Vänster korridorvägg
            VertWalls.Add(new Vert_Wall(120, 150, 48)); // Höger korridorvägg (kortare för svängen)

            // --- KORRIDOR (Svänger höger) ---
            HorWalls.Add(new Hor_Wall(50, 250, 48)); // Undre väggen i korridoren
            HorWalls.Add(new Hor_Wall(120, 163, 48)); // Övre väggen (efter svängen)

            // --- RUM 2 (Fyrkantigt rum i slutet av korridoren) ---
            HorWalls.Add(new Hor_Wall(230, 150, 48)); // Toppvägg
            VertWalls.Add(new Vert_Wall(330, 150, 48)); // Högervägg
            HorWalls.Add(new Hor_Wall(230, 250, 48)); // Bottenvägg

            // Vänsterväggen har en öppning för korridoren
            VertWalls.Add(new Vert_Wall(230, 55, 48)); // Övre delen av vänsterväggen
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Sortera och rita i lager-ordning
            var allWalls = HorWalls
                .Cast<object>()
                .Concat(VertWalls.Cast<object>())
                .Concat(CornerWalls.Cast<object>())
                .OrderBy(w => w is Hor_Wall hw ? hw.Layer : 
                              w is Vert_Wall vw ? vw.Layer : 
                              ((Corner_Wall)w).Layer);

            foreach (var wall in allWalls)
            {
                if (wall is Hor_Wall hw)
                {
                    hw.Texture = HorWallTexture;
                    hw.Draw(spriteBatch);
                }
                else if (wall is Vert_Wall vw)
                {
                    vw.Texture = VertWallTexture;
                    vw.Draw(spriteBatch);
                }
                else if (wall is Corner_Wall cw)
                {
                    cw.Texture = CornerWallTexture;
                    cw.Draw(spriteBatch);
                }
            }
        }
    }
}
    
