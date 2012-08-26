using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering.Geometry;

namespace Gaia.Rendering
{
    public class PostProcessReflectionsElementManager : RenderElementManager
    {
        Shader basicImageShader;
        Shader compositeShader;
        Shader fogShader;
        
        SceneRenderView mainRenderView; //Used to access GBuffer

        public PostProcessReflectionsElementManager(SceneRenderView renderView)
            : base(renderView)
        {
            mainRenderView = renderView;
            basicImageShader = ResourceManager.Inst.GetShader("Generic2D");
            compositeShader = ResourceManager.Inst.GetShader("Composite");
            fogShader = ResourceManager.Inst.GetShader("Fog");
        }

        void RenderComposite()
        {
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            
            
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / mainRenderView.GetResolution());
            GFX.Device.Textures[0] = mainRenderView.ColorMap.GetTexture();
            if (mainRenderView.PerformFullShading())
            {
                compositeShader.SetupShader();
                GFX.Device.Textures[1] = mainRenderView.LightMap.GetTexture();
                GFX.Device.Textures[2] = mainRenderView.DepthMap.GetTexture();
            }
            else
            {
                basicImageShader.SetupShader();
            }

            GFXPrimitives.Quad.Render();

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
        }

        void RenderFog()
        {
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            fogShader.SetupShader();
            GFX.Device.Textures[0] = mainRenderView.DepthMap.GetTexture();
            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Linear);
            GFX.Device.Textures[1] = mainRenderView.GetSkyTexture();

            float farplane = renderView.GetFarPlane();
            float fogStart = farplane * 0.14f;
            float fogEnd = farplane * 0.3f;
            float skyStart = farplane * 0.6f;
            GFX.Device.SetPixelShaderConstant(0, new Vector4(fogStart, fogEnd, fogEnd, skyStart)); //Fog parameters 
            //Vector4 fogColor = (float)-Math.Log(2)*Vector4.One / new Vector4(0.0549f, 0.4534f, 0.8512f, 1.0f);
            Vector4 fogColor = new Vector4(0.0960f, 0.3888f, 0.6280f, 1.0f);
            GFX.Device.SetPixelShaderConstant(1, fogColor);
            GFXPrimitives.Quad.Render();
            GFX.Inst.SetTextureFilter(1, TextureFilter.Point);
        }

        public override void Render()
        {
            for(int i = 0; i < 4; i++)
            {
                GFX.Inst.SetTextureFilter(i, TextureFilter.Point);
                GFX.Inst.SetTextureAddressMode(i, TextureAddressMode.Clamp);
            }
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.AlphaBlendEnable = true;

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetInverseViewProjectionLocal());
            
            RenderComposite();

            RenderFog();

            GFX.Device.RenderState.AlphaBlendEnable = false;

            GFX.Inst.ResetState();
        }
    }
}
