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
    public class InteractObject : Model
    {
        InteractNode interactNode;
        InteractTrigger interactTrigger;

        public void SetInteractNode(InteractNode node)
        {
            this.interactNode = node;
        }

        public InteractNode GetInteractNode()
        {
            return interactNode;
        }

        public InteractObject(InteractNode node, string modelName)
            : base(modelName)
        {
            this.interactNode = node;
            this.interactTrigger = new InteractTrigger(this);
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            this.interactTrigger.OnAdd(scene);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this.interactTrigger.OnDestroy();
            interactNode = null;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            BoundingBox meshBounds = this.Transformation.TransformBounds(this.mesh.GetBounds());
            Vector3 center = (meshBounds.Max + meshBounds.Min ) * 0.5f;
            Vector3 scale = (meshBounds.Max - meshBounds.Min) * 0.5f;
            this.interactTrigger.Transformation.SetPosition(center);
            //this.interactTrigger.Transformation.SetRotation(this.Transformation.GetRotation());
            this.interactTrigger.Transformation.SetScale(scale * 5.5f);
            this.interactTrigger.OnUpdate();
        }
    }

    public enum PickupName
    {
        Amulet,
        Key,
        Radio,
    };

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
