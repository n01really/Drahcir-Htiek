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
        public static int DefaultWidth = 48;
        public static int DefaultThickness = 48;
        
        public Rectangle Bounds;
        public Texture2D Texture;
        public int Layer;

        public Hor_Wall(int x, double y, int layer = 0)
        {
            Bounds = new Rectangle(x, (int)y, DefaultWidth, DefaultThickness);
            Layer = layer;
        }
        
        // Ny metod för att få kollisionsrektangeln baserat på spelarens position
        public Rectangle GetCollisionBounds(Rectangle oldPlayerBounds)
        {
            // Kolla om spelaren är under eller över väggen
            int playerBottomY = oldPlayerBounds.Bottom;
            int wallCenterY = Bounds.Center.Y;
            
            // Om spelaren är under väggen (kommer underifrån)
            if (playerBottomY > wallCenterY)
            {
                // Returnera de översta 24 pixlarna av väggen (ökat från 16)
                // Detta skapar en större kollisionzon för att förhindra att spelaren går in i väggen
                return new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 24);
            }
            // Om spelaren är över väggen (kommer ovanifrån)
            else
            {
                // Returnera de nedersta 24 pixlarna av väggen (ökat från 16)
                // Detta skapar en större kollisionzon
                return new Rectangle(Bounds.X, Bounds.Bottom - 24, Bounds.Width, 24);
            }
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }

    public class Vert_Wall
    {
        public static int DefaultHeight = 48;
        public static int DefaultThickness = 16;
        
        public Rectangle Bounds;
        //public Color Color = Color.Red;
        public Texture2D Texture;
        public int Layer;

        public Vert_Wall(int x, int y, int layer = 0)
        {
            Bounds = new Rectangle(x, y, DefaultThickness, DefaultHeight);
            Layer = layer;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }
    
    public class Corner_Wall
    {
        public static int DefaultHeight = 48;
        public static int DefaultThickness = 16;
        
        public Rectangle Bounds;
        public Texture2D Texture;
        public int Layer;
        
        public Corner_Wall(int x, int y, int layer = 0)
        {
            Bounds = new Rectangle(x, y, DefaultThickness, DefaultHeight);
            Layer = layer;
        }
        
        // Kollision baserad på HÖJD, inte bredd
        public Rectangle GetCollisionBounds(Rectangle oldPlayerBounds)
        {
            // Kolla om spelaren är under eller över väggen
            int playerBottomY = oldPlayerBounds.Bottom;
            int wallCenterY = Bounds.Center.Y;
            
            // Om spelaren är under väggen (kommer underifrån)
            if (playerBottomY > wallCenterY)
            {
                // Returnera de översta 24 pixlarna av väggen
                // Använd Bounds.Width så att varje vägg behåller sin egen bredd
                return new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 24);
            }
            // Om spelaren är över väggen (kommer ovanifrån)
            else
            {
                // Returnera de nedersta 24 pixlarna av väggen
                // Höjden (24 pixlar) är det som styr kollisionen, inte bredden
                return new Rectangle(Bounds.X, Bounds.Bottom - 24, Bounds.Width, 24);
            }
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }

    public class Door
    {
        public static int DefaultHeigtht = 34;
        public static int DefaultWidth = 16;

        public Rectangle Bounds;
        public Texture2D Texture;
        public int Layer;

        public Door(int x, int y, int layer = 0)
        {
            Bounds = new Rectangle(x, y, DefaultWidth, DefaultHeigtht);
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
        public List<Door> Doors;
        public Texture2D HorWallTexture;
        public Texture2D VertWallTexture;
        public Texture2D CornerWallTexture;
        public Texture2D DoorTexture;

        public Test_Map()
        {
            HorWalls = new List<Hor_Wall>();
            VertWalls = new List<Vert_Wall>();
            CornerWalls = new List<Corner_Wall>();
            Doors = new List<Door>();

            // --- RUM 1 (Startrummet uppe till vänster) ---
            HorWalls.Add(new Hor_Wall(15, 0, 1)); // Toppvägg
            HorWalls.Add(new Hor_Wall(62, 0, 1)); // Toppvägg
            HorWalls.Add(new Hor_Wall(109, 0, 1)); // Toppvägg
            CornerWalls.Add(new Corner_Wall(156, 0, 2)); // HögerÖvrehörn
            VertWalls.Add(new Vert_Wall(156, 11, 3)); // Högervägg
            VertWalls.Add(new Vert_Wall(156, 48, 3)); // Högervägg
            VertWalls.Add(new Vert_Wall(156, 85, 3)); // Högervägg
            CornerWalls.Add(new Corner_Wall(156, 122, 4)); // HögerNedrehörn
            CornerWalls.Add(new Corner_Wall(0, 0, 2)); // VänsterÖvrehörn
            VertWalls.Add(new Vert_Wall(0, 11, 3)); // Vänstervägg            
            VertWalls.Add(new Vert_Wall(0, 48, 3)); // Vänstervägg
            VertWalls.Add(new Vert_Wall(0, 85, 3)); // Vänstervägg
            CornerWalls.Add(new Corner_Wall(0, 122, 4)); // VänsterNedrehörn
            HorWalls.Add(new Hor_Wall(15, 122, 4)); // Bottenväggvänster
            HorWalls.Add(new Hor_Wall(109, 122, 4)); // Bottenvägghöger
            CornerWalls.Add(new Corner_Wall(62, 122, 4)); // Bottenväggmittenvänster
            Doors.Add(new Door(78, 135, 4)); // Dörr i mitten av bottenväggen
            CornerWalls.Add(new Corner_Wall(94, 122, 4)); // BottenväggmittenHöger

            //-- korridor 1 --
            VertWalls.Add(new Vert_Wall(110, 133, 5));
            VertWalls.Add(new Vert_Wall(110, 170, 5));
            CornerWalls.Add(new Corner_Wall(110, 207, 6));
            VertWalls.Add(new Vert_Wall(46, 133, 5));
            VertWalls.Add(new Vert_Wall(46, 170, 5));
            VertWalls.Add(new Vert_Wall(46, 207, 5));
            VertWalls.Add(new Vert_Wall(46, 244, 5));
            CornerWalls.Add(new Corner_Wall(46, 281, 5));
            HorWalls.Add(new Hor_Wall(61, 281, 5));
            HorWalls.Add(new Hor_Wall(108, 281, 5));
            HorWalls.Add(new Hor_Wall(125, 207, 5));
            CornerWalls.Add(new Corner_Wall(155, 281, 5));

            // -- rum 2 --
            VertWalls.Add(new Vert_Wall(155, 292, 6));
            VertWalls.Add(new Vert_Wall(155, 329, 6));
            VertWalls.Add(new Vert_Wall(156, 133, 5));
            VertWalls.Add(new Vert_Wall(156, 170, 5));
            CornerWalls.Add(new Corner_Wall(156, 207, 5));
            CornerWalls.Add(new Corner_Wall(155, 366, 6));
            HorWalls.Add(new Hor_Wall(171, 122, 6));
            HorWalls.Add(new Hor_Wall(218, 122, 6));
            CornerWalls.Add(new Corner_Wall(265, 122, 6));
            Doors.Add(new Door(281, 124, 6));
            CornerWalls.Add(new Corner_Wall(297, 122, 6));
            HorWalls.Add(new Hor_Wall(312, 122, 6));
            // två horWalls för hela toppVäggen
            // 5 HorWalls för hela bottenVäggen
            // 4 VertWalls 2 cornerwalls och en dörr för hela vänsterVäggen


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
                else if (wall is Door d)
                {
                    d.Texture = DoorTexture;
                    d.Draw(spriteBatch);
                }
            }
        }
    }
}
