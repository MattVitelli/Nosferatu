﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
namespace Gaia.Rendering.RenderViews
{
    public enum RenderViewType
    {
        SHADOWS=0,
        REFLECTIONS,
        MAIN,
        Count = 3,
    };

    public abstract class RenderView : IComparable
    {
        BoundingFrustum frustum = new BoundingFrustum(Matrix.Identity);
        CustomFrustum customFrustum = new CustomFrustum(Matrix.Identity, Vector3.Zero);
        Vector3 position;

        float farPlane;
        float nearPlane;

        Matrix projection;
        Matrix projectionLocal;

        Matrix view;
        Matrix viewLocal;

        Matrix viewProjection;
        Matrix viewProjectionLocal;

        Matrix inverseViewProjection;
        Matrix inverseViewProjectionLocal;

        Matrix worldMatrix;

        protected SortedList<RenderPass, RenderElementManager> ElementManagers;
        protected CustomList<Mesh> meshesToRender = new CustomList<Mesh>();

        bool dirtyMatrix;
        bool updateViewProjLocal = true;
        protected RenderViewType renderType;

        protected string Name;

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            RenderView other = obj as RenderView;
            if (other != null)
                return string.Compare(this.Name, other.Name);
            else
                throw new ArgumentException("Object is not a materials");
        }

        public RenderView(RenderViewType renderType, Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane)
        {
            this.renderType = renderType;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.position = position;
            this.view = view;
            this.projection = projection;
            dirtyMatrix = true;
            ElementManagers = new SortedList<RenderPass, RenderElementManager>();
        }

        public virtual void AddElement(Material material, RenderElement element)
        {
        }
        
        public void AddMeshToRender(Mesh mesh)
        {
            meshesToRender.Add(mesh);
        }
        
        public virtual void Render()
        {
            
            for (int i = 0; i < meshesToRender.Count; i++)
            {
                meshesToRender[i].RenderPostSceneQuery(this);
            }
            meshesToRender.Clear();
            
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjection());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosShader());
            
        }

        public RenderElementManager GetRenderElementManager(RenderPass type)
        {
            if (ElementManagers.ContainsKey(type))
                return ElementManagers[type];

            return null;
        }

        void ComputeMatrix()
        {
            if (!dirtyMatrix)
                return;

            dirtyMatrix = false;
            viewProjection = view * projection;
            viewProjectionLocal = viewLocal * projectionLocal;
            frustum.Matrix = viewProjection;
            customFrustum.SetMatrix(viewProjection, position);
            inverseViewProjection = Matrix.Invert(viewProjection);
            if (updateViewProjLocal)
            {
                viewLocal = view;
                viewLocal.Translation = Vector3.Zero;
                inverseViewProjectionLocal = Matrix.Invert(viewProjectionLocal);
            }
            worldMatrix = Matrix.Invert(view);
        }

        public RenderViewType GetRenderType()
        {
            return renderType;
        }

        public void SetViewProjectionLocal(Matrix value)
        {
            viewProjectionLocal = value;
            inverseViewProjectionLocal = Matrix.Invert(viewProjectionLocal);
            updateViewProjLocal = false;
        }

        public void SetView(Matrix view)
        {
            this.view = view;
            dirtyMatrix = true;
        }

        public Matrix GetView()
        {
            return view;
        }

        public void SetFarPlane(float value)
        {
            farPlane = value;
        }

        public float GetFarPlane()
        {
            return farPlane;
        }

        public void SetNearPlane(float value)
        {
            nearPlane = value;
        }

        public float GetNearPlane()
        {
            return nearPlane;
        }

        public Matrix GetWorldMatrix()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return worldMatrix;
        }

        public void SetProjectionLocal(Matrix projection)
        {
            this.projectionLocal = projection;
            dirtyMatrix = true;
        }

        public void SetProjection(Matrix projection)
        {
            this.projection = projection;
            dirtyMatrix = true;
        }

        public Matrix GetProjection()
        {
            return projection;
        }

        public Matrix GetProjectionLocal()
        {
            return projectionLocal;
        }

        public Matrix GetViewProjection()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return viewProjection;
        }

        public Matrix GetViewProjectionLocal()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return viewProjectionLocal;
        }

        public Matrix GetInverseViewProjection()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return inverseViewProjection;
        }

        public Matrix GetInverseViewProjectionLocal()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return inverseViewProjectionLocal;
        }

        public BoundingFrustum GetFrustum()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return frustum;
        }

        public CustomFrustum GetCustomFrustum()
        {
            if (dirtyMatrix)
                ComputeMatrix();
            return customFrustum;
        }

        public void SetPosition(Vector3 position)
        {
            this.position = position;
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public Vector4 GetEyePosShader()
        {
            return new Vector4(position, farPlane);
        }

        public Vector4 GetEyePosLocalShader()
        {
            return new Vector4(Vector3.Zero, farPlane);
        }

    }
}
