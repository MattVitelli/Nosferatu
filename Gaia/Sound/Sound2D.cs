using System;
using System.Collections.Generic;
using IrrKlang;

using Gaia.Resources;

namespace Gaia.Sound
{
    public class Sound2D
    {
        protected ISound sound;
        protected bool loop = false;
        protected bool paused = false;

        public ISound Sound
        {
            get { return sound; }
        }

        public bool Paused
        {
            set
            {
                paused = value;
                if (sound != null)
                    sound.Paused = paused;
            }
            get { return paused; }
        }

        public bool Looped
        {
            set
            {
                loop = value;
                if (sound != null)
                    sound.Looped = loop;
            }
            get { return loop; }
        }

        public int PlayPosition
        {
            set
            {
                sound.PlayPosition = (uint)value;
            }
            get { return (int)sound.PlayPosition; }
        }

        public Sound2D()
        {
        }

        public Sound2D(string soundName, bool looped, bool paused)
        {
            this.loop = looped;
            this.paused = paused;
            SoundEffect soundEffect = ResourceManager.Inst.GetSoundEffect(soundName);
            this.sound = SoundEngine.Device.Play2D(soundEffect.Sound, loop, paused, false);
        }
    }
}
