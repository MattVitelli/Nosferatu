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
        protected SortedList<string, AnimationNode> nodes;
        //protected SortedList<string, AnimationLayer> animationLayers = new SortedList<string, AnimationLayer>();
        protected SortedList<string, Vector3> defaultTranslations = new SortedList<string, Vector3>();
        protected SortedList<string, Vector3> defaultRotations = new SortedList<string, Vector3>();
        protected AnimationLayer mainAnimationLayer;
        protected SortedList<int, BoundingBox> hitBoxes;
        protected SortedList<int, BoundingBox> hitBoxesTransformed;

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
            hitBoxes = mesh.GetHitBoxes();
            hitBoxesTransformed = mesh.GetHitBoxes();
            int vertexCount = 0;
            VertexBuffer origBuffer = mesh.GetVertexBuffer(out vertexCount);
            vertices = new VertexPNTTI[vertexCount];
            origBuffer.GetData<VertexPNTTI>(vertices);
            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTTI.SizeInBytes * vertexCount, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPNTTI>(vertices);

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
                Matrix root = Matrix.Identity;
                for (int i = 0; i < rootNodes.Length; i++)
                    rootNodes[i].ApplyTransform(ref root);

                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexPNTTI currVertex = mesh.GetVertex(i);
                    int index = (int)currVertex.Index;
                    vertices[i].Position = Vector3.Transform(currVertex.Position, orderedNodes[index].Transform);
                    vertices[i].Normal = Vector3.TransformNormal(currVertex.Normal, orderedNodes[index].TransformIT);
                    vertices[i].Tangent = Vector3.TransformNormal(currVertex.Tangent, orderedNodes[index].TransformIT);
                    vertices[i].Index = 0;
                }
                vertexBuffer.SetData<VertexPNTTI>(vertices);

                UpdateHitBoxes();
            }
        }

        void UpdateHitBoxes()
        {
            if (hitBoxes != null)
            {
                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    int currKey = hitBoxes.Keys[i];

                    BoundingBox bounds = hitBoxes[currKey];
                    hitBoxesTransformed[currKey] = MathUtils.TransformBounds(MathUtils.TransformBounds(bounds, orderedNodes[currKey].Transform), worldMat);
                }
            }
        }

        public void OnUpdate()
        {
            worldMat = customMatrix * transform.GetTransform();
            worldBounds = MathUtils.TransformBounds(mesh.GetBounds(), worldMat);
            UpdateAnimation(Time.GameTime.ElapsedTime);
        }

        public void OnRender(RenderView view, bool performCulling)
        {
            if (rootNodes != null && rootNodes.Length > 0)
                mesh.Render(worldMat, vertexBuffer, view, performCulling);
            else
                mesh.Render(worldMat, view, performCulling);
        }

        public void RenderDebug(RenderView view)
        {
            for (int i = 0; i < hitBoxesTransformed.Count; i++)
            {
                Vector3[] corners = hitBoxesTransformed[hitBoxesTransformed.Keys[i]].GetCorners();
                VertexPositionColor[] debugVerts = new VertexPositionColor[24];
                Color hitboxColor = Color.Red;
                debugVerts[0] = new VertexPositionColor(corners[0], hitboxColor);
                debugVerts[1] = new VertexPositionColor(corners[1], hitboxColor);
                debugVerts[2] = new VertexPositionColor(corners[1], hitboxColor);
                debugVerts[3] = new VertexPositionColor(corners[5], hitboxColor);
                debugVerts[4] = new VertexPositionColor(corners[5], hitboxColor);
                debugVerts[5] = new VertexPositionColor(corners[4], hitboxColor);
                debugVerts[6] = new VertexPositionColor(corners[4], hitboxColor);
                debugVerts[7] = new VertexPositionColor(corners[0], hitboxColor);

                debugVerts[8] = new VertexPositionColor(corners[3], hitboxColor);
                debugVerts[9] = new VertexPositionColor(corners[2], hitboxColor);
                debugVerts[10] = new VertexPositionColor(corners[2], hitboxColor);
                debugVerts[11] = new VertexPositionColor(corners[6], hitboxColor);
                debugVerts[12] = new VertexPositionColor(corners[6], hitboxColor);
                debugVerts[13] = new VertexPositionColor(corners[7], hitboxColor);
                debugVerts[14] = new VertexPositionColor(corners[7], hitboxColor);
                debugVerts[15] = new VertexPositionColor(corners[3], hitboxColor);

                debugVerts[16] = new VertexPositionColor(corners[3], hitboxColor);
                debugVerts[17] = new VertexPositionColor(corners[0], hitboxColor);
                debugVerts[18] = new VertexPositionColor(corners[2], hitboxColor);
                debugVerts[19] = new VertexPositionColor(corners[1], hitboxColor);
                debugVerts[20] = new VertexPositionColor(corners[6], hitboxColor);
                debugVerts[21] = new VertexPositionColor(corners[5], hitboxColor);
                debugVerts[22] = new VertexPositionColor(corners[7], hitboxColor);
                debugVerts[23] = new VertexPositionColor(corners[4], hitboxColor);
                DebugElementManager debugMgr = (DebugElementManager)view.GetRenderElementManager(RenderPass.Debug);
                debugMgr.AddElements(debugVerts);
            }
        }
    }
}
