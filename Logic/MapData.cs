using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Drahcir_Htiek.Logic
{
    public class MapData
    {
        public List<WallData> HorWalls { get; set; } = new List<WallData>();
        public List<WallData> VertWalls { get; set; } = new List<WallData>();
        public List<WallData> CornerWalls { get; set; } = new List<WallData>();
        public List<WallData> Doors { get; set; } = new List<WallData>();
        public List<ChestData> Chests { get; set; } = new List<ChestData>();
        public Vector2? PlayerStartPosition { get; set; }
    }

    public class WallData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Layer { get; set; }
    }

    public class ChestData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
