using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gaia.Core
{
    public struct CustomPlane
    {
        public float a, b, c, d;

        public void SetCoeffs(float x, float y, float z, float w)
        {
            a = x;
            b = y;
            c = z;
            d = w;
        }

        public bool PointBehindPlane(Vector3 pt)
        {
            float dist = pt.X * a + pt.Y * b + pt.Z * c + d;

            if (dist < -0.00001f)
                return true;

            return false;	//otherwise
        }

        public void Normalize()
        {
            float invMag = 1.0f / (float)Math.Sqrt(a * a + b * b + c * c + 0.0001f);
            a *= invMag;
            b *= invMag;
            c *= invMag;
            d *= invMag;
        }
    }

    public class CustomFrustum
    {
        Matrix matrix;
        CustomPlane[] planes = new CustomPlane[6];
        Vector3 camPos;
        public CustomFrustum(Matrix matrix, Vector3 cameraPosition)
        {
            SetMatrix(matrix, cameraPosition);
        }

        public void SetMatrix(Matrix value, Vector3 cameraPosition)
        {
            this.matrix = Matrix.Transpose(value);
            this.camPos = cameraPosition;
            ComputePlanes();
        }

        void ComputePlanes()
        {
            /*
            planes[0].SetCoeffs(matrix.a4 + matrix.a3, matrix.b4 + matrix.b3, matrix.c4 + matrix.c3, matrix.d4 + matrix.d3);
            planes[1].SetCoeffs(matrix.a4 - matrix.a3, matrix.b4 - matrix.b3, matrix.c4 - matrix.c3, matrix.d4 - matrix.d3);
            planes[2].SetCoeffs(matrix.a4 + matrix.a2, matrix.b4 + matrix.b2, matrix.c4 + matrix.c2, matrix.d4 + matrix.d2);
            planes[3].SetCoeffs(matrix.a4 - matrix.a2, matrix.b4 - matrix.b2, matrix.c4 - matrix.c2, matrix.d4 - matrix.d2);
            planes[4].SetCoeffs(matrix.a4 + matrix.a1, matrix.b4 + matrix.b1, matrix.c4 + matrix.c1, matrix.d4 + matrix.d1);
            planes[5].SetCoeffs(matrix.a4 - matrix.a1, matrix.b4 - matrix.b1, matrix.c4 - matrix.c1, matrix.d4 - matrix.d1);
            */

            planes[0].SetCoeffs(matrix.M41 + matrix.M31, matrix.M42 + matrix.M32, matrix.M43 + matrix.M33, matrix.M44 + matrix.M34);
            planes[1].SetCoeffs(matrix.M41 - matrix.M31, matrix.M42 - matrix.M32, matrix.M43 - matrix.M33, matrix.M44 - matrix.M34);
            planes[2].SetCoeffs(matrix.M41 + matrix.M21, matrix.M42 + matrix.M22, matrix.M43 + matrix.M23, matrix.M44 + matrix.M24);
            planes[3].SetCoeffs(matrix.M41 - matrix.M21, matrix.M42 - matrix.M22, matrix.M43 - matrix.M23, matrix.M44 - matrix.M24);
            planes[4].SetCoeffs(matrix.M41 + matrix.M11, matrix.M42 + matrix.M12, matrix.M43 + matrix.M13, matrix.M44 + matrix.M14);
            planes[5].SetCoeffs(matrix.M41 - matrix.M11, matrix.M42 - matrix.M12, matrix.M43 - matrix.M13, matrix.M44 - matrix.M14);

            for (int i = 0; i < 6; i++)
            {
                planes[i].Normalize();
            }
        }

        public bool BoundingBoxVisible(BoundingBox bounds)
        {
            if (bounds.Contains(camPos) != ContainmentType.Disjoint)
                return true;
            for (int i = 0; i < 6; i++)
            {
                Vector3 vec;
                vec.X = (planes[i].a >= 0) ? bounds.Max.X : bounds.Min.X;
                vec.Y = (planes[i].b >= 0) ? bounds.Max.Y : bounds.Min.Y;
                vec.Z = (planes[i].c >= 0) ? bounds.Max.Z : bounds.Min.Z;

                if (planes[i].PointBehindPlane(vec))
                    return false;
            }
            return true;
        }
    }
}
