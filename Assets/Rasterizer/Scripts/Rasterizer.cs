using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


namespace Rasterizer
{
    //Pipeline: => Clear screen
    //          => Set Attributes
    //          => Draw Every Objects
    //          => Update Per frame
    //          => Rendering done and release
    
    public class Rasterizer
    {
        public int width, height;
        public float aspect;
        
        private RasterizerSettings m_Settings;
        private CommandBuffer m_Cmd;
        private string m_CmdProfileTag = "GPU-Driven Rasterization";
        
        private readonly ComputeShader m_RasterizeCS;

        private RenderTexture m_ColorTexture;
        public Texture colorTexture
        {
            get => m_ColorTexture;
        }

        private RenderTexture m_DepthTexture;
        public Texture DepthTexture
        {
            get => m_DepthTexture;
        }

        public int vertices;
        public int triangles;
        public int trianglesVis;
        
        

        public RasterizeUtils.UpdateDelegate updateDelegate;
        
        private static class Properties
        {
            //kernels
            public static int clearKernel;
            public static int vertexKernel;
            public static int rasterizeKernel;
            
            //shader ids:
            public static readonly int clearColorId = Shader.PropertyToID("_ClearColor");
            public static readonly int screenSizeId = Shader.PropertyToID("_ScreenSize");
            public static readonly int matrixMVPId = Shader.PropertyToID("_MatrixMVP");
            public static readonly int matrixMId = Shader.PropertyToID("_MatrixM");
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

            m_DepthTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RGFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            m_DepthTexture.Create();
            
            m_Settings = settings;

            m_RasterizeCS = Resources.Load<ComputeShader>("RasterizeShader");
            Properties.clearKernel = m_RasterizeCS.FindKernel("ClearScreen");
            Properties.vertexKernel = m_RasterizeCS.FindKernel("VertexTransform");
            Properties.rasterizeKernel = m_RasterizeCS.FindKernel("RasterizeTriangles");
            
        }
        
        public void Clear()
        {
            
        }

        public void SetAttributes(Camera camera, Light mainLight)
        {
            
        }
        
        public void DrawCall(RenderObject renderObject)
        {
            
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
            m_Cmd.Release();
            
        }
    }
}