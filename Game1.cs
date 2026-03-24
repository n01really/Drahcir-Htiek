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
        private Dundgeon _Map;
        private Chests _chest;
        private Camera_test _camera;
        private SpriteFont _debugFont;
        private Debug_Mode _debugMode;
        private Main_menu _mainMenu;
        private bool _inMenu = true;
        private bool _inMapMaker = false;
        private Map_Maker _mapMaker;

        private KeyboardState _previousKeyState;

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
            _Map = new Dundgeon();

            _player = new Player(54, 58);

            _chest = new Chests(260, 180);

            _camera = new Camera_test();

            _debugMode = new Debug_Mode();

            _mainMenu = new Main_menu(1920, 1080);

            _mapMaker = new Map_Maker();

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
            _Map.DundgeonFloorTexture = Content.Load<Texture2D>("Dundgeon_Floor");

            _debugFont = Content.Load<SpriteFont>("DebugFont");
            _debugMode.SetFont(_debugFont);
            _mainMenu.LoadContent(_debugFont, _pixel);
            _mapMaker.SetFont(_debugFont);
            _mapMaker.SetPixelTexture(_pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            if ((GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                (currentKeyState.IsKeyDown(Keys.Escape) && !_previousKeyState.IsKeyDown(Keys.Escape))))
            {
                // Escape tar dig tillbaka till menyn från map maker eller spelet
                if (_inMapMaker || !_inMenu)
                {
                    _inMenu = true;
                    _inMapMaker = false;
                    _mainMenu.Reset();
                }
                else
                {
                    Exit();
                }
            }

            _previousKeyState = currentKeyState;

            if (_inMenu)
            {
                _mainMenu.Update();

                if (_mainMenu.StartGameClicked)
                {
                    // Ladda kartan och starta spelet
                    Load_Map.LoadFromJson(_Map, "Dungeon_Floor_One");
                    
                    // Sätt spelarens position från kartan
                    var playerStartPos = Load_Map.GetPlayerStartPosition("Dungeon_Floor_One");
                    if (playerStartPos.HasValue)
                    {
                        _player.Bounds = new Rectangle(
                            (int)playerStartPos.Value.X,
                            (int)playerStartPos.Value.Y,
                            _player.Bounds.Width,
                            _player.Bounds.Height
                        );
                    }
                    
                    _inMenu = false;
                    _inMapMaker = false;
                    _mainMenu.Reset();
                }
                else if (_mainMenu.CreateMapClicked)
                {
                    _inMenu = false;
                    _inMapMaker = true;
                    _mainMenu.Reset();
                }
                else if (_mainMenu.QuitGameClicked)
                {
                    Exit();
                }
            }
            else if (_inMapMaker)
            {
                // Map Maker mode - låt Map_Maker hantera alla inputs inklusive zoom
                _mapMaker.Update(GraphicsDevice.Viewport);
                
                // Låt kameran röra sig fritt i map maker (t.x. med piltangenterna)
                var kstate = Keyboard.GetState();
                int cameraSpeed = 5;
                
                if (kstate.IsKeyDown(Keys.Left))
                    _camera.Position = new Vector2(_camera.Position.X - cameraSpeed, _camera.Position.Y);
                if (kstate.IsKeyDown(Keys.Right))
                    _camera.Position = new Vector2(_camera.Position.X + cameraSpeed, _camera.Position.Y);
                if (kstate.IsKeyDown(Keys.Up))
                    _camera.Position = new Vector2(_camera.Position.X, _camera.Position.Y - cameraSpeed);
                if (kstate.IsKeyDown(Keys.Down))
                    _camera.Position = new Vector2(_camera.Position.X, _camera.Position.Y + cameraSpeed);
            }
            else
            {
                // Normal game mode
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
            else if (_inMapMaker)
            {
                // Map Maker mode
                _mapMaker.Draw(_spriteBatch, GraphicsDevice, Mouse.GetState(),
                              _Map.HorWallTexture, _Map.VertWallTexture, 
                              _Map.CornerWallTexture, _chest.Texture, _Map.DundgeonFloorTexture);
            }
            else
            {
                // Normal game mode - rita spelet
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);
                
                // Rita kartan
                _Map.Draw(_spriteBatch);
                
                // Rita kistor
                if (_chest != null && _chest.Texture != null)
                {
                    _spriteBatch.Draw(_chest.Texture, _chest.Bounds, Color.White);
                }
                
                // Rita spelaren
                _spriteBatch.Draw(_pixel, _player.Bounds, Color.Blue);
                
                _spriteBatch.End();

                // Rita debug information (utan kamera transform)
                if (_debugMode != null)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    //_debugMode.DrawPlayerPosition(_spriteBatch, _player.Bounds, _camera.Position);
                    _spriteBatch.End();
                }
            }

            base.Draw(gameTime);
        }
    }
}
