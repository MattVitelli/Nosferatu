﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;

using JigLibX.Geometry;

namespace Gaia.Resources
{
    class Imposter
    {
        public RenderTarget2D BaseMap;
        public RenderTarget2D NormalMap;

        public Material ImposterMaterial;

        public RenderElement Element;

        public Matrix Scale;

        public List<Matrix> transforms = new List<Matrix>();
        public Imposter(Mesh mesh)
        {
            Element = new RenderElement();
            Element.IndexBuffer = GFXPrimitives.ImposterGeometry.GetInstanceIndexBuffer();
            Element.IsAnimated = false;
            Element.PrimitiveCount = 8;
            Element.StartVertex = 0;
            Element.VertexBuffer = GFXPrimitives.ImposterGeometry.GetInstanceVertexBuffer();
            Element.VertexCount = 16;
            Element.VertexDec = GFXVertexDeclarations.PTIDec;
            Element.VertexStride = VertexPTI.SizeInBytes;
            //GFXPrimitives.CreateBillboardElement();
            ImposterMaterial = new Material();
            Vector3 scale = (mesh.GetBounds().Max - mesh.GetBounds().Min);
            Scale = Matrix.CreateScale(scale);
        }
    }

    public class InteractNode
    {
        public BoundingBox Dimensions;
        public string NodeName;
    }

    public class Mesh : IResource
    {
        string name;

        public string Name
        {
            get { return name; }
        }

        bool isLoaded = false;
        string filename;

        bool nodesAreAnimated = true;

        ModelPart[] parts;
        AnimationNode[] nodes;
        SortedList<string, List<ModelPart> > LODS = new SortedList<string, List<ModelPart> >();
        SortedList<string, AnimationNode> namesToNodes = new SortedList<string, AnimationNode>();
        SortedList<string, Matrix> inverseMatrices = new SortedList<string, Matrix>();
        SortedList<int, BoundingBox> hitboxes;
        SortedList<HitType, int[]> hitboxGroupings;
        /*
        Texture2D[] instanceTexture = new Texture2D[3];
        Texture2D[] imposterInstanceTexture = new Texture2D[3];

        Vector4[] instanceTemp;
        Vector4[] imposterInstanceTemp;

        int instanceCount = 0;
        int imposterInstanceCount = 0;
        */

        List<Matrix> transforms = new List<Matrix>();

        InteractNode[] interactNodes;

        AnimationNode[] rootNodes;
        BoundingBox meshBounds;
        TriangleMesh collisionMesh;
        int vertexCount;
        VertexBuffer vertexBuffer;
        VertexPNTTI[] vertices;
        VertexBuffer vertexBufferInstanced;

        Imposter imposterGeometry = null;

        bool useImposter = false;

        bool useInstancing = false;

        bool addedToView = false;

        bool generateHitBoxes = false;

        string hitboxData = string.Empty;

        const float IMPOSTER_DISTANCE = 70;

        public const float IMPOSTER_DISTANCE_SQUARED = IMPOSTER_DISTANCE * IMPOSTER_DISTANCE;

        public void LoadMesh()
        {
            if (isLoaded)
                return;

            isLoaded = true;
            LoadMS3D(filename);

            ModifyMesh();
            
            if (useImposter)
            {
                CreateImposter();
            }

            if (useInstancing)
            {
                CreateInstanceData();
                /*
                if (useImposter)
                {
                    imposterInstanceTemp = new Vector4[instanceTemp.Length];
                    imposterInstanceCount = 0;
                    for (int i = 0; i < imposterInstanceTexture.Length; i++)
                        imposterInstanceTexture[i] = new Texture2D(GFX.Device, GFXShaderConstants.INSTANCE_TEXTURE_SIZE, GFXShaderConstants.INSTANCE_TEXTURE_SIZE, 1, TextureUsage.None, SurfaceFormat.Vector4);
                }
                */
            }

            vertices = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(vertices);

            if(generateHitBoxes)
                ComputeHitBoxes();
        }

        public bool HasHitBoxes()
        {
            return generateHitBoxes;
        }

        public SortedList<int, BoundingBox> GetHitBoxes()
        {
            if (hitboxes == null)
                return null;
            SortedList<int, BoundingBox> hitBoxesCopy = new SortedList<int, BoundingBox>();
            for (int i = 0; i < hitboxes.Count; i++)
            {
                hitBoxesCopy.Add(hitboxes.Keys[i], hitboxes[hitboxes.Keys[i]]);
            }
            return hitBoxesCopy;
        }

        public SortedList<HitType, int[]> GetHitBoxGroups()
        {
            return hitboxGroupings;
        }

        void ComputeHitBoxes()
        {
            hitboxes = new SortedList<int, BoundingBox>();
            for (int i = 0; i < vertices.Length; i++)
            {

                int boneIndex = (int)vertices[i].Index;
                if(!hitboxes.ContainsKey(boneIndex))
                    hitboxes.Add(boneIndex, new BoundingBox(vertices[i].Position, vertices[i].Position));
                BoundingBox bounds = hitboxes[boneIndex];
                bounds.Min = Vector3.Min(vertices[i].Position, bounds.Min);
                bounds.Max = Vector3.Max(vertices[i].Position, bounds.Max);
                hitboxes[boneIndex] = bounds;
            }
            if (hitboxData != string.Empty)
            {
                hitboxGroupings = new SortedList<HitType, int[]>();
                SortedList<string, int> nodesToIndices = new SortedList<string, int>();
                for (int i = 0; i < nodes.Length; i++)
                    nodesToIndices.Add(nodes[i].Name, i);
                string[] hitboxSectors = hitboxData.Split('^');
                for (int l = 0; l < hitboxSectors.Length; l++)
                {
                    string[] data = hitboxSectors[l].Split(' ');
                    HitType currHitType = HitType.None;
                    switch (data[0].ToLower())
                    {
                        case "torso":
                            currHitType = HitType.Torso;
                            break;
                        case "head":
                            currHitType = HitType.Head;
                            break;
                        case "arms":
                            currHitType = HitType.Arms;
                            break;
                        case "legs":
                            currHitType = HitType.Legs;
                            break;
                        case "tail":
                            currHitType = HitType.Tail;
                            break;
                        case "none":
                            currHitType = HitType.None;
                            break;
                    }
                    List<int> indices = new List<int>();
                    for (int j = 1; j < data.Length; j++)
                    {
                        int index = nodesToIndices[data[j]];
                        if(hitboxes.ContainsKey(index))
                            indices.Add(index);
                    }
                    hitboxGroupings.Add(currHitType, indices.ToArray());
                }
            }
        }

        public VertexBuffer GetVertexBuffer(out int vertCount)
        {
            vertCount = vertexCount;
            return vertexBuffer;
        }

        public TriangleMesh GetCollisionMesh()
        {
            return collisionMesh;
        }
        
        public BoundingBox GetBounds()
        {
            return meshBounds;
        }

        public VertexPNTTI GetVertex(int index)
        {
            return vertices[index];
        }

        public AnimationNode[] GetNodes()
        {
            return nodes;
        }

        public InteractNode[] GetInteractNodes()
        {
            return interactNodes;
        }

        class ModelPart
        {
            public RenderElement renderElement;
            public RenderElement renderElementInstanced;
            public string name;
            public Material material;
            public BoundingBox bounds;
        }

        struct ModelVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoord;
            public Vector3 Tangent;
            public Vector3 Tangent2;
            public int BoneIndex;
            public int Weight;

            public void AddNormal(Vector3 normal)
            {
                Normal += normal;
                Weight++;
            }

            public void NormalizeWeights()
            {
                Normal /= Weight;
                TexCoord /= Weight;
                Tangent /= Weight;
                Weight = 1;
            }

            public void AddTangent(ModelVertex v1, ModelVertex v2)
            {
                Vector3 d0 = v1.Position - Position;
                Vector3 d1 = v2.Position - Position;

                Vector2 s = v1.TexCoord - TexCoord;
                Vector2 t = v2.TexCoord - TexCoord;

                float r = 1.0F / (s.X * t.Y - t.X * s.Y); 

                Vector3 sdir = new Vector3(t.Y * d0.X - s.Y * d1.X, t.Y * d0.Y - s.Y * d1.Y, t.Y * d0.Z - s.Y * d1.Z) * r;
                Vector3 tdir = new Vector3(s.X * d1.X - t.X * d0.X, s.X * d1.Y - t.X * d0.Y, s.X * d1.Z - t.X * d0.Z) * r;


                Tangent += sdir;// new Vector3(s.Y * d1.X - t.Y * d0.X, s.Y * d1.Y - t.Y * d0.Y, s.Y * d1.Z - t.Y * d0.Z) * r;
                Tangent2 += tdir;
                Weight++;
            }
            public void AddTangent(Vector3 tangentDir, Vector3 tangentDir2)
            {
                Tangent += tangentDir;
                Tangent2 += tangentDir2;
                Weight++;
            }
        }

        struct Triangle
        {
            public ushort vertex0;
            public ushort vertex1;
            public ushort vertex2;
        }

        public AnimationNode[] GetRootNodes(out SortedList<string, AnimationNode> nodeCollection)
        {
            nodeCollection = new SortedList<string, AnimationNode>();

            if (rootNodes == null)
                return null;

            AnimationNode[] dupNodes = new AnimationNode[rootNodes.Length];
            for (int i = 0; i < rootNodes.Length; i++)
                dupNodes[i] = rootNodes[i].RecursiveCopy(nodeCollection);

            return dupNodes;
        }

        void CheckMissingMaterials(string filename, int[] materialIndices, string[] materialNames)
        {
            SortedList<int, byte> missingIndicesList = new SortedList<int, byte>();
            for (int i = 0; i < materialIndices.Length; i++)
            {
                if (materialIndices[i] != 255 && ResourceManager.Inst.GetMaterial(materialNames[materialIndices[i]]) == null && !missingIndicesList.ContainsKey(materialIndices[i]))
                    missingIndicesList.Add(materialIndices[i], 1);
            }

            if(missingIndicesList.Count == 0)
                return;

            string newFileName = filename.Substring(0, filename.Length - 4) + "_MISSING.txt";

            using (FileStream fs = new FileStream(newFileName, FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    for (int i = 0; i < missingIndicesList.Count; i++)
                        wr.WriteLine(materialNames[missingIndicesList.Keys[i]]);
                }
            }
        }

        void LoadMS3D(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fs, System.Text.Encoding.Default))
                {
                    br.ReadChars(10);
                    br.ReadInt32();
                    vertexCount = br.ReadUInt16();
                    ModelVertex[] vertices = new ModelVertex[vertexCount];

                    Vector3 minVert = Vector3.One * float.PositiveInfinity;
                    Vector3 maxVert = Vector3.One * float.NegativeInfinity;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        br.ReadByte();
                        vertices[i].Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        minVert = Vector3.Min(minVert, vertices[i].Position);
                        maxVert = Vector3.Max(maxVert, vertices[i].Position);
                        vertices[i].BoneIndex = (int)br.ReadChar();
                        if (vertices[i].BoneIndex >= 255)
                            vertices[i].BoneIndex = 0;
                        vertices[i].Weight = 1;
                        br.ReadByte();
                    }

                    ushort triangleCount = br.ReadUInt16();

                    Triangle[] triList = new Triangle[triangleCount];
                    for (int i = 0; i < triangleCount; i++)
                    {
                        br.ReadUInt16(); //flag

                        //Indices
                        ushort v0 = br.ReadUInt16();
                        ushort v1 = br.ReadUInt16();
                        ushort v2 = br.ReadUInt16();
                        triList[i].vertex0 = v0;
                        triList[i].vertex1 = v1;
                        triList[i].vertex2 = v2;

                        //Vertex 0 Normal
                        vertices[v0].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //Vertex 1 Normal
                        vertices[v1].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //Vertex 2 Normal
                        vertices[v2].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //U
                        vertices[v0].TexCoord.X = br.ReadSingle();
                        vertices[v1].TexCoord.X = br.ReadSingle();
                        vertices[v2].TexCoord.X = br.ReadSingle();

                        //V
                        vertices[v0].TexCoord.Y = br.ReadSingle();
                        vertices[v1].TexCoord.Y = br.ReadSingle();
                        vertices[v2].TexCoord.Y = br.ReadSingle();

                        vertices[v0].Weight++;
                        vertices[v1].Weight++;
                        vertices[v2].Weight++;

                        //Smoothing
                        br.ReadByte();

                        //Group index
                        br.ReadByte();
                    }

                    for (int i = 0; i < vertexCount; i++)
                    {
                        vertices[i].Normal /= vertices[i].Weight;
                        vertices[i].Weight = 1;
                    }

                    for (int i = 0; i < triangleCount; i++)
                    {
                        vertices[triList[i].vertex0].AddTangent(vertices[triList[i].vertex1], vertices[triList[i].vertex2]);
                        vertices[triList[i].vertex1].AddTangent(vertices[triList[i].vertex0].Tangent, vertices[triList[i].vertex0].Tangent2);
                        vertices[triList[i].vertex2].AddTangent(vertices[triList[i].vertex0].Tangent, vertices[triList[i].vertex0].Tangent2);
                        //vertices[triList[i].vertex1].AddTangent(vertices[triList[i].vertex0], vertices[triList[i].vertex2]);
                        //vertices[triList[i].vertex2].AddTangent(vertices[triList[i].vertex0], vertices[triList[i].vertex1]);
                    }

                    VertexPNTTI[] verts = new VertexPNTTI[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 N = vertices[i].Normal;
                        N.Normalize();
                        Vector3 tangent = vertices[i].Tangent / vertices[i].Weight;
                        tangent = (tangent - N * Vector3.Dot(N, tangent));
                        tangent.Normalize();
                        vertices[i].Tangent = tangent;
                        vertices[i].Tangent2 /= vertices[i].Weight;
                        vertices[i].Weight = 1;
                        float binormSign = (Vector3.Dot(Vector3.Cross(N, tangent), vertices[i].Tangent2) < 0.0f) ? -1.0f : 1.0f;
                        verts[i] = new VertexPNTTI(vertices[i].Position, vertices[i].Normal, vertices[i].TexCoord, vertices[i].Tangent, vertices[i].BoneIndex, binormSign);
                    }
                    vertexBuffer = new VertexBuffer(GFX.Device, vertexCount * VertexPNTTI.SizeInBytes, BufferUsage.None);
                    vertexBuffer.SetData<VertexPNTTI>(verts);

                    ushort groupCount = br.ReadUInt16();
                    parts = new ModelPart[groupCount];
                    int[] matIndices = new int[groupCount];
                    for (int i = 0; i < groupCount; i++)
                    {
                        br.ReadByte();
                        parts[i] = new ModelPart();
                        parts[i].name = new string(br.ReadChars(32)); //Group Name
                        parts[i].name = parts[i].name.Replace("\0",string.Empty);
                        ushort numTriangles = br.ReadUInt16(); //numTriangles

                        parts[i].renderElement = new RenderElement();
                        parts[i].renderElement.PrimitiveCount = numTriangles;
                        parts[i].renderElement.StartVertex = 0;
                        parts[i].renderElement.VertexBuffer = vertexBuffer;
                        parts[i].renderElement.VertexCount = vertexCount;
                        parts[i].renderElement.VertexStride = VertexPNTTI.SizeInBytes;
                        parts[i].renderElement.VertexDec = GFXVertexDeclarations.PNTTIDec;
                        parts[i].bounds.Max = Vector3.Zero;
                        parts[i].bounds.Min = Vector3.Zero;

                        bool useIntIndices = (numTriangles >= ushort.MaxValue);
                        IndexElementSize size = (useIntIndices) ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits;
                        int stride = (useIntIndices) ? sizeof(uint) : sizeof(ushort);

                        parts[i].renderElement.IndexBuffer = new IndexBuffer(GFX.Device, stride * numTriangles * 3, BufferUsage.None, size);

                        List<ushort> ushortIndices = new List<ushort>();
                        List<uint> uintIndices = new List<uint>();
                        for (int l = 0; l < numTriangles; l++)
                        {
                            ushort t = br.ReadUInt16(); //triangle index
                            if (useIntIndices)
                            {
                                uintIndices.Add((uint)triList[t].vertex2);
                                uintIndices.Add((uint)triList[t].vertex1);
                                uintIndices.Add((uint)triList[t].vertex0);

                            }
                            else
                            {
                                ushortIndices.Add((ushort)triList[t].vertex2);
                                ushortIndices.Add((ushort)triList[t].vertex1);
                                ushortIndices.Add((ushort)triList[t].vertex0);
                            }
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex0].Position);
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex1].Position);
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex2].Position);

                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex0].Position);
                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex1].Position);
                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex2].Position);
                        }
                        if (useIntIndices)
                            parts[i].renderElement.IndexBuffer.SetData<uint>(uintIndices.ToArray());
                        else
                            parts[i].renderElement.IndexBuffer.SetData<ushort>(ushortIndices.ToArray());

                        matIndices[i] = (int)br.ReadChar(); //Material index
                    }

                    meshBounds = new BoundingBox(parts[0].bounds.Min, parts[0].bounds.Max);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        meshBounds.Min = Vector3.Min(parts[i].bounds.Min, meshBounds.Min);
                        meshBounds.Max = Vector3.Max(parts[i].bounds.Max, meshBounds.Max);
                    }

                    ushort MaterialCount = br.ReadUInt16();
                    string[] materialNames = new string[MaterialCount];
                    for (int i = 0; i < MaterialCount; i++)
                    {

                        materialNames[i] = new string(br.ReadChars(32));
                        materialNames[i] = materialNames[i].Replace("\0", string.Empty);
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();

                        br.ReadSingle();
                        br.ReadSingle();
                        br.ReadChar();
                        br.ReadChars(128);
                        br.ReadChars(128);
                    }

                    CheckMissingMaterials(filename, matIndices, materialNames);

                    for (int i = 0; i < groupCount; i++)
                    {
                        int matIndex = matIndices[i];
                        if (matIndex < 255)
                            parts[i].material = ResourceManager.Inst.GetMaterial(materialNames[matIndex]);

                        if (parts[i].material == null)
                            parts[i].material = ResourceManager.Inst.GetMaterial("NULL");
                    }
                    
                    float fps = br.ReadSingle();//FPS
                    br.ReadSingle();//current time
                    int frameCount = br.ReadInt32(); //Total frames

                    ushort boneCount = br.ReadUInt16();
                    nodes = new AnimationNode[boneCount];
                    if (boneCount > 0)
                    {
                        List<string> nodeParentNames = new List<string>();

                        for (int i = 0; i < boneCount; i++)
                        {
                            br.ReadByte(); //flag

                            string name = new string(br.ReadChars(32)).Replace("\0", "");
                            string parentName = new string(br.ReadChars(32)).Replace("\0", "");
                            nodeParentNames.Add(parentName);

                            Vector3 rotation = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                            Vector3 position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                            nodes[i] = new AnimationNode(name, position, rotation);
                            namesToNodes.Add(name, nodes[i]);

                            ushort keyRotCount = br.ReadUInt16(); //Key frame rot count
                            ushort keyPosCount = br.ReadUInt16(); //Key frame pos count
                            //nodes[i].rotationFrames = new ModelBoneAnimationFrame[keyRotCount];
                            //nodes[i].translationFrames = new ModelBoneAnimationFrame[keyPosCount];
                            /*
                            for (int j = 0; j < keyRotCount; j++)
                            {
                                nodes[i].rotationFrames[j].time = br.ReadSingle() * fps; //time
                                nodes[i].rotationFrames[j].Displacement = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                nodes[i].rotationFrames[j].boneName = name;
                            }

                            for (int j = 0; j < keyPosCount; j++)
                            {
                                nodes[i].translationFrames[j].time = br.ReadSingle() * fps; //time
                                nodes[i].translationFrames[j].Displacement = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                nodes[i].translationFrames[j].boneName = name;
                            }*/
                            int count = (keyRotCount + keyPosCount) * 4;
                            for (int j = 0; j < count; j++)
                                br.ReadSingle();
                        }

                        List<AnimationNode> rootNodeList = new List<AnimationNode>();
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            if (namesToNodes.ContainsKey(nodeParentNames[i]))
                            {
                                AnimationNode node = namesToNodes[nodeParentNames[i]];
                                node.children.Add(nodes[i]);
                            }
                            else
                                rootNodeList.Add(nodes[i]);
                        }
                        rootNodes = rootNodeList.ToArray();
                        for (int i = 0; i < rootNodes.Length; i++)
                        {
                            Matrix identityMat = Matrix.Identity;
                            rootNodes[i].ApplyTransform(ref identityMat);
                        }

                        Matrix[] inverseMats = new Matrix[nodes.Length];
                        Matrix[] invRotMats = new Matrix[nodes.Length];
                        for (int i = 0; i < inverseMats.Length; i++)
                        {
                            inverseMats[i] = Matrix.Invert(nodes[i].Transform);
                            invRotMats[i] = MathUtils.Invert3x3(nodes[i].Transform);
                        }

                        if (nodesAreAnimated)
                        {
                            for (int i = 0; i < verts.Length; i++)
                            {
                                verts[i].Position = Vector3.Transform(verts[i].Position, inverseMats[(int)verts[i].Index]);
                                verts[i].Normal = Vector3.TransformNormal(verts[i].Normal, inverseMats[(int)verts[i].Index]);
                                verts[i].Tangent = Vector3.TransformNormal(verts[i].Tangent, inverseMats[(int)verts[i].Index]);
                            }
                        }
                        vertexBuffer.SetData<VertexPNTTI>(verts);
                        
                        
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Vector3 Rotation = nodes[i].Rotation;
                            Matrix transform = Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
                            transform.Translation = nodes[i].Translation;
                            nodes[i].Transform = transform;
                        }
                        
                    }
                }
            }
        }

        void ModifyMesh()
        {
            List<ModelPart> meshes = new List<ModelPart>();
            ModelPart collisionMesh = null;
            List<ModelPart> interactables = new List<ModelPart>();
            for (int i = 0; i < parts.Length; i++)
            {
                string[] meshName = parts[i].name.Split(':');
                if (meshName.Length == 1)
                {
                    if (meshName[0] == "COLLISION")
                        collisionMesh = parts[i];
                    else
                        meshes.Add(parts[i]);                        
                }
                else
                {
                    if (meshName[0] == "INTERACT")
                    {
                        interactables.Add(parts[i]);
                    }
                    else
                    {
                        if (!LODS.ContainsKey(meshName[0]))
                            LODS.Add(meshName[0], new List<ModelPart>());
                        int currLODValue = int.Parse(meshName[1].Substring(3));
                        parts[i].name = currLODValue.ToString();
                        bool addedPart = false;
                        for (int j = 0; j < LODS[meshName[0]].Count; j++)
                        {
                            int LODValue = int.Parse(LODS[meshName[0]][j].name);
                            if (currLODValue < LODValue)
                            {
                                LODS[meshName[0]].Insert(j, parts[i]);
                                addedPart = true;
                                break;
                            }
                        }
                        if (!addedPart)
                            LODS[meshName[0]].Add(parts[i]);
                    }
                }
            }

            if (collisionMesh != null)
                CreateCollisionMesh(collisionMesh);

            interactNodes = new InteractNode[interactables.Count];
            for (int i = 0; i < interactables.Count; i++)
            {
                interactNodes[i] = new InteractNode();
                interactNodes[i].NodeName = interactables[i].name;
                interactNodes[i].Dimensions = interactables[i].bounds;
            }

            parts = meshes.ToArray();
        }

        void CreateCollisionMesh(ModelPart collisionMesh)
        {
            SortedList<ushort, ushort> renamedVertexIndices = new SortedList<ushort, ushort>();
            SortedList<ushort, ushort> renamedVertexIndicesCollision = new SortedList<ushort, ushort>();
            RenderElement collisionElem = collisionMesh.renderElement;
            List<Vector3> collisionVerts = new List<Vector3>();
            List<TriangleVertexIndices> collisionIndices = new List<TriangleVertexIndices>(collisionElem.PrimitiveCount);
            VertexPNTTI[] vertexData = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(vertexData);
            ushort[] indexDataCollision = new ushort[collisionElem.PrimitiveCount * 3];
            collisionElem.IndexBuffer.GetData<ushort>(indexDataCollision);

            for (int i = 0; i < collisionElem.PrimitiveCount; i++)
            {
                int index = i * 3;
                for(int j = 0; j < 3; j++)
                {
                    ushort currIdx = indexDataCollision[index + j];
                    if(!renamedVertexIndicesCollision.ContainsKey(currIdx))
                    {
                        renamedVertexIndicesCollision.Add(currIdx, (ushort)collisionVerts.Count);
                        Vector3 pos;
                        pos.X = vertexData[currIdx].Position.X;
                        pos.Y = vertexData[currIdx].Position.Y;
                        pos.Z = vertexData[currIdx].Position.Z;
                        collisionVerts.Add(pos);
                    }
                }
                ushort idx0 = indexDataCollision[index + 0];
                ushort idx1 = indexDataCollision[index + 1];
                ushort idx2 = indexDataCollision[index + 2];
                collisionIndices.Add(new TriangleVertexIndices(renamedVertexIndicesCollision[idx2], renamedVertexIndicesCollision[idx1], renamedVertexIndicesCollision[idx0]));
            }
            /*
            this.collisionMesh = new TriangleMesh();
            this.collisionMesh.CreateMesh(collisionVerts, collisionIndices, 0, 0);
            */
            this.collisionMesh = new TriangleMesh(collisionVerts, collisionIndices);

            List<VertexPNTTI> newVertexData = new List<VertexPNTTI>();
            for (int i = 0; i < parts.Length; i++)
            {
                RenderElement currElem = parts[i].renderElement;
                ushort[] indexData = new ushort[currElem.PrimitiveCount * 3];
                currElem.IndexBuffer.GetData<ushort>(indexData);
                for (int j = 0; j < indexData.Length; j++)
                {
                    if (!renamedVertexIndices.ContainsKey(indexData[j]))
                    {
                        renamedVertexIndices.Add(indexData[j], (ushort)newVertexData.Count);
                        newVertexData.Add(vertexData[indexData[j]]);
                    }
                    indexData[j] = renamedVertexIndices[indexData[j]];
                }
                currElem.IndexBuffer.SetData<ushort>(indexData);
            }
            vertexBuffer.Dispose();

            vertexCount = newVertexData.Count;
            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTTI.SizeInBytes * newVertexData.Count, BufferUsage.None);
            vertexBuffer.SetData<VertexPNTTI>(newVertexData.ToArray());
            for (int i = 0; i < parts.Length; i++)
                parts[i].renderElement.VertexBuffer = vertexBuffer;
        }

        void InjectTransform(ref Matrix transform, ref Vector4[] transformField, ref int transformCount, ref Texture2D[] transformTextures)
        {
            int stride = transformTextures[0].Width * transformTextures[0].Height;
            int idxA = transformCount;
            int idxB = idxA + stride;
            int idxC = idxB + stride;
            transformField[idxA].X = transform.M11;
            transformField[idxA].Y = transform.M21;
            transformField[idxA].Z = transform.M31;
            transformField[idxA].W = transform.M41;

            transformField[idxB].X = transform.M12;
            transformField[idxB].Y = transform.M22;
            transformField[idxB].Z = transform.M32;
            transformField[idxB].W = transform.M42;

            transformField[idxC].X = transform.M13;
            transformField[idxC].Y = transform.M23;
            transformField[idxC].Z = transform.M33;
            transformField[idxC].W = transform.M43;
            transformCount = transformCount+1;
            /*
            if (idxA + 1 >= GFXShaderConstants.NUM_INSTANCES)
            {
                Vector4[] temp = new Vector4[instanceTexture[0].Width * instanceTexture[0].Height];
                int startIndex = transformCount - GFXShaderConstants.NUM_INSTANCES;
                transformTextures[0].GetData<Vector4>(temp);
                Array.Copy(transformField, 0, temp, startIndex, GFXShaderConstants.NUM_INSTANCES);
                transformTextures[0].SetData<Vector4>(temp);

                transformTextures[1].GetData<Vector4>(temp);
                Array.Copy(transformField, GFXShaderConstants.NUM_INSTANCES, temp, startIndex, GFXShaderConstants.NUM_INSTANCES);
                transformTextures[1].SetData<Vector4>(temp);

                transformTextures[2].GetData<Vector4>(temp);
                Array.Copy(transformField, GFXShaderConstants.NUM_INSTANCES*2, temp, startIndex, GFXShaderConstants.NUM_INSTANCES);
                transformTextures[2].SetData<Vector4>(temp);
                temp = null;
            }
            */
        }

        void FinalizeInstanceTexture(ref Vector4[] transforms, ref Texture2D[] transformTextures)
        {
            int stride = transformTextures[0].Width * transformTextures[0].Height;
            transformTextures[0].SetData<Vector4>(transforms, 0, stride, SetDataOptions.None);
            transformTextures[1].SetData<Vector4>(transforms, stride, stride, SetDataOptions.None);
            transformTextures[2].SetData<Vector4>(transforms, stride*2, stride, SetDataOptions.None);
        }

        public void RenderNoLOD(Matrix transform, RenderView view)
        {
            BoundingFrustum frustum = new BoundingFrustum(transform * view.GetViewProjection());

            for (int i = 0; i < parts.Length; i++)
            {
                if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                {
                    RenderElement element = parts[i].renderElement;
                    element.Transform = new Matrix[1] { transform };
                    view.AddElement(parts[i].material, element);
                }
            }
        }
        
        public void RenderPostSceneQuery(RenderView view)
        {
            if (!useInstancing)
                return;

            addedToView = false;
            if (transforms.Count > 0)
            {
                Matrix[] tempTransforms = transforms.ToArray();
                for (int i = 0; i < parts.Length; i++)
                {
                    RenderElement elem = parts[i].renderElementInstanced;
                    elem.Transform = tempTransforms;
                    view.AddElement(parts[i].material, elem);
                }
                tempTransforms = null;
                transforms.Clear();
            }
            if (imposterGeometry != null)
            {
                RenderElement elem = imposterGeometry.Element;
                elem.Transform = imposterGeometry.transforms.ToArray();
                view.AddElement(imposterGeometry.ImposterMaterial, elem);
                imposterGeometry.transforms.Clear();
            }
            /*
            if (instanceCount > 0)
            {
                FinalizeInstanceTexture(ref instanceTemp, ref instanceTexture);
                for (int i = 0; i < parts.Length; i++)
                {
                    for(int j = 0; j < instanceTexture.Length; j++)
                        parts[i].material.SetVertexTexture(j, instanceTexture[j]);
                    parts[i].renderElementInstanced.InstanceCount = instanceCount;
                    view.AddElement(parts[i].material, parts[i].renderElementInstanced);
                }
                instanceCount = 0;
            }
            if (imposterGeometry != null)
            {  
                if (imposterInstanceCount > 0)
                {
                    FinalizeInstanceTexture(ref imposterInstanceTemp, ref imposterInstanceTexture);
                    imposterGeometry.Element.InstanceCount = imposterInstanceCount;
                    for (int j = 0; j < instanceTexture.Length; j++)
                        imposterGeometry.ImposterMaterial.SetVertexTexture(j, imposterInstanceTexture[j]);
                    view.AddElement(imposterGeometry.ImposterMaterial, imposterGeometry.Element);
                    imposterInstanceCount = 0;
                }
            }
            */
        }
        
        public void RenderImposters(Matrix transform, RenderView view, bool performCulling)
        {
            
            if (useInstancing && !addedToView)
            {
                view.AddMeshToRender(this);
                addedToView = true;
            }
            
            if (performCulling)
            {
                BoundingFrustum frustum = view.GetFrustum();
                Matrix oldMat = frustum.Matrix;
                frustum.Matrix = transform * view.GetViewProjection();
                if (imposterGeometry != null && frustum.Contains(meshBounds) != ContainmentType.Disjoint)
                {
                    Matrix tempTransform = imposterGeometry.Scale * transform;
                    if (useInstancing)
                    {
                        imposterGeometry.transforms.Add(tempTransform);
                        //InjectTransform(ref tempTransform, ref imposterInstanceTemp, ref imposterInstanceCount, ref imposterInstanceTexture);
                    }
                    else
                    {
                        imposterGeometry.Element.Transform = new Matrix[1] {tempTransform};
                        view.AddElement(imposterGeometry.ImposterMaterial, imposterGeometry.Element);
                    }
                }
                frustum.Matrix = oldMat;
            }
            else if (imposterGeometry != null)
            {
                Matrix tempTransform = imposterGeometry.Scale * transform;
                if (useInstancing)
                {
                    imposterGeometry.transforms.Add(tempTransform);
                    //InjectTransform(ref transform, ref imposterInstanceTemp, ref imposterInstanceCount, ref imposterInstanceTexture);
                }
                else
                {
                    imposterGeometry.Element.Transform = new Matrix[1] {tempTransform};
                    view.AddElement(imposterGeometry.ImposterMaterial, imposterGeometry.Element);
                }
            }
        }

        public void Render(Matrix transform, RenderView view, bool performCulling)
        {

            if (useInstancing && !addedToView)
            {
                view.AddMeshToRender(this);
                addedToView = true;
            }
            
            if (performCulling)
            {
                BoundingFrustum frustum = view.GetFrustum();
                Matrix oldMat = frustum.Matrix;
                frustum.Matrix = transform * view.GetViewProjection();
                if (frustum.Contains(meshBounds) != ContainmentType.Disjoint)
                {
                    if (useInstancing)
                    {
                        transforms.Add(transform);
                        //InjectTransform(ref transform, ref instanceTemp, ref instanceCount, ref instanceTexture);
                    }
                    else
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                            {
                                parts[i].renderElement.Transform = new Matrix[1] {transform};
                                view.AddElement(parts[i].material, parts[i].renderElement);
                            }
                        }
                    }
                    
                }
                frustum.Matrix = oldMat;
            }
            else
            {
                if (useInstancing)
                {
                    transforms.Add(transform);
                    //InjectTransform(ref transform, ref instanceTemp, ref instanceCount, ref instanceTexture);
                }
                else
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i].renderElement.Transform = new Matrix[1] { transform };
                        view.AddElement(parts[i].material, parts[i].renderElement);
                    }
                }
            }
        }

        public void Render(Matrix[] animTransforms, RenderView view, bool performCulling)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                RenderElement element = parts[i].renderElement;
                element.Transform = animTransforms;
                element.IsAnimated = true;
                view.AddElement(parts[i].material, element);
            }
        }

        public void Render(Matrix transform, VertexBuffer animBuffer, RenderView view, bool performCulling)
        {
            if (performCulling)
            {
                BoundingFrustum frustum = view.GetFrustum();
                Matrix oldMat = frustum.Matrix;
                frustum.Matrix = transform * view.GetViewProjection();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                    {
                        RenderElement element = parts[i].renderElement;
                        element.VertexBuffer = animBuffer;
                        element.Transform = new Matrix[1] { transform };
                        element.IsAnimated = true;
                        view.AddElement(parts[i].material, element);
                    }
                }
                frustum.Matrix = oldMat;
            }
            else
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    RenderElement element = parts[i].renderElement;
                    element.VertexBuffer = animBuffer;
                    element.Transform = new Matrix[1] { transform };
                    element.IsAnimated = true;
                    view.AddElement(parts[i].material, element);
                }
            }
        }

        /*
        public void Render(Matrix transform, SortedList<string, AnimationNode> animNodes, RenderView view, bool performCulling)
        {
            if (performCulling)
            {
                BoundingFrustum frustum = view.GetFrustum();
                Matrix oldMat = frustum.Matrix;
                frustum.Matrix = transform * view.GetViewProjection();
                Matrix[] transforms = new Matrix[nodes.Length];
                for (int i = 0; i < nodes.Length; i++)
                    transforms[i] = animNodes[nodes[i].Name].Transform;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                    {
                        RenderElement element = parts[i].renderElement;
                        element.Transform = transforms;
                        element.IsAnimated = true;
                        view.AddElement(parts[i].material, element);
                    }
                }
                frustum.Matrix = oldMat;
            }
            else
            {
                Matrix[] transforms = new Matrix[nodes.Length];
                for (int i = 0; i < nodes.Length; i++)
                    transforms[i] = animNodes[nodes[i].Name].Transform;
                for (int i = 0; i < parts.Length; i++)
                {
                    RenderElement element = parts[i].renderElement;
                    element.Transform = transforms;
                    element.IsAnimated = true;
                    view.AddElement(parts[i].material, element);
                }
            }
        }
        */
        
        void CreateInstanceData()
        {
            /*
            instanceTemp = new Vector4[GFXShaderConstants.INSTANCE_TEXTURE_SIZE*GFXShaderConstants.INSTANCE_TEXTURE_SIZE * 3];
            instanceCount = 0;
            for (int i = 0; i < instanceTexture.Length; i++)
                instanceTexture[i] = new Texture2D(GFX.Device, GFXShaderConstants.INSTANCE_TEXTURE_SIZE, GFXShaderConstants.INSTANCE_TEXTURE_SIZE, 1, TextureUsage.None, SurfaceFormat.Vector4);
            */
            VertexPNTTI[] vertData = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(vertData);
            VertexPNTTI[] instVerts = new VertexPNTTI[vertexCount * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    
                    int index = i * vertexCount + j;
                    instVerts[index] = vertData[j];
                    instVerts[index].Index = i;
                }
            }

            vertexBufferInstanced = new VertexBuffer(GFX.Device, instVerts.Length * VertexPNTTI.SizeInBytes, BufferUsage.None);
            vertexBufferInstanced.SetData<VertexPNTTI>(instVerts);

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].renderElementInstanced = parts[i].renderElement;
                IndexElementSize elementSize = parts[i].renderElement.IndexBuffer.IndexElementSize;
                IndexBuffer indexBufferInstanced;

                if (instVerts.Length > ushort.MaxValue)
                    elementSize = IndexElementSize.ThirtyTwoBits;
                if (elementSize == IndexElementSize.SixteenBits)
                {
                    ushort[] indexData = new ushort[parts[i].renderElement.PrimitiveCount * 3];
                    parts[i].renderElement.IndexBuffer.GetData<ushort>(indexData);

                    ushort[] instIB = new ushort[indexData.Length * GFXShaderConstants.NUM_INSTANCES];
                    for (int k = 0; k < GFXShaderConstants.NUM_INSTANCES; k++)
                    {
                        for (int j = 0; j < indexData.Length; j++)
                        {
                            int newIndex = indexData[j] + k * vertexCount;
                            if (newIndex > ushort.MaxValue)
                                Console.WriteLine("This is very very bad!");
                            instIB[k * indexData.Length + j] = (ushort)newIndex;
                        }
                    }

                    indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(ushort) * instIB.Length, BufferUsage.None, elementSize);

                    indexBufferInstanced.SetData<ushort>(instIB);
                }
                else
                {
                    ushort[] indexData = new ushort[parts[i].renderElement.PrimitiveCount * 3];
                    parts[i].renderElement.IndexBuffer.GetData<ushort>(indexData);

                    uint[] instIB = new uint[indexData.Length * GFXShaderConstants.NUM_INSTANCES];
                    for (int k = 0; k < GFXShaderConstants.NUM_INSTANCES; k++)
                    {
                        for (int j = 0; j < indexData.Length; j++)
                        {
                            ulong index = (ulong)indexData[j] + (ulong)k * (ulong)vertexCount;
                            if (index > ulong.MaxValue)
                                Console.WriteLine("This is very very bad!");
                            instIB[k * indexData.Length + j] = (uint)(indexData[j] + k * vertexCount);
                        }
                    }

                    indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(uint) * instIB.Length, BufferUsage.None, elementSize);

                    indexBufferInstanced.SetData<uint>(instIB);
                }
                parts[i].renderElementInstanced.VertexDec = GFXVertexDeclarations.PNTTIDec;
                parts[i].renderElementInstanced.VertexStride = VertexPNTTI.SizeInBytes;
                parts[i].renderElementInstanced.IsAnimated = false;
                parts[i].renderElementInstanced.VertexBuffer = vertexBufferInstanced;
                parts[i].renderElementInstanced.IndexBuffer = indexBufferInstanced;
            }
        }

        void CreateImposter()
        {
            const int textureSize = 128;
            const int numViews = 4;

            int textureWidth = textureSize * numViews;
            int textureHeight = textureSize;

            imposterGeometry = new Imposter(this);
            imposterGeometry.BaseMap = new RenderTarget2D(GFX.Device, textureWidth, textureHeight, 1, SurfaceFormat.Color);
            imposterGeometry.NormalMap = new RenderTarget2D(GFX.Device, textureWidth, textureHeight, 1, SurfaceFormat.Vector2);
            
            ImposterRenderView renderViewImposter = new ImposterRenderView(Matrix.Identity, Matrix.Identity, Vector3.Zero, 1.0f, 1000.0f);

            Vector3 centerPos = (this.meshBounds.Min + this.meshBounds.Max)*0.5f;
            float rad = Math.Max(this.meshBounds.Min.Length(), this.meshBounds.Max.Length());

            renderViewImposter.SetNearPlane(1.0f);
            renderViewImposter.SetFarPlane(rad * rad);
            renderViewImposter.SetProjection(Matrix.CreateOrthographicOffCenter(-rad * 0.5f, rad * 0.5f, -rad*0.5f, rad*0.5f, 1.0f, rad * rad));

            Viewport oldViewport = GFX.Device.Viewport;
            DepthStencilBuffer oldDSBuffer = GFX.Device.DepthStencilBuffer;
            DepthStencilBuffer dsBufferImposter = new DepthStencilBuffer(GFX.Device, textureWidth, textureHeight, oldDSBuffer.Format);
            GFX.Device.DepthStencilBuffer = dsBufferImposter;

            float deltaTheta = MathHelper.TwoPi / (float)numViews;

            GFX.Device.SetRenderTarget(0, imposterGeometry.BaseMap);
            GFX.Device.SetRenderTarget(1, imposterGeometry.NormalMap);
            GFX.Device.Clear(Color.TransparentBlack);
            
            for (int i = 0; i < numViews; i++)
            {
                float theta = deltaTheta * i;
                Vector3 offset = new Vector3((float)Math.Sin(theta), 0, (float)Math.Cos(theta)) * rad;
                Vector3 camPos = centerPos + offset;

                renderViewImposter.SetPosition(camPos);
                renderViewImposter.SetView(Matrix.CreateLookAt(camPos, centerPos, Vector3.Up));

                Viewport newViewport = new Viewport();
                newViewport.X = i * textureSize;
                newViewport.Y = 0;
                newViewport.Width = textureSize;
                newViewport.Height = textureHeight;

                GFX.Device.Viewport = newViewport;
                this.RenderNoLOD(Matrix.Identity, renderViewImposter);
                renderViewImposter.Render();
            }

            GFX.Device.SetRenderTarget(1, null);

            for (int i = 0; i < numViews; i++)
            {
                float theta = deltaTheta * i;
                Vector3 offset = new Vector3((float)Math.Cos(theta), 0, (float)Math.Sin(theta)) * rad;
                Vector3 camPos = centerPos + offset;

                renderViewImposter.SetPosition(camPos);
                renderViewImposter.SetView(Matrix.CreateLookAt(camPos, centerPos, Vector3.Up));

                Viewport newViewport = new Viewport();
                newViewport.X = i * textureSize;
                newViewport.Y = 0;
                newViewport.Width = textureSize;
                newViewport.Height = textureHeight;

                GFX.Device.Viewport = newViewport;
                this.RenderNoLOD(Matrix.Identity, renderViewImposter);
                renderViewImposter.RenderBlended();
            }

            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.Viewport = oldViewport;
            GFX.Device.DepthStencilBuffer = oldDSBuffer;
            dsBufferImposter.Dispose();
            imposterGeometry.BaseMap.GetTexture().Save("BaseMapImposter.dds", ImageFileFormat.Dds);

            imposterGeometry.ImposterMaterial.SetShader(ResourceManager.Inst.GetShader("ImposterShader"));
            TextureResource baseMap = new TextureResource();
            baseMap.SetTexture(TextureResourceType.Texture2D, imposterGeometry.BaseMap.GetTexture());
            imposterGeometry.BaseMap.GetTexture().GenerateMipMaps(TextureFilter.GaussianQuad);
            TextureResource normalMap = new TextureResource();
            normalMap.SetTexture(TextureResourceType.Texture2D, imposterGeometry.NormalMap.GetTexture());
            imposterGeometry.ImposterMaterial.SetTexture(0, baseMap);
            imposterGeometry.ImposterMaterial.SetTexture(1, normalMap);
            imposterGeometry.ImposterMaterial.SetName(name + "_IMPOSTER_MATERIAL");
            imposterGeometry.ImposterMaterial.IsFoliage = true;
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "filename":
                        filename = attrib.Value;
                        break;
                    case "name":
                        name = attrib.Value;
                        break;
                    case "useimposter":
                        useImposter = bool.Parse(attrib.Value);
                        break;
                    case "useinstancing":
                        useInstancing = bool.Parse(attrib.Value);
                        break;
                    case "nodesanimated":
                        nodesAreAnimated = bool.Parse(attrib.Value);
                        break;
                    case "generatehitboxes":
                        generateHitBoxes = bool.Parse(attrib.Value);
                        break;
                    case "hitgroups":
                        hitboxData = attrib.Value;
                        break;
                }
            }
        }

        void IResource.Destroy()
        {

        }
    }

    public class AnimationNode
    {
        public Vector3 Translation;
        public Vector3 Rotation;
        public Matrix Transform;
        public Matrix TransformIT;
        public string Name;

        public List<AnimationNode> children = new List<AnimationNode>();

        public AnimationNode(string name, Vector3 translation, Vector3 rotation)
        {
            Name = name;
            Translation = translation;
            Rotation = rotation;
        }

        public void ApplyTransform(ref Matrix parentTransform)
        {
            Rotation.X = MathHelper.WrapAngle(Rotation.X);
            Rotation.Y = MathHelper.WrapAngle(Rotation.Y);
            Rotation.Z = MathHelper.WrapAngle(Rotation.Z);
            Matrix tempTransform = Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
            tempTransform.Translation = Translation;

            Transform = tempTransform * parentTransform;
            TransformIT = Matrix.Invert(Matrix.Transpose(Transform));
            for (int i = 0; i < children.Count; i++)
                children[i].ApplyTransform(ref Transform);
        }

        public AnimationNode RecursiveCopy(SortedList<string, AnimationNode> nodeCollection)
        {
            AnimationNode node = new AnimationNode(this.Name, this.Translation, this.Rotation);
            nodeCollection.Add(node.Name, node);
            for (int i = 0; i < this.children.Count; i++)
            {
                AnimationNode child = this.children[i].RecursiveCopy(nodeCollection);
                node.children.Add(child);
            }
            return node;
        }
    }
}
