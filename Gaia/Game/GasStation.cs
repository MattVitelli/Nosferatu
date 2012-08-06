using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;

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

            scene.AddEntity("GasStation", gasStation);
        }

        public Vector3 GetPosition()
        {
            return gasStationTransform.GetPosition();
        }
    }
}
