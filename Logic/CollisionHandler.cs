using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Drahcir_Htiek.Test_map;


namespace Drahcir_Htiek.Logic
{
    public static class CollisionHandler
    {
        public static bool IsColliding(Rectangle newBounds, Rectangle oldBounds, Test_Map map)
        { 
            foreach (var wall in map.HorWalls)
            {
                // Använd den dynamiska kollisionsrektangeln baserat på spelarens GAMLA position
                if (newBounds.Intersects(wall.GetCollisionBounds(oldBounds)))
                    return true;
            }

            foreach (var wall in map.VertWalls)
            {
                if (newBounds.Intersects(wall.Bounds))
                    return true;
            }
            
            foreach (var wall in map.CornerWalls)
            {
                // Ändra från wall.Bounds till wall.GetCollisionBounds(oldBounds)
                if (newBounds.Intersects(wall.GetCollisionBounds(oldBounds)))
                    return true;
            }
            
            return false;
        }

        public static bool IsColliding(Rectangle newBounds, Test_Chest chest)
        {
            
            if (newBounds.Intersects(chest.Bounds))
            {
                return true; 
            }

            return false;
        }
    }
}
