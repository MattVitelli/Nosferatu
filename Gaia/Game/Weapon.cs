using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Physics;
using Gaia.Sound;

using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Physics;

namespace Gaia.Game
{
    public class Weapon
    {
        Scene scene;
        ViewModel fpsModel;
        float coolDownTimeRemaining = 0;
        float timeTilFire = 0;
        int ammo = 5;
        int AmmoPerClip = 15;
        int ReserveAmmo = 0;
        IgnoreSkinPredicate ignorePred;
        Vector3 muzzleDir;
        Vector3 muzzlePos;
        bool hasFired = true;

        public Weapon(string modelName, Body body, Transform transform, Scene scene)
        {
            this.ignorePred = new IgnoreSkinPredicate(body);
            this.scene = scene;
            this.fpsModel = new ViewModel(modelName);
            this.fpsModel.SetRenderAlways(true, scene);
            fpsModel.SetTransform(transform);
            Matrix weaponTransform = Matrix.CreateScale(0.1f) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(MathHelper.PiOver2);
            fpsModel.SetCustomMatrix(weaponTransform);
            fpsModel.GetAnimationLayer().SetActiveAnimation("Pistol_Idle", true);
        }

        public void OnUpdate()
        {
            fpsModel.OnUpdate();

            if (coolDownTimeRemaining > 0)
            {
                coolDownTimeRemaining -= Time.GameTime.ElapsedTime;
            }
            if (timeTilFire > 0)
                timeTilFire -= Time.GameTime.ElapsedTime;
            if (timeTilFire <= 0 && !hasFired)
            {
                new Sound3D("PistolFire", muzzlePos);
                ammo--;
                hasFired = true;
                float dist;
                CollisionSkin skin;
                Vector3 pos, normal;

                Segment seg = new Segment(muzzlePos, muzzleDir * 50);

                scene.GetPhysicsEngine().CollisionSystem.SegmentIntersect(out dist, out skin, out pos, out normal, seg, ignorePred);
                if (skin != null)
                {
                    ParticleEffect bulletEffect = ResourceManager.Inst.GetParticleEffect("BulletEffect");
                    ParticleEmitter collideEmitter = new ParticleEmitter(bulletEffect, 16);
                    collideEmitter.EmitOnce = true;
                    NormalTransform newTransform = new NormalTransform();
                    newTransform.ConformToNormal(normal);
                    newTransform.SetPosition(pos);
                    collideEmitter.Transformation = newTransform;
                    scene.AddEntity("bulletEmitter", collideEmitter);
                }
            }
        }

        public bool IsManual()
        {
            return false;
        }

        public void Reload()
        {
            if (ReserveAmmo == 0)
                return;
            int ammoNeeded = AmmoPerClip - ammo;
            if (ReserveAmmo >= ammoNeeded)
            {
                ReserveAmmo -= ammoNeeded;
                ammo += ammoNeeded;
            }
            else
            {
                ammo += ReserveAmmo;
                ReserveAmmo = 0;
            }
            new Sound3D("PistolReload", muzzlePos);
            fpsModel.GetAnimationLayer().AddAnimation("PistolReload", true);
            coolDownTimeRemaining = ResourceManager.Inst.GetAnimation("PistolReload").EndTime;
        }

        public void OnFire(Vector3 muzzlePosition, Vector3 muzzleDir)
        {
            if (coolDownTimeRemaining <= 0)
            {
                this.muzzleDir = muzzleDir;
                this.muzzlePos = muzzlePosition;
                if (ammo > 0)
                {
                    hasFired = false;
                    //fpsModel.SetAnimationLayer("Pistol_Idle", 0.0f);
                    fpsModel.GetAnimationLayer().AddAnimation("Pistol_Fire", true);
                    //fpsModel.SetAnimationLayer("Pistol_Fire", 1.0f);
                    coolDownTimeRemaining = ResourceManager.Inst.GetAnimation("Pistol_Fire").EndTime;
                    timeTilFire = coolDownTimeRemaining * 0.35f;
                }
                else
                {
                    if (ReserveAmmo > 0)
                        Reload();
                    else
                    {
                        Sound3D emptySound = new Sound3D("PistolEmpty", muzzlePos);
                        fpsModel.GetAnimationLayer().AddAnimation("PistolFireEmpty", true);
                        coolDownTimeRemaining = ResourceManager.Inst.GetAnimation("PistolFireEmpty").EndTime;
                    }
                }
            }
        }

        public void OnRender(RenderView view)
        {
            fpsModel.OnRender(view, false);
            if (view.GetRenderType() == RenderViewType.MAIN)
            {
                GUIElementManager elemManager = GFX.Inst.GetGUI();
                int ammoRatio = (int)Math.Ceiling((float)ReserveAmmo / (float)AmmoPerClip);
                GUITextElement elem = new GUITextElement(new Vector2(0.85f, -0.85f), ammo.ToString() + "/"+ammoRatio);
                elemManager.AddElement(elem);
            }
        }
    }
}
