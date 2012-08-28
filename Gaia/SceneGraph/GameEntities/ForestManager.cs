using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.Voxels;
using Gaia.Resources;
using Gaia.Core;
using Gaia.Rendering.RenderViews;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;

namespace Gaia.SceneGraph.GameEntities
{
    public enum ImposterState
    {
        Disabled,
        Enabled,
        Both,
        None,
    };
    public class ForestElement
    {
        public Mesh Mesh;
        public Transform Transform;
        public ImposterState RenderImposters = ImposterState.Enabled;
        public BoundingBox Bounds;
    };

    public class ForestManager : Entity
    {
        public KDTree<ForestElement> visibleMeshes = new KDTree<ForestElement>(SceneCompareFunction, SceneBoundsFunction, false, false);
        Mesh[] meshes;
        const int defaultEntityCount = 1500;

        int entityCount = defaultEntityCount;
        string[] meshNames;

        BoundingBox region;

        public bool randomizeScale = false;
        bool useRegion = false;
        bool useImposters = true;
        public bool randomizeOrientation = true;
        public bool alignToSurface = false;

        bool isEnabled = false;

        public ForestManager(string[] names, int clusterCount)
        {
            meshNames = names;
            entityCount = clusterCount;
        }

        public void SetImposterState(bool enabled)
        {
            useImposters = enabled;
        }

        public ForestManager(string[] names, int clusterCount, BoundingBox region)
        {
            meshNames = names;
            useRegion = true;
            this.region = region;
            entityCount = clusterCount;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            meshes = new Mesh[meshNames.Length];
            for(int i = 0; i < meshNames.Length; i++)
                meshes[i] = ResourceManager.Inst.GetMesh(meshNames[i]);

            List<TriangleGraph> availableTriangles = null;
            if (useRegion)
            {
                scene.MainTerrain.GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, region);
                if (availableTriangles.Count == 0)
                {
                    isEnabled = false;
                    return;
                }
            }
            for(int i = 0; i < entityCount; i++)
            {
                Vector3 pos;
                Vector3 normal;
                if (useRegion)
                {
                    int index = i % (availableTriangles.Count+1);
                    int randIndex = RandomHelper.RandomGen.Next(availableTriangles.Count);//index);
                    normal = availableTriangles[randIndex].Normal;
                    pos = availableTriangles[randIndex].GeneratePointInTriangle(RandomHelper.RandomGen);
                }
                else
                    scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
                ForestElement element = new ForestElement();
                element.Transform = (alignToSurface) ? new NormalTransform() : new Transform();
                if (alignToSurface)
                {
                    NormalTransform transform = (NormalTransform)element.Transform;
                    transform.ConformToNormal(normal);
                    if (randomizeOrientation)
                        transform.SetAngle((float)RandomHelper.RandomGen.NextDouble() * MathHelper.TwoPi);
                }
                else if(randomizeOrientation)
                        element.Transform.SetRotation(new Vector3(0, (float)RandomHelper.RandomGen.NextDouble() * MathHelper.TwoPi, 0));
 
                element.Transform.SetPosition(pos);
                int randMeshIndex = RandomHelper.RandomGen.Next(meshes.Length);//i % (meshes.Length+1));
                element.Mesh = meshes[randMeshIndex];
                element.Bounds = element.Transform.TransformBounds(element.Mesh.GetBounds()); //This is only temporary
                visibleMeshes.AddElement(element, false);
            }
            isEnabled = true;
            visibleMeshes.BuildTree();
            RecursivelyBuildBounds(visibleMeshes.GetRoot());

            KDNode<ForestElement> root = visibleMeshes.GetRoot();
            root.bounds.Min -= Vector3.One * 1.15f;
            root.bounds.Max += Vector3.One * 1.15f;
        }

        static int SceneCompareFunction(ForestElement elementA, ForestElement elementB, int axis)
        {
            BoundingBox boundsA = elementA.Bounds;//.Transform.TransformBounds(elementA.Mesh.GetBounds());

            Vector3 posA = (boundsA.Max + boundsA.Min) * 0.5f;// elementA.Transform.GetPosition();
            float valueA = (axis == 0) ? posA.X : (axis == 1) ? posA.Y : posA.Z;

            BoundingBox boundsB = elementB.Bounds;//.Transform.TransformBounds(elementB.Mesh.GetBounds());

            Vector3 posB = (boundsB.Max + boundsB.Min) * 0.5f;//elementB.Transform.GetPosition();
            float valueB = (axis == 0) ? posB.X : (axis == 1) ? posB.Y : posB.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        static Vector2 SceneBoundsFunction(ForestElement element, int axis)
        {
            BoundingBox bounds = element.Bounds;//.Transform.TransformBounds(element.Mesh.GetBounds());
            return (axis == 0) ? new Vector2(bounds.Min.X, bounds.Max.X) : ((axis == 1) ? new Vector2(bounds.Min.Y, bounds.Max.Y) : new Vector2(bounds.Min.Z, bounds.Max.Z));
        }

        static BoundingBox SceneBoundsEval(ForestElement element)
        {
            return element.Bounds;
        }

        void RecursivelyBuildBounds(KDNode<ForestElement> node)
        {
            if (node == null)
                return;

            RecursivelyBuildBounds(node.leftChild);
            RecursivelyBuildBounds(node.rightChild);

            if (node.element != null)
            {
                node.bounds = node.element.Transform.TransformBounds(node.element.Mesh.GetBounds());
                node.element.Bounds = node.bounds;
            }
            else
            {
                if(node.leftChild != null)
                    node.bounds = node.leftChild.bounds;
                if (node.rightChild != null)
                    node.bounds = node.rightChild.bounds;
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
        int nodesRendered;
        public override void OnRender(RenderView view)
        {
            nodesRendered = 0;
            if (isEnabled && view.GetRenderType() != RenderViewType.REFLECTIONS)
                RecursivelyRender(visibleMeshes.GetRoot(), view);
            /*
            if (view.GetRenderType() == RenderViewType.MAIN && visibleMeshes.GetRoot().bounds.Contains(view.GetPosition()) != ContainmentType.Disjoint && entityCount != 3000)
            {
                Gaia.Rendering.GUIElementManager guiMgr = Gaia.Rendering.GFX.Inst.GetGUI();
                guiMgr.AddElement( new Gaia.Rendering.GUITextElement(new Vector2(0, 0.9f), "Nodes rendered: " + nodesRendered + "/" + entityCount));
            }
            
            if (view.GetRenderType() == RenderViewType.MAIN && visibleMeshes.GetRoot().bounds.Contains(view.GetPosition()) != ContainmentType.Disjoint)
            {
                RecursivelyRenderDebug(visibleMeshes.GetRoot(), view, 0, false);
            }
            */
            base.OnRender(view);
        }

        Color[] nodeColors = new Color[] { Color.Red, Color.Green, Color.Blue};
        Color[] nodeColorsLeft = new Color[] { Color.Purple, Color.Orange, Color.CornflowerBlue };
        int maxDepth = 2;
        void RecursivelyRenderDebug(KDNode<ForestElement> node, RenderView view, int depth, bool isLeft)
        {
            if (node == null || view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint)
                return;

            Gaia.Rendering.DebugElementManager debugMgr = (Gaia.Rendering.DebugElementManager)view.GetRenderElementManager(Gaia.Rendering.RenderPass.Debug);
            Color currColor = (isLeft) ? nodeColorsLeft[depth % nodeColorsLeft.Length] : nodeColors[depth % nodeColors.Length];
            debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(node.bounds, currColor));
            /*
            if (node.element != null)
            {
                debugMgr.AddElements(DebugHelper.GetVerticesFromBounds(node.element.Bounds, Color.White));
            }*/

            depth++;
            if (depth < maxDepth)
            {
                RecursivelyRenderDebug(node.leftChild, view, depth, true);
                RecursivelyRenderDebug(node.rightChild, view, depth, false);
            }
        }

        void RecursivelyRender(KDNode<ForestElement> node, RenderView view)
        {
            if (node == null || view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint)
                return;

            if (node.element != null && (view.GetFrustum().Contains(node.element.Bounds) != ContainmentType.Disjoint))
            {
                if (view.GetRenderType() == RenderViewType.MAIN && useImposters)
                {
                    float distToCamera = Vector3.DistanceSquared(node.element.Transform.GetPosition(), view.GetPosition());
                    node.element.RenderImposters = (distToCamera >= GFXShaderConstants.IMPOSTERDISTANCESQUARED) ? ImposterState.Enabled : ImposterState.Disabled;
                    if (node.element.RenderImposters == ImposterState.Enabled && distToCamera <= GFXShaderConstants.IMPOSTER_DISTANCE_FALLOFF)
                    {
                        node.element.RenderImposters = ImposterState.Both;
                    }
                    if (distToCamera > GFXShaderConstants.GRASS_FADE_SQUARED)
                        node.element.RenderImposters = ImposterState.None;
                }

                    if (useImposters)
                    {
                        if (node.element.RenderImposters != ImposterState.None)
                        {
                            if (node.element.RenderImposters == ImposterState.Enabled || node.element.RenderImposters == ImposterState.Both)
                                node.element.Mesh.RenderImposters(node.element.Transform.GetTransform(), view, false);
                            if (node.element.RenderImposters == ImposterState.Disabled || node.element.RenderImposters == ImposterState.Both)
                                node.element.Mesh.Render(node.element.Transform.GetTransform(), view, false);
                        }
                    }
                    else
                        node.element.Mesh.Render(node.element.Transform.GetTransform(), view, false);
                
                    nodesRendered++;
            }
            RecursivelyRender(node.leftChild, view);
            RecursivelyRender(node.rightChild, view);
        }
    }
}
