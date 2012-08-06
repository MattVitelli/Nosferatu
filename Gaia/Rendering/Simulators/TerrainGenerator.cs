using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Core;

namespace Gaia.Rendering.Simulators
{
    public class TerrainGenerator
    {
        Shader histogramShader;
        Shader interpolateShader;
        Shader peakShader;
        Shader gradientShader;
        Shader terraceShader;
        Shader varianceShader;
        Shader basicShader;

        Texture2D whiteTexture;
        NoiseParameters noiseParams = new NoiseParameters(16, 0.49f, 0.018499f, 0.70999f);
        int terraceCount = 6;
        float terraceExp = 0.56f;

        int mountainRes = 128;

        PlacementParameters mountainPlacementParams = new PlacementParameters(6, 14, 1.25f, 1.7f, 0.7f, 3.6f, new Vector2(0.35f, 0.35f), new Vector2(0.8f, 0.8f));
        NoiseParameters mountainParams = new NoiseParameters(10, 1.95f, 0.0045f, 0.73f);

        PlacementParameters peakPlacementParams = new PlacementParameters(8, 17, 0.1f, 0.32f, 1, 2.5f, new Vector2(0.05f, 0.05f), new Vector2(0.23f, 0.23f));

        PlacementParameters plateauPlacementParams = new PlacementParameters(2, 8, 0.5f, 0.82f, 0.8f, 1.75f, new Vector2(0.15f, 0.15f), new Vector2(0.33f, 0.33f));
        
        TextureResource[] randomIslandTextures;

        TerrainCreationDatablock creationDatablock;

        bool texturesLoaded = false;

        public TerrainGenerator()
        {
            histogramShader = new Shader();
            histogramShader.CompileFromFiles("Shaders/Procedural/Histogram2D.hlsl", "Shaders/PostProcess/GenericV.hlsl");

            interpolateShader = new Shader();
            interpolateShader.CompileFromFiles("Shaders/Procedural/Interpolate2D.hlsl", "Shaders/Procedural/GenericTransformV.hlsl");

            peakShader = new Shader();
            peakShader.CompileFromFiles("Shaders/Procedural/PeakP.hlsl", "Shaders/Procedural/GenericTransformV.hlsl");

            gradientShader = new Shader();
            gradientShader.CompileFromFiles("Shaders/Procedural/Gradient2DP.hlsl", "Shaders/PostProcess/GenericV.hlsl");

            terraceShader = new Shader();
            terraceShader.CompileFromFiles("Shaders/Procedural/TerraceP.hlsl", "Shaders/PostProcess/GenericV.hlsl");

            basicShader = new Shader();
            basicShader.CompileFromFiles("Shaders/PostProcess/GenericP.hlsl", "Shaders/PostProcess/GenericV.hlsl");

            varianceShader = new Shader();
            varianceShader.CompileFromFiles("Shaders/Procedural/Variance2DP.hlsl", "Shaders/PostProcess/GenericV.hlsl");

            whiteTexture = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] whiteData = new Color[1] { Color.White };
            whiteTexture.SetData<Color>(whiteData);
        }

        void LoadTextures()
        {
            texturesLoaded = true;
            randomIslandTextures = new TextureResource[] 
            { 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island1.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island2.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island3.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island4.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island5.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island6.dds"), 
                ResourceManager.Inst.GetTexture("Textures/HeightMap/island7.dds") 
            };

            creationDatablock = ResourceManager.Inst.GetTerrainCreationDatablock("Island");
        }

        struct PlacementParameters
        {
            public Vector2 MinSize;
            public Vector2 MaxSize;
            public float MinAmplitude;
            public float MaxAmplitude;
            public int MinCount;
            public int MaxCount;

            public float MinFalloff;
            public float MaxFalloff;

            public PlacementParameters(int minCount, int maxCount, float minAmplitude, float maxAmplitude, float minFalloff, float maxFalloff, Vector2 minSize, Vector2 maxSize)
            {
                MinCount = minCount;
                MaxCount = maxCount;
                MinAmplitude = minAmplitude;
                MaxAmplitude = maxAmplitude;
                MinSize = minSize;
                MaxSize = maxSize;
                MinFalloff = minFalloff;
                MaxFalloff = maxFalloff;
            }
        }

        enum LayerMode
        {
            Peaks,
            Mountains,
            Plateaus,
        }

        enum BlendMode
        {
            Additive,
            SoftAdditive,
            Interpolate,
            Multiply,
        }

        Texture2D GetRandomIsland()
        {
            int randIndex = RandomHelper.RandomGen.Next(0, randomIslandTextures.Length);
            return randomIslandTextures[randIndex].GetTexture() as Texture2D;
        }

        void DrawImage(Texture2D image)
        {
            basicShader.SetupShader();
            GFX.Device.Textures[0] = image;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, new Vector2(1.0f / (float)image.Width, 1.0f / (float)image.Height));
            GFXPrimitives.Quad.Render();
        }

        void CreateLayer(Texture2D heightmap, LayerMode mode, BlendMode blendMode, PlacementParameters parameters)
        {
            RenderTarget2D rtPeaks = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, heightmap.Format);

            GFX.Device.SetRenderTarget(0, rtPeaks);
            DrawImage(heightmap);

            GFX.Device.RenderState.AlphaBlendEnable = true;

            switch (blendMode)
            {
                case BlendMode.Additive:
                    GFX.Device.RenderState.SourceBlend = Blend.One;
                    GFX.Device.RenderState.DestinationBlend = Blend.One;
                    break;
                case BlendMode.SoftAdditive:
                    GFX.Device.RenderState.SourceBlend = Blend.InverseDestinationColor;
                    GFX.Device.RenderState.DestinationBlend = Blend.One;
                    break;
                case BlendMode.Multiply:
                    GFX.Device.RenderState.SourceBlend = Blend.DestinationColor;
                    GFX.Device.RenderState.DestinationBlend = Blend.Zero;
                    break;
                case BlendMode.Interpolate:
                    GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
                    GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                    break;
            }

            peakShader.SetupShader();
            int count = RandomHelper.RandomGen.Next(parameters.MinCount, parameters.MaxCount);
            for (int i = 0; i < count; i++)
            {
                Vector2 res = Vector2.One;
                switch (mode)
                {
                    case LayerMode.Peaks:
                        GFX.Device.Textures[0] = whiteTexture;
                        break;
                    case LayerMode.Mountains:
                        Texture2D mountainMap = GFX.Inst.PerlinNoiseGen.Generate2DNoise(mountainParams, mountainRes, mountainRes, 1);
                        GFX.Device.Textures[0] = mountainMap;
                        res = new Vector2(mountainRes, mountainRes);
                        break;
                }
                float amplitude = MathHelper.Lerp(parameters.MinAmplitude, parameters.MaxAmplitude, (float)RandomHelper.RandomGen.NextDouble());
                float falloff = MathHelper.Lerp(parameters.MinFalloff, parameters.MaxFalloff, (float)RandomHelper.RandomGen.NextDouble());
                Vector4 peakParameters = new Vector4(amplitude, falloff, 0, 0);
                Vector2 randPos = new Vector2((float)RandomHelper.RandomGen.NextDouble(), (float)RandomHelper.RandomGen.NextDouble()) * 2.0f - Vector2.One;

                Vector2 randRadius = Vector2.Lerp(parameters.MinSize, parameters.MaxSize, (float)RandomHelper.RandomGen.NextDouble());
                randPos = Vector2.Clamp(randPos, Vector2.One * -1 + randRadius, Vector2.One - randRadius);
                GFX.Device.SetPixelShaderConstant(0, peakParameters);
                GFX.Device.SetVertexShaderConstant(0, new Vector4(randRadius.X, randRadius.Y, randPos.X, randPos.Y));
                GFX.Device.SetVertexShaderConstant(1, Vector4.Zero);
                GFX.Device.SetVertexShaderConstant(2, Vector2.One / res);
            }

            GFX.Device.RenderState.AlphaBlendEnable = false;

            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = null;

            CopyToTexture(rtPeaks.GetTexture(), heightmap);
        }

        struct ErosionParticle
        {
            public int x;
            public int y;
            public float sediment;
        }

        void ApplyErosion(Texture2D heightmap, int iterations, float initialParticleCoverage)
        {
            Color[] heightDataOrig = new Color[heightmap.Width * heightmap.Height];
            heightmap.GetData<Color>(heightDataOrig);

            float[] heightValues = new float[heightDataOrig.Length];
            for (int i = 0; i < heightDataOrig.Length; i++)
                heightValues[i] = (float)heightDataOrig[i].R / 255.0f;

            RenderTarget2D normalTarget = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Vector2);

            int numParticles = (int)(initialParticleCoverage * heightmap.Width * heightmap.Height);
            ErosionParticle[] particles = new ErosionParticle[numParticles];
            for (int k = 0; k < iterations; k++)
            {
                Vector2 invRes = new Vector2(1.0f / (float)heightmap.Width, 1.0f / (float)heightmap.Height);

                GFX.Device.SetRenderTarget(0, normalTarget);
                gradientShader.SetupShader();

                GFX.Device.Textures[0] = heightmap;
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, invRes);
                GFX.Device.SetPixelShaderConstant(0, invRes);

                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);

                Vector2[] gradientData = new Vector2[normalTarget.Width * normalTarget.Height];
                normalTarget.GetTexture().GetData<Vector2>(gradientData);

                for (int i = 0; i < numParticles; i++)
                {
                    particles[i].x = RandomHelper.RandomGen.Next(heightmap.Width - 1);
                    particles[i].y = RandomHelper.RandomGen.Next(heightmap.Height - 1);
                    particles[i].sediment = 0;

                    int oldX = particles[i].x;
                    int oldY = particles[i].y;
                    while (true)
                    {
                        int index = particles[i].x + particles[i].y * heightmap.Width;
                        float oldHeight = heightValues[index];
                        float heightAmount = heightValues[index] * 0.035f;
                        heightValues[index] -= heightAmount;
                        particles[i].sediment += heightAmount;
                        Vector2 gradient = gradientData[index];
                        int shiftX = (gradient.X < 0.25f) ? -1 : (gradient.X > 0.25f) ? 1 : 0;
                        int shiftY = (gradient.Y < 0.25f) ? -1 : (gradient.Y > 0.25f) ? 1 : 0;
                        int newX = (int)(particles[i].x + shiftX);
                        int newY = (int)(particles[i].y + shiftY);
                        if (newX < 0 || newY < 0 || newX >= heightmap.Width || newY >= heightmap.Height)
                            break;
                        if (heightValues[newX + newY * heightmap.Width] > heightValues[index] || (newX == oldX && newY == oldY))
                        {
                            heightValues[index] += particles[i].sediment;
                            break;
                        }
                        oldX = particles[i].x;
                        oldY = particles[i].y;
                        particles[i].x = newX;
                        particles[i].y = newY;
                    }
                }

                GFX.Device.Textures[0] = null;
                for (int i = 0; i < heightDataOrig.Length; i++)
                {
                    byte value = (byte)(heightValues[i] * 255.0f);
                    heightDataOrig[i].R = value;
                    heightDataOrig[i].G = value;
                    heightDataOrig[i].B = value;
                    heightDataOrig[i].A = value;
                }
                heightmap.SetData<Color>(heightDataOrig);
            }
        }

        void CreateTerraces(Texture2D heightmap)
        {
            RenderTarget2D rtTerrace = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, heightmap.Format);

            GFX.Device.SetRenderTarget(0, rtTerrace);

            GFX.Device.Textures[0] = heightmap;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, new Vector2(1.0f / (float)heightmap.Width, 1.0f / (float)heightmap.Height));
            GFX.Device.SetPixelShaderConstant(0, new Vector4(terraceCount, terraceExp, 0, 0));
            terraceShader.SetupShader();

            GFXPrimitives.Quad.Render();

            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = null;

            CopyToTexture(rtTerrace.GetTexture(), heightmap);
        }

        void CreateLerpLayer(Texture2D heightmap, Texture2D shapeTexture, bool invert, float baseHeight, float intensity, Vector2 scale, float theta, Vector2 position)
        {
            GFX.Device.Textures[0] = heightmap;
            GFX.Device.Textures[1] = shapeTexture;
            GFX.Device.SetVertexShaderConstant(0, new Vector4(scale.X, scale.Y, position.X, position.Y));
            GFX.Device.SetVertexShaderConstant(1, Vector4.One * theta);
            GFX.Device.SetVertexShaderConstant(2, new Vector2(1.0f / (float)shapeTexture.Width, 1.0f / (float)shapeTexture.Height));
            GFX.Device.SetVertexShaderConstant(3, new Vector2(1.0f / (float)heightmap.Width, 1.0f / (float)heightmap.Height));
            GFX.Device.SetPixelShaderConstant(0, new Vector2(baseHeight, intensity));
            GFX.Device.SetPixelShaderConstant(1, (invert)?Vector4.One : Vector4.Zero);
            interpolateShader.SetupShader();

            GFXPrimitives.Quad.Render();

            GFX.Device.Textures[0] = null;
            GFX.Device.Textures[1] = null;
        }

        Vector2 GetMinMaxHeight(Texture2D heightmap)
        {
            Vector2 minmax = new Vector2(1, 0);
            switch (heightmap.Format)
            {
                case SurfaceFormat.Single:
                    float[] floatData = new float[heightmap.Width * heightmap.Height];
                    heightmap.GetData<float>(floatData);
                    for (int i = 0; i < floatData.Length; i++)
                    {
                        minmax.X = Math.Min(floatData[i], minmax.X);
                        minmax.Y = Math.Max(floatData[i], minmax.Y);
                    }
                    break;
                case SurfaceFormat.Color:
                    Color[] colorData = new Color[heightmap.Width * heightmap.Height];
                    heightmap.GetData<Color>(colorData);
                    float invScale = 1.0f / 255.0f;
                    for (int i = 0; i < colorData.Length; i++)
                    {
                        float value = (float)colorData[i].R * invScale;
                        minmax.X = Math.Min(value, minmax.X);
                        minmax.Y = Math.Max(value, minmax.Y);
                    }
                    break;
            }
            return minmax;
        }

        void PerformHistogramEqualization(Texture2D heightmap)
        {
            Vector2 MinMax = GetMinMaxHeight(heightmap);
            RenderTarget2D rtHist = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, heightmap.Format);

            GFX.Device.SetRenderTarget(0, rtHist);

            GFX.Device.Textures[0] = heightmap;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, new Vector2(1.0f / (float)heightmap.Width, 1.0f / (float)heightmap.Height));
            GFX.Device.SetPixelShaderConstant(0, MinMax);
            histogramShader.SetupShader();

            GFXPrimitives.Quad.Render();

            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = null;

            CopyToTexture(rtHist.GetTexture(), heightmap);
        }

        void CopyToTexture(Texture2D sourceTexture, Texture2D destTexture)
        {
            switch (sourceTexture.Format)
            {
                case SurfaceFormat.Single:
                    float[] floatData = new float[sourceTexture.Width * sourceTexture.Height];
                    sourceTexture.GetData<float>(floatData);
                    destTexture.SetData<float>(floatData);
                    break;
                case SurfaceFormat.Color:
                    Color[] colorData = new Color[sourceTexture.Width * sourceTexture.Height];
                    sourceTexture.GetData<Color>(colorData);
                    destTexture.SetData<Color>(colorData);
                    break;
            }
        }

        struct Int2D
        {
            public int X;
            public int Y;

            public Int2D(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        Vector2 GetTerrainPosition(Texture2D heightmap, Vector2 shapeScale, Vector2 heightRange, out float heightAtPosition, out float thetaAtPosition, List<Vector3> placedShapes)
        {
            Vector2 invRes = new Vector2(1.0f / (float)heightmap.Width, 1.0f / (float)heightmap.Height);

            RenderTarget2D rtVariance = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Vector2);
            RenderTarget2D rtNormal = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Vector2);
            GFX.Device.Textures[0] = heightmap;
            varianceShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, invRes);
            GFX.Device.SetPixelShaderConstant(0, invRes);
            GFX.Device.SetRenderTarget(0, rtVariance);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.SetRenderTarget(0, rtNormal);
            gradientShader.SetupShader();
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = null;

            int bufferSize = heightmap.Width * heightmap.Height;
            float[] heightData = new float[bufferSize];
            Vector2[] varianceData = new Vector2[bufferSize];
            rtVariance.GetTexture().GetData<Vector2>(varianceData);
            Vector2[] gradientData = new Vector2[rtNormal.Width * rtNormal.Height];
            rtNormal.GetTexture().GetData<Vector2>(gradientData);

            switch (heightmap.Format)
            {
                case SurfaceFormat.Single:
                    heightmap.GetData<float>(heightData);
                    break;
                case SurfaceFormat.Color:
                    Color[] colorData = new Color[bufferSize];
                    heightmap.GetData<Color>(colorData);
                    float invScale = 1.0f / 255.0f;
                    for (int i = 0; i < colorData.Length; i++)
                        heightData[i] = (float)colorData[i].R * invScale;
                    break;
            }
            
            List<Int2D> availableSpots = new List<Int2D>();
            for (int x = 0; x < heightmap.Width; x++)
            {
                for (int y = 0; y < heightmap.Height; y++)
                {
                    int index = x + y * heightmap.Width;
                    if (varianceData[index].Y >= heightRange.X && varianceData[index].Y <= heightRange.Y)
                    {
                        /*
                        Vector2 currPos = new Vector2(x, y) * invRes * 2.0f - Vector2.One;
                        bool canPlace = true;
                        for(int i = 0; i < placedShapes.Count; i++)
                        {
                            if (Vector2.Distance(currPos, new Vector2(placedShapes[i].X, placedShapes[i].Y)) <= placedShapes[i].Z)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if(canPlace)*/
                            availableSpots.Add(new Int2D(x, y));
                    }
                }
            }

            for (int i = 0; i < availableSpots.Count; i++)
            {
                Int2D temp = availableSpots[i];
                int randIndex = RandomHelper.RandomGen.Next(i, availableSpots.Count);
                availableSpots[i] = availableSpots[randIndex];
                availableSpots[randIndex] = temp;
            }

            PriorityQueue<float, Int2D> plantSpots = new PriorityQueue<float, Int2D>();
            for (int i = 0; i < availableSpots.Count; i++)
            {
                int index = availableSpots[i].X + availableSpots[i].Y * heightmap.Width;
                Vector2 currPos = new Vector2(availableSpots[i].X, availableSpots[i].Y) * invRes * 2.0f - Vector2.One;
                float netPriority = 0;
                for (int j = 0; j < placedShapes.Count; j++)
                {
                    float radius = Vector2.Distance(currPos, new Vector2(placedShapes[j].X, placedShapes[j].Y)) * placedShapes[j].Z;
                    if (radius == 0.0f)
                        radius = 0.001f;
                        netPriority += 1.0f / (radius * radius);
                    
                }
                plantSpots.Enqueue(availableSpots[i], varianceData[index].X + netPriority);
            }

            Int2D randPos = plantSpots.ExtractMin();// availableSpots[RandomHelper.RandomGen.Next(0, availableSpots.Count)];
            Vector2 pos = new Vector2(randPos.X, randPos.Y) * invRes * 2.0f - Vector2.One;
            int placedIndex = randPos.X + randPos.Y * heightmap.Width;
            heightAtPosition = varianceData[placedIndex].Y;
            thetaAtPosition = (float)Math.Atan2(gradientData[placedIndex].Y, gradientData[placedIndex].X);
            return pos;
        }

        void CreateConstraints(Texture2D heightmap, out SortedList<int, Vector4> landmarks)
        {
            RenderTarget2D rtHeightmap = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, heightmap.Format);

            float[] numDirs = new float[4] { 0, MathHelper.PiOver2, MathHelper.Pi, 3.0f * MathHelper.PiOver2 };
            GFX.Device.SetRenderTarget(0, rtHeightmap);
            float randomRot = numDirs[RandomHelper.RandomGen.Next(0, numDirs.Length)];
            CreateLerpLayer(heightmap, GetRandomIsland(), false, 0, 0.28f, Vector2.One, randomRot, Vector2.Zero);
            GFX.Device.SetRenderTarget(0, null);

            CopyToTexture(rtHeightmap.GetTexture(), heightmap);

            List<Vector3> placedLocations = new List<Vector3>();
            landmarks = new SortedList<int, Vector4>();
            for(int i = 0; i < creationDatablock.MapLocations.Length; i++)
            {
                TerrainCreationParameters currParams = creationDatablock.MapLocations[i];
                if(currParams !=  null)
                {
                    float baseHeight;
                    float theta = 0;
                    Vector2 position = GetTerrainPosition(heightmap, currParams.Scale, currParams.HeightRange, out baseHeight, out theta, placedLocations);
                    switch (currParams.Rotation)
                    {
                        case RotationParameters.Fixed:
                            theta = 0;
                            break;
                        case RotationParameters.FourAngles:
                            theta = numDirs[RandomHelper.RandomGen.Next(0, numDirs.Length)];
                            break;
                        case RotationParameters.Random:
                            theta = (float)RandomHelper.RandomGen.NextDouble() * MathHelper.TwoPi;
                            break;
                    }

                    GFX.Device.SetRenderTarget(0, rtHeightmap);
                    DrawImage(heightmap);
                    CreateLerpLayer(heightmap, currParams.Mask.GetTexture() as Texture2D, true, baseHeight, currParams.Intensity, currParams.Scale, theta, position);
                    GFX.Device.SetRenderTarget(0, null);
                    CopyToTexture(rtHeightmap.GetTexture(), heightmap);
                    landmarks.Add(i, new Vector4(position.X, position.Y, baseHeight, theta));
                    placedLocations.Add(new Vector3(position.X, position.Y, currParams.RepulsionFactor));
                    //placedLocations.Add(new Vector3(position.X, position.Y, currParams.Scale.Length() * currParams.RepulsionFactor));
                }
            }
            
        }

        public Texture2D GenerateTerrain(int width, int height, out SortedList<int, Vector4> landmarks)
        {
            if (!texturesLoaded)
                LoadTextures();

            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;
            Texture2D heightmap = GFX.Inst.PerlinNoiseGen.Generate2DNoise(noiseParams, width, height, 1);

            PerformHistogramEqualization(heightmap);

            ApplyErosion(heightmap, 6, 0.55f);

            CreateLayer(heightmap, LayerMode.Peaks, BlendMode.SoftAdditive, peakPlacementParams);

            CreateLayer(heightmap, LayerMode.Mountains, BlendMode.Additive, mountainPlacementParams);

            CreateLayer(heightmap, LayerMode.Peaks, BlendMode.Additive, plateauPlacementParams);

            CreateTerraces(heightmap);

            CreateConstraints(heightmap, out landmarks);

            ApplyErosion(heightmap, 6, 0.55f);

            ApplyErosion(heightmap, 24, 0.95f);

            GFX.Device.DepthStencilBuffer = dsOld;
            return heightmap;
        }
    }
}
