﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    public class ClusterManager : Entity
    {
        public static float CloudBlockScale = 32; //ClustersPerBlock * CloudRadius * 2;
        public const int CloudBlocksH = 2; //Horizontal cloud blocks
        public const int CloudBlocksV = 1; //Vertical cloud blocks

        const long width = long.MaxValue >> 48;
        const long height = long.MaxValue >> 48;
        const long sliceArea = width * height;

        SortedList<long, ForestManager> clusterCollection = new SortedList<long, ForestManager>();

        int camX;
        int camY;
        int camZ;

        string meshName;
        int clustersPerBlock;

        bool alignToSurface = false;

        public ClusterManager(string meshName, int clustersPerBlock) : base()
        {
            this.meshName = meshName;
            this.clustersPerBlock = clustersPerBlock;
        }

        public ClusterManager(string meshName, int clustersPerBlock, bool alignToSurface)
            : base()
        {
            this.meshName = meshName;
            this.clustersPerBlock = clustersPerBlock;
            this.alignToSurface = alignToSurface;
        }

        void UpdateClusterPlacement()
        {
            for (int z = camZ - CloudBlocksH; z <= camZ + CloudBlocksH; z++)
            {
                long zOff = sliceArea * (long)z;
                for (int y = camY - CloudBlocksV; y <= camY + CloudBlocksV; y++)
                {
                    long yOff = width * (long)y;

                    for (int x = camX - CloudBlocksH; x <= camX + CloudBlocksH; x++)
                    {
                        long idx = (long)x + yOff + zOff;
                        if (!clusterCollection.ContainsKey(idx))
                        {
                            Vector3 origin = new Vector3(x, y, z) * CloudBlockScale;
                            BoundingBox searchRegion = new BoundingBox(origin, origin + Vector3.One * CloudBlockScale);
                            ForestManager cluster = new ForestManager(meshName, clustersPerBlock, searchRegion);
                            cluster.alignToSurface = alignToSurface;
                            cluster.OnAdd(this.scene);
                            clusterCollection.Add(idx, cluster);
                        }
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Vector3 camPos = scene.MainCamera.GetPosition() / CloudBlockScale;

            camX = (int)camPos.X;
            camY = (int)camPos.Y;
            camZ = (int)camPos.Z;
            UpdateClusterPlacement();
        }

        public override void OnRender(RenderView view)
        {
            base.OnRender(view);

            int zStart = camZ - CloudBlocksH;
            int zEnd = camZ + CloudBlocksH;
            int yStart = camY - CloudBlocksV;
            int yEnd = camY + CloudBlocksV;
            int xStart = camX - CloudBlocksH;
            int xEnd = camX + CloudBlocksH;
            Vector3 camPos = view.GetPosition();
            BoundingFrustum camFrustum = view.GetFrustum();
            for (int z = zStart; z <= zEnd; z++)
            {
                long zOff = sliceArea * (long)z;
                for (int y = yStart; y <= yEnd; y++)
                {
                    long yOff = width * (long)y;

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        long idx = (long)x + yOff + zOff;
                        if (clusterCollection.ContainsKey(idx))
                        {
                            ForestManager currCluster = clusterCollection[idx];
                            clusterCollection[idx].OnRender(view);
                        }
                    }
                }
            }
        }
    }
}
