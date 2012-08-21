using System;
using System.Collections.Generic;
using Gaia.SceneGraph.GameEntities;
using Gaia.SceneGraph;

using Microsoft.Xna.Framework;

namespace Gaia.Game
{
    public class Trigger : Entity
    {
        protected bool isPlayerInTrigger = false;

        protected virtual void OnTriggerEnter() { }
        protected virtual void OnTriggerExit() { }
        protected virtual void OnTriggerTick() { }

        public override void OnUpdate()
        {
            Vector3 playerPos = scene.MainCamera.GetPosition();
            BoundingBox bounds = this.Transformation.GetBounds();
            if (bounds.Contains(playerPos) != ContainmentType.Disjoint)
            {
                if (isPlayerInTrigger)
                    OnTriggerTick();
                else
                {
                    isPlayerInTrigger = true;
                    OnTriggerEnter();
                }
            }
            else if (isPlayerInTrigger)
            {
                isPlayerInTrigger = false;
                OnTriggerExit();
            }
            base.OnUpdate();
        }
    }
}
