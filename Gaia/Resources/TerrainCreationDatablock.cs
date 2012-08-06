using System;
using System.Collections.Generic;
using System.Xml;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.Resources
{
    public enum RotationParameters
    {
        Fixed,
        FourAngles,
        Random,
        Gradient
    }
    
    public class TerrainCreationParameters
    {
        public Vector2 Scale;
        public Vector2 HeightRange;
        public RotationParameters Rotation = RotationParameters.Fixed;
        public TextureResource Mask;
        public float Intensity = 1.0f;
        public float RepulsionFactor = 1.25f;
    }

    public class TerrainCreationDatablock : IResource
    {
        string name;

        public string Name { get { return name; } }

        const int MaxCreationParams = 16;
        public TerrainCreationParameters[] MapLocations = new TerrainCreationParameters[MaxCreationParams];

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('_');
                int index = (attribs.Length > 1) ? int.Parse(attribs[1]) : -1;
                if (index >= 0 && index < MapLocations.Length && MapLocations[index] == null)
                    MapLocations[index] = new TerrainCreationParameters();
                switch (attribs[0])
                {
                    case "name":
                        name = attrib.Value;
                        break;
                    case "scale":
                        MapLocations[index].Scale = ParseUtils.ParseVector2(attrib.Value);
                        break;
                    case "rotation":
                        switch (attrib.Value.ToLower())
                        {
                            case "fourangle":
                                MapLocations[index].Rotation = RotationParameters.FourAngles;
                                break;
                            case "random":
                                MapLocations[index].Rotation = RotationParameters.Random;
                                break;
                            case "gradient":
                                MapLocations[index].Rotation = RotationParameters.Gradient;
                                break;
                            default:
                                MapLocations[index].Rotation = RotationParameters.Fixed;
                                break;
                        }
                        break;
                    case "heightrange":
                        MapLocations[index].HeightRange = ParseUtils.ParseVector2(attrib.Value);
                        break;
                    case "intensity":
                        MapLocations[index].Intensity = float.Parse(attrib.Value);
                        break;
                    case "texture":
                        MapLocations[index].Mask = ResourceManager.Inst.GetTexture(attrib.Value);
                        break;
                    case "repulsion":
                        MapLocations[index].RepulsionFactor = float.Parse(attrib.Value);
                        break;
                }
            }
        }
    }
}
