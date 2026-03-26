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
using Drahcir_Htiek.Entities;

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
        private Enemy_test _enemy;

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
            _Map = new Dundgeon();
            _player = new Player(54, 58);
            _enemy = new Enemy_test(200, 200);
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

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _debugMode.SetPixelTexture(_pixel);

            _player.Texture = Content.Load<Texture2D>("Female Skin1");

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
                    Load_Map.LoadFromJson(_Map, "Dungeon_Floor_One");

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
                _mapMaker.Update(GraphicsDevice.Viewport);

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
                _debugMode.Update();

                var kstate = Keyboard.GetState();
                int speed = 1;

                // Skapa nästa position för rendering
                Rectangle nextBounds = _player.Bounds;

                // Flytta X
                if (kstate.IsKeyDown(Keys.A)) nextBounds.X -= speed;
                if (kstate.IsKeyDown(Keys.D)) nextBounds.X += speed;

                // Skapa nästa kollisionsruta (16x32, centrerad)
                Rectangle nextCollisionBounds = new Rectangle(
                    nextBounds.X + (nextBounds.Width / 2) - 8,
                    nextBounds.Y + nextBounds.Height - 32,
                    16,
                    32
                );

                if (CollisionHandler.IsColliding(nextCollisionBounds, _player.CollisionBounds, _Map) ||
                    CollisionHandler.IsColliding(nextCollisionBounds, _chest))
                {
                    nextBounds.X = _player.Bounds.X;
                }

                // Flytta Y
                if (kstate.IsKeyDown(Keys.W)) nextBounds.Y -= speed;
                if (kstate.IsKeyDown(Keys.S)) nextBounds.Y += speed;

                // Uppdatera kollisionsruta för Y
                nextCollisionBounds = new Rectangle(
                    nextBounds.X + (nextBounds.Width / 2) - 8,
                    nextBounds.Y + nextBounds.Height - 32,
                    16,
                    32
                );

                if (CollisionHandler.IsColliding(nextCollisionBounds, _player.CollisionBounds, _Map) ||
                    CollisionHandler.IsColliding(nextCollisionBounds, _chest))
                {
                    nextBounds.Y = _player.Bounds.Y;
                }

                _player.Bounds = nextBounds;

                UpdatePlayerLayer();

                _camera.Update();
                _camera.Follow(_player.Bounds, GraphicsDevice.Viewport);
            }

            base.Update(gameTime);
        }

        private void UpdatePlayerLayer()
        {
            _player.Layer = 5;

            foreach (var wall in _Map.HorWalls)
            {
                bool isNearWallHorizontally = _player.Bounds.Right > wall.Bounds.Left &&
                                              _player.Bounds.Left < wall.Bounds.Right;

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

            foreach (var wall in _Map.CornerWalls)
            {
                bool isNearWallHorizontally = _player.Bounds.Right > wall.Bounds.Left &&
                                              _player.Bounds.Left < wall.Bounds.Right;

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
                _mapMaker.Draw(_spriteBatch, GraphicsDevice, Mouse.GetState(),
                              _Map.HorWallTexture, _Map.VertWallTexture,
                              _Map.CornerWallTexture, _chest.Texture, _Map.DundgeonFloorTexture);
            }
            else
            {
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.Transform);

                _Map.Draw(_spriteBatch);

                if (_chest != null && _chest.Texture != null)
                {
                    _spriteBatch.Draw(_chest.Texture, _chest.Bounds, Color.White);
                }

                _player.Draw(_spriteBatch, _pixel);

                _spriteBatch.End();

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
