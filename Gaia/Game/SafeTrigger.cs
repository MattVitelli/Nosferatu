using System;
using System.Collections.Generic;
using Gaia.SceneGraph.GameEntities;
using Gaia.SceneGraph;
using Microsoft.Xna.Framework;

namespace Gaia.Game
{
    public class SafeTrigger : Trigger
    {
        protected override void OnTriggerEnter()
        {
            PlayerScreen.GetInst().IsSafe = true;
            base.OnTriggerEnter();
        }

        protected override void OnTriggerExit()
        {
            PlayerScreen.GetInst().IsSafe = false;
            base.OnTriggerExit();
        }
    }
}
