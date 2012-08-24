using System;
using System.Collections.Generic;
using Gaia.Resources;
using JigLibX.Physics;
using JigLibX.Collision;

namespace Gaia.Physics
{
    public class PhysicsObject
    {
        public Body Body;
        public CollisionSkin Collision;
    }

    public class Ragdoll
    {
        SortedList<string, PhysicsObject> limbs;
        List<HingeJoint> joints;

        private void DisableCollisions(Body rb0, Body rb1)
        {
            if ((rb0.CollisionSkin == null) || (rb1.CollisionSkin == null))
                return;
            rb0.CollisionSkin.NonCollidables.Add(rb1.CollisionSkin);
            rb1.CollisionSkin.NonCollidables.Add(rb0.CollisionSkin);
        }

        public void CreateRagdoll(AnimationNode[] rootNodes)
        {
            limbs = new SortedList<string, PhysicsObject>();
            joints = new List<HingeJoint>();
            //DisableCollisions(limbs[(int)LimbId.Torso].PhysicsBody, limbs[(int)LimbId.LowerLegLeft].PhysicsBody);
        }
    }
}
