using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Drahcir_Htiek.Test_map;
using Drahcir_Htiek.Camera;
using Drahcir_Htiek.Logic;
using Drahcir_Htiek.Menues;
using System.Collections.Generic;
using System.Linq;
using System;

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
        private SpriteFont _debugFont;
        private Debug_Mode _debugMode;
        private Main_menu _mainMenu;
        private bool _inMenu = true;

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

            _player = new Player(54, 58);

            _chest = new Test_Chest(260, 180);

            _camera = new Camera_test();

            _debugMode = new Debug_Mode();

            _mainMenu = new Main_menu(1920, 1080);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _debugMode.SetPixelTexture(_pixel);

            _chest.Texture = Content.Load<Texture2D>("Chest");
            _Map.HorWallTexture = Content.Load<Texture2D>("Hori_Wall");
            _Map.CornerWallTexture = Content.Load<Texture2D>("Wall_Corner");
            _Map.VertWallTexture = Content.Load<Texture2D>("Vert_Wall");
            _Map.DoorTexture = Content.Load<Texture2D>("Door");

            _debugFont = Content.Load<SpriteFont>("DebugFont");
            _debugMode.SetFont(_debugFont);
            _mainMenu.LoadContent(_debugFont, _pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_inMenu)
            {
                _mainMenu.Update();

                if (_mainMenu.StartGameClicked)
                {
                    _inMenu = false;
                    _mainMenu.Reset();
                }
                else if (_mainMenu.QuitGameClicked)
                {
                    Exit();
                }
            }
            else
            {
                // Uppdatera debug mode (lyssna efter §-tangenten)
                _debugMode.Update();

                var kstate = Keyboard.GetState();
                int speed = 1; 

                
                Rectangle NextBounds = _player.Bounds;

               
                if (kstate.IsKeyDown(Keys.A)) NextBounds.X -= speed;
                if (kstate.IsKeyDown(Keys.D)) NextBounds.X += speed;

               
                if (CollisionHandler.IsColliding(NextBounds, _player.Bounds, _Map) || CollisionHandler.IsColliding(NextBounds, _chest))
                {
                    NextBounds.X = _player.Bounds.X; // Krock! Vi återställer X-positionen.
                }

               
                if (kstate.IsKeyDown(Keys.W)) NextBounds.Y -= speed;
                if (kstate.IsKeyDown(Keys.S)) NextBounds.Y += speed;

               
                if (CollisionHandler.IsColliding(NextBounds, _player.Bounds, _Map) || CollisionHandler.IsColliding(NextBounds, _chest))
                {
                    NextBounds.Y = _player.Bounds.Y; 
                }

               
                _player.Bounds = NextBounds;

                // Uppdatera spelarens layer baserat på position
                UpdatePlayerLayer();
                
                _camera.Update();
                _camera.Follow(_player.Bounds, GraphicsDevice.Viewport);
            }

            base.Update(gameTime);
        }

        private void UpdatePlayerLayer()
        {
            // Standard layer för spelaren (högre än de flesta väggar)
            _player.Layer = 5;

            // Kolla varje horisontell vägg
            foreach (var wall in _Map.HorWalls)
            {
                // Kontrollera om spelaren är nära väggen horisontellt (X-axeln)
                bool isNearWallHorizontally = _player.Bounds.Right > wall.Bounds.Left && 
                                              _player.Bounds.Left < wall.Bounds.Right;
                
                // Kontrollera om spelaren är nära väggen vertikalt (Y-axeln)
                // Spelaren måste vara inom rimligt avstånd från väggen (t.ex. 60 pixlar)
                int distanceToWall = System.Math.Abs(_player.Bounds.Center.Y - wall.Bounds.Center.Y);
                bool isNearWallVertically = distanceToWall < 60;
                
                if (!isNearWallHorizontally || !isNearWallVertically)
                    continue; // Skippa väggar som spelaren inte är nära

                // Jämför spelarens fötter (Bottom) med vägggens botten (Bottom)
                // Lägg till en buffert på 16 pixlar så att spelaren måste vara klart ovanför
                // för att ritas bakom väggen
                if (_player.Bounds.Bottom < (wall.Bounds.Bottom - 16))
                {
                    // Sätt spelarens layer lägre så den ritas bakom väggen
                    _player.Layer = wall.Layer - 1;
                    break; // En vägg är tillräcklig för att bestämma layer
                }
            }

            // Kolla även corner walls (exakt samma logik)
            foreach (var wall in _Map.CornerWalls)
            {
                // Kontrollera om spelaren är nära väggen horisontellt (X-axeln)
                bool isNearWallHorizontally = _player.Bounds.Right > wall.Bounds.Left && 
                                              _player.Bounds.Left < wall.Bounds.Right;
                
                // Kontrollera om spelaren är nära väggen vertikalt (Y-axeln)
                int distanceToWall = System.Math.Abs(_player.Bounds.Center.Y - wall.Bounds.Center.Y);
                bool isNearWallVertically = distanceToWall < 60;
                
                if (!isNearWallHorizontally || !isNearWallVertically)
                    continue;

                if (_player.Bounds.Bottom < (wall.Bounds.Bottom - 16))
                {
                    _player.Layer = wall.Layer - 1;
                    break;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_inMenu)
            {
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _mainMenu.Draw(_spriteBatch);
                _spriteBatch.End();
            }
            else
            {
                _spriteBatch.Begin(
                    transformMatrix: _camera.Transform,
                    samplerState: SamplerState.PointClamp);

                // Skapa en lista med alla objekt och deras layers
                var drawableObjects = new List<(int layer, Action draw)>();

                // Lägg till alla väggar
                foreach (var wall in _Map.HorWalls)
                {
                    var w = wall; // Capture variable
                    drawableObjects.Add((w.Layer, () => {
                        w.Texture = _Map.HorWallTexture;
                        w.Draw(_spriteBatch);
                    }));
                }

                foreach (var wall in _Map.VertWalls)
                {
                    var w = wall;
                    drawableObjects.Add((w.Layer, () => {
                        w.Texture = _Map.VertWallTexture;
                        w.Draw(_spriteBatch);
                    }));
                }

                foreach (var wall in _Map.CornerWalls)
                {
                    var w = wall;
                    drawableObjects.Add((w.Layer, () => {
                        w.Texture = _Map.CornerWallTexture;
                        w.Draw(_spriteBatch);
                    }));
                }

                foreach (var door in _Map.Doors)
                {
                    var d = door;
                    drawableObjects.Add((d.Layer, () => {
                        d.Texture = _Map.DoorTexture;
                        d.Draw(_spriteBatch);
                    }));
                }

                // Lägg till chest och player
                drawableObjects.Add((3, () => _chest.Draw(_spriteBatch)));
                drawableObjects.Add((_player.Layer, () => _player.Draw(_spriteBatch, _pixel)));

                // Sortera efter layer och rita i rätt ordning
                foreach (var obj in drawableObjects.OrderBy(o => o.layer))
                {
                    obj.draw();
                }

                _spriteBatch.End();

                // Rita den nya debug mode positionen (endast när den är aktiverad)
                if (_debugMode.IsActive)
                {
                    _spriteBatch.Begin();
                    _debugMode.DrawPlayerPosition(_spriteBatch, _player.Bounds, GraphicsDevice);
                    _spriteBatch.End();
                }
            }

            base.Draw(gameTime);
        }
    }
}
