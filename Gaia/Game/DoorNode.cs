using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;

using JigLibX.Collision;

namespace Gaia.Game
{
    public class DoorNode : InteractNode
    {
        bool isOpen = false;
        bool isOpening = false;
        const float OPEN_DOOR_TIME = 0.8f;
        float openTimer = 0;
        float oldTheta;
        Entity parent;

        public DoorNode(Entity parent)
        {
            this.parent = parent;
            this.oldTheta = parent.Transformation.GetRotation().Y;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasKeycard)
            {
                isOpen = !isOpen;
                isOpening = true;
                openTimer = 0;
                /*
                CollisionSkin skin = ((CollisionTransform)parent.Transformation).GetCollisionSkin();
                parent.GetScene().GetPhysicsEngine().CollisionSystem.RemoveCollisionSkin(skin);
                */
            }
            else
            {
                playerScreen.AddJournalEntry("It's locked. Maybe there's a key?");
            }
        }

        public override void OnUpdate()
        {
            if (isOpening)
            {
                openTimer += Time.GameTime.ElapsedTime;
                float theta = 0;
                if (isOpen)
                    theta = MathHelper.Lerp(0, MathHelper.PiOver2, Math.Min(1.0f, openTimer / OPEN_DOOR_TIME));
                else
                    theta = MathHelper.Lerp(MathHelper.PiOver2, 0, Math.Min(1.0f, openTimer / OPEN_DOOR_TIME));
                parent.Transformation.SetRotation(new Vector3(0, MathHelper.WrapAngle(theta+oldTheta), 0));
                if (openTimer >= OPEN_DOOR_TIME)
                {
                    //CollisionSkin skin = ((CollisionTransform)parent.Transformation).GetCollisionSkin();
                    //parent.GetScene().GetPhysicsEngine().CollisionSystem.AddCollisionSkin(skin);
                    isOpening = false;
                }
            }
            base.OnUpdate();
        }

        public override bool IsEnabled()
        {
            return !isOpening;
        }

        public override string GetInteractText()
        {
            return (isOpen) ? "Close Door" : "Open Door";
        }
    }
}
