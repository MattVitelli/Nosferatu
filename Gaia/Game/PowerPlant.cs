using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Sound;

namespace Gaia.Game
{
    public class PowerPlant
    {
        public int Puzzle = 0;
        public int PuzzleInput = 0;
        Transform powerPlantTransform;
        public bool IsEnabled = true;
        public Sound3D Sound;

        public bool AttemptPuzzle(int puzzle, int input)
        {
            bool a = ((input & 0x1) > 0);
            bool b = ((input & 0x2) > 0);
            bool c = ((input & 0x4) > 0);
            bool d = ((input & 0x8) > 0);

            if ((puzzle & 0x1) > 0)
            {
                a = !a;
            }
            if ((puzzle & 0x4) > 0)
            {
                b = !b;
            }
            if ((puzzle & 0xf) > 0)
            {
                c = !c;
            }
            if ((puzzle & 0x10) > 0)
            {
                d = !d;
            }

            bool result = false;
            if ((puzzle & 0x2) > 0)
                result = (a & b);
            else
                result = (a ^ b);

            if ((puzzle & 0x8) > 0)
                result &= c;
            else
                result ^= c;

            if ((puzzle & 0x20) > 0)
                result &= d;
            else
                result ^= d;

            return result;
        }

        bool IsValidPuzzle(int puzzle)
        {
            int numSuccesses = 0;
            for (int i = 0; i < 16; i++)
            {
                if (AttemptPuzzle(puzzle, i))
                    numSuccesses++;
            }

            return (numSuccesses <= 1 && numSuccesses > 0);
        }

        public PowerPlant(Scene scene)
        {
            Model powerPlant = new Model("PowerPlant");
            Model powerPlantFence = new Model("PowerPlantFence");
            InteractObject switchA = new InteractObject(new SwitchNode(this, 0x1), "PowerPlantSwitchA");
            InteractObject switchB = new InteractObject(new SwitchNode(this, 0x2), "PowerPlantSwitchB");
            InteractObject switchC = new InteractObject(new SwitchNode(this, 0x4), "PowerPlantSwitchC");
            InteractObject switchD = new InteractObject(new SwitchNode(this, 0x8), "PowerPlantSwitchD");

            (scene.MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.PowerPlant, powerPlant.Transformation, powerPlant.GetMesh().GetBounds());
            //powerPlant.Transformation.SetPosition(powerPlant.Transformation.GetPosition()-Vector3.Up*94);
            Vector3 pos = powerPlant.Transformation.GetPosition();
            Vector3 rot = powerPlant.Transformation.GetRotation();
            powerPlantTransform = powerPlant.Transformation;
            (switchA.GetInteractNode() as SwitchNode).SetInteractObject(switchA);
            (switchB.GetInteractNode() as SwitchNode).SetInteractObject(switchB);
            (switchC.GetInteractNode() as SwitchNode).SetInteractObject(switchC);
            (switchD.GetInteractNode() as SwitchNode).SetInteractObject(switchD);

            powerPlantFence.Transformation = powerPlant.Transformation;

            SafeTrigger campTrigger = new SafeTrigger();
            campTrigger.Transformation.SetPosition(powerPlantTransform.GetPosition());
            campTrigger.Transformation.SetScale(Vector3.One * 30);
            scene.AddEntity("SafeTrigger", campTrigger);
            
            switchA.Transformation.SetPosition(pos);
            switchA.Transformation.SetRotation(rot);
            
            switchB.Transformation.SetPosition(pos);
            switchB.Transformation.SetRotation(rot);
            
            switchC.Transformation.SetPosition(pos);
            switchC.Transformation.SetRotation(rot);

            switchD.Transformation.SetPosition(pos);
            switchD.Transformation.SetRotation(rot);

            
            AnimationNode[] nodes = powerPlantFence.GetMesh().GetNodes();
            Matrix worldMatrix = powerPlantFence.Transformation.GetTransform();
            //Vector3 meshCenter = 0.5f * (gasStation.GetMesh().GetBounds().Max + gasStation.GetMesh().GetBounds().Min);
            for (int i = 0; i < nodes.Length; i++)
            {
                InteractObject door = new InteractObject(null, "PowerPlantDoor", true);
                Vector3 doorPos = Vector3.Transform(nodes[i].Translation, worldMatrix);
                door.Transformation.SetPosition(doorPos);
                door.Transformation.SetRotation(powerPlantFence.Transformation.GetRotation());
                door.SetInteractNode(new PowerPlantDoorNode(door, new Vector3(0, MathHelper.PiOver2, 0), Vector3.Zero));
                scene.AddEntity("PowerPlantDoor", door);
            }
            

            //switchA.interactNode.OnInteract();
            //switchD.interactNode.OnInteract();

            scene.AddEntity("PowerPlant", powerPlant);
            scene.AddEntity("PowerPlantFence", powerPlantFence);
            
            
            scene.AddEntity("SwitchA", switchA);
            scene.AddEntity("SwitchB", switchB);
            scene.AddEntity("SwitchC", switchC);
            scene.AddEntity("SwitchD", switchD);

            List<int> potentialPuzzles = new List<int>();
            for (int i = 0; i < (1 << 7); i++)
            {
                if (IsValidPuzzle(i))
                {
                    potentialPuzzles.Add(i);
                }
            }
            Puzzle = potentialPuzzles[RandomHelper.RandomGen.Next(potentialPuzzles.Count)];
            int randInput = RandomHelper.RandomGen.Next(16);
            while (AttemptPuzzle(Puzzle, randInput))
                randInput = RandomHelper.RandomGen.Next(16);
            IsEnabled = false;
            if ((randInput & 0x01) > 0)
                switchA.GetInteractNode().OnInteract();
            if ((randInput & 0x02) > 0)
                switchB.GetInteractNode().OnInteract();
            if ((randInput & 0x04) > 0)
                switchC.GetInteractNode().OnInteract();
            if ((randInput & 0x08) > 0)
                switchD.GetInteractNode().OnInteract();
            IsEnabled = true;
        }

        public Vector3 GetPosition()
        {
            return powerPlantTransform.GetPosition();
        }
    }

    public class SwitchNode : InteractNode
    {
        PowerPlant plant;
        int bitFlag;
        InteractObject target;
        public SwitchNode(PowerPlant plant, int bitFlag)
        {
            this.plant = plant;
            this.bitFlag = bitFlag;
        }

        public void SetInteractObject(InteractObject entity)
        {
            this.target = entity;
        }

        public override void OnInteract()
        {
            base.OnInteract();
            this.plant.PuzzleInput ^= bitFlag;
            bool isActive = ((this.plant.PuzzleInput & bitFlag) > 0);
            Vector3 pos = this.target.Transformation.GetPosition();
            if (isActive)
                this.target.Transformation.SetPosition(pos + Vector3.Down);
            else
                this.target.Transformation.SetPosition(pos + Vector3.Up);
            if (plant.IsEnabled)
            {
                new Sound3D("FlipSwitch", this.target.Transformation.GetPosition());

                PlayerScreen playerScreen = PlayerScreen.GetInst();
                if (plant.AttemptPuzzle(plant.Puzzle, plant.PuzzleInput) && !playerScreen.ActivatedPower)
                {
                    playerScreen.ActivatedPower = true;
                    playerScreen.AddJournalEntry("Power has been restored!");
                    plant.Sound = new Sound3D("PowerPlantOn", plant.GetPosition());
                    plant.Sound.Looped = true;
                }
            }
        }

        public override bool IsEnabled()
        {
            return !PlayerScreen.GetInst().ActivatedPower;
        }

        public override string GetInteractText()
        {
            return "Flip Switch";
        }
    }
}
