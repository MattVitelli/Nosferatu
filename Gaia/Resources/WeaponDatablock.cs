using System;
using System.Collections.Generic;
using System.Xml;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.Resources
{
    public enum WeaponAnimations
    {
        Idle=0,
        Draw,
        Stow,
        Fire,
        Reload,
        MissHit,
        Empty,
        Count
    };
    public class WeaponDatablock : IResource
    {
        string name;

        public string Name { get { return name; } }

        public float Damage = 1.0f;

        List<string>[] Animations = new List<string>[(int)WeaponAnimations.Count];

        List<string>[] SoundEffects = new List<string>[(int)WeaponAnimations.Count];

        SortedList<string, float> DelayTimes = new SortedList<string, float>();

        public string MeshName;

        public Vector3 Scale = Vector3.One;

        public Vector3 Rotation = Vector3.Zero;

        public Vector3 Position = Vector3.Zero;

        public float FireDistance = 60;

        public int AmmoPerClip = 15;

        public bool IsMelee = false;

        public int DefaultAmmo = 15;

        public bool IsManual = false;

        public Matrix CustomMatrix = Matrix.Identity;

        public float GetDelayTime(string animationName)
        {
            if (!DelayTimes.ContainsKey(animationName))
                return 0;

            return DelayTimes[animationName];
        }

        public string GetAnimation(WeaponAnimations anim)
        {
            int index = (int)anim;
            if(Animations[index].Count == 0)
                return string.Empty;
            if (Animations[index].Count == 1)
                return Animations[index][0];

            int randIndex = RandomHelper.RandomGen.Next(0, Animations[index].Count);
            return Animations[index][randIndex];
        }

        public string GetSoundEffect(WeaponAnimations anim)
        {
            int index = (int)anim;
            if (SoundEffects[index].Count == 0)
                return string.Empty;
            if (SoundEffects[index].Count == 1)
                return SoundEffects[index][0];

            int randIndex = RandomHelper.RandomGen.Next(0, SoundEffects[index].Count);
            return SoundEffects[index][randIndex];
        }

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('_');
                switch (attribs[0])
                {
                    case "name":
                        name = attrib.Value;
                        break;
                    case "damage":
                        Damage = float.Parse(attrib.Value);
                        break;
                    case "firedist":
                        FireDistance = float.Parse(attrib.Value);
                        break;
                    case "mesh":
                        MeshName = attrib.Value;
                        break; 
                    case "defaultammo":
                        DefaultAmmo = int.Parse(attrib.Value);
                        break;
                    case "clipsize":
                        AmmoPerClip = int.Parse(attrib.Value);
                        break;
                    case "ismelee":
                        IsMelee = bool.Parse(attrib.Value);
                        break;
                    case "ismanual":
                        IsManual = bool.Parse(attrib.Value);
                        break;
                    case "animation":
                        int index = int.Parse(attribs[1]);
                        if (Animations[index] == null)
                            Animations[index] = new List<string>();
                        Animations[index].Add(attrib.Value);
                        break;
                    case "sound":
                        int indexSound = int.Parse(attribs[1]);
                        if (SoundEffects[indexSound] == null)
                            SoundEffects[indexSound] = new List<string>();
                        SoundEffects[indexSound].Add(attrib.Value);
                        break;
                    case "delay":
                        string[] data = attrib.Value.Split(' ');
                        DelayTimes.Add(data[0], float.Parse(data[1]));
                        break;
                    case "scale":
                        Scale = ParseUtils.ParseVector3(attrib.Value);
                        break;
                    case "rotation":
                        Rotation = ParseUtils.ParseVector3(attrib.Value);
                        break;
                    case "position":
                        Position = ParseUtils.ParseVector3(attrib.Value);
                        break;
                }
            }
            CustomMatrix = Matrix.CreateScale(Scale) * Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
            CustomMatrix.Translation = Position;
        }
    }
}
