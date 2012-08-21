using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;
namespace Gaia.Rendering
{
    public class LightElementManager : RenderElementManager
    {
        public CustomList<Light> AmbientLights = new CustomList<Light>();
        public CustomList<Light> DirectionalLights = new CustomList<Light>();
        public CustomList<Light> DirectionalShadowLights = new CustomList<Light>();
        public CustomList<Light> PointLights = new CustomList<Light>();
        public CustomList<Light> SpotLights = new CustomList<Light>();

        Shader ambientLightShader;
        Shader directionalLightShader;
        Shader directionalLightShadowsShader;
        Shader pointLightShader;
        Shader spotLightShader;

        public LightElementManager(RenderView renderView)
            : base(renderView)
        {
            ambientLightShader = ResourceManager.Inst.GetShader("AmbientLightShader");
            directionalLightShader = ResourceManager.Inst.GetShader("DirectionalLightShader");
            directionalLightShadowsShader = ResourceManager.Inst.GetShader("DirectionalLightShadowShader");
            pointLightShader = ResourceManager.Inst.GetShader("PointLightShader");
            spotLightShader = ResourceManager.Inst.GetShader("PointLightShader");
        }

        void SetupLightParameters(Light light)
        {
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPOS, light.Transformation.GetPosition());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCOLOR, light.Color);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPARAMS, light.Parameters);
        }

        public override void Render()
        {

            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.AlphaBlendOperation = BlendFunction.Add;
            GFX.Device.RenderState.AlphaSourceBlend = Blend.One;
            GFX.Device.RenderState.AlphaDestinationBlend = Blend.One;
            GFX.Device.RenderState.BlendFunction = BlendFunction.Add;
            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;
            GFX.Device.RenderState.DepthBufferEnable = false;

            GFX.Device.RenderState.CullMode = CullMode.None;

            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PDec;   

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjectionLocal());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, Matrix.Identity);

            ambientLightShader.SetupShader();

            for(int i = 0; i < AmbientLights.Count; i++)
            {
                Light currLight = AmbientLights[i];
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCOLOR, currLight.Color);
                GFXPrimitives.Cube.Render();
            }


            directionalLightShader.SetupShader();

            for(int i = 0; i < DirectionalLights.Count; i++)
            {
                Light currLight = DirectionalLights[i];
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            directionalLightShadowsShader.SetupShader();
            GFX.Inst.SetTextureFilter(3, TextureFilter.Linear);

            GFX.Device.SetPixelShaderConstant(0, renderView.GetView());
            for(int i = 0; i < DirectionalShadowLights.Count; i++)
            {
                Light currLight = DirectionalShadowLights[i];
                SetupLightParameters(currLight);
                Texture2D shadowMap = currLight.GetShadowMap();
                GFX.Device.Textures[3] = shadowMap;
                
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTMODELVIEW, currLight.GetModelViews());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCLIPPLANE, currLight.GetClipPlanes());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCLIPPOS, currLight.GetClipPositions());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_INVSHADOWRES, Vector2.One / new Vector2(shadowMap.Width, shadowMap.Height));
                GFXPrimitives.Cube.Render();
            }

            GFX.Device.RenderState.CullMode = CullMode.CullClockwiseFace;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjection());

            pointLightShader.SetupShader();
            for(int i = 0; i < PointLights.Count; i++)
            {
                Light currLight = PointLights[i];
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currLight.Transformation.GetTransform());
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            spotLightShader.SetupShader();
            for(int i = 0; i < SpotLights.Count; i++)
            {
                Light currLight = SpotLights[i];
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currLight.Transformation.GetTransform());
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            AmbientLights.Clear();
            DirectionalLights.Clear();
            DirectionalShadowLights.Clear();
            PointLights.Clear();
            SpotLights.Clear();

            GFX.Inst.ResetState();
        }
    }
}
