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
    public class WeaponBoxNode : InteractNode
    {
        Scene scene;

        public WeaponBoxNode(Scene scene)
        {
            this.scene = scene;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            scene.MainPlayer.ResetAmmo();
            new Sound2D("Pickup", false, false);
        }

        public override string GetInteractText()
        {
            return "Take Ammo";
        }
    }
}
