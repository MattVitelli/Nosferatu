using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
using Gaia.Core;
namespace Gaia.Rendering
{
    public class DebugElementManager : RenderElementManager
    {
        List<VertexPositionColor> vertexList = new List<VertexPositionColor>();
        Shader debugShader;
        public DebugElementManager(RenderView renderView) : base(renderView) 
        {
            debugShader = ResourceManager.Inst.GetShader("DebugShader");
        }

        public void AddElements(VertexPositionColor[] verts)
        {
            if (vertexList.Count > 0)
            {
                Vector3 v = vertexList[vertexList.Count - 1].Position;
                vertexList.Add(new VertexPositionColor(v, new Color(0, 0, 0, 0)));
                vertexList.Add(new VertexPositionColor(verts[0].Position, new Color(0, 0, 0, 0)));
            }
            vertexList.AddRange(verts);
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;

            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PCDec;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjection());
            debugShader.SetupShader();
            GFX.Device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineStrip,
                    vertexList.ToArray(), 0, vertexList.Count - 1);

            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.AlphaBlendEnable = false;
            GFX.Inst.ResetState();
            vertexList.Clear();
        }
    }
}
