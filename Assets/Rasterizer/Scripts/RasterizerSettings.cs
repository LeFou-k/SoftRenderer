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
        public bool FrustumCulling = true;
        public bool BackFaceCulling = true;
        public BufferOutput _BufferOutput = BufferOutput.Color;
        public MSAALevel _MSAALevel = MSAALevel.Disabled;
        
        
        public enum BufferOutput
        {
            Color,
            Depth
        }
        
        public enum MSAALevel
        {
            Disabled,
            x2 = 2,
            x4 = 4
        }
    }
}