using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Gaia.Core
{
    public class KDNode<T>
    {
        public T element;

        public BoundingBox bounds;

        public KDNode<T> leftChild;

        public KDNode<T> rightChild;
    }

    public class KDTree<T>
    {
        public delegate int KDCmpFunction(T elemA, T elemB, int axis);

        public delegate Vector2 KDCmpFunctionMinMax(T elemA, int axis);
        KDNode<T> rootNode;

        int maxDimension = 3;

        List<T> elementCollection;

        KDCmpFunction compareFunction;

        KDCmpFunctionMinMax adaptiveBoundsFunction;

        bool useAdaptiveSplits = false;

        bool resideAtLeafs = false;

        public KDTree(KDCmpFunction compareFunction)
        {
            this.rootNode = new KDNode<T>();
            this.elementCollection = new List<T>();
            this.compareFunction = compareFunction;
        }

        public KDTree(KDCmpFunction compareFunction, KDCmpFunctionMinMax adaptiveBoundsFunction, bool adaptiveSplitting, bool resideAtLeafs)
        {
            this.rootNode = new KDNode<T>();
            this.elementCollection = new List<T>();
            this.compareFunction = compareFunction;
            this.useAdaptiveSplits = adaptiveSplitting;
            this.resideAtLeafs = resideAtLeafs;
            this.adaptiveBoundsFunction = adaptiveBoundsFunction;
        }

        public KDNode<T> GetRoot()
        {
            return rootNode;
        }

        public void AddElement(T element, bool rebuildTree)
        {
            elementCollection.Add(element);
            if(rebuildTree)
                BuildTree();
        }

        public void AddElementRange(T[] elements, bool rebuildTree)
        {
            elementCollection.AddRange(elements);
            if(rebuildTree)
                BuildTree();
        }

        public void SetDimension(int dimension)
        {
            maxDimension = dimension;
            BuildTree();
        }
        
        public void BuildTree()
        {
            ConstructKDTree(rootNode, 0, elementCollection.ToArray()); 
        }

        int GetMajorAxis(ref T[] entities)
        {
            
            Vector2[] minMaxAxes = new Vector2[maxDimension];
            for(int i = 0; i < minMaxAxes.Length; i++)
                minMaxAxes[i] = adaptiveBoundsFunction(entities[0], i);

            for (int j = 1; j < entities.Length; j++)
            {
                for (int i = 0; i < maxDimension; i++)
                {
                    Vector2 minMax = adaptiveBoundsFunction(entities[j], i);
                    minMaxAxes[i].X = Math.Min(minMax.X, minMaxAxes[i].X);
                    minMaxAxes[i].Y = Math.Max(minMax.Y, minMaxAxes[i].Y);
                }
            }

            int bestAxis = 0;
            float bestAxisDist = Math.Abs(minMaxAxes[0].Y-minMaxAxes[0].X);
            for (int i = 1; i < maxDimension; i++)
            {
                float axisDist = Math.Abs(minMaxAxes[i].Y - minMaxAxes[i].X);
                if (axisDist > bestAxisDist)
                    bestAxis = i;
            }

            return bestAxis;
        }

        void ConstructKDTree(KDNode<T> currNode, int depth, T[] entities)
        {
            if (entities.Length == 0)
                return;
            int axis = depth % maxDimension;
            if (useAdaptiveSplits)
                axis = GetMajorAxis(ref entities);

            List<T> sortedList = new List<T>();
            for (int i = 0; i < entities.Length; i++)
            {
                bool insertedEntity = false;
                for (int j = 0; j < sortedList.Count; j++)
                {
                    if (compareFunction(entities[i], sortedList[j], axis) < 0)
                    {
                        sortedList.Insert(j, entities[i]);
                        insertedEntity = true;
                        break;
                    }
                }
                if (!insertedEntity)
                    sortedList.Add(entities[i]);
            }
            int medianIndex = sortedList.Count / 2;

            if (sortedList.Count > 1)
            {
                int leftCount = medianIndex - 1;
                if (resideAtLeafs)
                    leftCount++;
                if (leftCount > 0)
                {
                    T[] leftEntities = new T[leftCount];
                    sortedList.CopyTo(0, leftEntities, 0, leftCount);
                    currNode.leftChild = new KDNode<T>();
                    ConstructKDTree(currNode.leftChild, depth++, leftEntities);
                }

                int rightCount = sortedList.Count - (medianIndex + 1);
                if (rightCount > 0)
                {
                    T[] rightEntities = new T[rightCount];
                    sortedList.CopyTo(medianIndex + 1, rightEntities, 0, rightEntities.Length);
                    currNode.rightChild = new KDNode<T>();
                    ConstructKDTree(currNode.rightChild, depth++, rightEntities);
                }
            }
            if(!resideAtLeafs || sortedList.Count == 1)
                currNode.element = sortedList[medianIndex];
        }
    }
}
