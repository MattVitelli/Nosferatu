using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
using Gaia.Core;
namespace Gaia.Rendering
{
    public class SceneElementManager : RenderElementManager
    {
        protected SortedList<Material, CustomList<RenderElement>> Elements = new SortedList<Material, CustomList<RenderElement>>();
        protected Matrix[] tempTransforms = new Matrix[GFXShaderConstants.NUM_INSTANCES];

        public SceneElementManager(RenderView renderView) : base(renderView) { }

        public virtual void AddElement(Material material, RenderElement element)
        {
            if (!Elements.ContainsKey(material))
                Elements.Add(material, new CustomList<RenderElement>());
            Elements[material].Add(element);
        }

        protected void DrawElement(RenderElement currElem)
        {
            if (currElem.VertexDec != GFX.Device.VertexDeclaration)
                GFX.Device.VertexDeclaration = currElem.VertexDec;
            GFX.Device.Indices = currElem.IndexBuffer;
            GFX.Device.Vertices[0].SetSource(currElem.VertexBuffer, 0, currElem.VertexStride);

            for (int j = 0; j < currElem.Transform.Length; j += GFXShaderConstants.NUM_INSTANCES)
            {
                int binLength = currElem.Transform.Length - j;

                if (binLength > GFXShaderConstants.NUM_INSTANCES)
                    binLength = GFXShaderConstants.NUM_INSTANCES;

                if (currElem.IsAnimated)
                    binLength = 1;

                if (currElem.Transform.Length > 1 && !currElem.IsAnimated)
                {
                    // Upload transform matrices as shader constants.
                    Array.Copy(currElem.Transform, j, tempTransforms, 0, binLength);
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, tempTransforms);
                }
                else
                {
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currElem.Transform);
                }

                GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, currElem.StartVertex, currElem.VertexCount * binLength, 0, currElem.PrimitiveCount * binLength);
            }
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;

            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();

                for(int j = 0; j < Elements[key].Count; j++)
                {
                    DrawElement(Elements[key][j]);
                }
                Elements[key].Clear();
            }
        }
    }
}
