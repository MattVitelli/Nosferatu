﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Math;

using Gaia.SceneGraph.GameEntities;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;

namespace Gaia.Physics
{
    class ASkinPredicate : CollisionSkinPredicate1
    {
        public override bool ConsiderSkin(CollisionSkin skin0)
        {
            if (!(skin0.Owner is CharacterBody))
                return true;
            else
                return false;
        }
    }

    public class CharacterBody : Body
    {
        
        public CharacterBody(Actor actor)
            : base()
        {
            this.actor = actor;
        }

        Actor actor;

        float jumpForce = 16;
        public Vector3 DesiredVelocity { get; set; }
        const int MAX_JUMPS = 0;
        int jumpsRemaining = MAX_JUMPS;

        private bool doJump = false;

        bool grounded = false;
        public bool IsGrounded
        {
            get { return grounded; }
        }

        Vector3 contactNormal = Vector3.Up;
        public Vector3 GetContactNormal()
        {
            return contactNormal;
        }

        public Actor GetActor()
        {
            return actor;
        }

        public void Jump(float _jumpForce)
        {
            doJump = true;
            jumpForce = _jumpForce;
        }

        public override void AddExternalForces(float dt)
        {
            ClearForces();

            grounded = (CollisionSkin.Collisions.Count > 1);

            if (doJump)
            {
                bool hasJumped = false;
                foreach (CollisionInfo info in CollisionSkin.Collisions)
                {
                    Vector3 N = info.DirToBody0;
                    if (this == info.SkinInfo.Skin1.Owner)
                        Vector3.Negate(ref N, out N);

                    if (Vector3.Dot(N, Orientation.Up) > 0.17f)
                    {
                        Vector3 vel = Velocity; vel.Y = jumpForce;
                        Velocity = vel;
                        jumpsRemaining = MAX_JUMPS;
                        hasJumped = true;
                        break;
                    }
                }
                if (!hasJumped && jumpsRemaining > 0)
                {
                    Vector3 vel = Velocity; vel.Y = jumpForce;
                    Velocity = vel;
                    jumpsRemaining--;
                }
            }

            bool foundContactNormal = false;
            foreach (CollisionInfo info in CollisionSkin.Collisions)
            {
                Vector3 N = info.DirToBody0;
                if (this == info.SkinInfo.Skin1.Owner)
                    Vector3.Negate(ref N, out N);
                else if (info.SkinInfo.Skin1.Owner == null)
                {
                    if (N.Y > 0.34)
                    {
                        contactNormal = Vector3.Normalize(Vector3.Lerp(N, contactNormal, 0.975f));
                        foundContactNormal = true;
                    }
                }
            }

            if(!foundContactNormal)
                contactNormal = Vector3.Normalize(Vector3.Lerp(Vector3.Up, contactNormal, 0.975f));

            Vector3 deltaVel = DesiredVelocity - Velocity;

            bool running = true;

            if (DesiredVelocity.LengthSquared() < JiggleMath.Epsilon) running = false;
            else deltaVel.Normalize();

            deltaVel.Y = 0.0f;

            // start fast, slow down slower
            if (running) deltaVel *= 10.0f;
            else deltaVel *= 2.0f;

            float forceFactor = 1000.0f;
            AddBodyForce(deltaVel * Mass * dt * forceFactor);

            doJump = false;
            //if (this.Position.Y > 0)
                AddGravityToExternalForce();
            if (this.Position.Y < 0)
                AddBodyForce(Vector3.Up * Mass * Math.Min(-this.Position.Y,13.5f));
        }

        public void RenderCollisionSkin(Gaia.Rendering.RenderViews.RenderView view)
        {
            VertexPositionColor[] wf = CollisionSkin.GetLocalSkinWireframe();
            this.TransformWireframe(wf);
            DebugElementManager debugMgr = (DebugElementManager)view.GetRenderElementManager(Gaia.Rendering.RenderPass.Debug);
            debugMgr.AddElements(wf);
        }
    }
}
