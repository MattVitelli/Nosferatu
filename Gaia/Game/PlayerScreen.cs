using System;
using System.Collections.Generic;

using Gaia.UI;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Input;
using Gaia.Rendering.RenderViews;
using Gaia.Sound;
using Gaia.Rendering;

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
        UIButton blackFade;
        UIButton creditsLabel;
        UIButton crosshair;
        UIList list;

        Scene scene;

        const float MAX_INTERACT_DIST = 3.5f;
        const float journalFadeInTime = 1.5f;
        const float journalFadeOutTime = 2.0f;
        const float blinkTimeAmp = 2.85f; //1.20458f
        const float blinkPeriod = MathHelper.TwoPi / blinkTimeAmp;//5.215f;
        float journalFadeTime = 0;
        bool journalEntryAdded = false;

        float blinkTime = 0;
        float maxBlinkTime = 0;

        InteractSkinPredicate pred = new InteractSkinPredicate();

        Transform playerTransform;

        static PlayerScreen inst = null;

        public static PlayerScreen GetInst() { return inst; }

        public bool IsSafe = false;

        public bool HasKeycard = false;

        public bool ActivatedPower = false;

        public bool HasFuel = false;

        bool isGameRunning = true;

        const float timeTilCredits = 5;

        float fadeOutTime = 0;

        public List<InteractTrigger> interactables = new List<InteractTrigger>();

        public int FuelCount = 0;

        public const int RequiredFuelCount = 3;

        Sound2D bgSound;

        public PlayerScreen() : base()
        {
            inst = this;
            
            compass = new UICompass();
            compass.Scale = new Vector2(0.35f, 0.05f);
            compass.Position = new Vector2(0, -0.85f);
            
            crosshair = new UIButton(ResourceManager.Inst.GetTexture("UI/Game/crossHair.dds"), Vector4.One, string.Empty);
            crosshair.Position = Vector2.Zero;
            crosshair.Scale = Vector2.One * 0.035f;
            
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

            blackFade = new UIButton(null, Vector4.Zero, string.Empty);
            blackFade.Position = Vector2.Zero;
            blackFade.Scale = Vector2.One;
            blackFade.SetVisible(false);

            creditsLabel = new UIButton(null, Vector4.One, "Created by Matt Vitelli - Summer 2012");
            creditsLabel.Position = Vector2.Zero;
            creditsLabel.Scale = Vector2.One;
            creditsLabel.SetVisible(false);

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
            
            this.controls.Add(crosshair);
            this.controls.Add(journalStatus);
            this.controls.Add(compass);
            this.controls.Add(interactStatus);
            this.controls.Add(interactButton);
            this.controls.Add(blackFade);
            this.controls.Add(creditsLabel);

            this.scene = new Scene();

            compass.AddCustomMarker(scene.FindEntity("Tent").Transformation, ResourceManager.Inst.GetTexture("UI/Game/Home.dds"));
            bgSound = new Sound2D("Crickets", true, false);
            bgSound.Paused = false;
        }

        public void EndGame()
        {
            isGameRunning = false;
            fadeOutTime = 0;
            blackFade.SetVisible(true);
            compass.SetVisible(false);
            interactButton.SetVisible(false);
            interactStatus.SetVisible(false);
            //crosshair.SetVisible(false);
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
            AddJournalEntry(description, 0);
            /*
            journalFadeTime = 0;
            journalEntryAdded = true;
            journalStatus.SetText(description);
            */
        }

        public void AddJournalEntry(string description, float timeTilDisplay)
        {
            journalFadeTime = -timeTilDisplay;
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
                        InteractNode node = interactObj.GetInteractNode();
                        if (node.IsEnabled())
                        {
                            bestDist = dist;
                            bestNode = interactObj.GetInteractNode();
                        }
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
            if (node != null)
            {
                interactStatus.SetText(node.GetInteractText());

                crosshair.SetVisible(false);
                interactButton.SetVisible(true);
                interactStatus.SetVisible(true);

                if (InputManager.Inst.IsKeyDownOnce(GameKey.Interact))
                {
                    node.OnInteract();
                }
            }
            else
            {
                crosshair.SetVisible(true);
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
                journalStatus.SetTextColor(new Vector4(1, 1, 1, MathHelper.Clamp(alpha,0,1)));
            }
            else
            {
                journalStatus.SetVisible(false);
                journalEntryAdded = false;
            }
        }

        float BlinkFunction(float time)
        {
            float blinkAnim = MathHelper.Clamp((float)Math.Cos(time * blinkTimeAmp) * 0.5f + 0.5f, 0.0f, 1.0f);
            blinkAnim = MathHelper.Clamp((float)Math.Pow(blinkAnim, 0.25) * 2.5f - 0.7f, 0.0f, 1.0f);
            return blinkAnim;
        }

        public float CloseEyes(int blinkCount)
        {
            blinkTime = 0;
            maxBlinkTime = blinkPeriod * ((float)blinkCount - 0.5f);
            return maxBlinkTime;
        }

        public void OpenEyes()
        {
            blinkTime = -blinkPeriod * 1.5f;
            maxBlinkTime = 0;
        }

        void UpdateBlinkTime()
        {
            blinkTime += Time.GameTime.ElapsedTime;
            blinkTime = Math.Min(blinkTime, maxBlinkTime);
            PostProcessElementManager ppMgr = (PostProcessElementManager)scene.MainCamera.GetRenderElementManager(Gaia.Rendering.RenderPass.PostProcess);
            ppMgr.SetBlinkTime(BlinkFunction(blinkTime));
        }

        public override void OnUpdate(float timeDT)
        {
            scene.Update();
            Entity camera = scene.FindEntity("MainCamera");
            
            if (camera != null)
            {
                playerTransform = camera.Transformation;
                compass.SetTransformation(playerTransform);
                if(isGameRunning)
                    PerformInteraction();
            }
            /*
            MainRenderView view = (MainRenderView)scene.MainCamera;
            Entity enemy = scene.FindEntity("Amulet");
            if (enemy != null)
            {
                view.SetBlurTarget(enemy.Transformation.GetPosition(), Vector3.Normalize(camera.Transformation.GetTransform().Forward));
            }
            */
            if(journalEntryAdded)
                DisplayJournalStatus(timeDT);

            UpdateBlinkTime();

            if (!isGameRunning)
            {
                fadeOutTime += timeDT;
                if (fadeOutTime < timeTilCredits)
                {
                    Vector4 color = Vector4.Zero;
                    color.W = fadeOutTime / timeTilCredits;
                    blackFade.SetButtonColor(color);
                }
                else
                {
                    creditsLabel.SetVisible(true);
                    float alpha = MathHelper.Clamp((fadeOutTime - timeTilCredits) / 3.0f, 0.0f, 1.0f);
                    creditsLabel.SetTextColor(Vector4.One * alpha);
                }
            }
            
            base.OnUpdate(timeDT);
        }

        public override void OnRender()
        {
            scene.Render();
            base.OnRender();
        }
    }
}
