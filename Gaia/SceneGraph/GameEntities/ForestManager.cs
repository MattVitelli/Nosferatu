using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.Voxels;
using Gaia.Resources;
using Gaia.Core;
using Gaia.Rendering.RenderViews;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.SceneGraph.GameEntities
{
    public class ForestElement
    {
        public Mesh Mesh;
        public Transform Transform;
        public bool RenderImposters = true;
    };

    public class ForestManager : Entity
    {
        public KDTree<ForestElement> visibleMeshes = new KDTree<ForestElement>(SceneCompareFunction);
        Mesh mesh;
        const int defaultEntityCount = 1500;

        int entityCount = defaultEntityCount;
        string meshName;

        BoundingBox region;

        public bool randomizeScale = false;
        bool useRegion = false;
        public bool randomizeOrientation = true;
        public bool alignToSurface = false;

        bool isEnabled = false;

        public ForestManager(string name, int clusterCount)
        {
            meshName = name;
            entityCount = clusterCount;
        }

        public ForestManager(string name, int clusterCount, BoundingBox region)
        {
            meshName = name;
            useRegion = true;
            this.region = region;
            entityCount = clusterCount;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            mesh = ResourceManager.Inst.GetMesh(meshName);
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
                    int index = i % availableTriangles.Count;
                    int randIndex = RandomHelper.RandomGen.Next(index);
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
                element.Mesh = mesh;
                visibleMeshes.AddElement(element, false);
            }
            isEnabled = true;
            visibleMeshes.BuildTree();
            RecursivelyBuildBounds(visibleMeshes.GetRoot());
        }

        static int SceneCompareFunction(ForestElement elementA, ForestElement elementB, int axis)
        {
            Vector3 posA = elementA.Transform.GetPosition();
            float valueA = (axis == 0) ? posA.X : (axis == 1) ? posA.Y : posA.Z;

            Vector3 posB = elementB.Transform.GetPosition();
            float valueB = (axis == 0) ? posB.X : (axis == 1) ? posB.Y : posB.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        void RecursivelyBuildBounds(KDNode<ForestElement> node)
        {
            if (node == null)
                return;

            RecursivelyBuildBounds(node.leftChild);
            RecursivelyBuildBounds(node.rightChild);
            
            node.bounds = node.element.Transform.TransformBounds(node.element.Mesh.GetBounds());

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

        public override void OnRender(RenderView view)
        {
            if (isEnabled)
                RecursivelyRender(visibleMeshes.GetRoot(), view);
            base.OnRender(view);
        }

        void RecursivelyRender(KDNode<ForestElement> node, RenderView view)
        {
            if (node == null || view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint)
                return;

            if(view.GetRenderType() == RenderViewType.MAIN)
            {
                float distToCamera = Vector3.DistanceSquared(node.element.Transform.GetPosition(), view.GetPosition());
                node.element.RenderImposters = (distToCamera >= Mesh.IMPOSTER_DISTANCE_SQUARED);
            }
            
            if(node.element.RenderImposters)
                node.element.Mesh.RenderImposters(node.element.Transform.GetTransform(), view, true);
            else
                node.element.Mesh.Render(node.element.Transform.GetTransform(), view, true);
            RecursivelyRender(node.leftChild, view);
            RecursivelyRender(node.rightChild, view);
        }
    }
}
