using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rasterizer
{
    public class PanelUI : MonoBehaviour
    {
        private Text m_TriangleText;
        private Text m_VertexText;

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public void OnGUI()
        {
            throw new NotImplementedException();
        }

        public void PanelDelegate(int vertices, int triangles)
        {
            m_TriangleText.text = $"Triangles: {triangles}";
            m_VertexText.text = $"Vertices: {vertices}";
        }
    }
}