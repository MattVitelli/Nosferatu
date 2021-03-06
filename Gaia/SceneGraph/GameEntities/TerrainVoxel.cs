﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Physics;

using Gaia.Core;
using Gaia.Voxels;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Physics;

namespace Gaia.SceneGraph.GameEntities
{
    public class TerrainVoxel : Terrain
    {
        public byte IsoValue = 127; //Defines density field isosurface cutoff value (ie the transition between solid and empty space)
                                    //so if a voxel had an element of 127 or lower, that would be empty space. A value higher than 127
                                    //Would be solid space.
        public int VoxelGridSize = 8; //Defines how many voxel geometries we have (used to balance performance)
        public int DensityFieldWidth = 129; //Density field is (2^n)+1 in size. (e.g. 65, 129, 257, 513) 
        public int DensityFieldHeight;
        public int DensityFieldDepth;

        VoxelGeometry[] Voxels;
        BoundingBox[] VoxelBounds;
        //VoxelCollision[] VoxelCollisions;
        class VoxelElement
        {
            public VoxelGeometry geometry;
            public BoundingBox bounds;
            public VoxelElement(VoxelGeometry geometry, BoundingBox bounds)
            {
                this.geometry = geometry;
                this.bounds = bounds;
            }
        }
        KDTree<VoxelElement> voxelKDTree;

        RenderElement giantQuadElement;

        const int NUM_BITS_BLENDING = 4;

        const int NUM_BITS_MATERIAL = 8 - NUM_BITS_BLENDING;

        int MAX_MATERIALS = (int)Math.Pow(2, NUM_BITS_MATERIAL);

        public byte[] DensityField;

        public Color[] MaterialField;

        Gaia.Resources.Material terrainMaterial;
        Gaia.Resources.Material terrainQuadMaterial;

        Matrix textureMatrix = Matrix.Identity;
        float TerrainSize = 768; 

        RenderTarget2D srcTarget;
        Texture3D[] noiseTextures;

        Texture3D blendTexture;
        Texture3D blendIDTexture;
        int blendMapWidth = 32;
        int blendMapHeight = 32;
        int blendMapDepth = 32;
        TerrainClimate climate;

        public int[] surfaceIndices;

        List<TriangleGraph> availableTriangles;
        SortedList<int, Vector4> landmarks;

        public TerrainVoxel()
        {
            Transformation.SetScale(new Vector3(1, 0.25f, 1) * TerrainSize);
            Transformation.SetPosition(Vector3.Up * TerrainSize * 0.22f);
            //GenerateFloatingIslands(128);
            GenerateTerrainProcedurally();
            terrainMaterial = ResourceManager.Inst.GetMaterial("TerrainMaterial");
            climate = ResourceManager.Inst.GetTerrainClimate("TestTerrain");
            PrepareTriangles();
            TransformLandmarks();
        }

        public TerrainVoxel(string filename)
        {
            Transformation.SetScale(new Vector3(1, 0.25f, 1) * TerrainSize);
            Transformation.SetPosition(Vector3.Up * TerrainSize * 0.0725f);

            terrainMaterial = ResourceManager.Inst.GetMaterial("TerrainMaterial");
            climate = ResourceManager.Inst.GetTerrainClimate("TestTerrain");

            GenerateTerrainFromFile(filename);
            PrepareTriangles();
            TransformLandmarks();
        }

        void PrepareTriangles()
        {
            availableTriangles = new List<TriangleGraph>();
            BoundingBox region = Transformation.GetBounds();
            GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, region);
        }

        void AssembleTextureAtlas(Texture3D target, Texture2D[] srcTextures, int textureSize, int mipCount)
        {
            Shader imageShader = ResourceManager.Inst.GetShader("Generic2D");
            RenderTarget2D rtTarget = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Color);
            target = new Texture3D(GFX.Device, textureSize, textureSize, srcTextures.Length, mipCount, TextureUsage.AutoGenerateMipMap, SurfaceFormat.Color);

            Color[] colorBuffer = new Color[textureSize * textureSize];

            GFX.Device.SamplerStates[0].MagFilter = TextureFilter.Linear;
            GFX.Device.SamplerStates[0].MinFilter = TextureFilter.Linear;
            GFX.Device.SamplerStates[0].MipFilter = TextureFilter.Linear;
            imageShader.SetupShader();
            for (int i = 0; i < srcTextures.Length; i++)
            {
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(srcTextures[i].Width, srcTextures[i].Height));
                GFX.Device.Textures[0] = srcTextures[i];
                GFX.Device.SetRenderTarget(0, rtTarget);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
                rtTarget.GetTexture().GetData<Color>(colorBuffer);
                target.SetData<Color>(colorBuffer, colorBuffer.Length * i, colorBuffer.Length, SetDataOptions.None);
            }

            target.GenerateMipMaps(TextureFilter.Linear);
        }
        
        public override void GenerateRandomTransform(Random rand, out Vector3 position, out Vector3 normal)
        {
            int randomIndex = rand.Next(0, availableTriangles.Count);
            TriangleGraph triangle = availableTriangles[randomIndex];
            while (usedLandmarkTriangles.ContainsKey(triangle.ID))
            {
                randomIndex = rand.Next(0, availableTriangles.Count);
                triangle = availableTriangles[randomIndex];
            }
            position = triangle.GeneratePointInTriangle(RandomHelper.RandomGen);
            normal = triangle.Normal;
        }

        public override bool GetTrianglesInRegion(Random rand, out List<TriangleGraph> availableTriangles, BoundingBox region)
        {
            return GetTrianglesInRegion(rand, out availableTriangles, region, false);
        }

        public override bool GetTrianglesInRegion(Random rand, out List<TriangleGraph> availableTriangles, BoundingBox region, bool isLandmark)
        {
            BoundingBox invBounds = MathUtils.TransformBounds(region, Transformation.GetObjectSpace());
            //region.Min = invRegionMin;
            //region.Max = invRegionMax;

            if (float.IsNaN(invBounds.Max.X) || float.IsNaN(invBounds.Max.Y) || float.IsNaN(invBounds.Max.Z)
                || float.IsNaN(invBounds.Min.X) || float.IsNaN(invBounds.Min.Y) || float.IsNaN(invBounds.Min.Z))
            {
                Console.WriteLine("This is very bad");
            }

            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;

            int voxelMinX = (int)MathHelper.Clamp((invBounds.Min.X + 1.0f) * 0.5f * voxelCountX, 0, voxelCountX - 1);
            int voxelMinY = (int)MathHelper.Clamp((invBounds.Min.Y + 1.0f) * 0.5f * voxelCountY, 0, voxelCountY - 1);
            int voxelMinZ = (int)MathHelper.Clamp((invBounds.Min.Z + 1.0f) * 0.5f * voxelCountZ, 0, voxelCountZ - 1);

            int voxelMaxX = (int)MathHelper.Clamp((invBounds.Max.X + 1.0f) * 0.5f * voxelCountX, 0, voxelCountX - 1);
            int voxelMaxY = (int)MathHelper.Clamp((invBounds.Max.Y + 1.0f) * 0.5f * voxelCountY, 0, voxelCountY - 1);
            int voxelMaxZ = (int)MathHelper.Clamp((invBounds.Max.Z + 1.0f) * 0.5f * voxelCountZ, 0, voxelCountZ - 1);

            availableTriangles = new List<TriangleGraph>();

            for (int z = voxelMinZ; z <= voxelMaxZ; z++)
            {
                for (int y = voxelMinY; y <= voxelMaxY; y++)
                {
                    for (int x = voxelMinX; x <= voxelMaxX; x++)
                    {
                        int voxelIndex = x + (y + z * voxelCountY) * voxelCountX;
                        if (Voxels[voxelIndex].CanRender)
                        {
                            KDTree<TriangleGraph> collisionTree = Voxels[voxelIndex].GetCollisionTree();
                            PerformKDRegionSearch(collisionTree.GetRoot(), ref region, availableTriangles, isLandmark);
                        }
                    }
                }
            }

            return (availableTriangles.Count > 0);
        }

        static int SceneCompareFunction(VoxelGeometry elementA, VoxelGeometry elementB, int axis)
        {
            BoundingBox boundsA = elementA.GetBounds();//.Transform.TransformBounds(elementA.Mesh.GetBounds());

            Vector3 posA = (boundsA.Max + boundsA.Min) * 0.5f;// elementA.Transform.GetPosition();
            float valueA = (axis == 0) ? posA.X : (axis == 1) ? posA.Y : posA.Z;

            BoundingBox boundsB = elementB.GetBounds();//.Transform.TransformBounds(elementB.Mesh.GetBounds());

            Vector3 posB = (boundsB.Max + boundsB.Min) * 0.5f;//elementB.Transform.GetPosition();
            float valueB = (axis == 0) ? posB.X : (axis == 1) ? posB.Y : posB.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        static Vector2 SceneBoundsFunction(VoxelGeometry element, int axis)
        {
            BoundingBox bounds = element.GetBounds();//.Transform.TransformBounds(element.Mesh.GetBounds());
            return (axis == 0) ? new Vector2(bounds.Min.X, bounds.Max.X) : ((axis == 1) ? new Vector2(bounds.Min.Y, bounds.Max.Y) : new Vector2(bounds.Min.Z, bounds.Max.Z));
        }

        public void GenerateRandomTransformConnected(Random rand, out Vector3 position, out Vector3 normal)
        {
            int bestY = -1;
            int randX = 0;
            int randZ = 0;
            while(bestY == -1)
            {
                randX = rand.Next(DensityFieldWidth- 1);
                randZ = rand.Next(DensityFieldDepth-1);
                int vX = (int)MathHelper.Clamp(((float)randX / (float)DensityFieldWidth) * VoxelGridSize, 0, VoxelGridSize - 1);
                
                int vZ = (int)MathHelper.Clamp(((float)randZ / (float)DensityFieldDepth) * VoxelGridSize, 0, VoxelGridSize - 1);
                int index = randX + randZ * DensityFieldWidth * DensityFieldHeight;
                int voxelIndex = vX + vZ * VoxelGridSize * VoxelGridSize;
                for (int i = 0; i < DensityFieldHeight; i++)
                {
                    int vY = (int)MathHelper.Clamp(((float)i / (float)DensityFieldHeight) * VoxelGridSize, 0, VoxelGridSize - 1);
                    if (DensityField[index + i * DensityFieldWidth] <= (IsoValue + 10) && Voxels[voxelIndex + vY].CanRender)
                    {
                        SortedList<ulong, TriangleGraph> graph = null;
                        if (Voxels[voxelIndex + vY].GetCollisionNodesAtPoint(out graph, ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, randX, i, randZ))
                        {
                            bestY = i;
                            break;
                        }
                    }
                }
            }

            Vector3 vec = new Vector3((float)randX, 1.0f + (float)bestY, (float)randZ) / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            position = Vector3.Transform(2.0f * vec - Vector3.One, Transformation.GetTransform());
            normal = ComputeNormal(randX, bestY, randZ);
        }

        Vector3 ComputeNormal(int x, int y, int z)
        {
            int sliceArea = DensityFieldWidth * DensityFieldHeight;
            int idx = x + DensityFieldWidth * y + z * sliceArea;
            int x0 = (x - 1 >= 0) ? -1 : 0;
            int x1 = (x + 1 < DensityFieldWidth) ? 1 : 0;
            int y0 = (y - 1 >= 0) ? -DensityFieldWidth : 0;
            int y1 = (y + 1 < DensityFieldHeight) ? DensityFieldWidth : 0;
            int z0 = (z - 1 >= 0) ? -sliceArea : 0;
            int z1 = (z + 1 < DensityFieldDepth) ? sliceArea : 0;

            //Take the negative gradient (hence the x0-x1)
            Vector3 nrm = new Vector3(DensityField[idx + x0] - DensityField[idx + x1], DensityField[idx + y0] - DensityField[idx + y1], DensityField[idx + z0] - DensityField[idx + z1]);

            double magSqr = nrm.X * nrm.X + nrm.Y * nrm.Y + nrm.Z * nrm.Z + 0.0001; //Regularization constant (very important!)
            double invMag = 1.0 / Math.Sqrt(magSqr);
            nrm.X = (float)(nrm.X * invMag);
            nrm.Y = (float)(nrm.Y * invMag);
            nrm.Z = (float)(nrm.Z * invMag);

            return nrm;
        }

        public void GetBlockPos(Vector3 pos, out int xPos, out int yPos, out int zPos)
        {
            Vector3 posObjSpace = Vector3.Transform(pos, Transformation.GetObjectSpace());

            posObjSpace = posObjSpace * 0.5f + Vector3.One * 0.5f;
            posObjSpace *= new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth);

            xPos = (int)MathHelper.Clamp(posObjSpace.X, 0, DensityFieldWidth - 1);
            yPos = (int)MathHelper.Clamp(posObjSpace.Y, 0, DensityFieldHeight - 1);
            zPos = (int)MathHelper.Clamp(posObjSpace.Z, 0, DensityFieldDepth - 1);
        }

        public void SetUnavailableRegion(BoundingBox region)
        {
            List<TriangleGraph> triangles;
            if (GetTrianglesInRegion(RandomHelper.RandomGen, out triangles, region, true))
            {
                for (int i = 0; i < triangles.Count; i++)
                {
                    if (!usedLandmarkTriangles.ContainsKey(triangles[i].ID))
                        usedLandmarkTriangles.Add(triangles[i].ID, (char)1);
                }
            }
        }

        public void GetLandmarkTransform(MapLandmark marker, Transform transform, BoundingBox meshBounds)
        {
            Vector4 param = landmarks[(int)marker];
            Vector3 pos = new Vector3(param.X, param.Y, param.Z);
            transform.SetPosition(pos);
            Vector3 rotation = transform.GetRotation();
            rotation.Y = param.W;
            transform.SetRotation(rotation);
            List<TriangleGraph> triangles;
            BoundingBox region = meshBounds;
            Vector3 sides = (meshBounds.Max - meshBounds.Min)*0.5f;
            region.Min = pos - sides;
            region.Max = pos + sides;
            region.Min.Y = this.Transformation.GetBounds().Min.Y;
            region.Max.Y = this.Transformation.GetBounds().Max.Y;
            if (GetTrianglesInRegion(RandomHelper.RandomGen, out triangles, region, true))
            {
                TriangleGraph bestTri = triangles[0];
                float bestDist = Vector3.DistanceSquared(bestTri.Centroid, pos);
                for (int i = 1; i < triangles.Count; i++)
                {
                    float dist = Vector3.DistanceSquared(triangles[i].Centroid, pos);
                    if (dist < bestDist)
                    {
                        bestTri = triangles[i];
                        bestDist = dist;
                    }
                }
                transform.SetPosition(bestTri.GeneratePointInTriangle(RandomHelper.RandomGen));
                BoundingBox newRegion = transform.TransformBounds(meshBounds);
                Vector3 offsetVector = Vector3.One * 5.0f;
                newRegion.Min = newRegion.Min - offsetVector;
                newRegion.Max = newRegion.Max + offsetVector;
                triangles = null;
                if (GetTrianglesInRegion(RandomHelper.RandomGen, out triangles, newRegion, true))
                {
                    for (int i = 0; i < triangles.Count; i++)
                    {
                        if(!usedLandmarkTriangles.ContainsKey(triangles[i].ID))
                            usedLandmarkTriangles.Add(triangles[i].ID, (char)1);
                    }
                }
            }
        }

        void TransformLandmarks()
        {
            for (int i = 0; i < landmarks.Count; i++)
            {
                int currKey = landmarks.Keys[i];
                Vector3 pos = new Vector3(landmarks[currKey].X, landmarks[currKey].Z*2-1, -1*landmarks[currKey].Y);
                pos = Vector3.Transform(pos, this.Transformation.GetTransform());
                landmarks[currKey] = new Vector4(pos.X, pos.Y, pos.Z, landmarks[currKey].W);
            }
        }

        void GenerateTerrainProcedurally()
        {
            Texture2D heightMap = GFX.Inst.TerrainGen.GenerateTerrain(257, 257, out landmarks);
            DensityFieldWidth = heightMap.Width;
            DensityFieldDepth = DensityFieldWidth;
            DensityFieldHeight = ((DensityFieldWidth - 1) / 4) + 1;

            InitializeFieldData();

            PerformBlur(heightMap);

            //InitializeClimateMap();

            //InitializeMaterial();

            InitializeSurfaceIndices();

            InitializeVoxels();
        }

        void GenerateTerrainFromFile(string filename)
        {
            Texture2D heightMap = Texture2D.FromFile(GFX.Device, filename);

            DensityFieldWidth = heightMap.Width;
            DensityFieldDepth = DensityFieldWidth;
            DensityFieldHeight = ((DensityFieldWidth-1) / 4) + 1;

            InitializeFieldData();

            PerformBlur(heightMap);

            //InitializeClimateMap();

            //InitializeMaterial();

            InitializeSurfaceIndices();

            InitializeVoxels();
            DensityField = null;
        }

        /*
        void InitializeMaterial()
        {
            
            terrainMaterial.SetTexture(0, climate.BaseMapAtlas);
            terrainMaterial.SetTexture(1, climate.NormalMapAtlas);
            TextureResource blendTex = new TextureResource();
            blendTex.SetTexture(TextureResourceType.Texture3D, blendTexture);
            TextureResource blendIDTex = new TextureResource();
            blendIDTex.SetTexture(TextureResourceType.Texture3D, blendIDTexture);
            terrainMaterial.SetTexture(2, blendTex);
            terrainMaterial.SetTexture(3, blendIDTex);
            terrainMaterial.kAmbient = climate.GetInverseResolution();
            terrainMaterial.kDiffuse = Vector3.One / new Vector3(blendMapWidth, blendMapHeight, blendMapDepth);
            
        }
        */

        void InitializeClimateMap()
        {

            Texture3D densityFieldTexture = new Texture3D(GFX.Device, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, 1, TextureUsage.None, GFX.Inst.ByteSurfaceFormat);
            CopyDensityTextureData(ref DensityField, densityFieldTexture);

            GFX.Device.Textures[0] = densityFieldTexture;

            RenderTarget2D rtClimateMap = new RenderTarget2D(GFX.Device, blendMapWidth, blendMapHeight, 1, SurfaceFormat.Color);
            RenderTarget2D rtIDMap = new RenderTarget2D(GFX.Device, blendMapWidth, blendMapHeight, 1, SurfaceFormat.Color);

            int stride = blendMapWidth * blendMapHeight;
            Color[] colorData = new Color[stride * blendMapDepth];
            Color[] blendIDData = new Color[stride * blendMapDepth];
            
            Vector4[] climateParams = new Vector4[4];
            Vector4[] climateParams2 = new Vector4[4];

            for (int i = 0; i < climate.heightCoeffs.Length; i++)
            {
                climateParams[i] = new Vector4(climate.heightCoeffs[i], climate.gradientCoeffs[i], climate.curvatureCoeffs[i], climate.baseScores[i]);
                climateParams2[i] = Vector4.One * climate.blendZones[i];
            }



            GFX.Device.SetPixelShaderConstant(0, Vector3.One / new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth));

            RenderTarget2D rtGradientMap = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldHeight, 1, SurfaceFormat.Single);
            int gradStride = DensityFieldWidth * DensityFieldHeight;
            float[] gradientValues = new float[gradStride * DensityFieldDepth];
            Texture3D gradientTexture = new Texture3D(GFX.Device, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, 1, TextureUsage.None, SurfaceFormat.Single);
            Shader gradientShader = ResourceManager.Inst.GetShader("GradientShader");
            gradientShader.SetupShader();

            for (int z = 0; z < DensityFieldDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(DensityFieldDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth

                GFX.Device.SetRenderTarget(0, rtGradientMap);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);

                rtGradientMap.GetTexture().GetData<float>(gradientValues, z * gradStride, gradStride);
            }
            gradientTexture.SetData<float>(gradientValues);




            GFX.Device.Textures[1] = gradientTexture;
            GFX.Device.SetPixelShaderConstant(1, climateParams);
            GFX.Device.SetPixelShaderConstant(5, climateParams2);


            Shader climateShader = ResourceManager.Inst.GetShader("ClimateShader");
            climateShader.SetupShader();

            for (int z = 0; z < blendMapDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(blendMapDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth

                GFX.Device.SetRenderTarget(0, rtClimateMap);
                GFX.Device.SetRenderTarget(1, rtIDMap);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);
                GFX.Device.SetRenderTarget(1, null);

                rtClimateMap.GetTexture().GetData<Color>(colorData, z * stride, stride);
                rtIDMap.GetTexture().GetData<Color>(blendIDData, z * stride, stride);
            }
            GFX.Device.Textures[0] = null;
            GFX.Device.Textures[1] = null;

            gradientTexture.Dispose();

            blendTexture = new Texture3D(GFX.Device, blendMapWidth, blendMapHeight, blendMapDepth, 1, TextureUsage.None, SurfaceFormat.Color);
            blendTexture.SetData<Color>(colorData);

            blendIDTexture = new Texture3D(GFX.Device, blendMapWidth, blendMapHeight, blendMapDepth, 1, TextureUsage.None, SurfaceFormat.Color);
            blendIDTexture.SetData<Color>(blendIDData);

            blendTexture.Save("TestClimateMap.dds", ImageFileFormat.Dds);
            blendIDTexture.Save("TestClimateMapID.dds", ImageFileFormat.Dds);

        }

        void PerformBlur(Texture2D heightmap)
        {
            Shader heightBlur2DShader = ResourceManager.Inst.GetShader("HeightmapBlurMinMax");
            RenderTarget2D paramTarget = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldDepth, 1, SurfaceFormat.Vector4);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(DensityFieldWidth, DensityFieldDepth));
            GFX.Device.SetPixelShaderConstant(0, Vector2.One / new Vector2(DensityFieldWidth, DensityFieldDepth));
            heightBlur2DShader.SetupShader();
            GFX.Device.Textures[0] = heightmap;

            GFX.Device.SetRenderTarget(0, paramTarget);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);

            Shader gradShader = ResourceManager.Inst.GetShader("GradientHeightmap");
            GFX.Device.Textures[0] = paramTarget.GetTexture();
            gradShader.SetupShader();

            RenderTarget2D gradTarget = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldDepth, 1, SurfaceFormat.Single);
            GFX.Device.SetRenderTarget(0, gradTarget);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.Textures[0] = null;

            Shader blurShader = ResourceManager.Inst.GetShader("VoxelBlur3x3x3");

            srcTarget = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldHeight, 1, GFX.Inst.ByteSurfaceFormat);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;

            blurShader.SetupShader();
            GFX.Device.SetPixelShaderConstant(0, Vector3.One / new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth));

            GFX.Device.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
            GFX.Device.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
            GFX.Device.SamplerStates[0].AddressW = TextureAddressMode.Clamp;

            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;

            //Here we generate our noise textures
            int nSize = 32;
            noiseTextures = new Texture3D[4];
            float[] noiseData = new float[nSize * nSize * nSize];
            Random rand = new Random();
            for (int i = 0; i < noiseTextures.Length; i++)
            {
                noiseTextures[i] = new Texture3D(GFX.Device, nSize, nSize, nSize, 1, TextureUsage.None, SurfaceFormat.Single);
                for (int j = 0; j < noiseData.Length; j++)
                {
                    noiseData[j] = (float)(rand.NextDouble() * 2 - 1);
                }
                noiseTextures[i].SetData<float>(noiseData);
            }

            noiseData = null;

            //Lets activate our textures
            for (int i = 0; i < noiseTextures.Length; i++)
                GFX.Device.Textures[i+3] = noiseTextures[i];

            //CopyDensityTextureData(ref DensityField, currDensityField);
            GFX.Device.Textures[0] = heightmap;
            GFX.Device.Textures[1] = paramTarget.GetTexture();
            GFX.Device.Textures[2] = gradTarget.GetTexture();
            for (int z = 0; z < DensityFieldDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(DensityFieldDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth

                GFX.Device.SetRenderTarget(0, srcTarget);
                //GFX.Device.Clear(Color.TransparentBlack);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);

                //Now the copying stage.
                ExtractDensityTextureData(ref DensityField, z);

            }
            GFX.Device.Textures[0] = null;
            /*
            Texture3D voxelTexture = new Texture3D(GFX.Device, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, 1, TextureUsage.None, GFX.Inst.ByteSurfaceFormat);
            switch (GFX.Inst.ByteSurfaceDataType)
            {
                case GFXTextureDataType.BYTE:
                    voxelTexture.SetData<byte>(DensityField);
                    break;
                case GFXTextureDataType.COLOR:
                    Color[] tempColor = new Color[DensityField.Length];
                    for(int i = 0; i < tempColor.Length; i++)
                        tempColor[i] = new Color(DensityField[i], DensityField[i], DensityField[i], DensityField[i]);
                    voxelTexture.SetData<Color>(tempColor);
                    tempColor = null;
                    break;
                case GFXTextureDataType.SINGLE:
                    float[] tempFloat = new float[DensityField.Length];
                    float invScale = 1.0f / 255.0f;
                    for (int i = 0; i < tempFloat.Length; i++)
                        tempFloat[i] = (float)DensityField[i] * invScale;
                    voxelTexture.SetData<float>(tempFloat);
                    tempFloat = null;
                    break;
            }

            Shader blurShaderGeneric = ResourceManager.Inst.GetShader("VoxelBlurGeneric");
            blurShaderGeneric.SetupShader();
            GFX.Device.Textures[0] = voxelTexture;
            for (int z = 0; z < DensityFieldDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(DensityFieldDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth

                GFX.Device.SetRenderTarget(0, srcTarget);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);

                //Now the copying stage.
                ExtractDensityTextureData(ref DensityField, z);

            }
            GFX.Device.Textures[0] = null;
            voxelTexture.Dispose();
            */

            GFX.Device.DepthStencilBuffer = dsOld;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
        }

        void GenerateFloatingIslands(int size)
        {
            DensityFieldWidth = size + 1;
            DensityFieldHeight = size/2 + 1;
            DensityFieldDepth = DensityFieldWidth;

            InitializeFieldData();
            
            //Here we generate our noise textures
            int nSize = 16;
            noiseTextures = new Texture3D[4];
            float[] noiseData = new float[nSize * nSize * nSize];
            Random rand = new Random();
            for (int i = 0; i < noiseTextures.Length; i++)
            {
                noiseTextures[i] = new Texture3D(GFX.Device, nSize, nSize, nSize, 1, TextureUsage.None, SurfaceFormat.Single);
                for (int j = 0; j < noiseData.Length; j++)
                {
                    noiseData[j] = (float)(rand.NextDouble() * 2 - 1);
                }
                noiseTextures[i].SetData<float>(noiseData);
            }

            noiseData = null;

            //The program we'll be using
            Shader islandShader = ResourceManager.Inst.GetShader("ProceduralIsland");
            islandShader.SetupShader();

            GFX.Device.SetPixelShaderConstant(0, Vector3.One / new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth));
            //Lets activate our textures
            for (int i = 0; i < noiseTextures.Length; i++)
                GFX.Device.Textures[i] = noiseTextures[i];

            GFX.Device.SetVertexShaderConstant(1, textureMatrix);

            //Set swizzle axis to the z axis
            GFX.Device.SetPixelShaderConstant(1, Vector4.One * 2);
            
            //Here we setup our render target. 
            //This is used to fetch what is rendered to our screen and store it in a texture.
            srcTarget = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldHeight, 1, GFX.Inst.ByteSurfaceFormat);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;

            for (int z = 0; z < DensityFieldDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(DensityFieldDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth
                
                GFX.Device.SetRenderTarget(0, srcTarget);
                GFX.Device.Clear(Color.TransparentBlack);

                GFXPrimitives.Quad.Render();
    
                GFX.Device.SetRenderTarget(0, null);

                //Now the copying stage.
                ExtractDensityTextureData(ref DensityField, z);

            }
            GFX.Device.DepthStencilBuffer = dsOld;

            InitializeSurfaceIndices();

            InitializeVoxels();
        }

        void CopyDensityTextureData(ref byte[] byteField, Texture3D texture)
        {
            //In the lines below, we copy the texture data into the density field buffer
            if (GFX.Inst.ByteSurfaceDataType == GFXTextureDataType.BYTE)
                texture.SetData<byte>(byteField);
            else
            {
                int elementCount = texture.Width * texture.Height * texture.Depth;
                switch (GFX.Inst.ByteSurfaceDataType)
                {
                    case GFXTextureDataType.COLOR:
                        Color[] colorData = new Color[elementCount];
                        srcTarget.GetTexture().GetData<Color>(colorData);
                        for (int i = 0; i < colorData.Length; i++)
                        {
                            colorData[i].R = byteField[i];
                            colorData[i].G = byteField[i];
                            colorData[i].B = byteField[i];
                            colorData[i].A = byteField[i];
                        }
                        texture.SetData<Color>(colorData);
                        break;
                    case GFXTextureDataType.HALFSINGLE:
                        HalfSingle[] hsingData = new HalfSingle[elementCount];
                        for (int i = 0; i < hsingData.Length; i++)
                            hsingData[i] = new HalfSingle((float)byteField[i] / 255.0f);
                        texture.SetData<HalfSingle>(hsingData);
                        break;
                    case GFXTextureDataType.SINGLE:
                        float[] singData = new float[elementCount];
                        for (int i = 0; i < singData.Length; i++)
                            singData[i] = (float)byteField[i] / 255.0f;
                        texture.SetData<float>(singData);
                        break;
                }
            }
        }

        void ExtractDensityTextureData(ref byte[] byteField, int z)
        {
            //In the lines below, we copy the texture data into the density field buffer
            if (GFX.Inst.ByteSurfaceDataType == GFXTextureDataType.BYTE)
                srcTarget.GetTexture().GetData<byte>(byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
            else
            {
                byte[] densityData = new byte[srcTarget.Width * srcTarget.Height];
                switch (GFX.Inst.ByteSurfaceDataType)
                {
                    case GFXTextureDataType.COLOR:
                        Color[] colorData = new Color[densityData.Length];
                        srcTarget.GetTexture().GetData<Color>(colorData);
                        for (int i = 0; i < colorData.Length; i++)
                            densityData[i] = colorData[i].R;
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                    case GFXTextureDataType.HALFSINGLE:
                        HalfSingle[] hsingData = new HalfSingle[densityData.Length];
                        srcTarget.GetTexture().GetData<HalfSingle>(hsingData);
                        for (int i = 0; i < hsingData.Length; i++)
                            densityData[i] = (byte)(hsingData[i].ToSingle() * 255.0f);
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                    case GFXTextureDataType.SINGLE:
                        float[] singData = new float[densityData.Length];
                        srcTarget.GetTexture().GetData<float>(singData);
                        for (int i = 0; i < singData.Length; i++)
                            densityData[i] = (byte)(singData[i] * 255.0f);
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                }
            }
        }

        void InitializeFieldData()
        {
            int fieldSize = DensityFieldWidth * DensityFieldHeight * DensityFieldDepth;
            DensityField = new byte[fieldSize];
        }

        void InitializeSurfaceIndices()
        {
            surfaceIndices = new int[DensityFieldWidth * DensityFieldDepth];
            for (int z = 0; z < DensityFieldDepth; z++)
            {
                int zOff = z * DensityFieldWidth * DensityFieldHeight;
                for (int x = 0; x < DensityFieldWidth; x++)
                {
                    int indexSurface = x + z * DensityFieldWidth;
                    for (int y = DensityFieldHeight - 1; y >= 0; y--)
                    {
                        int index = x + y * DensityFieldWidth + zOff;
                        surfaceIndices[indexSurface] = -1;
                        if (DensityField[index] >= IsoValue)
                        {
                            surfaceIndices[indexSurface] = y;
                            break;
                        }
                    }
                }
            }
        }

        void InitializeVoxels()
        {
            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;
            Voxels = new VoxelGeometry[voxelCountX * voxelCountY * voxelCountZ];
            VoxelBounds = new BoundingBox[Voxels.Length];
            voxelKDTree = new KDTree<VoxelElement>(VoxelCompareFunction, VoxelBoundsFunction, false, true);
            Vector3 ratio = Vector3.One * 2.0f * (float)VoxelGridSize / new Vector3(DensityFieldWidth-1,DensityFieldHeight-1,DensityFieldDepth-1);

            for (int z = 0; z < voxelCountZ; z++)
            {
                int zOff = voxelCountX * voxelCountY * z;
                for (int y = 0; y < voxelCountY; y++)
                {
                    int yOff = voxelCountX * y;

                    for (int x = 0; x < voxelCountX; x++)
                    {
                        int idx = x + yOff + zOff;
                        /*
                        VoxelBounds[idx] = new BoundingBox(new Vector3(x, y, z) * ratio - Vector3.One, new Vector3(x + 1, y + 1, z + 1) * ratio - Vector3.One);
                        VoxelBounds[idx].Min = Vector3.Transform(VoxelBounds[idx].Min, Transformation.GetTransform());
                        VoxelBounds[idx].Max = Vector3.Transform(VoxelBounds[idx].Max, Transformation.GetTransform());
                        */
                        Voxels[idx] = new VoxelGeometry((ushort)idx);
                        Voxels[idx].renderElement.Transform = new Matrix[1] { Transformation.GetTransform() };
                        Vector3 geometryRatio = 2.0f*Vector3.One / new Vector3(DensityFieldWidth-1,DensityFieldHeight-1,DensityFieldDepth-1);
                        Voxels[idx].GenerateGeometry(ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, VoxelGridSize, VoxelGridSize, VoxelGridSize, x * VoxelGridSize, y * VoxelGridSize, z * VoxelGridSize, geometryRatio, this.Transformation.GetTransform());

                        VoxelBounds[idx] = MathUtils.TransformBounds(Voxels[idx].GetBounds(), Transformation.GetTransform());
                        if(Voxels[idx].CanRender)
                            voxelKDTree.AddElement(new VoxelElement(Voxels[idx], VoxelBounds[idx]), false);
                    }
                }
            }
            voxelKDTree.BuildTree();
            RecursivelyBuildBounds(voxelKDTree.GetRoot());

            terrainQuadMaterial = ResourceManager.Inst.GetMaterial("TerrainQuadMaterial");
            giantQuadElement = GFXPrimitives.Decal.GetRenderElement();
            Matrix quadMatrix = Matrix.CreateScale(1.5f) * Matrix.CreateRotationX(MathHelper.Pi) * this.Transformation.GetTransform();
            quadMatrix.Translation = Vector3.Up * this.Transformation.GetBounds().Min.Y;
            giantQuadElement.Transform = new Matrix[1] { quadMatrix };
        }

        void GenerateCollisionMesh(VoxelGeometry geometry)
        {
            List<Vector3> vertices = new List<Vector3>(geometry.verts.Length);
            List<TriangleVertexIndices> indices = new List<TriangleVertexIndices>(geometry.ib.Length / 3);
            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < geometry.verts.Length; i++)
                vertices.Add(Vector3.Transform(geometry.verts[i].Position, transform));
            for (int i = 0; i < geometry.ib.Length; i += 3)
            {
                TriangleVertexIndices tri = new TriangleVertexIndices(geometry.ib[i + 2], geometry.ib[i + 1], geometry.ib[i]);
                indices.Add(tri);
            }

            TriangleMesh collisionMesh = new TriangleMesh(vertices, indices);
            CollisionSkin collision = new CollisionSkin(null);
            collision.AddPrimitive(collisionMesh, (int)MaterialTable.MaterialID.NotBouncyRough);
            CollisionSkin collision2 = new CollisionSkin(null);
            collision2.AddPrimitive(new JigLibX.Geometry.Plane(Vector3.Up, Transformation.GetBounds().Min.Y-3f), (int)MaterialTable.MaterialID.NotBouncyRough);
            scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision);
            //scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision2);
        }

        public override BoundingBox GetWorldSpaceBoundsAtPoint(Vector3 point, int size)
        {
            Vector3 offset = TerrainSize * Vector3.One * (float)size / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            BoundingBox bounds = new BoundingBox();
            bounds.Min = point - offset;
            bounds.Max = point + offset;

            return bounds;
        }

        public override void CarveTerrainAtPoint(Vector3 point, int size, int isoBrush)
        {
            List<VoxelGeometry> UpdateVoxels = new List<VoxelGeometry>();
            int DensityFieldSqr = DensityFieldWidth * DensityFieldHeight;

            Vector3 pointObjSpace = Vector3.Transform(point, Transformation.GetObjectSpace()) * 0.5f + Vector3.One * 0.5f;
            pointObjSpace *= new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth);

            int xW = (int)MathHelper.Clamp(pointObjSpace.X, 0, DensityFieldWidth - 1);
            int yW = (int)MathHelper.Clamp(pointObjSpace.Y, 0, DensityFieldHeight - 1);
            int zW = (int)MathHelper.Clamp(pointObjSpace.Z, 0, DensityFieldDepth - 1);

            List<int[]> UpdateShifts = new List<int[]>();
            int xStart = (int)MathHelper.Clamp(xW - size, 0, DensityFieldWidth - 1);
            int xEnd = (int)MathHelper.Clamp(xW + size, 0, DensityFieldWidth - 1);
            int yStart = (int)MathHelper.Clamp(yW - size, 0, DensityFieldHeight - 1);
            int yEnd = (int)MathHelper.Clamp(yW + size, 0, DensityFieldHeight - 1);
            int zStart = (int)MathHelper.Clamp(zW - size, 0, DensityFieldDepth - 1);
            int zEnd = (int)MathHelper.Clamp(zW + size, 0, DensityFieldDepth - 1);
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    int yStride = y * DensityFieldWidth;
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int idx = x + yStride + z * DensityFieldSqr;
                        
                        int density = (int)(isoBrush * MathHelper.Clamp(1 - Vector3.Distance(new Vector3(x, y, z), new Vector3(xW, yW, zW)) / (float)size, 0.0f, 1.0f)) + (int)DensityField[idx];
                        DensityField[idx] = (byte)MathHelper.Clamp(density, 0, 255);
                    }
                }
            }

            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;
            int voxelCountSqr = voxelCountX * voxelCountY;

            int bSizeP1 = (int)(size * 1.5f);
            int bSizeN1 = bSizeP1;// brushSize * 2;
            for (int x = xW - bSizeN1; x < xW + bSizeP1; x++)
            {
                for (int y = yW - bSizeN1; y < yW + bSizeP1; y++)
                {
                    for (int z = zW - bSizeN1; z < zW + bSizeP1; z++)
                    {
                        int xV = (int)MathHelper.Clamp((float)voxelCountX * ((float)x / (float)DensityFieldWidth), 0, voxelCountX - 1);
                        int yV = (int)MathHelper.Clamp((float)voxelCountY * ((float)y / (float)DensityFieldHeight), 0, voxelCountY - 1);
                        int zV = (int)MathHelper.Clamp((float)voxelCountZ * ((float)z / (float)DensityFieldDepth), 0, voxelCountZ - 1);
                        int voxelIndex = xV + yV * voxelCountX + zV * voxelCountSqr;
                        if (!UpdateVoxels.Contains(Voxels[voxelIndex]))
                        {
                            UpdateVoxels.Add(Voxels[voxelIndex]);
                            UpdateShifts.Add(new int[] { xV, yV, zV });
                        }
                    }
                }
            }

            Vector3 ratio = 2.0f * (float)VoxelGridSize * Vector3.One / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            for (int i = 0; i < UpdateVoxels.Count; i++)
            {
                UpdateVoxels[i].GenerateGeometry(ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, VoxelGridSize, VoxelGridSize, VoxelGridSize, UpdateShifts[i][0] * VoxelGridSize, UpdateShifts[i][1] * VoxelGridSize, UpdateShifts[i][2] * VoxelGridSize, ratio, this.Transformation.GetTransform());
            }
        }

        public override void OnAdd(Scene scene)
        {
            //for(int i = 0; i < Voxels.Length; i++)
            //    VoxelCollisions[i] = new VoxelCollision(Voxels[i], this.Transformation, VoxelBounds[i], scene);
            base.OnAdd(scene);

            for (int i = 0; i < Voxels.Length; i++)
            {
                
                if (Voxels[i].CanRender)
                    GenerateCollisionMesh(Voxels[i]);
                
            }
        }

        public override void OnUpdate()
        {
            //HandleCameraMotion();
            if (Input.InputManager.Inst.IsKeyDownOnce(Gaia.Input.GameKey.TurnLeft))
                maxDepth = Math.Max(0, maxDepth - 1);
            if (Input.InputManager.Inst.IsKeyDownOnce(Gaia.Input.GameKey.TurnRight))
                maxDepth = maxDepth + 1;
            base.OnUpdate();
        }

        static int VoxelCompareFunction(VoxelElement elementA, VoxelElement elementB, int axis)
        {
            BoundingBox boundsA = elementA.bounds;//.Transform.TransformBounds(elementA.Mesh.GetBounds());

            Vector3 posA = (boundsA.Max + boundsA.Min) * 0.5f;// elementA.Transform.GetPosition();
            float valueA = (axis == 0) ? posA.X : (axis == 1) ? posA.Y : posA.Z;

            BoundingBox boundsB = elementB.bounds;//.Transform.TransformBounds(elementB.Mesh.GetBounds());

            Vector3 posB = (boundsB.Max + boundsB.Min) * 0.5f;//elementB.Transform.GetPosition();
            float valueB = (axis == 0) ? posB.X : (axis == 1) ? posB.Y : posB.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        static Vector2 VoxelBoundsFunction(VoxelElement element, int axis)
        {
            BoundingBox bounds = element.bounds;//.Transform.TransformBounds(element.Mesh.GetBounds());
            return (axis == 0) ? new Vector2(bounds.Min.X, bounds.Max.X) : ((axis == 1) ? new Vector2(bounds.Min.Y, bounds.Max.Y) : new Vector2(bounds.Min.Z, bounds.Max.Z));
        }

        static BoundingBox VoxelBoundsEval(VoxelElement element)
        {
            return element.bounds;
        }
        
        void RecursivelyBuildBounds(KDNode<VoxelElement> node)
        {
            if (node == null)
                return;

            RecursivelyBuildBounds(node.leftChild);
            RecursivelyBuildBounds(node.rightChild);

            if (node.element != null)
            {
                node.bounds = node.element.bounds;
                const float extra = 1.000001f;
                node.bounds.Min = node.bounds.Min * extra;
                node.bounds.Max = node.bounds.Max * extra;
            }
            else
            {
                if (node.leftChild != null)
                    node.bounds = node.leftChild.bounds;
                else
                {
                    if (node.rightChild != null)
                        node.bounds = node.rightChild.bounds;
                    else
                        node.bounds = new BoundingBox(Vector3.One * float.PositiveInfinity, Vector3.One * float.NegativeInfinity);
                }
            }
            if (node.leftChild != null)
            {
                node.bounds.Min = Vector3.Min(node.leftChild.bounds.Min, node.bounds.Min);
                node.bounds.Max = Vector3.Max(node.leftChild.bounds.Max, node.bounds.Max);
            }

            if (node.rightChild != null)
            {
                node.bounds.Min = Vector3.Min(node.rightChild.bounds.Min, node.bounds.Min);
                node.bounds.Max = Vector3.Max(node.rightChild.bounds.Max, node.bounds.Max);
            }
        }
        
        void RecursivelyRender(KDNode<VoxelElement> node, RenderView view)
        {
            if (node == null || (view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint))// && node.bounds.Contains(view.GetPosition()) == ContainmentType.Disjoint))
                return;

            if (node.element != null && (view.GetFrustum().Contains(node.element.bounds) != ContainmentType.Disjoint))
            {
                view.AddElement(terrainMaterial, node.element.geometry.renderElement);
            }
            RecursivelyRender(node.leftChild, view);
            RecursivelyRender(node.rightChild, view);
        }

        Color[] nodeColors = new Color[] { Color.Red, Color.Green, Color.Blue };
        Color[] nodeColorsLeft = new Color[] { Color.Purple, Color.Orange, Color.CornflowerBlue };
        int maxDepth = 5;
        void RecursivelyRenderDebug(KDNode<VoxelElement> node, RenderView view, int depth, bool isLeft)
        {
            if (node == null)// || view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint)
                return;
            
            {
                
                Gaia.Rendering.DebugElementManager debugMgr = (Gaia.Rendering.DebugElementManager)view.GetRenderElementManager(Gaia.Rendering.RenderPass.Debug);
                Color currColor = (isLeft) ? nodeColorsLeft[depth % nodeColorsLeft.Length] : nodeColors[depth % nodeColors.Length];
                if (depth == maxDepth)
                    debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(node.bounds, currColor));

                if (node.element != null)
                {
                    debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(node.element.bounds, Color.White));
                }
            }
            depth++;
            //if (depth < maxDepth)
            {
                RecursivelyRenderDebug(node.leftChild, view, depth, true);
                RecursivelyRenderDebug(node.rightChild, view, depth, false);
            }
        }

        public override void OnRender(RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            view.AddElement(terrainQuadMaterial, giantQuadElement);
            RecursivelyRender(voxelKDTree.GetRoot(), view);
            /*
            if (view.GetRenderType() == RenderViewType.MAIN)
            {
                RecursivelyRenderDebug(voxelKDTree.GetRoot(), view, 0, false);
                GUIElementManager guiElem = GFX.Inst.GetGUI();
                guiElem.AddElement(new GUITextElement(new Vector2(-0.85f, 0.95f), "Depth: " + maxDepth));
            }
            
            for (int i = 0; i < Voxels.Length; i++)
            {
                if (Voxels[i].CanRender && frustum.Contains(VoxelBounds[i]) != ContainmentType.Disjoint)
                {
                    view.AddElement(terrainMaterial, Voxels[i].renderElement);
                }
            }
            */
            base.OnRender(view);
        }
    }
}
