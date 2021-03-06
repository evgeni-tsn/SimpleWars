﻿namespace SimpleWars.DisplayManagement.Displays
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using SimpleWars.Assets;
    using SimpleWars.Camera;
    using SimpleWars.Comms;
    using SimpleWars.Environment.Skybox;
    using SimpleWars.Environment.Terrain;
    using SimpleWars.Environment.Terrain.Terrains;
    using SimpleWars.Factories;
    using SimpleWars.GUI.Layouts.PrimitiveLayouts;
    using SimpleWars.Input;
    using SimpleWars.ModelDTOs;
    using SimpleWars.ModelDTOs.Entities;
    using SimpleWars.ModelDTOs.Enums;
    using SimpleWars.ModelDTOs.Resources;
    using SimpleWars.Models.Entities.DynamicEntities;
    using SimpleWars.Models.Entities.DynamicEntities.BattleUnits;
    using SimpleWars.Models.Entities.Interfaces;
    using SimpleWars.Models.Entities.StaticEntities.ResourceProviders;
    using SimpleWars.Users;
    using SimpleWars.Utils;

    public class HomeWorldDisplay : Display
    {
        private const int SendUpdateInterval = 30000;

        private CameraPerspective camera;

        private Terrain terrain;

        private Skybox skybox;

        private EntityDetailsLayout details;

        private Thread updateThread;

        private Queue<Unit> deadUnits; 

        private bool active;

        public override void LoadContent()
        {
            this.deadUnits = new Queue<Unit>();
            this.active = true;
            var aspectRatio = DisplayManager.Instance.Dimensions.X / DisplayManager.Instance.Dimensions.Y;
            this.camera = new CameraPerspective(
                aspectRatio,
                new Vector3(50, 30, 0));

            this.terrain = new HomeTerrain(DisplayManager.Instance.GraphicsDevice, UsersManager.CurrentPlayer.HomeSeed, new Vector3(-400, 0, -400));

            this.skybox = new Skybox(DisplayManager.Instance.GraphicsDevice);

            if (!UsersManager.CurrentPlayer.AllEntities.Any())
            {
                var random = new Random();
                var numberOfTrees = random.Next(300, 400);

                for (int i = 0; i < numberOfTrees; i++)
                {
                    var x = random.Next(-200, 200);
                    var z = random.Next(-200, 200);
                    var weight = random.Next(5, 10);
                    var y = 100;

                    var tree = new Tree(Guid.NewGuid(), UsersManager.CurrentPlayer.Id, new Vector3(x, y, z), Quaternion.Identity, weight);
                    Client.Socket.Writer.Send(Message.Create(Service.AddResProv, ResProvFactory.ToDto(tree)));

                    UsersManager.CurrentPlayer.ResourceProviders.Add(tree);
                    UsersManager.CurrentPlayer.AllEntities.Add(tree);
                }
            }
            else
            {
                foreach (var entity in UsersManager.CurrentPlayer.AllEntities)
                {
                    entity.LoadModel();
                }
            }       

            this.updateThread = new Thread(this.SendUpdates);
            
            this.updateThread.Start();  
        }

        public override void UnloadContent()
        {
            EntitySelector.Deselect();
            EntitySelector.PlaceEntity();
            this.active = false;
            this.updateThread.Abort();
            this.SendUpdate();
            UsersManager.LogoutCurrentUser();
            UsersManager.CurrentPlayer = null;
            ModelsManager.Instance.DisposeAll();           
        }

        public override void Update(GameTime gameTime)
        {
            this.CleanDead();

            var allEntities = UsersManager.CurrentPlayer.AllEntities;

            foreach (var entity in allEntities)
            {
                entity.GravityAffect(gameTime, this.terrain);
            }

            foreach (var unit in UsersManager.CurrentPlayer.Units)
            {
                unit.Update(gameTime, this.terrain, allEntities);             
            }

            this.details?.Update(gameTime);
                
            if (Input.KeyPressed(Keys.D1))
            {
                if (EntitySelector.HasPicked())
                {
                    EntitySelector.PlaceEntity();
                }
                else
                {
                    var unit = new Swordsman(Guid.NewGuid(), UsersManager.CurrentPlayer.Id, Vector3.Zero, Quaternion.Identity);
                    Client.Socket.Writer.Send(Message.Create(Service.AddUnit, UnitFactory.ToDto(unit)));
                    UsersManager.CurrentPlayer.Units.Add(unit);
                    UsersManager.CurrentPlayer.AllEntities.Add(unit);
                    EntitySelector.EntityPicked = unit;
                }
            }

            if (Input.LeftMouseClick())
            {
                if (EntitySelector.HasPicked())
                {
                    EntitySelector.PlaceEntity();
                }
                else
                {
                    EntitySelector.SelectEntity(
                      DisplayManager.Instance.GraphicsDevice,
                      this.camera.ProjectionMatrix,
                      this.camera.ViewMatrix,
                      allEntities);

                    if (EntitySelector.HasSelected())
                    {
                        var projectedPosition =
                            DisplayManager.Instance.GraphicsDevice.Viewport.Project(
                                EntitySelector.EntitySelected.Position,
                                this.camera.ProjectionMatrix,
                                this.camera.ViewMatrix,
                                Matrix.Identity);

                        this.details = new EntityDetailsLayout(EntitySelector.EntitySelected, PointTextures.TransparentBlackPoint, projectedPosition);
                    }
                }
            }

            if (EntitySelector.HasSelected())
            {
                if (Input.RightMouseDoubleClick)
                {
                    IEntity selected = EntitySelector.EntitySelected;
                    this.CommandSelectedEntity(selected, allEntities.Where(e => e != selected));
                }
            }

            if (this.details != null)
            {
                this.ProjectClickedEntity();
                this.ReadDetailsCommand();
            }

            EntitySelector.DragEntity(
                DisplayManager.Instance.GraphicsDevice,
                this.camera.ProjectionMatrix,
                this.camera.ViewMatrix,
                this.terrain);

            this.skybox.Update(gameTime);
            this.camera.Update(gameTime, this.terrain);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            this.skybox.Draw(this.camera.ProjectionMatrix, this.camera.ViewMatrix);
            this.terrain.Draw(this.camera.ViewMatrix, this.camera.ProjectionMatrix);

            foreach (var entity in UsersManager.CurrentPlayer.ResourceProviders)
            {
                entity.Draw(this.camera.ViewMatrix, this.camera.ProjectionMatrix);          
            }

            foreach (var unit in UsersManager.CurrentPlayer.Units)
            {
                unit.Draw(this.camera.ViewMatrix, this.camera.ProjectionMatrix);
            }

            this.details?.Draw(spriteBatch);
        }

        private void CommandSelectedEntity(IEntity selected, IEnumerable<IEntity> others)
        {
            if (selected is IUnit)
            {
                IEntity target = RayCaster.CastToEntities(
                    DisplayManager.Instance.GraphicsDevice,
                    this.camera.ProjectionMatrix,
                    this.camera.ViewMatrix,
                    others);

                bool targetedSelf = target == selected;

                if (!targetedSelf)
                {
                    this.CommandSelectedUnit(selected, target);
                }

                Vector3 destination = RayCaster.GetTerrainPoint(
                    DisplayManager.Instance.GraphicsDevice,
                    this.camera.ProjectionMatrix,
                    this.camera.ViewMatrix,
                    this.terrain);

                ((IMoveable)selected).Destination = destination;
            }
        }

        private void CommandSelectedUnit(IEntity selected, IEntity target)
        {
            if (!(target is IKillable))
            {
                (selected as ICombatUnit)?.ChangeAttackTarget(null);
            }
            else
            {
                (selected as ICombatUnit)?.ChangeAttackTarget((IKillable)target);
            }
        }

        private void ProjectClickedEntity()
        {
            var projectedPosition = DisplayManager.Instance.GraphicsDevice.Viewport.Project(
                this.details.Entity.Position,
                this.camera.ProjectionMatrix,
                this.camera.ViewMatrix,
                Matrix.Identity);

            this.details.AdjustPosition(new Vector2(projectedPosition.X, projectedPosition.Y));
        }

        private void ReadDetailsCommand()
        {
            if (this.details.Command == DetailCommand.PickEntity)
            {
                EntitySelector.PickEntity(this.details.Entity);
                this.details = null;
            }
            else if (this.details.Command == DetailCommand.GatherResource)
            {
                ((IResourceProvider)this.details.Entity).Gather(5);
            }
            else if (this.details.Command == DetailCommand.Close)
            {
                this.details = null;
            }
        }

        private void CleanDead()
        {
            var markedForDestruction = UsersManager.CurrentPlayer.Units.Where(u => u.IsAlive == false).ToArray();

            foreach (var unit in markedForDestruction)
            {
                this.deadUnits.Enqueue(unit);
                UsersManager.CurrentPlayer.Units.Remove(unit);
                UsersManager.CurrentPlayer.AllEntities.Remove(unit);
                if (EntitySelector.EntityPicked == unit || EntitySelector.EntitySelected == unit)
                {
                    EntitySelector.Deselect();
                    EntitySelector.PlaceEntity();
                }

                if (this.details?.Entity == unit)
                {
                    this.details = null;
                }
            }
        }

        private void SendUpdate()
        {
            Unit[] currentlyDeadUnits = new Unit[this.deadUnits.Count];
            for (int i = 0; i < currentlyDeadUnits.Length; i++)
            {      
                currentlyDeadUnits[i] = this.deadUnits.Dequeue();
            }

            List<UnitDTO> units = UsersManager.CurrentPlayer.Units.Where(u => u.Modified).Concat(currentlyDeadUnits).Select(UnitFactory.ToDto).ToList();

            List<ResourceProviderDTO> resProvs = UsersManager.CurrentPlayer.ResourceProviders.Where(rp => rp.Modified).Select(ResProvFactory.ToDto).ToList();

            ResourceSetDTO resSet = ResourceSetFactory.ToDto(UsersManager.CurrentPlayer.ResourceSet);

            ICollection<EntityDTO> modifiedEntities = units.Concat<EntityDTO>(resProvs).ToArray();

            if (modifiedEntities.Count > 0)
            {
                Client.Socket.Writer.Send(Message.Create(Service.UpdateEntities, modifiedEntities));
            }

            foreach (var unit in UsersManager.CurrentPlayer.Units)
            {
                unit.Modified = false;
            }

            foreach (var rp in UsersManager.CurrentPlayer.ResourceProviders)
            {
                rp.Modified = false;
            }

            Client.Socket.Writer.Send(Message.Create(Service.UpdateResourceSet, resSet));
        }

        private void SendUpdates()
        {
            while (this.active)
            {
                Thread.Sleep(5000);

                this.SendUpdate();
            }
        }
    }
}