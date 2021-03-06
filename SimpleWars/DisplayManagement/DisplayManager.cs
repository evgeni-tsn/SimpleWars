﻿namespace SimpleWars.DisplayManagement
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    
    using SimpleWars.DisplayManagement.Displays;
    using SimpleWars.DisplayManagement.Interfaces;
    using SimpleWars.GUI.Interfaces;

    /// <summary>
    /// The display manager.
    /// </summary>
    public class DisplayManager : IDisplayManager
    {
        /// <summary>
        /// The instance.
        /// </summary>
        public static DisplayManager Instance => instance ?? (instance = new DisplayManager());

        /// <summary>
        /// The instance.
        /// </summary>
        private static DisplayManager instance;

        /// <summary>
        /// The graphics manager.
        /// </summary>
        private GraphicsDeviceManager graphicsManager;
    
        private DisplayManager()
        {
            this.Dimensions = new Vector2(1280, 720);

            this.CurrentDisplay = new InitialDisplay();
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets or sets the graphics manager.
        /// </summary>
        public GraphicsDeviceManager GraphicsManager
        {
            get
            {
                return this.graphicsManager;
            }

            set
            {
                this.graphicsManager = value;
                this.GraphicsDevice = this.GraphicsManager.GraphicsDevice;
            }
        }

        /// <summary>
        /// Gets or sets the dimensions.
        /// </summary>
        public Vector2 Dimensions { get; private set; }


        /// <summary>
        /// Gets the current display.
        /// </summary>
        public IDisplay CurrentDisplay { get; private set; }

        public IGui ResponseText { get; set; }

        /// <summary>
        /// The load content.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        public void LoadContent(ContentManager content)
        {
            this.CurrentDisplay.LoadContent();
        }

        /// <summary>
        /// The unload content.
        /// </summary>
        public void UnloadContent()
        {
            this.CurrentDisplay.UnloadContent();
        }

        /// <summary>
        /// Changes the display
        /// </summary>
        /// <param name="display">
        /// The display.
        /// </param>
        /// <param name="context">
        /// The game context
        /// </param>
        public void ChangeDisplay(IDisplay display)
        {
            display.LoadContent();
            this.CurrentDisplay.UnloadContent();
            this.CurrentDisplay = display;

            GC.Collect();
        }

        /// <summary>
        /// Changes the game screen dimensions
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        public void ChangeDimensions(int width, int height)
        {
            this.Dimensions = new Vector2(width, height);
            this.GraphicsManager.PreferredBackBufferWidth = width;
            this.GraphicsManager.PreferredBackBufferHeight = height;
            this.GraphicsManager.PreferMultiSampling = true;
            
            this.GraphicsManager.ApplyChanges();
        }

        /// <summary>
        /// The update.
        /// </summary>
        /// <param name="gameTime">
        /// The game time.
        /// </param>
        public void Update(GameTime gameTime)
        {
            this.CurrentDisplay.Update(gameTime);
        }

        /// <summary>
        /// The draw.
        /// </summary>
        /// <param name="spriteBatch">
        /// The sprite batch.
        /// </param>
        public void Draw(SpriteBatch spriteBatch)
        {
            this.CurrentDisplay.Draw(spriteBatch);
            this.ResponseText?.Draw(spriteBatch);
        }
    }
}