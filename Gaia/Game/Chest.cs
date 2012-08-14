using System;
using System.Collections.Generic;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;


namespace Gaia.Game
{
    public class InteractObject : Model
    {
        InteractBody interactBody;
        InteractNode interactNode;

        public void SetInteractNode(InteractNode node)
        {
            this.interactNode = node;
            interactBody.Node = node;
        }

        public InteractNode GetInteractNode()
        {
            return interactNode;
        }

        public InteractObject(InteractNode node, string modelName)
            : base(modelName)
        {
            this.interactBody = new InteractBody(node);
            this.interactNode = node;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            if (collision != null)
            {
                interactBody.CollisionSkin = collision;
                collision.Owner = interactBody;
            }   
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            interactBody.CollisionSkin = null;
            interactBody.DisableBody();
            interactBody.Node = null;
            this.scene.GetPhysicsEngine().RemoveBody(interactBody);
            interactBody = null;
            interactNode = null;
        }
    }

    public enum PickupName
    {
        Amulet,
        Key,
        Radio,
    };

    /*
    public class AmuletNode : InteractNode
    {
        protected string chestName;
        protected Entity parent;

        public AmuletNode(Entity parent, string name)
        {
            this.chestName = "Take " + name;
            this.parent = parent;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            playerScreen.AddJournalEntry("Return to Camp");
            playerScreen.AddMarker(this.parent.GetScene().FindEntity("CampFire").Transformation);
            playerScreen.HasAmulet = true;
            this.parent.GetScene().RemoveEntity(parent);
        }

        public override string GetInteractText()
        {
            return chestName;
        }
    }
    */

    public class PickupNode : InteractNode
    {
        protected string chestName;
        protected Entity parent;
        protected PickupName mode;

        public PickupNode(Entity parent, PickupName pickupMode, string name)
        {
            this.chestName = "Take " + name;
            this.parent = parent;
            this.mode = pickupMode;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            switch(mode)
            {
                case PickupName.Amulet:
                    playerScreen.AddJournalEntry("Return to Camp");
                    playerScreen.AddMarker(this.parent.GetScene().FindEntity("CampFire").Transformation);
                    playerScreen.HasAmulet = true;
                    break;
                case PickupName.Key:
                    playerScreen.AddJournalEntry("Unlock the Power Plant");
                    playerScreen.AddMarker(this.parent.GetScene().FindEntity("PowerPlant").Transformation);
                    playerScreen.HasKeycard = true;
                    break;
                case PickupName.Radio:
                    playerScreen.AddJournalEntry("Meet the Rescue Team at the Docks");
                    playerScreen.AddMarker(this.parent.GetScene().FindEntity("Docks").Transformation);
                    playerScreen.UsedRadio = true;
                    break;
            }
            this.parent.GetScene().RemoveEntity(parent);
        }

        public override string GetInteractText()
        {
            return chestName;
        }
    }
}
