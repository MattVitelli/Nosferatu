using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Sound;

namespace Gaia.Game
{
    public class AirplaneNode : InteractNode
    {
        Scene scene;
        bool isSleeping = false;
        float fadeOutTimer = float.PositiveInfinity;

        public AirplaneNode(Scene scene)
        {
            this.scene = scene;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasFuel)
            {
                
            }
            else
            {
                playerScreen.AddJournalEntry("It's out of fuel. If only there were a gas station here.");
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override string GetInteractText()
        {
            return "Fly Plane";
        }
    }
}
