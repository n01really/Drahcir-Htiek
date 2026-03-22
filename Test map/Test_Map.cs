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
        public Color Color = Color.Red;
        public Hor_Wall(int x, int y, int width, int thickness = 5)
        {
            Bounds = new Rectangle(x, y, width, thickness);
        }
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Bounds, Color);
        }
    }

    public class Vert_Wall
    {
        public Rectangle Bounds;
        public Color Color = Color.Red;
        public Vert_Wall(int x, int y, int height, int thickness = 5)
        {
            Bounds = new Rectangle(x, y, thickness, height);
        }
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, Bounds, Color);
        }
    }

    public class Test_Map
    {
        public List<Hor_Wall> HorWalls;
        public List<Vert_Wall> VertWalls;

        public Test_Map()
        {
            HorWalls = new List<Hor_Wall>();
            VertWalls = new List<Vert_Wall>();

            // --- RUM 1 (Startrummet uppe till vänster) ---
            HorWalls.Add(new Hor_Wall(50, 50, 100)); // Toppvägg
            VertWalls.Add(new Vert_Wall(50, 50, 1000)); // Vänstervägg
            VertWalls.Add(new Vert_Wall(150, 50, 105)); // Högervägg

            // Bottenväggen i rum 1 delas i två för att skapa en dörröppning till korridoren
            HorWalls.Add(new Hor_Wall(50, 150, 30)); // Botten (vänster om dörr)
            HorWalls.Add(new Hor_Wall(120, 150, 30)); // Botten (höger om dörr)
            // --- KORRIDOR (Går neråt) ---
            VertWalls.Add(new Vert_Wall(80, 150, 100)); // Vänster korridorvägg
            VertWalls.Add(new Vert_Wall(120, 150, 60)); // Höger korridorvägg (kortare för svängen)

            // --- KORRIDOR (Svänger höger) ---
            HorWalls.Add(new Hor_Wall(50, 250, 1500)); // Undre väggen i korridoren
            HorWalls.Add(new Hor_Wall(120, 210, 110)); // Övre väggen (efter svängen)

            // --- RUM 2 (Fyrkantigt rum i slutet av korridoren) ---
            HorWalls.Add(new Hor_Wall(230, 150, 100)); // Toppvägg
            VertWalls.Add(new Vert_Wall(330, 150, 100)); // Högervägg
            HorWalls.Add(new Hor_Wall(230, 250, 100)); // Bottenvägg

            // Vänsterväggen har en öppning för korridoren
            VertWalls.Add(new Vert_Wall(230, 155, 60)); // Övre delen av vänsterväggen
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Rita alla horisontella väggar
            foreach (var hw in HorWalls)
                hw.Draw(spriteBatch, pixel);

            // Rita alla vertikala väggar
            foreach (var vw in VertWalls)
                vw.Draw(spriteBatch, pixel);
        }
    }
}
    
