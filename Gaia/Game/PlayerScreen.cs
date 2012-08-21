using System;
using System.Collections.Generic;

using Gaia.UI;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Input;
using Gaia.Rendering.RenderViews;
using Gaia.Sound;

using Microsoft.Xna.Framework;

using JigLibX.Collision;
using JigLibX.Geometry;

namespace Gaia.Game
{
    public class PlayerScreen : UIScreen
    {
        UICompass compass;
        UIButton journalStatus;
        UIButton interactButton;
        UIButton interactStatus;
        //UIButton crosshair;
        UIList list;

        Scene scene;

        const float MAX_INTERACT_DIST = 3.5f;
        const float journalFadeInTime = 1.5f;
        const float journalFadeOutTime = 2.0f;
        float journalFadeTime = 0;
        bool journalEntryAdded = false;

        InteractSkinPredicate pred = new InteractSkinPredicate();

        Transform playerTransform;

        static PlayerScreen inst = null;

        public static PlayerScreen GetInst() { return inst; }

        public bool IsVampireAwake = false;

        public bool HasAmulet = false;

        public bool HasKeycard = false;

        public bool UsedRadio = false;

        public bool ActivatedPower = false;

        public bool HasFuel = false;

        public List<InteractTrigger> interactables = new List<InteractTrigger>();

        Sound2D bgSound;

        public PlayerScreen() : base()
        {
            inst = this;
            this.scene = new Scene();
            compass = new UICompass();
            compass.Scale = new Vector2(0.35f, 0.05f);
            compass.Position = new Vector2(0, -0.85f);
            /*
            crosshair = new UIButton(ResourceManager.Inst.GetTexture("UI/Game/crossHair.dds"), Vector4.One, string.Empty);
            crosshair.Position = Vector2.Zero;
            crosshair.Scale = Vector2.One * 0.035f;
            */
            journalStatus = new UIButton(null, Vector4.One, "Journal Updated!");
            journalStatus.Position = new Vector2(0, 0.15f);// new Vector2(-0.7f, 0.85f);
            journalStatus.SetVisible(false);

            interactStatus = new UIButton(null, Vector4.One, "Examine Object");
            interactStatus.Position = new Vector2(-0.7f, -0.85f);
            interactStatus.SetVisible(false);

            interactButton = new UIButton(ResourceManager.Inst.GetTexture("UI/Game/interact.dds"), Vector4.One, string.Empty);
            interactButton.Position = Vector2.Zero;
            interactButton.Scale = Vector2.One * 0.035f * 1.15f;
            interactButton.SetVisible(false);

            list = new UIList();
            list.ItemColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            list.Position = new Vector2(0.5f, 0);
            list.Scale = new Vector2(0.25f, 0.5f);
            string[] testStrings = new string[] 
            { 
                "Hello", "A Carton of Cigarettes", "Oh boy", "I hope this works", "25 or 6 to 4", "Rawr", "Christ", 
                "El Captain", "John Wayne", "Walter White", "Stanford Cow", "Dr. Evil", "Tom Hanks", "Lindsey Lohan",
                "Raptor Jesus", "Timothy Leary", "Bauldur", "Frank Lloyd Wright", "Pink Floyd", "The Beatles", "JoCo",
                "Is this twenty yet??",
            };
            list.Items.AddRange(testStrings);
            list.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            //this.controls.Add(list);
            compass.AddCustomMarker(scene.FindEntity("Tent").Transformation, ResourceManager.Inst.GetTexture("UI/Game/Home.dds"));
            //this.controls.Add(crosshair);
            this.controls.Add(journalStatus);
            this.controls.Add(compass);
            this.controls.Add(interactStatus);
            this.controls.Add(interactButton);
            bgSound = new Sound2D("Crickets", true, false);
            bgSound.Paused = false;
        }

        public void AddMarker(Transform transform)
        {
            compass.AddMarker(transform);
        }

        public void RemoveMarker(Transform transform)
        {
            compass.RemoveMarker(transform);
        }

        public void AddJournalEntry(string description)
        {
            journalFadeTime = 0;
            journalEntryAdded = true;
            journalStatus.SetText(description);
        }

        public void AddInteractable(InteractTrigger trigger)
        {
            interactables.Add(trigger);
        }

        public void RemoveInteractable(InteractTrigger trigger)
        {
            int index = interactables.IndexOf(trigger);
            if (index >= 0 && index < interactables.Count)
                interactables.RemoveAt(index);
        }

        InteractNode GetInteractNode(Microsoft.Xna.Framework.Ray lookRay)
        {
            float collisionDist;
            CollisionSkin skin = null;
            Vector3 pos, normal;
            Segment seg = new Segment(lookRay.Position, lookRay.Direction * MAX_INTERACT_DIST);
            scene.GetPhysicsEngine().CollisionSystem.SegmentIntersect(out collisionDist, out skin, out pos, out normal, seg, pred);

            float bestDist = (skin != null) ? collisionDist : float.PositiveInfinity;
            InteractNode bestNode = null;
            for (int i = 0; i < interactables.Count; i++)
            {
                InteractObject interactObj = interactables[i].GetInteractObject();
                
                BoundingBox bounds =  interactObj.Transformation.TransformBounds(interactObj.GetMesh().GetBounds());
                float? intersection = lookRay.Intersects(bounds);
                if (intersection.HasValue)
                {
                    float dist = intersection.Value;
                    if (dist < bestDist && dist <= MAX_INTERACT_DIST)
                    {
                        bestDist = dist;
                        bestNode = interactObj.GetInteractNode();
                    }
                }
            }
            return bestNode;
        }

        void PerformInteraction()
        { 
            Vector3 origin = playerTransform.GetPosition();
            Vector3 lookDir = playerTransform.GetTransform().Forward;

            Microsoft.Xna.Framework.Ray lookRay;
            lookRay.Position = origin;
            lookRay.Direction = lookDir;

            InteractNode node = GetInteractNode(lookRay);
            //scene.GetPhysicsEngine().CollisionSystem.SegmentIntersect(out dist, out skin, out pos, out normal, seg, pred);
            if (node != null)//skin != null && skin.Owner != null && ((InteractBody)skin.Owner).Node != null)
            {
                //InteractBody body = (InteractBody)skin.Owner;
                //InteractNode node = body.Node;
                //Do collision detection at a later date!
                //skin.Owner
                interactStatus.SetText(node.GetInteractText());

                //crosshair.SetVisible(false);
                interactButton.SetVisible(true);
                interactStatus.SetVisible(true);

                if (InputManager.Inst.IsKeyDownOnce(GameKey.Interact))
                {
                    node.OnInteract();
                }
            }
            else
            {
                //crosshair.SetVisible(true);
                interactButton.SetVisible(false);
                interactStatus.SetVisible(false);
            }
        }

        void DisplayJournalStatus(float timeDT)
        {
            if (journalFadeTime < (journalFadeInTime + journalFadeOutTime))
            {
                journalFadeTime += timeDT;

                float alpha = (journalFadeTime <= journalFadeInTime) ? (journalFadeTime / journalFadeInTime) : (1.0f - (journalFadeTime - journalFadeInTime) / journalFadeOutTime);

                journalStatus.SetVisible(true);
                journalStatus.SetTextColor(new Vector4(1, 1, 1, alpha));
            }
            else
            {
                journalStatus.SetVisible(false);
                journalEntryAdded = false;
            }
        }

        public override void OnUpdate(float timeDT)
        {
            scene.Update();
            Entity camera = scene.FindEntity("MainCamera");
            
            if (camera != null)
            {
                playerTransform = camera.Transformation;
                compass.SetTransformation(playerTransform);
                PerformInteraction();
            }
            MainRenderView view = (MainRenderView)scene.MainCamera;
            Entity enemy = scene.FindEntity("Amulet");
            if (enemy != null)
            {
                view.SetBlurTarget(enemy.Transformation.GetPosition(), Vector3.Normalize(camera.Transformation.GetTransform().Forward));
            }

            if(journalEntryAdded)
                DisplayJournalStatus(timeDT);
            
            base.OnUpdate(timeDT);
        }

        public override void OnRender()
        {
            scene.Render();
            base.OnRender();
        }
    }
}
