using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering.Geometry;
using Gaia.Core;

namespace Gaia.Rendering
{
    public class PostProcessElementManager : RenderElementManager
    {
        Shader basicImageShader;
        Shader compositeShader;
        Shader fogShader;
        Shader motionBlurShader;
        Shader colorCorrectShader;
        Shader godRayShader;
        Shader gaussBlurHShader;
        Shader gaussBlurVShader;

        Shader boxBlurHShader;
        Shader boxBlurVShader;
        Shader bloomShader;
        Shader debugNormalsShader;

        TextureResource colorCorrectTexture;

        Shader underwaterShader;
        TextureResource underwaterRamp;

        TextureResource oceanTexture;
        Shader oceanShader;
        Shader oceanScreenSpaceShader;
        ProjectiveOcean oceanGeometry;
        Vector4[] bumpCoords;
        Vector4[] waveDirs;
        Vector4[] waveAmplitudes;
        
        Matrix prevViewProjection = Matrix.Identity; //Used for motion blur

        MainRenderView mainRenderView; //Used to access GBuffer

        float bloomBias = 0.1492f;
        float bloomMultiplier = 3.75f;

        float waterScale = 8;
        float waveScale = 3;
        BoundingBox waveMeshBounds;
        Matrix waterMatrix = Matrix.CreateScale(Vector3.One * 4096)*Matrix.CreateRotationX(MathHelper.Pi);
        Shader waterShader;


        public Vector3 BlurTarget;

        public Vector3 ForwardVec;

        float blinkTime = 0;

        public void SetBlinkTime(float value)
        {
            blinkTime = value;
        }

        public PostProcessElementManager(MainRenderView renderView)
            : base(renderView)
        {
            mainRenderView = renderView;
            waterShader = ResourceManager.Inst.GetShader("WaterShader");
            basicImageShader = ResourceManager.Inst.GetShader("Generic2D");
            debugNormalsShader = ResourceManager.Inst.GetShader("DebugNormals");
            motionBlurShader = ResourceManager.Inst.GetShader("MotionBlur");
            compositeShader = ResourceManager.Inst.GetShader("Composite");
            fogShader = ResourceManager.Inst.GetShader("Fog");
            colorCorrectShader = ResourceManager.Inst.GetShader("ColorCorrect");
            colorCorrectTexture = ResourceManager.Inst.GetTexture("Textures/Color Correction/colorRamp0.dds");
            godRayShader = ResourceManager.Inst.GetShader("GodRay");
            bloomShader = ResourceManager.Inst.GetShader("Bloom");
            gaussBlurHShader = ResourceManager.Inst.GetShader("GaussH");
            gaussBlurVShader = ResourceManager.Inst.GetShader("GaussV");
            boxBlurHShader = ResourceManager.Inst.GetShader("BoxH");
            boxBlurVShader = ResourceManager.Inst.GetShader("BoxV");

            underwaterRamp = ResourceManager.Inst.GetTexture("Textures/Water/underwater_color.dds");
            underwaterShader = ResourceManager.Inst.GetShader("UnderwaterShader");

            oceanShader = ResourceManager.Inst.GetShader("Ocean");
            oceanScreenSpaceShader = ResourceManager.Inst.GetShader("OceanPP");
            oceanTexture = ResourceManager.Inst.GetTexture("Textures/Water/noise02.dds");
            oceanGeometry = new ProjectiveOcean(64);
            bumpCoords = new Vector4[] 
            {
                new Vector4(0.264000f,0.178000f, 0.2f, 0.1f),
                new Vector4(-2.06840f, -1.52640f, -1.0f, 0.23f),
                new Vector4(1.87920f, 1.9232f, 0.2f, 0.15f),
                new Vector4(0.096000f, 0.04f, -0.3f, 0.1f),
            };
            waveDirs = new Vector4[]
            {
                new Vector4(-0.66f, -0.208f, 0.2f, MathHelper.TwoPi/0.4f),
                new Vector4(-1.62f, 0.1708f, 0.38f, MathHelper.TwoPi/2.2f),
                new Vector4(-2.16f, 1.338f, 0.28f, MathHelper.TwoPi/1.0f),
                new Vector4(-2.72f, 8.6108f, 0.15f, MathHelper.TwoPi/4.8f),
            };
            float ratio = waveScale / waterScale;
            waveAmplitudes = new Vector4[]
            {
                ratio*Vector4.One*0.8f,
                ratio*Vector4.One*0.4f,
                ratio*Vector4.One*0.4f,
                ratio*Vector4.One*0.8f,
            };
            
        }

        void RenderComposite()
        {
            //GFX.Device.Clear(Color.TransparentBlack);
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            
            compositeShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
            GFX.Device.Textures[0] = mainRenderView.ColorMap.GetTexture();
            GFX.Device.Textures[1] = mainRenderView.LightMap.GetTexture();

            GFXPrimitives.Quad.Render();
        }

        void RenderCompositeFirstPerson()
        {
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
            GFX.Device.Textures[0] = mainRenderView.LightMap.GetTexture();
            mainRenderView.RenderFirstPerson();
        }

        public enum BlurFilter
        {
            Box,
            Gauss,
            Bilateral
        };
        public void BlurRenderTarget(RenderTarget2D target, BlurFilter filterType, int blurPasses)
        {
            Vector2 invRes = Vector2.One / new Vector2(target.Width, target.Height);
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, invRes);
            GFX.Device.SetPixelShaderConstant(1, Vector4.One * 0.015f);
            GFX.Device.SetPixelShaderConstant(0, invRes);

            Shader activeHShader = boxBlurHShader;
            Shader activeVShader = boxBlurVShader;
            switch (filterType)
            {
                case BlurFilter.Gauss:
                    activeHShader = gaussBlurHShader;
                    activeVShader = gaussBlurVShader;
                    break;
            }

            for (int i = 0; i < blurPasses; i++)
            {
                GFX.Device.Textures[0] = target.GetTexture();
                GFX.Device.SetRenderTarget(0, target);
                activeHShader.SetupShader();
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);

                GFX.Device.Textures[0] = target.GetTexture();

                GFX.Device.SetRenderTarget(0, target);
                activeVShader.SetupShader();
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
            }
        }

        public void DownsampleGlow(RenderTarget2D colorMap, RenderTarget2D lightMap, RenderTarget2D dataMap, RenderTarget2D emissiveMap)
        {
            bloomShader.SetupShader();
            for(int i = 0; i <= 2; i++)
                GFX.Inst.SetTextureFilter(i, TextureFilter.Linear);
            GFX.Device.Textures[0] = colorMap.GetTexture();
            GFX.Device.Textures[1] = lightMap.GetTexture();
            GFX.Device.Textures[2] = dataMap.GetTexture();
            Vector2 invRes = Vector2.One / new Vector2(colorMap.Width, colorMap.Height);
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, invRes);
            GFX.Device.SetPixelShaderConstant(0, new Vector2(bloomBias, bloomMultiplier));
            GFX.Device.SetRenderTarget(0, emissiveMap);
            GFX.Device.Clear(Color.TransparentBlack);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);

            BlurRenderTarget(emissiveMap, BlurFilter.Box, 4);
        }

        void RenderBloom()
        {
            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;
            basicImageShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(mainRenderView.EmissiveMap.Width, mainRenderView.EmissiveMap.Height));
            GFX.Device.Textures[0] = mainRenderView.EmissiveMap.GetTexture();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFXPrimitives.Quad.Render();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Point);
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
        }

        void RenderCompositeParticles()
        {
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            basicImageShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(mainRenderView.ParticleBuffer.Width, mainRenderView.ParticleBuffer.Height));
            GFX.Device.Textures[0] = mainRenderView.ParticleBuffer.GetTexture();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFXPrimitives.Quad.Render();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Point);
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
            float fogStart = farplane*0.14f;
            float fogEnd = farplane*0.3f;
            float skyStart = farplane*0.6f;
            GFX.Device.SetPixelShaderConstant(0, new Vector4(fogStart, fogEnd, fogEnd, skyStart)); //Fog parameters 
            //Vector4 fogColor = (float)-Math.Log(2)*Vector4.One / new Vector4(0.0549f, 0.4534f, 0.8512f, 1.0f);
            float lerpAmount = (float)Math.Pow(MathHelper.Clamp(-mainRenderView.scene.GetMainLightDirection().Y+0.075f, 0.0f, 1.0f),0.15f);
            Vector4 fogColor = Vector4.Lerp(new Vector4(0.0960f, 0.3888f, 0.6280f, 1.0f), new Vector4(Vector3.One*0.25f,1), lerpAmount);
            GFX.Device.SetPixelShaderConstant(1, fogColor);
            GFXPrimitives.Quad.Render();
            GFX.Inst.SetTextureFilter(1, TextureFilter.Point);
        }

        void RenderUnderwater()
        {
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);

            underwaterShader.SetupShader();
            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = mainRenderView.DepthMap.GetTexture();
            GFX.Device.Textures[2] = underwaterRamp.GetTexture();
            GFXPrimitives.Quad.Render();
        }

        void RenderMotionBlur()
        {
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);

            motionBlurShader.SetupShader();

            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Clamp);
            //mainRenderView.planarReflection.ReflectionMap.GetTexture();//
            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = mainRenderView.DepthMap.GetTexture();
            GFX.Device.SetPixelShaderConstant(0, mainRenderView.GetViewProjection());
            GFX.Device.SetPixelShaderConstant(4, prevViewProjection);


            Vector3 posToVec = BlurTarget - renderView.GetPosition();
            float len = posToVec.Length();
            posToVec /= len;
            float blurCoeff = MathHelper.Clamp(Vector3.Dot(posToVec, ForwardVec)-0.35f, 0.0f, 1.0f);
                
            blurCoeff *= (float)Math.Pow(MathHelper.Clamp(1.0f-len/40.0f,0.0f,1.0f),0.5f);
            
            Vector4 transformedVec = Vector4.Transform(new Vector4(BlurTarget, 1.0f), renderView.GetViewProjection());
            transformedVec /= transformedVec.W;
            transformedVec.Y *= -1;
            transformedVec = (transformedVec + Vector4.One) * 0.5f;
            
            Vector3 blurVector = new Vector3(transformedVec.X, transformedVec.Y, blurCoeff);
            //float blinkAnim = MathHelper.Clamp((float)Math.Cos(Time.RenderTime.TotalTime * 1.20458) * 0.5f + 0.5f, 0.0f, 1.0f);
            //blinkAnim = MathHelper.Clamp((float)Math.Pow(blinkAnim, 0.25) * 2.5f - 0.7f, 0.0f, 1.0f);
            Vector2 ellipse = new Vector2(2.07846f, 1.06f) * new Vector2(Math.Max(blinkTime, 0.7f), blinkTime);

            GFX.Device.SetPixelShaderConstant(10, blurVector);
            GFX.Device.SetPixelShaderConstant(11, ellipse);

            GFXPrimitives.Quad.Render();
            prevViewProjection = mainRenderView.GetViewProjection();
        }

        void RenderColorCorrection()
        {
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);

            colorCorrectShader.SetupShader();
            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = colorCorrectTexture.GetTexture();

            GFXPrimitives.Quad.Render();
        }

        void RenderNormals()
        {
            debugNormalsShader.SetupShader();
            GFX.Device.Textures[0] = mainRenderView.NormalMap.GetTexture();

            GFXPrimitives.Quad.Render();
        }

        void RenderGodRays()
        {
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);
            
            godRayShader.SetupShader();
            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = mainRenderView.DepthMap.GetTexture();
            Vector4 lightVec = new Vector4(mainRenderView.scene.MainLight.Transformation.GetPosition(), 1);
            Vector4 ssSunPos = Vector4.Transform(lightVec, mainRenderView.GetViewProjectionLocal());
            ssSunPos /= ssSunPos.W;
            ssSunPos = ssSunPos*0.5f+Vector4.One*0.5f;
            GFX.Device.SetPixelShaderConstant(0, ssSunPos);
            GFX.Device.SetPixelShaderConstant(1, new Vector3(1.0f, 0.36f, 0.7f));
            GFX.Device.SetPixelShaderConstant(2, Vector3.One);

            GFXPrimitives.Quad.Render();
        }

        void RenderOcean()
        {
            
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);
            GFX.Device.Clear(Color.TransparentBlack);

            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPOS, mainRenderView.scene.GetMainLightDirection());
            GFX.Device.SetPixelShaderConstant(0, bumpCoords);
            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = mainRenderView.DepthMap.GetTexture();
            GFX.Device.Textures[2] = mainRenderView.planarReflection.ReflectionMap.GetTexture();
            GFX.Device.Textures[3] = oceanTexture.GetTexture();
            GFX.Inst.SetTextureFilter(2, TextureFilter.Linear);
            GFX.Inst.SetTextureAddressMode(3, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(3, TextureFilter.Anisotropic);
            GFX.Inst.SetTextureAddressMode(2, TextureAddressMode.Wrap);
            GFX.Inst.SetTextureAddressMode(1, TextureAddressMode.Wrap);

            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;
            
            basicImageShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
            GFX.Device.SetPixelShaderConstant(4, Vector4.One * waterScale);
            GFXPrimitives.Quad.Render();
            GFX.Device.RenderState.AlphaBlendEnable = false;

            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = mainRenderView.depthStencilScene;
            GFX.Device.RenderState.DepthBufferEnable = true;

            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPOS, mainRenderView.scene.GetMainLightDirection());
            GFX.Device.SetPixelShaderConstant(0, bumpCoords);
            GFX.Device.Textures[3] = oceanTexture.GetTexture();

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
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetViewProjection());
            GFXPrimitives.Decal.Render();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetInverseViewProjectionLocal());
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.DepthStencilBuffer = dsOld;
            GFX.Device.RenderState.AlphaBlendEnable = true;
            /*
            oceanScreenSpaceShader.SetupShader();
            GFXPrimitives.Quad.Render();

            float dist = mainRenderView.GetPosition().Y;
            if (mainRenderView.GetFrustum().Contains(waveMeshBounds) != ContainmentType.Disjoint && dist <= waterScale)
            {
                oceanShader.SetupShader();
                GFX.Device.SetVertexShaderConstant(4, waveDirs);
                GFX.Device.SetVertexShaderConstant(33, Vector4.One * waterScale);
                GFX.Device.SetVertexShaderConstant(34, waveAmplitudes);
                Matrix worldMatrix = Matrix.CreateScale(waterScale);
                worldMatrix.Translation = new Vector3(1, 0, 1) * mainRenderView.GetPosition();

                waveMeshBounds.Min = Vector3.Transform(new Vector3(-1, 0, -1), worldMatrix);
                waveMeshBounds.Max = Vector3.Transform(new Vector3(1, 0, 1), worldMatrix);
                

                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, worldMatrix);
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetViewProjection());
                GFX.Device.RenderState.DepthBufferEnable = true;
                GFX.Device.RenderState.DepthBufferWriteEnable = false;
                GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                
                    oceanGeometry.Render();
                
                GFX.Device.RenderState.DepthBufferEnable = false;
                GFX.Device.RenderState.CullMode = CullMode.None;

                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetInverseViewProjectionLocal());
            }*/
            GFX.Inst.SetTextureFilter(3, TextureFilter.Point);
            GFX.Inst.SetTextureAddressMode(3, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(2, TextureFilter.Point);
            GFX.Inst.SetTextureAddressMode(2, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureAddressMode(1, TextureAddressMode.Clamp);
        }

        SortedList<Material, Queue<RenderElement>> Elements = new SortedList<Material, Queue<RenderElement>>();
        Matrix[] tempTransforms = new Matrix[GFXShaderConstants.NUM_INSTANCES];

        public void AddElement(Material material, RenderElement element)
        {
            return;
            if (!Elements.ContainsKey(material))
                Elements.Add(material, new Queue<RenderElement>());
            Elements[material].Enqueue(element);
        }

        void DrawElement(Material key)
        {
            /*
            while (Elements[key].Count > 0)
            {
                RenderElement currElem = Elements[key].Dequeue();
                if (currElem.VertexDec != GFX.Device.VertexDeclaration)
                    GFX.Device.VertexDeclaration = currElem.VertexDec;
                GFX.Device.Indices = currElem.IndexBuffer;
                GFX.Device.Vertices[0].SetSource(currElem.VertexBuffer, 0, currElem.VertexStride);
                for (int j = 0; j < currElem.Transform.Length; j += GFXShaderConstants.NUM_INSTANCES)
                {
                    int binLength = currElem.Transform.Length - j;

                    if (binLength > GFXShaderConstants.NUM_INSTANCES)
                        binLength = GFXShaderConstants.NUM_INSTANCES;
                    
                    if (currElem.Transform.Length > 1)
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
            }*/
        }

        void RenderRefractive()
        {
            GFX.Device.ResolveBackBuffer(mainRenderView.BackBufferTexture);
            GFX.Device.Clear(Color.TransparentBlack);

            GFX.Device.Textures[0] = mainRenderView.BackBufferTexture;
            GFX.Device.Textures[1] = mainRenderView.DepthMap.GetTexture();
            //GFX.Device.Textures[2] = mainRenderView.CubeMap.GetTexture();
            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureAddressMode(1, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureAddressMode(2, TextureAddressMode.Wrap);

            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;

            basicImageShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
            GFXPrimitives.Quad.Render();


            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, this.renderView.GetViewProjection());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.VC_EYEPOS, this.renderView.GetEyePosShader());

            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.Less;

            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();

                DrawElement(key);
            }

            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.CullMode = CullMode.None;

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetInverseViewProjectionLocal());
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
            
            RenderRefractive();

            RenderBloom();

            RenderFog();

            RenderOcean();

            RenderCompositeParticles();

            //RenderGodRays();

            GFX.Device.RenderState.AlphaBlendEnable = false;

            if (mainRenderView.GetPosition().Y < 0.0)
                RenderUnderwater();

            RenderMotionBlur();

            RenderColorCorrection();

            

            //RenderNormals();

            //RenderCompositeFirstPerson();

            

            GFX.Inst.ResetState();
        }
    }
}
