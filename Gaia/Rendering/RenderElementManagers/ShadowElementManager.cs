using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering.RenderViews;
using Gaia.Resources;
namespace Gaia.Rendering
{
    public class ShadowElementManager : SceneElementManager
    {
        Shader shadowShader;
        Shader shadowShaderInst;

        Shader activeShader = null;

        public ShadowElementManager(RenderView renderView)
            : base(renderView)
        {
            shadowShader = ResourceManager.Inst.GetShader("ShadowVSM");
            shadowShaderInst = ResourceManager.Inst.GetShader("ShadowVSMInst");
        }

        public void SetShaders(Shader defaultShader, Shader instanceShader)
        {
            shadowShader = defaultShader;
            shadowShaderInst = instanceShader;
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.Less;

            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];
                if (key.IsFoliage)
                {
                    key.SetupTextures();
                }
                for(int j = 0; j < Elements[key].Count; j++)
                {
                    if (Elements[key][j].Transform.Length > 1 || Elements[key][j].IsAnimated)
                    {
                        if (activeShader != shadowShaderInst)
                            shadowShaderInst.SetupShader();
                        activeShader = shadowShaderInst;
                    }
                    else
                    {
                        if (activeShader != shadowShader)
                            shadowShader.SetupShader();
                        activeShader = shadowShader;
                    }
                    DrawElement(Elements[key][j]);
                }
                Elements[key].Clear();
            }

            activeShader = null;
        }
    }
}
