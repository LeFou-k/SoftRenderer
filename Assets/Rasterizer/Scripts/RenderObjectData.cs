using System.Drawing;
using UnityEngine;

namespace Rasterizer
{
    public class RenderObjectData
    {
        public ComputeBuffer vertexBuffer;
        public ComputeBuffer normalBuffer;
        public ComputeBuffer uvBuffer;
        public ComputeBuffer triIndexBuffer;
        public ComputeBuffer varyingsBuffer;
        
        
        public RenderObjectData(Mesh mesh)
        { 
            int vertexNums = mesh.vertexCount;
            vertexBuffer = new ComputeBuffer(vertexNums, 3 * sizeof(float));
            vertexBuffer.SetData(mesh.vertices);
            normalBuffer = new ComputeBuffer(vertexNums, 3 * sizeof(float));
            normalBuffer.SetData(mesh.normals);
            uvBuffer = new ComputeBuffer(vertexNums, 2 * sizeof(float));
            uvBuffer.SetData(mesh.uv);
            
            //remember to transform from 0,1,2 to 1,0,2
            var meshTris = mesh.triangles;
            int triNums = meshTris.Length / 3;
            Vector3Int[] triangles = new Vector3Int[triNums];
            for (int i = 0; i < triNums; ++i)
            {
                int j = i * 3;
                triangles[i].x = meshTris[j + 1];
                triangles[i].y = meshTris[j];
                triangles[i].z = meshTris[j + 2];
            }

            triIndexBuffer = new ComputeBuffer(triNums, 3 * sizeof(uint));
            triIndexBuffer.SetData(triangles);

            varyingsBuffer = new ComputeBuffer(vertexNums, 15 * sizeof(float));
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