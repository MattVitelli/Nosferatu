using System;
using System.Collections.Generic;
using Gaia.SceneGraph.GameEntities;
using Gaia.SceneGraph;
using Microsoft.Xna.Framework;

namespace Gaia.Game
{
    public class SafeTrigger : Trigger
    {
        bool useCustomSpawnPos = false;
        Vector3 customSpawnPos = Vector3.Zero;
        public SafeTrigger()
        {

        }

        public SafeTrigger(Vector3 spawnPos)
        {
            useCustomSpawnPos = true;
            this.customSpawnPos = spawnPos;
        }

        protected override void OnTriggerEnter()
        {
            PlayerScreen.GetInst().IsSafe = true;
            if (useCustomSpawnPos)
                scene.MainPlayer.SetSpawnPosition(customSpawnPos);
            else
                scene.MainPlayer.SetSpawnPosition(this.Transformation.GetPosition());
            base.OnTriggerEnter();
        }

        protected override void OnTriggerExit()
        {
            PlayerScreen.GetInst().IsSafe = false;
            base.OnTriggerExit();
        }
    }
}
