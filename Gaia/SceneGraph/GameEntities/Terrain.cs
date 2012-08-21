using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Voxels;
using Gaia.Core;

namespace Gaia.SceneGraph.GameEntities
{
    public abstract class Terrain : Entity
    {
        protected SortedList<ulong, char> usedLandmarkTriangles = new SortedList<ulong, char>();

        public virtual void GenerateRandomTransform(Random rand, out Vector3 position, out Vector3 normal)
        {
            position = Vector3.Zero;
            normal = Vector3.Up;
        }

        public virtual bool GetTrianglesInRegion(Random rand, out List<TriangleGraph> availableTriangles, BoundingBox region)
        {
            availableTriangles = null;
            return false;
        }

        public virtual bool GetTrianglesInRegion(Random rand, out List<TriangleGraph> availableTriangles, BoundingBox region, bool isLandmark)
        {
            availableTriangles = null;
            return false;
        }

        public virtual bool IsCollision(Vector3 pos, out Vector3 normal)
        {
            normal = Vector3.Up;
            return false;
        }

        public virtual bool GetYPos(ref Vector3 pos, out Vector3 normal, float minY, float maxY)
        {
            normal = Vector3.Up;
            return false;
        }

        public virtual void CarveTerrainAtPoint(Vector3 point, int size, int isoBrush)
        {

        }

        protected void PerformKDRegionSearch(KDNode<TriangleGraph> node, ref BoundingBox region, List<TriangleGraph> triangleCollection, bool isLandmark)
        {
            if (node != null && (isLandmark || !usedLandmarkTriangles.ContainsKey(node.element.ID)))
            {
                if (region.Contains(node.element.Centroid) != ContainmentType.Disjoint
                    || region.Contains(node.element.GetVertex0()) != ContainmentType.Disjoint
                    || region.Contains(node.element.GetVertex1()) != ContainmentType.Disjoint
                    || region.Contains(node.element.GetVertex2()) != ContainmentType.Disjoint)
                {
                    triangleCollection.Add(node.element);
                }
                PerformKDRegionSearch(node.leftChild, ref region, triangleCollection, isLandmark);

                PerformKDRegionSearch(node.rightChild, ref region, triangleCollection, isLandmark);
            }
        }

        public virtual BoundingBox GetWorldSpaceBoundsAtPoint(Vector3 point, int size)
        {
            return new BoundingBox(Vector3.One * -1, Vector3.One);
        }
    }
}
