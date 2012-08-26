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
using JigLibX.Collision;

namespace Gaia.Game
{
    public class PowerPlantDoorNode : DoorNode
    {
        public PowerPlantDoorNode(Entity parent, Vector3 goalRot, Vector3 goalDisp) : base(parent, goalRot, goalDisp)
        {

        }

        public override void OnInteract()
        {
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasKeycard)
            {
                OpenDoor();
                new Sound3D("MetalDoorOpen", this.oldPos);
                playerScreen.RemoveMarker(parent.GetScene().FindEntity("PowerPlant").Transformation);
            }
            else
            {
                playerScreen.AddJournalEntry("It's locked. Maybe there's a key?");
                new Sound3D("MetalDoorLocked", this.oldPos);
            }
        }
    }

    public class HangarDoorNode : DoorNode
    {
        bool isLocked = false;

        public HangarDoorNode(Entity parent, Vector3 goalRot, Vector3 goalDisp)
            : base(parent, goalRot, goalDisp)
        {

        }

        public void SetLocked(bool value)
        {
            isLocked = value;
            if (isOpen && isLocked)
                OpenDoor(); //This will close the door
        }

        public override void OnInteract()
        {
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (isLocked)
            {
                playerScreen.AddJournalEntry("It's jammed. Try another entrance.");
                new Sound3D("MetalDoorLocked", this.oldPos);
            }
            else
            {
                if (playerScreen.ActivatedPower)
                {
                    Sound3D sound = new Sound3D("ElectricDoorOpen", this.oldPos);
                    OPEN_DOOR_TIME = sound.PlayLength;
                    OpenDoor();
                }
                else
                {
                    playerScreen.AddJournalEntry("It's locked. Turn on the power first.");
                    new Sound3D("MetalDoorLocked", this.oldPos);
                }
            }
        }
    }

    public class DoorNode : InteractNode
    {
        protected bool isOpen = false;
        protected bool isOpening = false;
        protected float OPEN_DOOR_TIME = 0.8f;
        protected float openTimer = 0;
        protected Vector3 oldRot;
        protected Vector3 oldPos;

        protected Vector3 goalRot;
        protected Vector3 goalDisp;
        protected Entity parent;

        public DoorNode(Entity parent, Vector3 goalRot, Vector3 goalDisp)
        {
            this.parent = parent;
            this.oldRot = parent.Transformation.GetRotation();
            this.oldPos = parent.Transformation.GetPosition();
            this.goalRot = goalRot;
            this.goalDisp = goalDisp;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            /*
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasKeycard)
            {
                OpenDoor()
                new Sound3D("MetalDoorOpen", this.oldPos);
            }
            else
            {
                playerScreen.AddJournalEntry("It's locked. Maybe there's a key?");
                new Sound3D("MetalDoorLocked", this.oldPos);
            }
            */
        }

        protected void OpenDoor()
        {
            isOpen = !isOpen;
            isOpening = true;
            openTimer = 0;
        }

        public override void OnUpdate()
        {
            if (isOpening)
            {
                openTimer += Time.GameTime.ElapsedTime;
                Vector3 pos;
                Vector3 rot; 
                float lerpCoeff = Math.Max(0.0f, Math.Min(1.0f, openTimer / OPEN_DOOR_TIME));
                if (isOpen)
                {
                    pos = Vector3.Lerp(Vector3.Zero, goalDisp, lerpCoeff);
                    rot = Vector3.Lerp(Vector3.Zero, goalRot, lerpCoeff);
                }
                else
                {
                    pos = Vector3.Lerp(goalDisp, Vector3.Zero, lerpCoeff);
                    rot = Vector3.Lerp(goalRot, Vector3.Zero, lerpCoeff);
                }
                Vector3 newRot = new Vector3(MathHelper.WrapAngle(oldRot.X+rot.X), MathHelper.WrapAngle(oldRot.Y+rot.Y), MathHelper.WrapAngle(oldRot.Z+rot.Z));
                parent.Transformation.SetPosition(oldPos + pos);
                parent.Transformation.SetRotation(newRot);
                if (openTimer >= OPEN_DOOR_TIME)
                {
                    isOpening = false;
                }
            }
            base.OnUpdate();
        }

        public override bool IsEnabled()
        {
            return !isOpening;
        }

        public override string GetInteractText()
        {
            return (isOpen) ? "Close Door" : "Open Door";
        }
    }
}
