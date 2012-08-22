using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Collision;

using Gaia.SceneGraph;
namespace Gaia.Core
{
    public class CollisionTransform : Transform
    {
        CollisionSkin collision;
        JigLibX.Math.Transform oldTransform;
        JigLibX.Math.Transform collisionTransform;
        Scene scene;

        public CollisionSkin GetCollisionSkin()
        {
            return collision;
        }

        public CollisionTransform(CollisionSkin collision, Scene scene)
        {
            this.scene = scene;
            this.collision = collision;
            this.collisionTransform = new JigLibX.Math.Transform(position, worldMatrix);
            this.oldTransform = collisionTransform;
        }

        protected override void UpdateMatrix()
        {
            base.UpdateMatrix();

            if (collision != null)
            {
                this.oldTransform = collisionTransform;
                Matrix currOrientation = GetTransform();
                currOrientation.Translation = Vector3.Zero;
                Vector3 currPosition = GetPosition();
                collisionTransform.Orientation = currOrientation;
                collisionTransform.Position = currPosition;
                //collision.ApplyLocalTransform(new JigLibX.Math.Transform(Vector3.Zero, Matrix.Identity));
                collision.SetTransform(ref oldTransform, ref collisionTransform);
                collision.UpdateWorldBoundingBox();
                collision.CollisionSystem.CollisionSkinMoved(collision);
                
            }
            
        }
    }
}
