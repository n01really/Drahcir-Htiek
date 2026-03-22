using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Drahcir_Htiek.Test_map;
using Drahcir_Htiek.Camera;

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
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            var kstate = Keyboard.GetState();
            int speed = 2;

            if (kstate.IsKeyDown(Keys.W))
                _player.Bounds.Y -= speed;
            if (kstate.IsKeyDown(Keys.S))
                _player.Bounds.Y += speed;
            if (kstate.IsKeyDown(Keys.A))
                _player.Bounds.X -= speed;
            if (kstate.IsKeyDown(Keys.D))
                _player.Bounds.X += speed;

            _camera.Follwo(_player.Bounds, GraphicsDevice.Viewport);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            _Map.Draw(_spriteBatch, _pixel);
            _chest.Draw(_spriteBatch, _pixel);
            _player.Draw(_spriteBatch, _pixel);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
