using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Resources;
using Gaia.Voxels;

namespace Gaia.Game
{
    public class AIDirector : Entity
    {
        const float TIME_TIL_NEXT_WAVE = 30; //number of seconds before next wave
        int MAX_ENEMIES = 5;
        float timeTilWave = TIME_TIL_NEXT_WAVE;
        DinosaurDatablock raptorDatablock;
        List<Raptor> activeDinosaurs = new List<Raptor>();
        List<Raptor> dinosaursToPrune = new List<Raptor>();
        int aliveDinosaurCount = 0;

        public int GetActiveDinosaurCount()
        {
            return aliveDinosaurCount;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            raptorDatablock = ResourceManager.Inst.GetDinosaurDatablock("AlphaRaptor");
        }

        public void PruneAllDinosaurs()
        {
            for (int i = 0; i < activeDinosaurs.Count; i++)
            {
                scene.RemoveEntity(activeDinosaurs[i]);
            }
            activeDinosaurs.Clear();
        }

        void SpawnDinosaurs()
        {
            double difficulty = 0.15 * Math.Pow(scene.MainPlayer.GetHealthPercent(), 0.25);
            double nightTerm = 0.35 * Math.Min(0.0, Math.Max(1.0, -scene.MainLight.Transformation.GetPosition().Y));
            double spawnProbability = difficulty + nightTerm + 0.6 * RandomHelper.RandomGen.NextDouble();
            int spawnCount = Math.Max(1, (int)spawnProbability * MAX_ENEMIES);
            Vector3 playerPosition = scene.MainPlayer.Transformation.GetPosition();
            Vector3 fwd = scene.MainPlayer.GetForwardVector();
            Vector3 right = scene.MainPlayer.GetRightVector();
            Vector3 minVector = playerPosition - right - fwd*100 - Vector3.Up*50;
            Vector3 maxVector = playerPosition + right - fwd*15 + Vector3.Up*50;
            BoundingBox spawnRegion = new BoundingBox(Vector3.Min(minVector, maxVector), Vector3.Max(minVector, maxVector));
            List<TriangleGraph> availableTriangles = null;
            scene.MainTerrain.GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, spawnRegion);
            if (availableTriangles.Count > 0)
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    Raptor raptor = new Raptor(raptorDatablock);
                    Vector3 spawnPos = availableTriangles[RandomHelper.RandomGen.Next(availableTriangles.Count)].Centroid + Vector3.Up * 3.0f;
                    raptor.SetSpawnPosition(spawnPos);
                    scene.AddEntity("Raptor", raptor);
                    activeDinosaurs.Add(raptor);
                }
            }
        }

        void SpawnWave()
        {
            double difficulty = 0.15*Math.Pow(scene.MainPlayer.GetHealthPercent(), 0.25);
            double nightTerm = 0.1*Math.Min(0.0,Math.Max(1.0,-scene.MainLight.Transformation.GetPosition().Y));
            double spawnProbability = difficulty + nightTerm + 0.85*RandomHelper.RandomGen.NextDouble();
            if (spawnProbability > 0.5f && !PlayerScreen.GetInst().IsSafe)
            {
                SpawnDinosaurs();
            }
        }

        void HandleDinosaurCleanup()
        {
            aliveDinosaurCount = 0;
            for (int i = 0; i < activeDinosaurs.Count; i++)
            {
                float dist = Vector3.Distance(activeDinosaurs[i].Transformation.GetPosition(), scene.MainPlayer.Transformation.GetPosition());
                bool isDead = (activeDinosaurs[i].IsDead() && !activeDinosaurs[i].IsVisible() && (activeDinosaurs[i].GetDeathTime() > 15.0f));
                bool isFar = ((dist > 300.0f) && !activeDinosaurs[i].IsVisible());
                if (!activeDinosaurs[i].IsDead())
                    aliveDinosaurCount += 1;
                if (isDead || isFar)
                    dinosaursToPrune.Add(activeDinosaurs[i]);
            }
            for (int i = 0; i < dinosaursToPrune.Count; i++)
            {
                scene.RemoveEntity(dinosaursToPrune[i]);
                int index = activeDinosaurs.IndexOf(dinosaursToPrune[i]);
                activeDinosaurs.RemoveAt(index);
            }
            if (dinosaursToPrune.Count > 0)
                dinosaursToPrune.Clear();
        }

        public override void OnUpdate()
        {
            HandleDinosaurCleanup();  
            timeTilWave -= Time.GameTime.ElapsedTime;
            if (timeTilWave <= 0.0f && aliveDinosaurCount < 2)
            {
                SpawnWave();
                timeTilWave = TIME_TIL_NEXT_WAVE;
            }
        }
    }
}
