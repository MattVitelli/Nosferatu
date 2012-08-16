using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.SceneGraph;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
using Gaia.Sound;

namespace Gaia.Game
{
    public class SoundMaster 
    {
        Scene scene;
        SoundEffect[] ambientSounds;
        SoundEffect[] exploreSounds;
        SoundEffect[] tensionSounds;
        SoundEffect[] dangerSounds;

        public SoundMaster(Scene scene)
        {
            this.scene = scene;

        }
    }
}
