using System;
using System.IO;
using System.Text.Json;
using Drahcir_Htiek.Test_map;
using Microsoft.Xna.Framework;

namespace Drahcir_Htiek.Logic
{
    public class Load_Map
    {
        public static void LoadFromJson(Dundgeon dungeon, string mapName)
        {
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string filepath = Path.Combine(projectRoot, "Maps", mapName + ".json");

            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"Map file not found: {filepath}");
                return;
            }

            string jsonString = File.ReadAllText(filepath);
            var mapData = JsonSerializer.Deserialize<MapData>(jsonString);

            dungeon.HorWalls.Clear();
            dungeon.VertWalls.Clear();
            dungeon.CornerWalls.Clear();
            dungeon.Doors.Clear();
            dungeon.FloorTiles.Clear();

            foreach (var wallData in mapData.HorWalls)
            {
                dungeon.HorWalls.Add(new Hor_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.VertWalls)
            {
                dungeon.VertWalls.Add(new Vert_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.CornerWalls)
            {
                dungeon.CornerWalls.Add(new Corner_Wall(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var wallData in mapData.Doors)
            {
                dungeon.Doors.Add(new Door(wallData.X, wallData.Y, wallData.Layer));
            }

            foreach (var floorData in mapData.FloorTiles)
            {
                dungeon.FloorTiles.Add(new Dundgeon_Floor(floorData.X, floorData.Y, floorData.Layer));
            }

            System.Diagnostics.Debug.WriteLine($"Map loaded from: {filepath}");
        }

        public static Vector2? GetPlayerStartPosition(string mapName)
        {
            string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
            string filepath = Path.Combine(projectRoot, "Maps", mapName + ".json");

            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"Map file not found: {filepath}");
                return null;
            }

            string jsonString = File.ReadAllText(filepath);
            var mapData = JsonSerializer.Deserialize<MapData>(jsonString);
            return mapData.PlayerStartPosition;
        }
    }
}
