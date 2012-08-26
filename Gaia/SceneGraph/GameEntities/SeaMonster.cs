using System;
using System.Collections.Generic;
using Gaia.Sound;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.SceneGraph.GameEntities
{
    public class SeaMonster : Model
    {
        Actor target = null;
        float speed = 15;
        float killDistance = 7;
        Vector3 moveDirection = Vector3.Forward;

        public SeaMonster()
            : base("SeaMonster")
        {

        }

        void AcquireTarget()
        {
            if (target == null)
            {
                for (int i = 0; i < scene.Actors.Count; i++)
                {
                    Actor currActor = scene.Actors[i];
                    if (!currActor.IsDead())
                    {
                        Vector3 actorPos = currActor.Transformation.GetPosition();
                        Vector3 terrainPos = scene.MainTerrain.Transformation.GetPosition();
                        Vector3 terrainSize = scene.MainTerrain.Transformation.GetScale()*1.35f;
                        float dist = Vector3.Distance(terrainPos * new Vector3(1, 0, 1), actorPos * new Vector3(1, 0, 1));
                        if (actorPos.Y < 6 && dist > terrainSize.X)
                        {
                            Transformation.SetPosition(actorPos - currActor.GetForwardVector()*speed*10.0f);
                            target = currActor;
                            new Sound3D("SeaMonster", Transformation.GetPosition());
                            break;
                        }
                    }
                }
            }
        }

        void Move(Vector3 moveDir)
        {
            
            Vector3 forwardVec = this.Transformation.GetTransform().Forward;
            Vector3 strafeVec = this.Transformation.GetTransform().Right;

            float radianAngle = (float)Math.Acos(Vector3.Dot(forwardVec, moveDir));
            Vector3 rot = Transformation.GetRotation();
            if (Math.Abs(radianAngle) >= 0.075f)
            {
                radianAngle = MathHelper.Clamp(radianAngle, -1, 1) * ((Vector3.Dot(strafeVec, moveDir) < 0) ? 1.0f : -1.0f);
                rot.Y += radianAngle * 0.0015f;
            }
            Transformation.SetRotation(rot);
            Transformation.SetPosition(Transformation.GetPosition() + moveDir * speed * Time.GameTime.ElapsedTime);
            
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            AcquireTarget();
            if (target != null)
            {
                Vector3 moveDir = (target.Transformation.GetPosition()-Transformation.GetPosition());
                float distToTarget = moveDir.Length();
                moveDir /= distToTarget;
                Move(moveDir);
                if (distToTarget <= killDistance)
                {
                    target.ApplyDamage(999999);
                    target = null;
                    moveDirection = Vector3.Down + moveDir;
                }
            }
            else
            {
                if(this.Transformation.GetPosition().Y > -500)
                    Move(moveDirection);
            }
        }
    }
}
