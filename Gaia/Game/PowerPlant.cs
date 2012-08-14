using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;

namespace Gaia.Game
{
    public class PowerPlant
    {
        public int Puzzle = 0;
        Transform powerPlantTransform;
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
            
            switchA.Transformation.SetPosition(pos);
            switchA.Transformation.SetRotation(rot);
            
            switchB.Transformation.SetPosition(pos);
            switchB.Transformation.SetRotation(rot);
            
            switchC.Transformation.SetPosition(pos);
            switchC.Transformation.SetRotation(rot);

            switchD.Transformation.SetPosition(pos);
            switchD.Transformation.SetRotation(rot);

            //switchA.interactNode.OnInteract();
            //switchD.interactNode.OnInteract();

            scene.AddEntity("PowerPlant", powerPlant);
            scene.AddEntity("PowerPlantFence", powerPlantFence);
            
            scene.AddEntity("SwitchA", switchA);
            scene.AddEntity("SwitchB", switchB);
            scene.AddEntity("SwitchC", switchC);
            scene.AddEntity("SwitchD", switchD);
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
            this.plant.Puzzle ^= bitFlag;
            bool isActive = ((this.plant.Puzzle & bitFlag) > 0);
            Vector3 pos = this.target.Transformation.GetPosition();
            if (isActive)
                this.target.Transformation.SetPosition(pos + Vector3.Down);
            else
                this.target.Transformation.SetPosition(pos + Vector3.Up);
        }

        public override string GetInteractText()
        {
            return "Flip Switch";
        }
    }
}
