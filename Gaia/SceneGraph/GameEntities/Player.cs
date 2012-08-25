using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Input;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Voxels;
using Gaia.Rendering;
using Gaia.Game;
using Gaia.Sound;
namespace Gaia.SceneGraph.GameEntities
{
    public class Player : Actor
    {
        protected bool isEnabled = false;
        protected bool isControllable = false;

        Camera camera;
        Weapon gun;

        const string painSoundName = "HumanPain";
        const string deathSoundName = "HumanDeath";
        const float RespawnTime = 7;
        float timeTilRespawn = RespawnTime;

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            camera = (Camera)scene.FindEntity("MainCamera");
            gun = new Weapon("SMG", this.body, camera.Transformation, scene);
        }

        public bool IsEnabled()
        {
            return isEnabled;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public bool IsControllable()
        {
            return isControllable;
        }

        public void SetControllable(bool controllable)
        {
            isControllable = controllable;
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            timeTilRespawn = RespawnTime;
            PlayerScreen.GetInst().AddJournalEntry("You are dead", PlayerScreen.GetInst().CloseEyes(2)*1.1f);
            SetEnabled(false);
            SetControllable(false);
        }

        void UpdateControls()
        {
            if (InputManager.Inst.IsKeyDownOnce(GameKey.ToggleCamera))
            {
                isControllable = !isControllable;
                isEnabled = isControllable;
            }
            if (InputManager.Inst.IsKeyDownOnce(GameKey.DropPlayerAtCamera))
            {
                isControllable = true;
                isEnabled = true;
                this.Transformation.SetPosition(camera.Transformation.GetPosition());
            }
            if (isControllable)
            {
                Vector3 velocity = Vector3.Zero;
                Vector3 rot = camera.Transformation.GetRotation();
                Matrix transform = Matrix.CreateRotationY(rot.Y);
                forwardVector = transform.Forward;
                strafeVector = transform.Right;
                if (InputManager.Inst.IsKeyDown(GameKey.MoveFoward))
                {
                    velocity += transform.Forward;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveBackward))
                {
                    velocity -= transform.Forward;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveLeft))
                {
                    velocity -= transform.Right;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveRight))
                {
                    velocity += transform.Right;
                }
                if (InputManager.Inst.IsKeyDownOnce(GameKey.Jump))
                {
                    body.Jump(12.5f);
                }
                if (InputManager.Inst.IsKeyDownOnce(GameKey.Crouch))
                {
                    SetupPosture(true);
                    this.team++;
                }
                if (InputManager.Inst.IsKeyUpOnce(GameKey.Crouch))
                {
                    SetupPosture(false);
                }

                float sprintCoeff = 0;
                if (InputManager.Inst.IsKeyDown(GameKey.Sprint) && velocity.Length() > 0.001f)
                {
                    energy -= Time.GameTime.ElapsedTime * sprintEnergyCost;
                    sprintCoeff = sprintSpeedBoost * MathHelper.Clamp(energy, 0, 1);
                }

                body.DesiredVelocity = velocity * (7.5f + sprintCoeff);

                if(gun != null)
                {
                    if (gun.IsManual())
                    {
                        if (InputManager.Inst.IsKeyDownOnce(GameKey.Fire))
                            gun.OnFire(camera.Transformation.GetPosition(), camera.Transformation.GetTransform().Forward);
                    }
                    else if (InputManager.Inst.IsKeyDown(GameKey.Fire))
                        gun.OnFire(camera.Transformation.GetPosition(), camera.Transformation.GetTransform().Forward);
                    
                }
            }
        }

        public override void ApplyDamage(float damage)
        {
            base.ApplyDamage(damage);
            if (IsDead())
                new Sound2D(deathSoundName, false, false);
            else
                new Sound2D(painSoundName, false, false);
        }

        protected override void UpdateState()
        {
            base.UpdateState();
            if (IsDead() && timeTilRespawn > 0.0f)
            {
                timeTilRespawn -= Time.GameTime.ElapsedTime;
                if (timeTilRespawn <= 0.0f)
                {
                    ResetState();
                    scene.MainDirector.PruneAllDinosaurs();
                    this.Transformation.SetPosition(startPos);
                    PlayerScreen.GetInst().OpenEyes();
                    SetEnabled(true);
                    SetControllable(true);
                }
            }
        }

        public override void OnUpdate()
        {
            if (isEnabled)
            {
                camera.Transformation.SetPosition(this.Transformation.GetPosition() + Vector3.Up * this.standCapsule.Length);
            }

            UpdateControls();
            gun.OnUpdate();
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            gun.OnRender(view);
        }
    }
}
