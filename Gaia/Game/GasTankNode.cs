using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;

namespace Gaia.Game
{
    public class GasTankNode : InteractNode
    {
        protected Entity parent;
        const float FUELING_TIME = 60.0f;
        protected float fuelTime = FUELING_TIME;
        protected bool hasStarted = false;

        public GasTankNode(Entity parent)
        {
            this.parent = parent;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (!hasStarted)
            {
                if (playerScreen.ActivatedPower)
                {
                    playerScreen.AddJournalEntry("Wait for the tank to fill");
                    hasStarted = true;
                }
                else
                {
                    playerScreen.AddJournalEntry("Turn on the power first");
                }
            }
            if (hasStarted && fuelTime <= 0.0f)
            {
                playerScreen.AddJournalEntry("Proceed to the Hangar");
                playerScreen.HasFuel = true;
                InteractObject door = (InteractObject)this.parent.GetScene().FindEntity("HangarDoor");
                HangarDoorNode doorNode = (HangarDoorNode)door.GetInteractNode();
                doorNode.SetLocked(true);
                this.parent.GetScene().RemoveEntity(parent);
            }
        }

        public override bool IsEnabled()
        {
            return (!hasStarted || fuelTime <= 0.0f);
        }

        public override string GetInteractText()
        {
            return (hasStarted) ? "Begin Fueling" : "Take Fuel Tank";
        }
    }
}
