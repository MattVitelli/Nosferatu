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

        const string painSoundName = "HumanPain";
        const string deathSoundName = "HumanDeath";

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            camera = scene.MainPlayer;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
        public void SetControllable(bool controllable)
        {
            isControllable = controllable;
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
                Vector3 rot = scene.MainPlayer.Transformation.GetRotation();
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

        public override void OnUpdate()
        {
            if (isEnabled)
            {
                camera.Transformation.SetPosition(this.Transformation.GetPosition() + Vector3.Up * this.standCapsule.Length);
            }

            UpdateControls();
           
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
        }
    }
}
