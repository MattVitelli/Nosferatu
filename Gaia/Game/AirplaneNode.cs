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
        float timeTilEnd = float.PositiveInfinity;
        float timeTilRoar = float.PositiveInfinity;
        AnimatedModel trex = null;
        Sound3D trexSound = null;
        bool isEnding = false;
        bool hasRoared = false;
        bool hasEnded = false;
       
        public AirplaneNode(Scene scene)
        {
            this.scene = scene;
        }

        public void SetEndCameraPosition(Vector3 pos, Vector3 forward)
        {

        }

        public override void OnInteract()
        {
            base.OnInteract();
            PlayerScreen playerScreen = PlayerScreen.GetInst();
            if (playerScreen.HasFuel)
            {
                if (trex == null)
                {
                    new Sound3D("PlaneEngine", scene.FindEntity("Plane").Transformation.GetPosition());
                    playerScreen.AddJournalEntry("It won't start. Search outside for a tool.");
                    trex = (AnimatedModel)scene.FindEntity("TRex");
                    trex.SetVisible(true);
                    trex.Model.GetAnimationLayer().SetActiveAnimation("TRexIdle", false);
                    trexSound = new Sound3D("TRexIdle", trex.Transformation.GetPosition());
                    trexSound.Looped = true;
                    //cameraLerpTime = -0.25f;
                }
            }
            else
            {
                playerScreen.AddJournalEntry("It's out of fuel. If only there were a gas station here.");
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (trex != null && !isEnding)
            {
                Vector3 distToDino = scene.MainCamera.GetPosition() - trex.Transformation.GetPosition();
                if (distToDino.Length() < 35.0f)
                {
                    isEnding = true;
                    trex.Model.GetAnimationLayer().AddAnimation("TRexRoar", true);
                    AnimationSequence seq = ResourceManager.Inst.GetAnimation("TRexRoar");
                    timeTilEnd = seq.EndTime*1.35f;
                    timeTilRoar = seq.EndTime * 0.25f;
                }
            }
            else if (isEnding)
            {
                timeTilEnd -= Time.GameTime.ElapsedTime;
                timeTilRoar -= Time.GameTime.ElapsedTime;
                if (timeTilEnd <= 0.0f && !hasEnded)
                {
                    hasEnded = true;
                    PlayerScreen playerScreen = PlayerScreen.GetInst();
                    playerScreen.CloseEyes(1);
                    playerScreen.AddJournalEntry("To Be Continued...");
                    scene.MainPlayer.SetControllable(false);
                    scene.MainPlayer.SetEnabled(false);
                    playerScreen.EndGame();
                }
                if (timeTilRoar <= 0.0f && !hasRoared)
                {
                    hasRoared = true;
                    trexSound.Paused = true;
                    trexSound.Looped = false;
                    new Sound3D("TRexRoar", trex.Transformation.GetPosition());
                }
            }
        }

        public override bool IsEnabled()
        {
            return (trex == null);// base.IsEnabled();
        }

        public override string GetInteractText()
        {
            return "Fly Plane";
        }
    }
}
