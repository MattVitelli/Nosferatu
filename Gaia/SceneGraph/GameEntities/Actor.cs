using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Vehicles;

using Gaia.Core;
using Gaia.Physics;
using Gaia.Sound;

namespace Gaia.SceneGraph.GameEntities
{
    public class Actor : Entity
    {
        const float MAX_HEALTH = 100;
        const float MAX_ENERGY = 100;

        protected float health = MAX_HEALTH;

        protected float energy = MAX_ENERGY;

        protected float energyRechargeRate = 15;

        protected float sprintEnergyCost = 20;

        protected float sprintSpeedBoost = 5.5f;

        protected bool isCrouching = false;

        protected int team = 0;

        protected float deathTime = 0;

        protected Vector3 startPos = Vector3.Zero;

        public void SetSpawnPosition(Vector3 pos)
        {
            startPos = pos + Vector3.Up*3f;
            if (standCapsule != null)
                startPos = pos + Vector3.Up * standCapsule.Length * 1.5f;
        }

        public int GetTeam()
        {
            return team;
        }

        public bool IsDead()
        {
            return (health <= 0.0f);
        }

        public float GetDeathTime()
        {
            return deathTime;
        }

        public float GetHealth()
        {
            return health;
        }

        public float GetHealthPercent()
        {
            return health / MAX_HEALTH;
        }

        public void ResetHealth()
        {
            health = MAX_HEALTH;
            energy = MAX_ENERGY;
        }

        public virtual HitType GetHit(Microsoft.Xna.Framework.Ray ray, float maxDistance, out float hitDistance)
        {
            hitDistance = 0;
            return HitType.None;
        }

        public virtual void ApplyDamage(float damage)
        {
            if (IsDead())
                return;
            health -= damage;
            if (IsDead())
                OnDeath();
        }

        public Vector3 GetForwardVector()
        {
            return forwardVector;
        }

        public Vector3 GetRightVector()
        {
            return strafeVector;
        }

        protected Vector3 forwardVector;

        protected Vector3 strafeVector;

        const float footstepThreshold = 10.75f;
        Vector3 lastPos = Vector3.Zero;

        protected CharacterBody body;

        protected CollisionSkin collision;

        protected Capsule standCapsule;
        protected Capsule crouchCapsule;

        protected virtual void AddCustomPrimitives() { }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            scene.AddActor(this);

            PhysicsSystem world = scene.GetPhysicsEngine();
            
            Vector3 pos = Vector3.Up * 256 + 15*(new Vector3((float)RandomHelper.RandomGen.NextDouble(), (float)RandomHelper.RandomGen.NextDouble(), (float)RandomHelper.RandomGen.NextDouble())*2-Vector3.One);
            //pos.X += (scene.MainTerrain as TerrainHeightmap).GetWidth() * 0.5f;
            //pos.Z += (scene.MainTerrain as TerrainHeightmap).GetDepth() * 0.5f;
            Vector3 normal = Vector3.Up;
            scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
            //pos = pos + Vector3.Up * 5;

            pos = startPos;// scene.FindEntity("Hangar").Transformation.GetPosition() + Vector3.Up * 15;// *(float)RandomHelper.RandomGen.NextDouble();

            body = new CharacterBody(this);
            collision = new CollisionSkin(body);

            standCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.778f);
            crouchCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.0f);
            SetupPosture(false);
            collision.AddPrimitive(standCapsule, (int)MaterialTable.MaterialID.NormalRough);
            
            Vector3 com = PhysicsHelper.SetMass(75.0f, body, collision);
            body.CollisionSkin = collision;
            
            body.MoveTo(pos + com, Matrix.Identity);
            collision.ApplyLocalTransform(new JigLibX.Math.Transform(-com, Matrix.Identity));

            body.SetBodyInvInertia(0.0f, 0.0f, 0.0f);

            body.AllowFreezing = false;
            body.EnableBody();
            Transformation = new Transform(body);

            ResetState();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            scene.RemoveActor(this);
        }

        protected void SetupPosture(bool crouching)
        {
            collision.RemoveAllPrimitives();
            Capsule currCapsule = (crouching) ? crouchCapsule : standCapsule;
            collision.AddPrimitive(currCapsule, (int)MaterialTable.MaterialID.NormalRough);
        }

        protected virtual void UpdateState()
        {
            energy += Time.GameTime.ElapsedTime * energyRechargeRate;
            if (IsDead())
                deathTime += Time.GameTime.ElapsedTime;
        }

        protected virtual void UpdateSounds()
        {
            if (body.IsGrounded && Vector3.DistanceSquared(lastPos, body.Position) > footstepThreshold)
            {
                lastPos = body.Position;
                Sound2D sound = new Sound3D("Footstep", body.Position);
               
            }
        }

        protected virtual void OnDeath()
        {

        }

        protected virtual void ResetState()
        {
            ResetHealth();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            UpdateState();
            UpdateSounds();
        }
    }
}
