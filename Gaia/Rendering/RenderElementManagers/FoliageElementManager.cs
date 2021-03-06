﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
namespace Gaia.Rendering
{
    public class FoliageElementManager : SceneElementManager
    {

        Shader prepassAlphaShader;

        RenderTarget2D alphaBuffer;

        public FoliageElementManager(RenderView renderView) : base(renderView) 
        {
            prepassAlphaShader = ResourceManager.Inst.GetShader("AlphaPrepass");
        }

        public void RenderPrePass()
        {
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            GFX.Device.RenderState.AlphaTestEnable = true;
            GFX.Device.RenderState.AlphaFunction = CompareFunction.Greater;
            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.AlphaBlendOperation = BlendFunction.Max;
            GFX.Device.RenderState.BlendFunction = BlendFunction.Add;

            prepassAlphaShader.SetupShader();
            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                {
                    key.SetupTextures();
                    for(int j = 0; j < Elements[key].Count; j++)
                    {
                        DrawElement(Elements[key][j]);
                    }
                    Elements[key].Clear();
                }

            }

            GFX.Device.RenderState.AlphaTestEnable = false;
        }

        public override void Render()
        {
            /*
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            GFX.Device.RenderState.AlphaTestEnable = true;
            GFX.Device.RenderState.AlphaFunction = CompareFunction.GreaterEqual;
            GFX.Device.RenderState.ReferenceAlpha = 235;
            
            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();

                for(int j = 0; j < Elements[key].Count; j++)
                {
                    RenderElement currElem = Elements[key].Dequeue();
                    DrawElement(currElem);
                    Elements[key].Enqueue(currElem);
                }
                
            }
            */
            GFX.Device.RenderState.AlphaTestEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.AlphaFunction = CompareFunction.LessEqual;
            GFX.Device.RenderState.ReferenceAlpha = GFXShaderConstants.ALPHACUTOFF;
            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();

                for (int j = 0; j < Elements[key].Count; j++)
                {
                   DrawElement(Elements[key][j]);
                }
                Elements[key].Clear();
            }
            GFX.Device.RenderState.AlphaBlendEnable = false;
            GFX.Device.RenderState.AlphaTestEnable = false;
        }
    }
}
