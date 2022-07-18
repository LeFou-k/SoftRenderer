using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Rasterizer
{
    public class CameraObject : MonoBehaviour
    {
        [SerializeField] 
        private RasterizerSettings m_Settings;

        private Camera m_Camera;
        private List<RenderObject> m_RenderObjects;
        private Rasterizer m_Rasterizer;
        [SerializeField] private Light m_MainLight;
        
        private RawImage m_RawImage;
        private void Start()
        {
            Initialize();
            Render();
        }

        private void OnPostRender()
        {
            
        }

        private void Initialize()
        {
            m_Camera = GetComponent<Camera>();
            m_Camera.cullingMask = 0;
            m_RawImage.gameObject.SetActive(true);

            //set render objects:
            m_RenderObjects.Clear();
            GameObject[] rootRenderObjs = this.gameObject.scene.GetRootGameObjects();
            foreach (GameObject obj in rootRenderObjs)
            {
                m_RenderObjects.AddRange(obj.GetComponentsInChildren<RenderObject>());
            }
            Debug.LogFormat("Render {0} objects totally.", m_RenderObjects.Count);

            RectTransform rect = m_RawImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Screen.width, Screen.height);
            int w = Mathf.FloorToInt(rect.rect.width);
            int h = Mathf.FloorToInt(rect.rect.height);
            Debug.Log($"Screen size: {w}x{h}");

            m_Rasterizer = new Rasterizer(w, h, m_Settings);
        }

        private void Render()
        {
            Profiler.BeginSample("Rendering per frame");
            m_Rasterizer.Clear();
            m_Rasterizer.SetAttributes(m_Camera, m_MainLight);

            foreach (RenderObject obj in m_RenderObjects)
            {
                if (obj.gameObject.activeInHierarchy)
                {
                    m_Rasterizer.DrawCall(obj);
                }
            }
            
            m_Rasterizer.Update();
            Profiler.EndSample();

        }
        
        private void OnDestroy()
        {
            m_Rasterizer.Release();
        }
    }
}