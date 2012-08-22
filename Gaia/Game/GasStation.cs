using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;
using Gaia.Resources;
namespace Gaia.Game
{
    public class GasStation
    {
        Transform gasStationTransform;
        public GasStation(Scene scene)
        {
            Model gasStation = new Model("GasStation");

            (scene.MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.GasStation, gasStation.Transformation, gasStation.GetMesh().GetBounds());
            //powerPlant.Transformation.SetPosition(powerPlant.Transformation.GetPosition()-Vector3.Up*94);
            Vector3 pos = gasStation.Transformation.GetPosition();
            gasStationTransform = gasStation.Transformation;
            AnimationNode[] nodes = gasStation.GetMesh().GetNodes();
            Matrix worldMatrix = gasStation.Transformation.GetTransform();
            //Vector3 meshCenter = 0.5f * (gasStation.GetMesh().GetBounds().Max + gasStation.GetMesh().GetBounds().Min);
            for (int i = 0; i < nodes.Length; i++)
            {
                InteractObject gasTank = new InteractObject(null, "GasTank");
                gasTank.SetInteractNode(new GasTankNode(gasTank));
                Vector3 gasTankPos = Vector3.Transform(nodes[i].Translation, worldMatrix);
                gasTank.Transformation.SetPosition(gasTankPos);
                gasTank.Transformation.SetRotation(gasStation.Transformation.GetRotation());
                scene.AddEntity("GasTank", gasTank);
            }
            scene.AddEntity("GasStation", gasStation);
        }

        public Vector3 GetPosition()
        {
            return gasStationTransform.GetPosition();
        }
    }
}
