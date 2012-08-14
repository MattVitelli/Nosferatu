using System;
using System.Collections.Generic;
using Gaia.SceneGraph.GameEntities;
using Gaia.SceneGraph;
using Microsoft.Xna.Framework;

namespace Gaia.Game
{
    public class CampTrigger : Trigger
    {
        protected override void OnTriggerEnter()
        {
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasAmulet && !playerScreen.IsVampireAwake)
            {
                playerScreen.AddJournalEntry("Light the Camp Fire");
            }
            base.OnTriggerEnter();
        }
    }
}
