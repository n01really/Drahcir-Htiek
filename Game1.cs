using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Drahcir_Htiek.Test_map;
using Drahcir_Htiek.Camera;
using Drahcir_Htiek.Logic;

namespace Drahcir_Htiek
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _pixel;
        private Player _player;
        private Test_Map _Map;
        private Test_Chest _chest;
        private Camera_test _camera;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _Map = new Test_Map();

            _player = new Player(100, 100);

            _chest = new Test_Chest(260, 180);

            _camera = new Camera_test();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _chest.Texture = Content.Load<Texture2D>("Chest");
            _Map.HorWallTexture = Content.Load<Texture2D>("Hori_Wall");
            _Map.VertWallTexture = Content.Load<Texture2D>("Vert_Wall");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var kstate = Keyboard.GetState();
            int speed = 1; 

            
            Rectangle NextBounds = _player.Bounds;

           
            if (kstate.IsKeyDown(Keys.A)) NextBounds.X -= speed;
            if (kstate.IsKeyDown(Keys.D)) NextBounds.X += speed;

           
            if (CollisionHandler.IsColliding(NextBounds, _Map) || CollisionHandler.IsColliding(NextBounds, _chest))
            {
                NextBounds.X = _player.Bounds.X; // Krock! Vi återställer X-positionen.
            }

            
            if (kstate.IsKeyDown(Keys.W)) NextBounds.Y -= speed;
            if (kstate.IsKeyDown(Keys.S)) NextBounds.Y += speed;

            
            if (CollisionHandler.IsColliding(NextBounds, _Map) || CollisionHandler.IsColliding(NextBounds, _chest))
            {
                NextBounds.Y = _player.Bounds.Y; 
            }

            
            _player.Bounds = NextBounds;

            
            _camera.Follow(_player.Bounds, GraphicsDevice.Viewport);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(
                transformMatrix: _camera.Transform,
                samplerState: SamplerState.PointClamp);

            _Map.Draw(_spriteBatch);
            _chest.Draw(_spriteBatch);
            _player.Draw(_spriteBatch, _pixel);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
