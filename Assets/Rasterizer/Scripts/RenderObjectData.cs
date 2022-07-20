using System.Drawing;
using UnityEngine;

namespace Rasterizer
{
    public class RenderObjectData
    {
        public readonly ComputeBuffer vertexBuffer;
        public readonly ComputeBuffer normalBuffer;
        public readonly ComputeBuffer uvBuffer;
        public readonly ComputeBuffer triIndexBuffer;
        public readonly ComputeBuffer varyingsBuffer;

        public readonly int triangleNum;
        public readonly int vertexNum;
        
        public RenderObjectData(Mesh mesh)
        { 
            vertexNum = mesh.vertexCount;
            vertexBuffer = new ComputeBuffer(vertexNum, 3 * sizeof(float));
            vertexBuffer.SetData(mesh.vertices);
            normalBuffer = new ComputeBuffer(vertexNum, 3 * sizeof(float));
            normalBuffer.SetData(mesh.normals);
            uvBuffer = new ComputeBuffer(vertexNum, 2 * sizeof(float));
            uvBuffer.SetData(mesh.uv);
            
            //remember to transform from 0,1,2 to 1,0,2
            var meshTris = mesh.triangles;
            triangleNum = meshTris.Length / 3;
            Vector3Int[] triangles = new Vector3Int[triangleNum];
            for (int i = 0; i < triangleNum; ++i)
            {
                int j = i * 3;
                triangles[i].x = meshTris[j + 1];
                triangles[i].y = meshTris[j];
                triangles[i].z = meshTris[j + 2];
            }

            triIndexBuffer = new ComputeBuffer(triangleNum, 3 * sizeof(uint));
            triIndexBuffer.SetData(triangles);

            varyingsBuffer = new ComputeBuffer(vertexNum, 15 * sizeof(float));
        }

        public void Release()
        {
            vertexBuffer.Release();
            normalBuffer.Release();
            uvBuffer.Release();
            triIndexBuffer.Release();
            varyingsBuffer.Release();
        }
    }
}