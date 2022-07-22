using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rasterizer
{
    public class RenderObject : MonoBehaviour
    {
        [NonSerialized]
        public Mesh mesh;
        public Texture2D texture;

        public RenderObjectData renderObjectData;

        public ShadingType _ShadingType = ShadingType.BlinPhong;
        public enum ShadingType
        {
            BlinPhong,
            PBR
        }
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

            if (texture == null)
            {
                texture = Texture2D.whiteTexture;
            }

            if (mesh != null)
            {
                renderObjectData = new RenderObjectData(mesh);
            }
        }

        public Matrix4x4 GetModelMatrix()
        {
            if (transform == null)
            {
                return RasterizeUtils.GetRotZMatrix(0);
            }

            Matrix4x4 scaleMat = RasterizeUtils.GetScaleMatrix(transform.lossyScale);
            
            Vector3 rotation = transform.rotation.eulerAngles;
            Matrix4x4 rotXMat = RasterizeUtils.GetRotationMatrix(Vector3.right, -rotation.x);
            Matrix4x4 rotYMat = RasterizeUtils.GetRotationMatrix(Vector3.up, -rotation.y);
            Matrix4x4 rotZMat = RasterizeUtils.GetRotationMatrix(Vector3.forward, rotation.z);
            Matrix4x4 rotationMat = rotYMat * rotXMat * rotZMat;
            
            Matrix4x4 translateMat = RasterizeUtils.GetTranslationMatrix(transform.position);

            return translateMat * rotationMat * scaleMat;
        }
        
    }
}

