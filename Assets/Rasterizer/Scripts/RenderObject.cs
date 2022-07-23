using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
            PBR,
            PBRTexture
        }

        public PBRSettings _PbrSettings;
        public PBRTextureSettings _PbrTextureSettings;
        
        [Serializable]
        public class PBRSettings
        {
            public Color albedo;
            [Range(0.0f, 1.0f)]
            public float metallic;
            [Range(0.0f, 1.0f)]
            public float roughness;
            [Range(0.0f, 1.0f)]
            public float ao;
        }
        
        [Serializable]
        public class PBRTextureSettings
        {
            public Texture2D albedo;
            public Texture2D normal;
            public Texture2D height;
            public Texture2D metallic;
            public Texture2D ao;
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
            
            if (mesh != null)
            {
                renderObjectData = new RenderObjectData(mesh);
            }
            
            if (texture == null)
            {
                texture = Texture2D.whiteTexture;
            }
            
            _PbrTextureSettings.albedo ??= Texture2D.whiteTexture;
            _PbrTextureSettings.normal ??= Texture2D.whiteTexture;
            _PbrTextureSettings.metallic ??= Texture2D.whiteTexture;
            if (_PbrTextureSettings.albedo == null)
            {
                _PbrTextureSettings.albedo = Texture2D.whiteTexture;
            }

            if (_PbrTextureSettings.metallic == null)
            {
                _PbrTextureSettings.metallic = Texture2D.blackTexture;
            }

            if (_PbrTextureSettings.height == null)
            {
                _PbrTextureSettings.height = Texture2D.grayTexture;
            }

            _PbrTextureSettings.ao ??= Texture2D.whiteTexture;

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

