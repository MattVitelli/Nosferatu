using System;
using System.Collections.Generic;
using Gaia.SceneGraph.GameEntities;
using Gaia.SceneGraph;
using Microsoft.Xna.Framework;

namespace Gaia.Game
{
    public class InteractTrigger : Trigger
    {

        InteractObject parent;

        public InteractObject GetInteractObject()
        {
            return parent;
        }

        public InteractTrigger(InteractObject parent)
        {
            this.parent = parent;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            parent = null;
            OnTriggerExit();
        }

        protected override void OnTriggerEnter()
        {
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            playerScreen.AddInteractable(this);
            base.OnTriggerEnter();
        }

        protected override void OnTriggerExit()
        {
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            playerScreen.RemoveInteractable(this);
            base.OnTriggerExit();
        }
    }
}
