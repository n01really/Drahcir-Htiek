using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Drahcir_Htiek
{
    public static class TextureLoader
    {
        // Game textures
        public static Texture2D Pixel { get; private set; }
        public static Texture2D PlayerTexture { get; private set; }
        public static Texture2D ChestTexture { get; private set; }

        // Wall textures
        public static Texture2D HorWallTexture { get; private set; }
        public static Texture2D VertWallTexture { get; private set; }
        public static Texture2D CornerWallTexture { get; private set; }
        public static Texture2D DoorTexture { get; private set; }

        // Floor textures
        public static Texture2D DungeonFloorTexture { get; private set; }

        // Font
        public static SpriteFont DebugFont { get; private set; }

        public static void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            // Create pixel texture
            Pixel = new Texture2D(graphicsDevice, 1, 1);
            Pixel.SetData(new[] { Microsoft.Xna.Framework.Color.White });

            // Load game textures
            PlayerTexture = content.Load<Texture2D>("SpriteSheettest");
            ChestTexture = content.Load<Texture2D>("Chest");

            // Load wall textures
            HorWallTexture = content.Load<Texture2D>("Hori_Wall");
            CornerWallTexture = content.Load<Texture2D>("Wall_Corner");
            VertWallTexture = content.Load<Texture2D>("Vert_Wall");
            DoorTexture = content.Load<Texture2D>("Door");

            // Load floor textures
            DungeonFloorTexture = content.Load<Texture2D>("Dundgeon_Floor");

            // Load font
            DebugFont = content.Load<SpriteFont>("DebugFont");
        }
    }
}
