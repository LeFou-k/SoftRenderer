using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


namespace Rasterizer
{
    // Pipeline: => Clear screen
    //           => Set Attributes
    //           => ShadowMap pass
    //           => Rasterize pass
    //           => Update Per frame
    //           => Rendering done and release

    public class Rasterizer
    {
        public int width, height;
        public float aspect;

        private RasterizerSettings m_Settings;

        private readonly ComputeShader m_RasterizeCS;

        private RenderTexture m_ColorTexture;

        public Texture colorTexture
        {
            get => m_ColorTexture;
        }

        private RenderTexture m_DepthTexture;

        public Texture depthTexture
        {
            get => m_DepthTexture;
        }

        private RenderTexture m_ShadowMapTexture;

        public Texture shadowMapTexture
        {
            get => m_ShadowMapTexture;
        }
        
        public int vertices;
        public int triangles;

        //matrices:
        private Matrix4x4 m_MatrixView;
        private Matrix4x4 m_MatrixProj;
        private Matrix4x4 m_MatrixLightView;
        private Matrix4x4 m_MatrixLightProj;
        
        private Matrix4x4 m_MatrixModel;
        private Matrix4x4 m_MatrixModelIT;
        private Matrix4x4 m_MatrixMVP;
        private Matrix4x4 m_MatrixLightMVP;
        private Matrix4x4 m_MatrixLightVP;

        public delegate void UpdateDelegate(int vertices, int triangles);
        public UpdateDelegate updateDelegate;

        private static class Properties
        {
            //kernels
            public static int clearKernel;
            public static int vertexKernel;
            public static int rasterizeKernel;

            public static int shadowMapVertexKernel;
            public static int shadowMapRasterizeKernel;
            
            //shader ids:
            public static readonly int clearColorId = Shader.PropertyToID("_ClearColor");
            public static readonly int screenSizeId = Shader.PropertyToID("_ScreenSize");
            public static readonly int matrixMVPId = Shader.PropertyToID("_MatrixMVP");
            public static readonly int matrixMId = Shader.PropertyToID("_MatrixM");
            public static readonly int matrixMITId = Shader.PropertyToID("_MatrixM_IT");
            public static readonly int cameraWSId = Shader.PropertyToID("_CameraWS");
            public static readonly int lightDirWSId = Shader.PropertyToID("_LightDirWS");
            public static readonly int lightColorId = Shader.PropertyToID("_LightColor");
            public static readonly int ambientColorId = Shader.PropertyToID("_AmbientColor");
            public static readonly int vertexBufferId = Shader.PropertyToID("_VertexBuffer");
            public static readonly int normalBufferId = Shader.PropertyToID("_NormalBuffer");
            public static readonly int uvBufferId = Shader.PropertyToID("_UVBuffer");
            public static readonly int triIndexBufferId = Shader.PropertyToID("_TriIndexBuffer");
            public static readonly int varyingsBufferId = Shader.PropertyToID("_VaryingsBuffer");
            public static readonly int colorTextureId = Shader.PropertyToID("_ColorTexture");
            public static readonly int depthTextureId = Shader.PropertyToID("_DepthTexture");
            public static readonly int uvTextureId = Shader.PropertyToID("_UVTexture");

            public static readonly int matrixLightMVPId = Shader.PropertyToID("_MatrixLightMVP");
            public static readonly int matrixLightVPId = Shader.PropertyToID("_MatrixLightVP");
            public static readonly int shadowVaryingsId = Shader.PropertyToID("_ShadowVaryingsBuffer");
            public static readonly int rwShadowMapTextureId = Shader.PropertyToID("_RWShadowMapTexture");
            public static readonly int shadowMapTextureId = Shader.PropertyToID("_ShadowMapTexture");
        }

        public Rasterizer(int w, int h, RasterizerSettings settings)
        {
            width = w;
            height = h;
            aspect = h == 0 ? 0.0f : (float)w / h;

            m_ColorTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            m_ColorTexture.Create();

            m_DepthTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            m_DepthTexture.Create();

            m_ShadowMapTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            m_ShadowMapTexture.Create();

            m_Settings = settings;

            m_RasterizeCS = Resources.Load<ComputeShader>("RasterizeShader");
            Properties.clearKernel = m_RasterizeCS.FindKernel("ClearScreen");
            Properties.vertexKernel = m_RasterizeCS.FindKernel("VertexTransform");
            Properties.rasterizeKernel = m_RasterizeCS.FindKernel("RasterizeTriangles");
            
            Properties.shadowMapVertexKernel = m_RasterizeCS.FindKernel("ShadowMapVertexTransform");
            Properties.shadowMapRasterizeKernel = m_RasterizeCS.FindKernel("ShadowMapRasterize");
        }

        public void Clear()
        {
            m_RasterizeCS.SetTexture(Properties.clearKernel, Properties.colorTextureId, colorTexture);
            m_RasterizeCS.SetTexture(Properties.clearKernel, Properties.depthTextureId, depthTexture);
            m_RasterizeCS.SetTexture(Properties.clearKernel, Properties.rwShadowMapTextureId, shadowMapTexture);
            var clearColor = m_Settings.ClearColor;
            m_RasterizeCS.SetFloats(Properties.clearColorId, clearColor.r, clearColor.g, clearColor.b, clearColor.a);
            m_RasterizeCS.Dispatch(Properties.clearKernel, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f),
                1);
            
            triangles  = vertices = 0;
        }
        
        
        public void SetUniforms(Camera camera, Camera lightCamera, Light mainLight)
        {
            Vector3 cameraPos = camera.transform.position;
            m_RasterizeCS.SetFloats(Properties.cameraWSId, cameraPos.x, cameraPos.y, -cameraPos.z);   
            //lightDir: z = -z to right hand, -lightDir from shading point to light
            Vector3 lightDir = mainLight.transform.forward;
            m_RasterizeCS.SetFloats(Properties.lightDirWSId, -lightDir.x, -lightDir.y, lightDir.z);

            Color lightColor = mainLight.color * mainLight.intensity;
            m_RasterizeCS.SetFloats(Properties.lightColorId, lightColor.r, lightColor.g, lightColor.b, lightColor.a);

            Color ambientColor = m_Settings.AmbientColorr;
            m_RasterizeCS.SetFloats(Properties.ambientColorId, ambientColor.r, ambientColor.g, ambientColor.b, ambientColor.a);
            
            m_RasterizeCS.SetInts(Properties.screenSizeId, width, height);
            
            //get camera view and projection matrix
            RasterizeUtils.SetViewProjectionMatrix(camera, aspect, out m_MatrixView, out m_MatrixProj);
            //get light camera view and projection matrix
            RasterizeUtils.SetViewProjectionMatrix(lightCamera, aspect, out m_MatrixLightView, out m_MatrixLightProj);
        }

        private void SetObjectParams(RenderObject renderObject)
        {
            m_MatrixModel = renderObject.GetModelMatrix();
            m_MatrixModelIT = m_MatrixModel.inverse.transpose;
            m_MatrixMVP = m_MatrixProj * m_MatrixView * m_MatrixModel;
            
            m_MatrixLightMVP = m_MatrixLightProj * m_MatrixLightView * m_MatrixModel;
            m_MatrixLightVP = m_MatrixLightProj * m_MatrixLightView;
            
            m_RasterizeCS.SetMatrix(Properties.matrixMVPId, m_MatrixMVP);
            m_RasterizeCS.SetMatrix(Properties.matrixMId, m_MatrixModel);
            m_RasterizeCS.SetMatrix(Properties.matrixMITId, m_MatrixModelIT);
            m_RasterizeCS.SetMatrix(Properties.matrixLightVPId, m_MatrixLightVP);
            m_RasterizeCS.SetMatrix(Properties.matrixLightMVPId, m_MatrixLightMVP);
            
        }

        public void DrawCall(List<RenderObject> renderObjects)
        {
            foreach (var rObj in renderObjects)
            {
                ShadowMapPass(rObj);
            }

            foreach (var rObj in renderObjects)
            {
                RasterizePass(rObj);
            }
        }
        
        private void ShadowMapPass(RenderObject renderObject)
        {

            SetObjectParams(renderObject);
            var data = renderObject.renderObjectData;
            
            vertices += data.vertexNum;
            triangles += data.triangleNum;
            
            Profiler.BeginSample("Shadow Vertex Transformation");
            m_RasterizeCS.SetBuffer(Properties.shadowMapVertexKernel, Properties.vertexBufferId, data.vertexBuffer);
            m_RasterizeCS.SetBuffer(Properties.shadowMapVertexKernel, Properties.shadowVaryingsId, data.shadowVaryingsBuffer);
            m_RasterizeCS.Dispatch(Properties.shadowMapVertexKernel, Mathf.CeilToInt(data.vertexNum / 512.0f), 1, 1);
            Profiler.EndSample();
            
            Profiler.BeginSample("Shadow Rasterization");
            m_RasterizeCS.SetBuffer(Properties.shadowMapRasterizeKernel, Properties.triIndexBufferId, data.triIndexBuffer);
            m_RasterizeCS.SetBuffer(Properties.shadowMapRasterizeKernel, Properties.shadowVaryingsId, data.shadowVaryingsBuffer);
            m_RasterizeCS.SetTexture(Properties.shadowMapRasterizeKernel, Properties.rwShadowMapTextureId, shadowMapTexture);
            m_RasterizeCS.Dispatch(Properties.shadowMapRasterizeKernel, Mathf.CeilToInt(triangles / 512.0f), 1, 1);
            Profiler.EndSample();
        }
        
        private void RasterizePass(RenderObject renderObject)
        {
            SetObjectParams(renderObject);
            var data = renderObject.renderObjectData;
            
            Profiler.BeginSample("Vertex transformation");
            m_RasterizeCS.SetBuffer(Properties.vertexKernel, Properties.vertexBufferId, data.vertexBuffer);
            m_RasterizeCS.SetBuffer(Properties.vertexKernel, Properties.normalBufferId, data.normalBuffer);
            m_RasterizeCS.SetBuffer(Properties.vertexKernel, Properties.uvBufferId, data.uvBuffer);
            m_RasterizeCS.SetBuffer(Properties.vertexKernel, Properties.varyingsBufferId, data.varyingsBuffer);
            m_RasterizeCS.Dispatch(Properties.vertexKernel, Mathf.CeilToInt(data.vertexNum / 512.0f), 1, 1);
            Profiler.EndSample();
            //
            Profiler.BeginSample("Rasterization");
            m_RasterizeCS.SetBuffer(Properties.rasterizeKernel, Properties.triIndexBufferId, data.triIndexBuffer);
            m_RasterizeCS.SetBuffer(Properties.rasterizeKernel, Properties.varyingsBufferId, data.varyingsBuffer);
            m_RasterizeCS.SetTexture(Properties.rasterizeKernel, Properties.colorTextureId, colorTexture);
            m_RasterizeCS.SetTexture(Properties.rasterizeKernel, Properties.depthTextureId, depthTexture);
            m_RasterizeCS.SetTexture(Properties.rasterizeKernel, Properties.shadowMapTextureId, shadowMapTexture);
            m_RasterizeCS.SetTexture(Properties.rasterizeKernel, Properties.uvTextureId, renderObject.texture);
            m_RasterizeCS.Dispatch(Properties.rasterizeKernel, Mathf.CeilToInt(triangles / 512.0f), 1, 1);
            Profiler.EndSample();
            
        }

        public void UpdateFrame()
        {
            if (updateDelegate != null)
            {
                updateDelegate(vertices, triangles);
            }
        }

        public void Release()
        {
            m_ColorTexture.Release();
            m_DepthTexture.Release();
        }
    }
}