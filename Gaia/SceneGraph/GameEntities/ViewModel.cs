using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Animation;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;
using Gaia.Physics;

namespace Gaia.SceneGraph.GameEntities
{
    public class ViewModel
    {
        protected Mesh mesh;
        protected VertexBuffer vertexBuffer;
        protected VertexPNTTI[] vertices;
        protected RenderElement[] renderElements;
        protected AnimationNode[] rootNodes;
        protected AnimationNode[] orderedNodes;
        protected Matrix[] transforms;
        protected Matrix[] transformsIT;
        protected SortedList<string, AnimationNode> nodes;
        //protected SortedList<string, AnimationLayer> animationLayers = new SortedList<string, AnimationLayer>();
        protected SortedList<string, Vector3> defaultTranslations = new SortedList<string, Vector3>();
        protected SortedList<string, Vector3> defaultRotations = new SortedList<string, Vector3>();
        protected AnimationLayer mainAnimationLayer;
        protected SortedList<int, BoundingBox> hitBoxes;
        protected SortedList<int, BoundingBox> hitBoxesTransformed;
        protected SortedList<HitType, BoundingBox> hitBoxesGrouped;
        protected SortedList<HitType, int[]> hitBoxGroups;
        BoundingBox hitBounds;
        Vector3 hitBoxExtRight = Vector3.Zero;
        Vector3 hitBoxExtFwd = Vector3.Zero;

        protected Ragdoll ragdoll = null;
        protected bool ragdollEnabled = false;

        Scene scene;
        Transform transform;

        Matrix customMatrix = Matrix.Identity;

        Matrix worldMat = Matrix.Identity;
        BoundingBox worldBounds;

        bool renderAlways = true;

        public ViewModel(string name)
        {
            InitializeMesh(name);
        }

        public void SetRagdoll(Ragdoll ragdoll)
        {
            this.ragdoll = ragdoll;
        }

        public void SetRagdollEnabled(bool enabled)
        {
            this.ragdollEnabled = enabled;
        }

        public void SetRenderAlways(bool renderAlways, Scene scene)
        {
            this.renderAlways = renderAlways;
            this.scene = scene;
        }

        public BoundingBox GetMeshBounds()
        {
            return mesh.GetBounds();
        }

        public BoundingBox GetHitBounds()
        {
            return hitBounds;
        }

        public AnimationLayer GetAnimationLayer()
        {
            return mainAnimationLayer;
        }

        public void SetCustomMatrix(Matrix value)
        {
            customMatrix = value;
        }

        protected void InitializeMesh(string name)
        {
            mesh = ResourceManager.Inst.GetMesh(name);
            rootNodes = mesh.GetRootNodes(out nodes);
            CreateHitBoxes();
            /*
            int vertexCount = 0;
            VertexBuffer origBuffer = mesh.GetVertexBuffer(out vertexCount);
            vertices = new VertexPNTTI[vertexCount];
            origBuffer.GetData<VertexPNTTI>(vertices);
            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTTI.SizeInBytes * vertexCount, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPNTTI>(vertices);
            */
            List<AnimationNode> orderedNodes = new List<AnimationNode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                defaultTranslations.Add(currKey, nodes[currKey].Translation);
                defaultRotations.Add(currKey, nodes[currKey].Rotation);
            }

            AnimationNode[] tempNodes = mesh.GetNodes();
            for(int i = 0; i < tempNodes.Length; i++)
            {
                orderedNodes.Add(nodes[tempNodes[i].Name]);
            }
            this.orderedNodes = orderedNodes.ToArray();
            this.transforms = new Matrix[orderedNodes.Count];

            mainAnimationLayer = new AnimationLayer(this);
        }

        public void SetTransform(Transform transform)
        {
            this.transform = transform;
        }

        /*
        public void SetAnimationLayer(string name, float weight)
        {
            if (!animationLayers.ContainsKey(name))
                animationLayers.Add(name, new AnimationLayer(name, this, weight));
            else
            {
                animationLayers[name].Weight = weight;
            }
        }

        public void RemoveAnimationLayer(string name)
        {
            if (animationLayers.ContainsKey(name))
                animationLayers.Remove(name);
        }
        */

        protected void UpdateAnimation(float timeDT)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                nodes[currKey].Translation = defaultTranslations[currKey];
                nodes[currKey].Rotation = defaultRotations[currKey];
            }
            /*
            for (int i = 0; i < animationLayers.Count; i++)
            {
                animationLayers.Values[i].UpdateAnimation(timeDT, this.nodes);
            }
            */
            mainAnimationLayer.UpdateAnimation(timeDT, this.nodes);
            if (renderAlways || (scene != null && scene.TestBoundsVisibility(worldBounds)))
            {
                Matrix root = worldMat;// Matrix.Identity;
                for (int i = 0; i < rootNodes.Length; i++)
                    rootNodes[i].ApplyTransform(ref root);
                
                for (int i = 0; i < orderedNodes.Length; i++)
                {
                    transforms[i] = orderedNodes[i].Transform;
                }
                /*
                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexPNTTI currVertex = mesh.GetVertex(i);
                    int index = (int)currVertex.Index;
                    vertices[i].Position = Vector3.Transform(currVertex.Position, orderedNodes[index].Transform);
                    vertices[i].Normal = Vector3.TransformNormal(currVertex.Normal, orderedNodes[index].Transform);//.TransformIT);
                    vertices[i].Tangent = Vector3.TransformNormal(currVertex.Tangent, orderedNodes[index].Transform);//.TransformIT);
                    vertices[i].Index = 0;
                }
                vertexBuffer.SetData<VertexPNTTI>(vertices);
                */
                UpdateHitBoxes();
            }
        }

        void CreateHitBoxes()
        {
            hitBoxes = mesh.GetHitBoxes();
            hitBoxesTransformed = mesh.GetHitBoxes();
            hitBoxesGrouped = new SortedList<HitType, BoundingBox>();
            hitBoxGroups = mesh.GetHitBoxGroups();
        }

        void UpdateHitBoxes()
        {
            if (hitBoxes != null)
            {
                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    int currKey = hitBoxes.Keys[i];

                    BoundingBox bounds = hitBoxes[currKey];
                    hitBoxesTransformed[currKey] = MathUtils.TransformBounds(bounds, orderedNodes[currKey].Transform);// MathUtils.TransformBounds(MathUtils.TransformBounds(bounds, orderedNodes[currKey].Transform), worldMat);
                    if (i == 0)
                        hitBounds = hitBoxesTransformed[currKey];
                    else
                    {
                        hitBounds.Min = Vector3.Min(hitBounds.Min, hitBoxesTransformed[currKey].Min);
                        hitBounds.Max = Vector3.Max(hitBounds.Max, hitBoxesTransformed[currKey].Max);
                    }
                }

                Vector3 netExt = (hitBoxExtRight + hitBoxExtFwd)*0.5f;
                hitBounds.Min -= netExt;
                hitBounds.Max += netExt;

                for (int i = 0; i < hitBoxGroups.Count; i++)
                {
                    HitType currKey = hitBoxGroups.Keys[i];
                    if (!hitBoxesGrouped.ContainsKey(currKey))
                            hitBoxesGrouped.Add(currKey, new BoundingBox());
                    int currIndex = hitBoxGroups[currKey][0];
                    BoundingBox bounds = hitBoxesTransformed[currIndex];
                    for (int j = 1; j < hitBoxGroups[currKey].Length; j++)
                    {
                        currIndex = hitBoxGroups[currKey][j];
                        bounds.Min = Vector3.Min(bounds.Min, hitBoxesTransformed[currIndex].Min);
                        bounds.Max = Vector3.Max(bounds.Max, hitBoxesTransformed[currIndex].Max);
                    }

                    bounds.Min -= netExt;
                    bounds.Max += netExt;

                    hitBoxesGrouped[currKey] = bounds;
                }
            }
        }

        public SortedList<HitType, BoundingBox> GetHitBoxes()
        {
            return hitBoxesGrouped;
        }

        public void OnUpdate()
        {
            worldMat = customMatrix * transform.GetTransform();
            worldBounds = MathUtils.TransformBounds(mesh.GetBounds(), worldMat);
            UpdateAnimation(Time.GameTime.ElapsedTime);
        }

        public void SetHitboxExtensionVectors(Vector3 strafeVector, Vector3 forwardVector)
        {
            Vector3 absRight = new Vector3(Math.Abs(strafeVector.X), Math.Abs(strafeVector.Y), Math.Abs(strafeVector.Z));
            Vector3 absFwd = new Vector3(Math.Abs(forwardVector.X), Math.Abs(forwardVector.Y), Math.Abs(forwardVector.Z));
            hitBoxExtRight = absRight;
            hitBoxExtFwd = absFwd;
        }

        public void OnRender(RenderView view, bool performCulling)
        {
            if (performCulling && view.GetFrustum().Contains(hitBounds) == ContainmentType.Disjoint)
                return;

            if (rootNodes != null && rootNodes.Length > 0)
            {
                //mesh.Render(worldMat, vertexBuffer, view, performCulling);
                mesh.Render(transforms, view, performCulling);
            }
            else
                mesh.Render(worldMat, view, performCulling);
        }

        public void RenderDebug(RenderView view)
        {
            /*
            for (int i = 0; i < hitBoxesTransformed.Count; i++)
            {
                DebugElementManager debugMgr = (DebugElementManager)view.GetRenderElementManager(RenderPass.Debug);
                debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(hitBoxesTransformed.Values[i], Color.Red));
            }
            */
            DebugElementManager debugMgr = (DebugElementManager)view.GetRenderElementManager(RenderPass.Debug);
            for (int i = 0; i < hitBoxesGrouped.Count; i++)
            {
                debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(hitBoxesGrouped.Values[i], Color.Red));
            }
            debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(hitBounds, Color.Blue));
        }
    }
}
