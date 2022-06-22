using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;

namespace _2048
{
    public class _2048 : Game
    {
        public static GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        static readonly Random random = new Random();

        // window
        Vector2 windowOffset = Vector2.Zero;
        Vector2 windowspeed = Vector2.Zero;
        Point defaultWindowPos;
        const float movementamount = 1f;
        float movementfactor;

        // grid
        static Tile[,] grid = new Tile[4,4];
        static readonly int[] possibleSpawns = { 2, 4 };
        static int currentHighest;

        // assets
        SpriteFont font;
        Texture2D t0;
        Texture2D t2;
        Texture2D t4;
        Texture2D t8;
        Texture2D t16;
        Texture2D t32;
        Texture2D t64;
        Texture2D t128;
        Texture2D t256;
        Texture2D t512;
        Texture2D t1024;
        Texture2D t2048;
        Texture2D losemenu;
        Texture2D winmenu;
        Texture2D loadsave;
        Texture2D startmenu;

        // menu
        readonly Vector2 menupos = new Vector2(-512, 0);
        static double menuoffset;
        static bool menuleave = false;
        static float multiplier = 1;

        // input
        KeyboardState kb;
        Keys[] keys;
        Keys[] lastkeys;
        Keys[] changedkeys;
        MouseState ms;
        MouseState lastms;

        //gameplay
        static bool won = false;
        static bool lost = false;
        static bool lastlost = false;
        static bool lastwon = false;
        public static bool shouldresetgrid = false;
        static bool showautosave = false;
        static int notificationtime = 0;
        static float notifoffset;
        static bool showstartmenu = true;
        static bool hasstarted = false;

        // debug
        static bool debug = false;
        static bool dosaving = true;
        static double framerate;
        static bool doTileSpawn = true;
        static bool slowAnimate = false;
        static bool doWindowMove = true;

        public _2048()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // lock to 60fps so animations are consistent across different hardware
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1d/60d);
        }

        protected override void Initialize()
        {
            // scale to screen
            // note that this does NOT increase texture resolution
            if (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height >= 2160)
            {
                // 4k or more
                _graphics.PreferredBackBufferWidth = 1536;
                _graphics.PreferredBackBufferHeight = 1536;
                multiplier=3f;
            }
            else if (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height >= 1440)
            {
                // 1440p
                _graphics.PreferredBackBufferWidth = 1024;
                _graphics.PreferredBackBufferHeight = 1024;
                multiplier=2f;
            }
            else if (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height >= 1080)
            {
                // 1080p
                _graphics.PreferredBackBufferWidth = 896;
                _graphics.PreferredBackBufferHeight = 896;
                multiplier=1.75f;
            }
            else
            {
                // less than 1080p
                _graphics.PreferredBackBufferWidth = 512;
                _graphics.PreferredBackBufferHeight = 512;
            }
            _graphics.ApplyChanges();

            defaultWindowPos = Window.Position;

            // initialise grid
            grid = GetEmptyGrid();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // font
            font = Content.Load<SpriteFont>("font");

            // textures
            t0 = Content.Load<Texture2D>("0");
            t2 = Content.Load<Texture2D>("2");
            t4 = Content.Load<Texture2D>("4");
            t8 = Content.Load<Texture2D>("8");
            t16 = Content.Load<Texture2D>("16");
            t32 = Content.Load<Texture2D>("32");
            t64 = Content.Load<Texture2D>("64");
            t128 = Content.Load<Texture2D>("128");
            t256 = Content.Load<Texture2D>("256");
            t512 = Content.Load<Texture2D>("512");
            t1024 = Content.Load<Texture2D>("1024");
            t2048 = Content.Load<Texture2D>("2048");

            // menus
            losemenu = Content.Load<Texture2D>("menulost");
            winmenu = Content.Load<Texture2D>("menuwon");
            startmenu = Content.Load<Texture2D>("menustart");

            // notifications
            loadsave = Content.Load<Texture2D>("loadsave");
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            // if the game hasn't been won or lost, save the grid
            if (!won && !lost) Save();

            // dispose everything
            t0.Dispose();
            t2.Dispose();
            t4.Dispose();
            t8.Dispose();
            t16.Dispose();
            t32.Dispose();
            t64.Dispose();
            t128.Dispose();
            t256.Dispose();
            t512.Dispose();
            t1024.Dispose();
            t2048.Dispose();
            losemenu.Dispose();
            winmenu.Dispose();
            loadsave.Dispose();
            startmenu.Dispose();
            _spriteBatch.Dispose();
            _graphics.Dispose();

            base.OnExiting(sender, args);
        }

        protected override void Update(GameTime gameTime)
        {
            // window
            windowOffset += windowspeed;
            movementfactor = (GetTileSum(grid)/64);
            if (windowspeed.X > 0) windowspeed.X--;
            if (windowspeed.X < 0) windowspeed.X++;
            if (windowspeed.Y > 0) windowspeed.Y--;
            if (windowspeed.Y < 0) windowspeed.Y++;
            if (windowspeed.X < 1 && windowspeed.X > -1 && windowspeed.Y > -1 && windowspeed.Y < 1) windowspeed = Vector2.Zero;
            if (windowspeed == Vector2.Zero)
            {
                float easefactor = 0.9375f;
                if (windowOffset.X > 1) windowOffset.X *= easefactor;
                if (windowOffset.X < -1) windowOffset.X *= easefactor;
                if (windowOffset.Y > 1) windowOffset.Y *= easefactor;
                if (windowOffset.Y < -1) windowOffset.Y *= easefactor;
                if (windowOffset.X < 1 && windowOffset.X > -1) windowOffset.X = 0;
                if (windowOffset.Y < 1 && windowOffset.Y > -1) windowOffset.Y = 0;
            }
            float maxspeed = 80f;
            if (windowOffset.X > maxspeed || windowOffset.X < -maxspeed ||
            windowOffset.Y > maxspeed ||windowOffset.Y < -maxspeed) windowspeed = Vector2.Zero;

            // move the window
            if (doWindowMove) Window.Position = defaultWindowPos + new Point((int)windowOffset.X, (int)windowOffset.Y);

            // debug mode
            if (debug)
            {
                lastms = ms;
                ms = Mouse.GetState();
                if (ms.LeftButton==ButtonState.Pressed&&lastms.LeftButton==ButtonState.Released && !won && !lost)
                {
                    grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value *= 2;
                    if (grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value == 0) grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value = 2;
                }
                else if (ms.RightButton==ButtonState.Pressed&&lastms.RightButton==ButtonState.Released && !won && !lost)
                {
                    grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value /= 2;
                    if (grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value == 1) grid[(int)((ms.Y/multiplier) / 128),(int)((ms.X/multiplier) / 128)].Value = 0;
                }
            }

            if (shouldresetgrid)
            {
                grid = GetEmptyGrid();
                shouldresetgrid = false;
                if (doTileSpawn) {SpawnRandom(); SpawnRandom();} // spawn 2 tiles
                currentHighest = 0;
            }

            if (!menuleave)
            {
                lastwon = won;
                lastlost = lost;
            }

            if (OutOfMoves()&&!lastlost&&CountNotZero(grid)>0)
            {
                lost = true;
                menuoffset = 8192;
            }
            else if ((GetHightestTile() == 2048)&&!lastwon)
            {
                won = true;
                menuoffset = 8192;
            }

            // menu slide animation
            if (menuoffset>1 && !menuleave) menuoffset*=0.825d;
            else if (menuleave && menuoffset > -1024) menuoffset*=1.21212d;
            else 
            {
                menuoffset=0;
                menuleave=false;
                if (hasstarted && showstartmenu) 
                {
                    showstartmenu = false;
                    
                    // if the save file exists, show autosave notification
                    if (File.Exists("info.2048"))
                    {
                        if (HighestInSave() > 8)
                        {
                            showautosave = true;
                            notificationtime = 300;
                            notifoffset = -512;
                        }
                    }
                }
            }
            
            if (notificationtime == 0 && !showstartmenu)
            {
                showautosave = false;
                notifoffset = 0f;
                File.Delete("info.2048");
            }
            else 
            {
                if (notifoffset < 0 && notificationtime > 200)
                {
                    notifoffset*=0.9f;
                }
                if (notifoffset > -1)
                {
                    notifoffset=0;
                }
                if (notificationtime < 60 && notificationtime > 0)
                {
                    notifoffset*=1.1f;
                }
                if (notificationtime==60)
                {
                    notifoffset = -1.1f;
                }
            }

            if (notificationtime > 0) notificationtime--;

            // update tile animations
            foreach (var x in grid)
            {
                if (x.AnimationTimer > 1 || x.Fade > 1 || x.doFadeOut) x.UpdateTile(slowAnimate);
                else 
                {
                    x.AnimationTimer = 0;
                    x.AnimationDirection = "none";
                    x.OldValue = 0;
                }
            }

            // make an array of all keypress changes since the last frame
            kb = Keyboard.GetState();
            lastkeys = keys;
            keys = kb.GetPressedKeys();
            if (lastkeys == null) changedkeys = keys;
            else changedkeys = keys.Except(lastkeys).ToArray();

            // iterate through all keypresses as a switch statement,
            // this is more efficient than using many 'if' statements
            foreach (var x in changedkeys)
            {
                var lastgrid = CopyGrid(grid);
                switch (x)
                {
                    case Keys.Escape:
                        Exit();
                        break;
                    case Keys.Left:
                        if (!won) grid = MoveLeft(grid);
                        windowspeed.X -= movementfactor;
                        break;
                    case Keys.Right:
                        if (!won) grid = MoveRight(grid);
                        windowspeed.X += movementfactor;
                        break;
                    case Keys.Up:
                        if (!won) grid = MoveUp(grid);
                        windowspeed.Y -= movementfactor;
                        break;
                    case Keys.Down:
                        if (!won) grid = MoveDown(grid);
                        windowspeed.Y += movementfactor;
                        break;
                    case Keys.F3:
                        debug = !debug;
                        dosaving = false;
                        if (debug) doTileSpawn = false;
                        else doTileSpawn = true;
                        slowAnimate = false;
                        Reset();
                        if (won || lost)
                        {
                            menuleave=true;
                            menuoffset = -1;
                        }
                        lost = false;
                        won = false;
                        break;
                    case Keys.Space:
                        Reset();
                        if (won || lost || showstartmenu)
                        {
                            menuleave=true;
                            menuoffset = -1;
                        }
                        lost = false;
                        won = false;
                        hasstarted = true;
                        if (showautosave)
                        {
                            if (notificationtime > 60) notificationtime = 60;
                            grid = Load();
                        }
                        break;
                    case Keys.F2:
                        if (debug) doTileSpawn = !doTileSpawn;
                        break;
                    case Keys.F4:
                        if (debug) slowAnimate = !slowAnimate;
                        break;
                    case Keys.F5:
                        doWindowMove = !doWindowMove;
                        break;
                }
                // spawn a random tile if there was a change
                if (!IsEqual(grid, lastgrid)&&doTileSpawn) SpawnRandom();
                else windowspeed *= 0.25f;

                if (GetHightestTile() > currentHighest)
                {
                    currentHighest = GetHightestTile();
                }
            }

            if (debug) framerate = Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            /*
            iterate through the whole grid and compare values in a switch statement,
            load the texture depending on what value the square has
            */

            // render at 512x512 resolution first
            SpriteBatch targetbatch = new SpriteBatch(GraphicsDevice);
            RenderTarget2D rendertarget = new RenderTarget2D(GraphicsDevice, 512, 512);
            GraphicsDevice.SetRenderTarget(rendertarget);

            GraphicsDevice.Clear(Color.DimGray);
            _spriteBatch.Begin(samplerState:SamplerState.PointClamp);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    // draw background first
                    _spriteBatch.Draw(t0, (new Vector2(x * 128, y * 128)), Color.White);

                    // then draw the tiles with values that aren't animated
                    if (grid[y, x].AnimationTimer == 0)
                    {
                        switch (grid[y,x].Value)
                        {
                            case 2:
                                _spriteBatch.Draw(t2, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 4:
                                _spriteBatch.Draw(t4, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 8:
                                _spriteBatch.Draw(t8, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 16:
                                _spriteBatch.Draw(t16, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 32:
                                _spriteBatch.Draw(t32, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 64:
                                _spriteBatch.Draw(t64, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 128:
                                _spriteBatch.Draw(t128, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 256:
                                _spriteBatch.Draw(t256, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 512:
                                _spriteBatch.Draw(t512, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 1024:
                                _spriteBatch.Draw(t1024, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                            case 2048:
                                _spriteBatch.Draw(t2048, new Vector2(x * 128, y * 128), Color.White*grid[y, x].GetAlpha());
                                break;
                        }
                    }
                }
            }

            // draw the animated ones last so that they aren't obstrucing others
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (grid[y, x].AnimationTimer > 0)
                    {
                        // render old tile first
                        switch (grid[y,x].OldValue)
                        {
                            case 2:
                                _spriteBatch.Draw(t2, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 4:
                                _spriteBatch.Draw(t4, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 8:
                                _spriteBatch.Draw(t8, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 16:
                                _spriteBatch.Draw(t16, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 32:
                                _spriteBatch.Draw(t32, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 64:
                                _spriteBatch.Draw(t64, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 128:
                                _spriteBatch.Draw(t128, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 256:
                                _spriteBatch.Draw(t256, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 512:
                                _spriteBatch.Draw(t512, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 1024:
                                _spriteBatch.Draw(t1024, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                            case 2048:
                                _spriteBatch.Draw(t2048, new Vector2(x * 128, y * 128)+grid[y, x].OldOffset(), Color.White);
                                break;
                        }

                        // render new tile sliding into place, with old texture fading away
                        switch (grid[y,x].Value)
                        {
                            case 2:
                                _spriteBatch.Draw(t2, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                break;
                            case 4:
                                _spriteBatch.Draw(t4, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t2, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 8:
                                _spriteBatch.Draw(t8, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t4, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 16:
                                _spriteBatch.Draw(t16, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t8, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 32:
                                _spriteBatch.Draw(t32, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t16, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 64:
                                _spriteBatch.Draw(t64, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t32, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 128:
                                _spriteBatch.Draw(t128, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t64, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 256:
                                _spriteBatch.Draw(t256, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t128, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 512:
                                _spriteBatch.Draw(t512, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t256, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 1024:
                                _spriteBatch.Draw(t1024, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t512, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                            case 2048:
                                _spriteBatch.Draw(t2048, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White);
                                _spriteBatch.Draw(t1024, (new Vector2(x * 128, y * 128)+grid[y, x].Offset()), Color.White*grid[y, x].GetOldAlpha());
                                break;
                        }
                    }
                }
            }

            if (lost || menuleave && lastlost)
            {
                _spriteBatch.Draw(losemenu, menupos + new Vector2((float)menuoffset, 0), Color.White * 0.875f);
            }
            else if (won || menuleave && lastwon)
            {
                _spriteBatch.Draw(winmenu, menupos + new Vector2((float)menuoffset, 0), Color.White * 0.875f);
            }
            else if (showautosave)
            {
                _spriteBatch.Draw(loadsave, new Vector2(0, notifoffset), Color.White * 0.75f);
            }
            else if (showstartmenu)
            {
                _spriteBatch.Draw(startmenu, menupos + new Vector2((float)menuoffset, 0), Color.White*0.875f);
            }

            // debug mode overlay
            if (debug)
            {
                _spriteBatch.DrawString(font, "debug (F3)\nSAVING IS DISABLED\nLMB/RMB = increment/decrement\n\nVALUES\n"+
                "framesPerSecond: "+framerate+"\nnonZeroCount: "+CountNotZero(grid)+
                "\nmaxTileValue: "+GetHightestTile()+"\nisFull: "+IsFull()+"\noutOfMoves: "+OutOfMoves()+
                "\ntileSum: "+GetTileSum(grid).ToString()+"\ndoTileSpawn (F2): "+doTileSpawn.ToString()+"\nslowAnimate (F4): "+slowAnimate.ToString(), Vector2.Zero, Color.Black);
            }
            _spriteBatch.End();

            // scale up to window size
            _graphics.GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin();
            _spriteBatch.Draw(rendertarget, new Rectangle(0, 0, (int)(512*multiplier), (int)(512*multiplier)), Color.White);
            _spriteBatch.End();

            // dispose of render target and targetbatch
            rendertarget.Dispose();
            targetbatch.Dispose();
            // this prevents memory leaks!


            base.Draw(gameTime);
        }

        // sets all the values in the grid to 0
        static Tile[,] GetEmptyGrid()
        {
            var newgrid = new Tile[4, 4];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    newgrid[x, y] = new Tile(0);
                }
            }
            return newgrid;
        }

        // returns the number on non-zero cells on the grid
        static int CountNotZero(Tile[,] grid)
        {
            int count = 0;
            foreach (var x in grid) if (x.Value > 0) count++;
            return count;
        }

        // returns true when the grid is full
        static bool IsFull() { return CountNotZero(grid) == 16; }

        // spawns either a 2 or 4 cell at a random location,
        // repeat until a valid location is chosen
        static void SpawnRandom()
        {
            Tile[,] lastgrid = CopyGrid(grid);
            do
            {
                int xpos = random.Next(0, 4);
                int ypos = random.Next(0, 4);
                if (grid[ypos, xpos].Value == 0)
                {
                    if (random.Next(0, 9) != 0)
                    {
                        grid[ypos, xpos] = new Tile(2);
                    }
                    else
                    {
                        grid[ypos, xpos] = new Tile(4);
                    }
                }
            } while (IsEqual(grid, lastgrid));
        }

        // move everything as far as possible,
        // repeat until you can't move them anymore
        static Tile[,] MoveLeft(Tile[,] grid)
        {
            Tile[,] lastgrid;
            
            // set all AnimationTimer values to 0
            foreach (var x in grid)
            {
                x.AnimationTimer = 0;
                x.OldTimer = 0;
            }

            int numberofmoves = 0;

            do
            {
                lastgrid = CopyGrid(grid);
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 1; x < 4; x++)
                    {
                        if (grid[y, x].Value > 0 && grid[y, x - 1].Value == 0)
                        {
                            grid[y, x - 1].Value = grid[y, x].Value;
                            grid[y, x].Value = 0;
                            grid[y, x - 1].AnimationDirection = "left";
                            grid[y, x - 1].AnimationTimer += (128 + grid[y, x].AnimationTimer);
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                        else if (grid[y, x].Value > 0 && grid[y, x].Value == grid[y, x - 1].Value && !grid[y, x].HasMerged && !grid[y, x - 1].HasMerged)
                        {
                            grid[y, x].Value = 0;
                            grid[y, x - 1].Value *= 2;
                            grid[y, x - 1].HasMerged = true;
                            grid[y, x - 1].AnimationDirection = "left";
                            grid[y, x - 1].AnimationTimer += (128);
                            grid[y, x - 1].OldValue = grid[y, x - 1].Value / 2;
                            grid[y, x - 1].OldTimer += grid[y, x].AnimationTimer;
                            if (grid[y, x - 1].AnimationTimer != 128+grid[y, x - 1].OldTimer) { grid[y, x - 1].OldTimer -= 128; grid[y, x - 1].AnimationTimer += 128; }                            
                            if (numberofmoves == 2 && grid[y, x].AnimationTimer == 256 && grid[y, x - 1].AnimationTimer == 256) { grid[y, x - 1].OldTimer -= 128; grid[y, x - 1].AnimationTimer += 128; }
                            grid[y, x - 1].FadeOutOld = 15;
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                    }
                }
                numberofmoves++;
            } while (!IsEqual(grid, lastgrid));
            ResetMergeState();
            return grid;
        }
        static Tile[,] MoveRight(Tile[,] grid)
        {
            Tile[,] lastgrid;
            
            // set all AnimationTimer values to 0
            foreach (var x in grid)
            {
                x.AnimationTimer = 0;
                x.OldTimer = 0;
            }

            int numberofmoves = 0;

            do
            {
                lastgrid = CopyGrid(grid);
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 2; x >= 0; x--)
                    {
                        if (grid[y, x].Value > 0 && grid[y, x + 1].Value == 0)
                        {
                            grid[y, x + 1].Value = grid[y, x].Value;
                            grid[y, x].Value = 0;
                            grid[y, x + 1].AnimationDirection = "right";
                            grid[y, x + 1].AnimationTimer += (128 + grid[y, x].AnimationTimer);
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                        else if (grid[y, x].Value > 0 && grid[y, x].Value == grid[y, x + 1].Value && !grid[y, x].HasMerged && !grid[y, x + 1].HasMerged)
                        {
                            grid[y, x].Value = 0;
                            grid[y, x + 1].Value *= 2;
                            grid[y, x + 1].HasMerged = true;
                            grid[y, x + 1].AnimationDirection = "right";
                            grid[y, x + 1].AnimationTimer += (128);
                            grid[y, x + 1].OldValue = grid[y, x + 1].Value / 2;
                            grid[y, x + 1].OldTimer += grid[y, x].AnimationTimer;
                            if (grid[y, x + 1].AnimationTimer != 128+grid[y, x + 1].OldTimer) { grid[y, x + 1].OldTimer -= 128; grid[y, x + 1].AnimationTimer += 128; }                            
                            if (numberofmoves == 2 && grid[y, x].AnimationTimer == 256 && grid[y, x + 1].AnimationTimer == 256) { grid[y, x + 1].OldTimer -= 128; grid[y, x + 1].AnimationTimer += 128; }
                            grid[y, x + 1].FadeOutOld = 15;
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                    }
                }
                numberofmoves++;
            } while (!IsEqual(grid, lastgrid));
            ResetMergeState();
            return grid;
        }
        static Tile[,] MoveUp(Tile[,] grid)
        {
            Tile[,] lastgrid;
            
            // set all AnimationTimer values to 0
            foreach (var x in grid)
            {
                x.AnimationTimer = 0;
                x.OldTimer = 0;
            }

            int numberofmoves = 0;

            do
            {
                lastgrid = CopyGrid(grid);
                for (int y = 1; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        if (grid[y, x].Value > 0 && grid[y - 1, x].Value == 0)
                        {
                            grid[y - 1, x].Value = grid[y, x].Value;
                            grid[y, x].Value = 0;
                            grid[y - 1, x].AnimationDirection = "up";
                            grid[y - 1, x].AnimationTimer += (128 + grid[y, x].AnimationTimer);
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                        else if (grid[y, x].Value > 0 && grid[y, x].Value == grid[y - 1, x].Value && !grid[y, x].HasMerged && !grid[y - 1, x].HasMerged)
                        {
                            grid[y, x].Value = 0;
                            grid[y - 1, x].Value *= 2;
                            grid[y - 1, x].HasMerged = true;
                            grid[y - 1, x].AnimationDirection = "up";
                            grid[y - 1, x].AnimationTimer += (128);
                            grid[y - 1, x].OldValue = grid[y - 1, x].Value / 2;
                            grid[y - 1, x].OldTimer += grid[y, x].AnimationTimer;
                            if (grid[y - 1, x].AnimationTimer != 128+grid[y - 1, x].OldTimer) { grid[y - 1, x].OldTimer -= 128; grid[y - 1, x].AnimationTimer += 128; }                            
                            if (numberofmoves == 2 && grid[y, x].AnimationTimer == 256 && grid[y - 1, x].AnimationTimer == 256) { grid[y - 1, x].OldTimer -= 128; grid[y - 1, x].AnimationTimer += 128; }
                            grid[y - 1, x].FadeOutOld = 15;
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                    }
                }
                numberofmoves++;
            } while (!IsEqual(grid, lastgrid));
            ResetMergeState();
            return grid;
        }
        static Tile[,] MoveDown(Tile[,] grid)
        {
            Tile[,] lastgrid;
            
            // set all AnimationTimer values to 0
            foreach (var x in grid)
            {
                x.AnimationTimer = 0;
                x.OldTimer = 0;
            }

            int numberofmoves = 0;

            do
            {
                lastgrid = CopyGrid(grid);
                for (int y = 2; y >= 0; y--)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        if (grid[y, x].Value > 0 && grid[y + 1, x].Value == 0)
                        {
                            grid[y + 1, x].Value = grid[y, x].Value;
                            grid[y, x].Value = 0;
                            grid[y + 1, x].AnimationDirection = "down";
                            grid[y + 1, x].AnimationTimer += (128 + grid[y, x].AnimationTimer);
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                        else if (grid[y, x].Value > 0 && grid[y, x].Value == grid[y + 1, x].Value && !grid[y, x].HasMerged && !grid[y + 1, x].HasMerged)
                        {
                            grid[y, x].Value = 0;
                            grid[y + 1, x].Value *= 2;
                            grid[y + 1, x].HasMerged = true;
                            grid[y + 1, x].AnimationDirection = "down";
                            grid[y + 1, x].AnimationTimer += (128);
                            grid[y + 1, x].OldValue = grid[y + 1, x].Value / 2;
                            grid[y + 1, x].OldTimer += grid[y, x].AnimationTimer;
                            if (grid[y + 1, x].AnimationTimer != 128+grid[y + 1, x].OldTimer) { grid[y + 1, x].OldTimer -= 128; grid[y + 1, x].AnimationTimer += 128; }                            
                            if (numberofmoves == 2 && grid[y, x].AnimationTimer == 256 && grid[y + 1, x].AnimationTimer == 256) { grid[y + 1, x].OldTimer -= 128; grid[y + 1, x].AnimationTimer += 128; }
                            grid[y + 1, x].FadeOutOld = 15;
                            grid[y, x].AnimationTimer = 0;
                            grid[y, x].AnimationDirection = "none";
                        }
                    }
                }
                numberofmoves++;
            } while (!IsEqual(grid, lastgrid));
            ResetMergeState();
            return grid;
        }

        static int GetTileSum(Tile[,] grid)
        {
            int sum = 0;
            foreach (var x in grid) sum += x.Value;
            return sum;
        }
        
        // compare 2 tile arrays and return true if they are equal
        static bool IsEqual(Tile[,] a, Tile[,] b)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (a[i, j].Value != b[i, j].Value) return false;
                }
            }
            return true;
        }

        // copies an array to another array
        static Tile[,] CopyGrid(Tile[,] oldgrid)
        {
            var newgrid = GetEmptyGrid(); ;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    newgrid[i,j] = new Tile(oldgrid[i,j].Value);
                }
            }
            return newgrid;
        }

        // after merging, tiles cannot be merged until the next move
        // this tells all tiles that they can be merged
        static void ResetMergeState()
        {
            foreach (var x in grid)
            {
                x.HasMerged = false;
            }
        }

        // returns the largest value tile in the grid
        static int GetHightestTile()
        {
            int largest = 0;
            foreach (var x in grid)
            {
                if (x.Value > largest) largest = x.Value;
            }
            return largest;
        }

        // returns true if there are no possible moves
        static bool OutOfMoves()
        {
            Tile[,] lastgrid = CopyGrid(grid);
            Tile[,] testgrid = CopyGrid(grid);
            if (!IsEqual(MoveUp(testgrid), lastgrid)) return false;
            if (!IsEqual(MoveDown(testgrid), lastgrid)) return false;
            if (!IsEqual(MoveLeft(testgrid), lastgrid)) return false;
            if (!IsEqual(MoveRight(testgrid), lastgrid)) return false;
            return true;
        }

        static void Reset()
        {
            foreach (var x in grid)
            {
                x.doFadeOut = true;
                
            }
        }

        // save the game
        static void Save()
        {
            if (!dosaving || GetHightestTile() <= 8) return;
            string savename = "info.2048";
            var stream = File.OpenWrite(savename);
            var bw = new BinaryWriter(stream);
            foreach (var x in grid)
            {
                bw.Write((UInt16)x.Value);
            }
            bw.Close();
            stream.Close();

            // make the file hidden
            File.SetAttributes(savename, FileAttributes.Hidden); 
        }

        // load the save
        static Tile[,] Load()
        {
            var newgrid = GetEmptyGrid();
            string savename = "info.2048";
            var stream = File.Open(savename, FileMode.Open);
            var br = new BinaryReader(stream);
            for (int i = 0; i < 16; i++)
            {
                newgrid[i / 4, i % 4].Value = br.ReadUInt16();
            }
            br.Close();
            stream.Close();
            File.Delete(savename);
            return newgrid;
        }

        // open the file and check what the highest value is
        static int HighestInSave()
        {
            string savename = "info.2048";
            var stream = File.Open(savename, FileMode.Open);
            var br = new BinaryReader(stream);
            int highest = 0;
            for (int i = 0; i < 16; i++)
            {
                int number = br.ReadUInt16();
                if (number > highest) highest = number;
            }
            br.Close();
            stream.Close();
            return highest;
        }
    }
}
