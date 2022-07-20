using UnityEngine;

namespace Rasterizer
{
    [System.Serializable]
    public class RasterizerSettings
    {
        [Header("Common Settings")]
        public Color ClearColor = Color.black;
        public Color AmbientColorr = Color.black;
        
        [Header("Rasterizer Settings")]
        // public bool FrustumCulling = true;
        // public bool BackFaceCulling = true;
        public BufferOutput _BufferOutput = BufferOutput.Color;
        //
        public enum BufferOutput
        {
            Color,
            Depth,
            ShadowMap
        }
        
    }
}