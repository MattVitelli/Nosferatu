using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.Core
{
    public static class DebugHelper
    {
        public static VertexPositionColor[] GetVerticesFromBounds(BoundingBox bounds, Color hitboxColor)
        {
            Vector3[] corners = bounds.GetCorners();
            VertexPositionColor[] debugVerts = new VertexPositionColor[24];
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

            return debugVerts;
        }
    }
}
