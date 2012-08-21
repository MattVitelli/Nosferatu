using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.SceneGraph;

namespace Gaia.Game
{
    public class AIDirector : Entity
    {
        const float TIME_TIL_NEXT_WAVE = 60; //number of seconds before next wave
        const int MAX_ENEMIES = 5;
        float timeTilWave = TIME_TIL_NEXT_WAVE;

        void SpawnWave()
        {

        }

        public void OnUpdate()
        {
            timeTilWave -= Time.GameTime.ElapsedTime;
            if (timeTilWave <= 0.0f)
            {
                SpawnWave();
                timeTilWave = TIME_TIL_NEXT_WAVE;
            }
        }
    }
}
