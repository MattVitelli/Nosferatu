using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public class BlendModelResource : IResource
    {
        string name = "";
        public string Name { get { return name; } }

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            for (int i = 0; i < node.Attributes.Count; i++)
            {
                XmlAttribute currAttrib = node.Attributes[i];
                switch (currAttrib.Name.ToLower())
                {
                    case "basemesh":

                        break;
                    case "blend":

                        break;
                }
            }
        }
    }
}
