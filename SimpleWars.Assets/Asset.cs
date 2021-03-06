﻿namespace SimpleWars.Assets
{
    using System;

    using Microsoft.Xna.Framework.Content;

    using SimpleWars.Assets.Interfaces;

    /// <summary>
    /// The asset.
    /// </summary>
    public abstract class Asset : IAsset
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Asset2D"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        protected Asset(string name)
        {
            this.Name = name;
            this.LoadContentManager();
        }

        /// <summary>
        /// Gets the content.
        /// </summary>
        public ContentManager Content { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Loads the asset
        /// </summary>
        /// <param name="dir">
        /// The dir.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public abstract void LoadAsset(string dir, string name);

        /// <summary>
        /// The unload content.
        /// </summary>
        public virtual void UnloadContent()
        {
            this.Content.Unload();
            this.Content.Dispose();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <summary>
        /// The equals.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var other = obj as IAsset;
            return other != null && this.Name.Equals(other.Name);
        }

        /// <summary>
        /// The get hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Loads the content manager.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Throws error if the content manager has already been loaded
        /// </exception>
        private void LoadContentManager()
        {
            if (this.Content != null)
            {
                throw new InvalidOperationException("You are trying to load over existing content manager!");
            }

            this.Content = new ContentManager(
                ContentServiceProvider.ContentService,
                ContentServiceProvider.RootDirectory);
        }
    }
}