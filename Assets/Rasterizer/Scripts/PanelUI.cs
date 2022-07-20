using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rasterizer
{
    public class PanelUI : MonoBehaviour
    {
        //FPS:
        public float sampleTime = 1f;
        private const int FONTSIZE = 20;
        public Color fontColor = Color.white;
        
        public Text fpsText;
        public Text triangleText;
        public Text vertexText;
        
        private int frameCount;
        private float timeCost;

        public void Start()
        {
            frameCount = 0;
            timeCost = 0.0f;
            
            UpdateText(fpsText);
            UpdateText(triangleText);
            UpdateText(vertexText);
            
        }

        public void Update()
        {
            frameCount++;
            timeCost += Time.unscaledDeltaTime;

            if (timeCost >= sampleTime)
            {
                float fps = frameCount / timeCost;
                frameCount = 0;
                timeCost = 0.0f;
                UpdateText(fpsText, $"FPS: {fps.ToString("F2")}");
                UpdateText(triangleText);
                UpdateText(vertexText);
            }
        }

        private void UpdateText(Text t, string str = "")
        {
            if (t != null)
            {
                if (str != "")
                {
                    t.text = str;
                }
                if (t.color != fontColor)
                {
                    t.color = fontColor;
                }

                if (t.fontSize != FONTSIZE)
                {
                    t.fontSize = FONTSIZE;
                }
            }
        }
        // public void OnGUI()
        // {
        //     throw new NotImplementedException();
        // }
        
        public void PanelDelegate(int vertices, int triangles)
        {
            UpdateText(fpsText);
            UpdateText(triangleText, $"Triangles: {triangles}");
            UpdateText(vertexText, $"Vertices: {vertices}");
        }
    }
}