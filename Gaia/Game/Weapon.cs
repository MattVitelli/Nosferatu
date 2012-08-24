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
        int ReserveAmmo = 2;
        IgnoreSkinPredicate ignorePred;
        Vector3 muzzleDir;
        Vector3 muzzlePos;
        bool hasFired = true;

        WeaponDatablock datablock;

        public Weapon(string datablockName, Body body, Transform transform, Scene scene)
        {
            this.ignorePred = new IgnoreSkinPredicate(body);
            this.scene = scene;
            this.datablock = ResourceManager.Inst.GetWeaponDatablock(datablockName);
            this.fpsModel = new ViewModel(datablock.MeshName);
            this.fpsModel.SetRenderAlways(true, scene);
            this.ammo = datablock.AmmoPerClip;
            this.ReserveAmmo = datablock.DefaultAmmo;
            fpsModel.SetTransform(transform);
            fpsModel.SetCustomMatrix(datablock.CustomMatrix);
            fpsModel.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(WeaponAnimations.Idle), true);
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
                hasFired = true;
                new Sound3D(datablock.GetSoundEffect((ammo > 0 || datablock.IsMelee)?WeaponAnimations.Fire:WeaponAnimations.Empty), muzzlePos);
                if (ammo > 0 || datablock.IsMelee)
                {
                    ammo--;
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
        }

        public bool IsManual()
        {
            return datablock.IsMelee || datablock.IsManual;
        }

        public void Reload()
        {
            if (ReserveAmmo == 0)
                return;
            int ammoNeeded = datablock.AmmoPerClip - ammo;
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
            new Sound3D(datablock.GetSoundEffect(WeaponAnimations.Reload), muzzlePos);
            string animName = datablock.GetAnimation(WeaponAnimations.Reload);
            fpsModel.GetAnimationLayer().AddAnimation(animName, true);
            coolDownTimeRemaining = ResourceManager.Inst.GetAnimation(animName).EndTime;
        }

        public void OnFire(Vector3 muzzlePosition, Vector3 muzzleDir)
        {
            if (coolDownTimeRemaining <= 0)
            {
                this.muzzleDir = muzzleDir;
                this.muzzlePos = muzzlePosition;
                if (ammo > 0 || datablock.IsMelee)
                {
                    hasFired = false;
                    string animName = datablock.GetAnimation(WeaponAnimations.Fire);
                    fpsModel.GetAnimationLayer().AddAnimation(animName, true);
                    coolDownTimeRemaining = ResourceManager.Inst.GetAnimation(animName).EndTime;
                    timeTilFire = coolDownTimeRemaining * datablock.GetDelayTime(animName);
                }
                else
                {
                    if (ReserveAmmo > 0)
                        Reload();
                    else
                    {
                        string animName = datablock.GetAnimation(WeaponAnimations.Empty);
                        fpsModel.GetAnimationLayer().AddAnimation(animName, true);
                        coolDownTimeRemaining = ResourceManager.Inst.GetAnimation(animName).EndTime;
                        timeTilFire = coolDownTimeRemaining*datablock.GetDelayTime(animName);
                        hasFired = false;
                    }
                }
            }
        }

        public void OnRender(RenderView view)
        {
            fpsModel.OnRender(view, false);
            if (view.GetRenderType() == RenderViewType.MAIN && !datablock.IsMelee)
            {
                GUIElementManager elemManager = GFX.Inst.GetGUI();
                int ammoRatio = (int)Math.Ceiling((float)ReserveAmmo / (float)AmmoPerClip);
                GUITextElement elem = new GUITextElement(new Vector2(0.85f, -0.85f), ammo.ToString() + "/"+ammoRatio);
                elemManager.AddElement(elem);
            }
        }
    }
}
