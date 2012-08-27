using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Sound;
using JigLibX.Geometry;
using JigLibX.Collision;
using JigLibX.Physics;
namespace Gaia.SceneGraph.GameEntities
{
    public class Raptor : Actor
    {
        ViewModel model;
        DinosaurDatablock datablock;

        public enum RaptorState
        {
            Wander = 0,
            Stealth,
            Chase,
            Attack,
            LeapAttack,
            AquiredTarget,
            BackOff,
            Flank,
            Dead
        }

        const float GOAL_POINT_THRESHOLD = 5.5f;
        const float GOAL_DISTANCE = 20.0f;
        const float DISTANCE_EPSILON = 1.0f;

        const int WANDER_MAX_MOVES = 3;
        const int WANDER_DISTANCE = 160;
        const float WANDER_DELAY_SECONDS = 4.0f;
        const float ATTACK_DELAY_SECONDS = 1.5f;
        const float SIGHT_DISTANCE = 120;
        const float ATTACK_DISTANCE = 5;
        const float MIN_ATTACK_DISTANCE = 3;
        const float MAX_FLANK_OFFSET = 15.0f;
        const float FLANK_CHASE_DIST = 9.5f;
        const float MAX_IDLE_SOUNDTIME = 6.5f;
        float idleSoundTime = 0;

        int wanderMovesCount;
        Vector3 wanderPosition;
        Vector3 wanderStartPosition;
        float wanderDelayTime;

        float animationDelay = 0;

        Vector3 velocityVector = Vector3.Zero;
        const float speed =  12.5f;
        NormalTransform grounding = new NormalTransform();
        float deathAnimationTime = 1.0f;

        Actor enemy = null;

        RaptorState state;
        RaptorState prevState;
        Vector3 oldStrafeVec = Vector3.Zero;

        Vector3 movementVector = Vector3.Forward;

        public Raptor(DinosaurDatablock datablock)
        {
            this.datablock = datablock;
            model = new ViewModel(datablock.MeshName);
            model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Idle), true);

            grounding.SetScale(datablock.Scale);
            grounding.SetRotation(datablock.Rotation);
            grounding.SetPosition(datablock.Position);

            model.SetCustomMatrix(grounding.GetTransform());
            team = datablock.Team;
            MAX_HEALTH = datablock.Health;
            healthRechargeRate = 3;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            model.SetTransform(this.Transformation);
            model.SetRenderAlways(false, scene);
        }

        void UpdateAnimation()
        {
            if (!IsDead())
            {
                float vel = velocityVector.Length();
                if (vel < 0.015f)
                {
                    model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Idle), false);
                }
                else
                {
                    //model.SetAnimationLayer(IDLE_NAME, 0.0f);
                    /*
                    float walkWeight = MathHelper.Clamp(1 - vel / 3.5f, 0.0f, 1.0f);
                    float runWeight = 1.0f - walkWeight;
                    if (walkWeight > 0.5f)
                        model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Walk), false);
                    else*/
                    model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Run), false);
                }
                if (state == RaptorState.Attack)
                {
                    if (animationDelay <= 0.0f)
                    {
                        string attackAnim = datablock.GetAnimation(DinosaurAnimationsSimple.Attack);
                        model.GetAnimationLayer().AddAnimation(attackAnim, true);
                        animationDelay = ResourceManager.Inst.GetAnimation(attackAnim).EndTime;
                        new Sound3D(datablock.AttackSoundEffect, this.Transformation.GetPosition());
                    }
                }
                grounding.SetForwardVector(Vector3.Normalize(movementVector));

                //if (velocityVector.Length() > 0.01f)
                //    grounding.SetForwardVector(Vector3.Normalize(velocityVector));

                
            }
            if (IsDead())
            {
                float lerper = MathHelper.Clamp(deathTime / deathAnimationTime, 0, 1);
                Vector3 rot = Vector3.Lerp(datablock.Rotation, datablock.DeathRotation, lerper);
                rot.X = MathHelper.WrapAngle(rot.X);
                rot.Y = MathHelper.WrapAngle(rot.Y);
                rot.Z = MathHelper.WrapAngle(rot.Z);
                grounding.SetRotation(rot);
                grounding.SetPosition(Vector3.Lerp(datablock.Position, datablock.DeathPosition, lerper));
            }
            grounding.ConformToNormal(body.GetContactNormal());
            model.SetCustomMatrix(grounding.GetTransform());
            model.OnUpdate();
            model.SetHitboxExtensionVectors(this.GetRightVector(), this.GetForwardVector());
        }

        private void Wander()
        {
            // Calculate wander vector on X, Z axis
            Vector3 wanderVector = wanderPosition - Transformation.GetPosition();
            wanderVector.Y = 0;
            float wanderVectorLength = wanderVector.Length();

            // Reached the destination position
            if (wanderVectorLength < DISTANCE_EPSILON)
            {
                Random rand = RandomHelper.RandomGen;
                // Generate new random position
                if (wanderMovesCount < WANDER_MAX_MOVES)
                {
                    wanderPosition = Transformation.GetPosition() +
                        WANDER_DISTANCE * (2.0f * new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()) - Vector3.One);

                    wanderMovesCount++;
                }
                // Go back to the start position
                else
                {
                    wanderPosition = wanderStartPosition;
                    wanderMovesCount = 0;
                }

                // Next time wander
                wanderDelayTime = WANDER_DELAY_SECONDS +
                    WANDER_DELAY_SECONDS * (float)rand.NextDouble();

                velocityVector = Vector3.Zero;
            }

            wanderDelayTime -= Time.GameTime.ElapsedTime;

            // Wait for the next action time
            if (wanderDelayTime <= 0.0f)
            {
                Move(Vector3.Normalize(wanderVector));
            }
        }

        void Move(Vector3 moveDir)
        {
            /*
            Vector3 forwardVec = this.Transformation.GetTransform().Forward;
            Vector3 strafeVec = this.Transformation.GetTransform().Right;

            float radianAngle = (float)Math.Acos(Vector3.Dot(forwardVec, moveDir));
            Vector3 rot = Transformation.GetRotation();
            if (Math.Abs(radianAngle) >= 0.075f)
            {
                radianAngle = MathHelper.Clamp(radianAngle, -1, 1) * ((Vector3.Dot(strafeVec, moveDir) < 0) ? 1.0f : -1.0f);
                rot.Y -= radianAngle * 0.015f;
            }
            Transformation.SetRotation(rot);
            */
            movementVector = Vector3.Lerp(moveDir, movementVector, 0.95f);
            velocityVector = moveDir * speed;//, velocityVector, 0.95f);
        }

        void AcquireEnemy()
        {
            enemy = null;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < scene.Actors.Count; i++)
            {
                Actor currActor = scene.Actors[i];
                if (currActor.GetTeam() != this.GetTeam() && !currActor.IsDead())
                {
                    float dist = Vector3.DistanceSquared(currActor.Transformation.GetPosition(), this.Transformation.GetPosition());
                    if (dist < minDist)
                    {
                        enemy = currActor;
                        minDist = dist;
                    }
                }
            }
            if (enemy != null)
            {
                state = RaptorState.AquiredTarget;
                //Roar when they're spotted
                string attackAnim = datablock.GetAnimation(DinosaurAnimationsSimple.Roar);
                new Sound3D(datablock.RoarSoundEffect, this.Transformation.GetPosition());
                model.GetAnimationLayer().AddAnimation(attackAnim, true);
                animationDelay = ResourceManager.Inst.GetAnimation(attackAnim).EndTime;
            }
            else
                state = RaptorState.Wander;
        }

        void SetState(RaptorState newState)
        {
            prevState = state;
            state = newState;
        }

        void EvaluateAttack(float distToTarget)
        {
            if (distToTarget <= ATTACK_DISTANCE)
            {
                SetState(RaptorState.Attack);
                wanderDelayTime = 0;
            }
        }

        

        void PerformBehavior()
        {
            if (this.IsDead())
            {
                return;
            }

            if (enemy == null || enemy.IsDead())
            {
                AcquireEnemy();
            }
            float distanceToTarget = float.PositiveInfinity;
            Vector3 targetVec = Vector3.Forward;
            animationDelay -= Time.GameTime.ElapsedTime;

            if (enemy != null)
            {

                targetVec = enemy.Transformation.GetPosition() - this.Transformation.GetPosition();
                distanceToTarget = targetVec.Length();
                targetVec *= 1.0f / distanceToTarget; //Normalize the vector
            }

            switch (state)
            {
                case RaptorState.Flank:
                    if (animationDelay <= 0.0f)
                    {
                        float interpCoeff = MathHelper.Clamp(distanceToTarget / 18.0f, 0.0f, 1.0f);  //* ((float)RandomHelper.RandomGen.NextDouble() * 2.0f - 1.0f)
                        Vector3 strafeVec = enemy.GetRightVector() * MAX_FLANK_OFFSET * interpCoeff * ((Vector3.Dot(enemy.GetRightVector(), targetVec) < 0) ? 1.0f : -1.0f);
                        oldStrafeVec = Vector3.Lerp(strafeVec, oldStrafeVec, 0.95f);
                        wanderPosition = enemy.Transformation.GetPosition() + oldStrafeVec;
                        Vector3 diff = wanderPosition - this.Transformation.GetPosition();

                        const float MIN_ANGLE = 0.15f;
                        float angleDiff = Vector3.Dot(enemy.GetForwardVector() * new Vector3(1, 0, 1), targetVec * new Vector3(1, 0, 1));
                        float angleLerp = MathHelper.Clamp(angleDiff-MIN_ANGLE, 0.0f, 1.0f);
                        Move(Vector3.Normalize(Vector3.Lerp(Vector3.Normalize(diff), targetVec, (float)Math.Pow(angleLerp,3.5f))));
                        
                        
                        //If we've reached our goal point
                        if ((float)Math.Sqrt(diff.X * diff.X + diff.Z * diff.Z) <= FLANK_CHASE_DIST || angleDiff >= MIN_ANGLE)
                            SetState(RaptorState.Chase);
                        EvaluateAttack(distanceToTarget);
                    }
                    break;

                case RaptorState.Wander:
                    if (distanceToTarget < SIGHT_DISTANCE)
                        SetState(RaptorState.Chase);
                    else
                        Wander();
                    break;

                case RaptorState.Chase:
                    EvaluateAttack(distanceToTarget);
                    if (distanceToTarget > SIGHT_DISTANCE * 1.35f)
                        SetState(RaptorState.Wander);
                    else if (distanceToTarget > MIN_ATTACK_DISTANCE)
                    {
                        if (distanceToTarget > MIN_ATTACK_DISTANCE * 3.0f)
                            SetState(RaptorState.Flank);
                        else
                            Move(targetVec);
                    }
                    else
                    {
                        Move(-targetVec);
                    }
                    break;

                case RaptorState.Attack:
                    if (distanceToTarget > ATTACK_DISTANCE * 1.5f)// || distanceToTarget < MIN_ATTACK_DISTANCE)
                    {
                        SetState(RaptorState.Chase);
                        new Sound3D(datablock.BarkSoundEffect, this.Transformation.GetPosition());
                    }
                    else
                    {
                        if(distanceToTarget > ATTACK_DISTANCE)
                            Move(targetVec);
                        SetState(RaptorState.Attack);
                        if (animationDelay <= 0.0f)
                        {
                            new Sound3D(datablock.MaulSoundEffect, enemy.Transformation.GetPosition());
                            enemy.ApplyDamage(datablock.Damage);
                            SetState(RaptorState.BackOff);
                            animationDelay = 0.2f;
                        }
                        //Attack
                    }
                    break;
                case RaptorState.AquiredTarget:
                    if (animationDelay <= 0.0f)
                        SetState(RaptorState.Chase);
                    break;
                case RaptorState.BackOff:
                    if (animationDelay <= 0.0f)
                    {
                        if (prevState != RaptorState.BackOff)
                        {
                            //Choose goal point
                            Vector3 randDir = Vector3.Normalize(new Vector3((float)RandomHelper.RandomGen.NextDouble() * 2.0f - 1.0f, 0, (float)RandomHelper.RandomGen.NextDouble() * 2.0f - 1.0f)) * GOAL_DISTANCE;
                            wanderPosition = enemy.Transformation.GetPosition() + randDir;
                            //Update state so new goal point isn't chosen
                            SetState(RaptorState.BackOff);
                        }
                        else
                        {
                            Move(Vector3.Normalize(wanderPosition - this.Transformation.GetPosition()));
                            Vector3 diff = wanderPosition - this.Transformation.GetPosition();
                            float distToGoal = diff.X * diff.X + diff.Z * diff.Z;
                            //If we've reached our goal point
                            if (distToGoal*0.05f <= GOAL_POINT_THRESHOLD)
                                EvaluateAttack(distanceToTarget);

                            if (distToGoal <= GOAL_POINT_THRESHOLD)
                            {
                                SetState(RaptorState.Chase);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            state = RaptorState.Dead;
            velocityVector = Vector3.Zero;
            new Sound3D(datablock.DeathSoundEffect, this.Transformation.GetPosition());
            model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.DeathIdle), false);
            string deathAnim = datablock.GetAnimation(DinosaurAnimationsSimple.Death);
            model.GetAnimationLayer().AddAnimation(deathAnim, true);
            deathAnimationTime = ResourceManager.Inst.GetAnimation(deathAnim).EndTime;
            grounding.SetPosition(datablock.DeathPosition);
            grounding.SetRotation(datablock.DeathRotation);
        }

        public override void ApplyDamage(float damage)
        {
            base.ApplyDamage(damage);
            if (!IsDead())
            {
                if (enemy != null)
                {
                    if (Vector3.Distance(enemy.Transformation.GetPosition(), Transformation.GetPosition()) > ATTACK_DISTANCE * 2.0f && GetHealthPercent() > 0.8f)
                        SetState(RaptorState.BackOff);
                    new Sound3D(datablock.BarkSoundEffect, this.Transformation.GetPosition());
                }
            }
        }

        protected override void ResetState()
        {
            base.ResetState();
            wanderMovesCount = 0;
            // Unit configurations
            enemy = null;

            wanderPosition = Transformation.GetPosition();
            wanderStartPosition = wanderPosition;
            state = RaptorState.Wander;
        }

        protected override void UpdateSounds()
        {
            base.UpdateSounds();
            idleSoundTime -= Time.GameTime.ElapsedTime;
            if (idleSoundTime <=  0 && !IsDead())
            {
                new Sound3D(datablock.IdleSoundEffect, this.Transformation.GetPosition());
                idleSoundTime = MAX_IDLE_SOUNDTIME;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            PerformBehavior();
            UpdateAnimation();
            body.DesiredVelocity = velocityVector;
        }

        public bool IsVisible()
        {
            BoundingBox bounds = Transformation.TransformBounds(model.GetMeshBounds());
            return (scene.MainCamera.GetFrustum().Contains(bounds) != ContainmentType.Disjoint);
        }

        public override HitType GetHit(Microsoft.Xna.Framework.Ray ray, float maxDistance, out float hitDistance)
        {
            BoundingBox hitBounds = model.GetHitBounds();
            float? dist;
            ray.Intersects(ref hitBounds, out dist);
            if (dist.HasValue && dist.Value <= maxDistance)
            {
                float bestDist = float.PositiveInfinity;
                HitType bestHit = HitType.None;
                SortedList<HitType, BoundingBox> hitBoxes = model.GetHitBoxes();
                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    HitType currHitType = hitBoxes.Keys[i];
                    float? hitDist;
                    BoundingBox bounds = hitBoxes[currHitType];
                    ray.Intersects(ref bounds, out hitDist);
                    if (hitDist.HasValue && hitDist.Value <= maxDistance && hitDist.Value < bestDist)
                    {
                        bestDist = hitDist.Value;
                        bestHit = currHitType;
                    }
                }
                hitDistance = bestDist;
                return bestHit;
            }
            hitDistance = 0;
            return HitType.None;
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            model.OnRender(view, true);
            /*
            if (view.GetRenderType() == Gaia.Rendering.RenderViews.RenderViewType.MAIN)
            {
                body.RenderCollisionSkin(view);
                model.RenderDebug(view);
            }
            */
        }
    }
}
