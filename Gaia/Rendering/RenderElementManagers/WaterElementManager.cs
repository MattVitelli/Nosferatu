using System;
using System.Collections.Generic;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.Rendering
{
    public class WaterElementManager : RenderElementManager
    {
        Shader waterShader;
        Shader generic2DShader;
        TextureResource noiseTexture;

        Vector4[] bumpCoords;
        Matrix waterMatrix;

        public WaterElementManager(RenderView view) : base(view)
        {
            waterShader = ResourceManager.Inst.GetShader("WaterShader");
            generic2DShader = ResourceManager.Inst.GetShader("Generic2D");
            noiseTexture = ResourceManager.Inst.GetTexture("Textures/Water/noise02.dds");
            bumpCoords = new Vector4[] 
            {
                new Vector4(0.264000f,0.178000f, 0.2f, 0.1f),
                new Vector4(-2.06840f, -1.52640f, -1.0f, 0.23f),
                new Vector4(1.87920f, 1.9232f, 0.2f, 0.15f),
                new Vector4(0.096000f, 0.04f, -0.3f, 0.1f),
            };
            waterMatrix = Matrix.CreateScale(Vector3.One * 4096)*Matrix.CreateRotationX(MathHelper.Pi);
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
            generic2DShader.SetupShader();
            GFXPrimitives.Quad.Render();


            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            //GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPOS, mainRenderView.scene.GetMainLightDirection());
            GFX.Device.SetPixelShaderConstant(0, bumpCoords);
            GFX.Device.Textures[3] = noiseTexture.GetTexture();
            
            GFX.Inst.SetTextureFilter(3, TextureFilter.Anisotropic);
            GFX.Inst.SetTextureAddressMode(3, TextureAddressMode.Wrap);
            GFX.Inst.SetTextureFilter(2, TextureFilter.Linear);
            GFX.Inst.SetTextureAddressMode(2, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Point);
            GFX.Inst.SetTextureAddressMode(1, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Clamp);

            //GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_TEXGEN, GFX.Inst.ComputeTextureMatrix(GFX.Inst.DisplayRes));
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, waterMatrix);
            waterShader.SetupShader();

            GFXPrimitives.Decal.Render();
            
        }
    }
}
