using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rasterizer
{
    public class RenderObject : MonoBehaviour
    {
        public Mesh mesh;
        public Texture2D texture;

        public RenderObjectData renderObjectData;
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            renderObjectData.Release();
        }

        private void Initialize()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogError("None mesh filter found!");
            }
            mesh = meshFilter.sharedMesh;
            
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            {
                Debug.LogError("None mesh renderer or material found!");
            }
            texture = meshRenderer.sharedMaterial.mainTexture as Texture2D ?? Texture2D.whiteTexture;

            if (mesh != null)
            {
                renderObjectData = new RenderObjectData(mesh);
            }
        }
        
        
    }
}

