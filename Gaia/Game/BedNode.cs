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
    public class BedNode : InteractNode
    {
        Scene scene;
        bool isSleeping = false;
        float sleepTimer = float.PositiveInfinity;

        public BedNode(Scene scene)
        {
            this.scene = scene;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (scene.Actors.Count > 1)
            {
                playerScreen.AddJournalEntry("You cannot sleep while dinosaurs are near!");
            }
            else
            {
                sleepTimer = playerScreen.CloseEyes(3)*1.5f;
                scene.MainPlayer.SetControllable(false);
                scene.MainPlayer.SetEnabled(false);
                isSleeping = true;
                new Sound2D("Sleep", false, false);
            }
        }

        public override void OnUpdate()
        {
            if (isSleeping)
            {
                sleepTimer -= Time.GameTime.ElapsedTime;
                if (sleepTimer <= 0.0f)
                {
                    Sunlight sun = (Sunlight)scene.MainLight;
                    if (sun.IsNight())
                        sun.ResetToDay();
                    scene.MainPlayer.SetControllable(true);
                    scene.MainPlayer.SetEnabled(true);
                    scene.MainPlayer.ResetHealth();
                    isSleeping = false;
                    PlayerScreen playerScreen = PlayerScreen.GetInst();
                    playerScreen.OpenEyes();
                    playerScreen.AddJournalEntry("You feel rested and refreshed!");
                }
            }
            base.OnUpdate();
        }

        public override string GetInteractText()
        {
            return "Sleep";
        }
    }
}
