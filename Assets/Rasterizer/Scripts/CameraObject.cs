using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Rasterizer
{
    public class CameraObject : MonoBehaviour
    {
        [SerializeField] 
        private RasterizerSettings m_Settings;
        [SerializeField] 
        private Light m_MainLight;

        public RawImage rawImage;
        
        private Camera m_Camera;
        
        private Camera m_LightCamera;
        
        private List<RenderObject> m_RenderObjects = new List<RenderObject>();
        
        private Rasterizer m_Rasterizer;

        public PanelUI panelUI;
        
        void Start()
        {
            Initialize();
        }

        private void OnPostRender()
        {
            Render();
        }

        private void CreateLightCamera()
        {
            GameObject lightGO = m_MainLight.gameObject;
            m_LightCamera = lightGO.AddComponent<Camera>();
            m_LightCamera.orthographic = true;
            m_LightCamera.orthographicSize = 8f;
            m_LightCamera.nearClipPlane = m_Camera.nearClipPlane;
            m_LightCamera.farClipPlane = 34.0f;
            m_LightCamera.enabled = false;
        }
        
        private void Initialize()
        {
            m_Camera = GetComponent<Camera>();
            CreateLightCamera();

            //set render objects:
            m_RenderObjects.Clear();
            GameObject[] rootRenderObjs = this.gameObject.scene.GetRootGameObjects();
            foreach (GameObject obj in rootRenderObjs)
            {
                m_RenderObjects.AddRange(obj.GetComponentsInChildren<RenderObject>());
            }
            Debug.LogFormat("Render {0} objects totally.", m_RenderObjects.Count);

            RectTransform rect = rawImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Screen.width, Screen.height);
            int w = Mathf.FloorToInt(rect.rect.width);
            int h = Mathf.FloorToInt(rect.rect.height);
            Debug.Log($"Screen size: {w}x{h}");

            m_Rasterizer = new Rasterizer(w, h, m_Settings);

            if (panelUI != null)
            {
                m_Rasterizer.updateDelegate = panelUI.PanelDelegate;
                
            }
        }

        private void Render()
        {
            Profiler.BeginSample("Rendering per frame");
            
            // rawImage.gameObject.SetActive(true);
            
            //clear screen
            m_Rasterizer.Clear();

            //set vertex attributes
            m_Rasterizer.SetUniforms(m_Camera, m_LightCamera, m_MainLight);

            //Drawcall
            m_Rasterizer.DrawCall(m_RenderObjects);
            
            //show texture:
            switch (m_Settings._BufferOutput)
            {
                case RasterizerSettings.BufferOutput.Depth:
                    rawImage.texture = m_Rasterizer.depthTexture;
                    break;
                case RasterizerSettings.BufferOutput.ShadowMap:
                    rawImage.texture = m_Rasterizer.shadowMapTexture;
                    break;
                default:
                    rawImage.texture = m_Rasterizer.colorTexture;
                    break;
            }

            m_Rasterizer.UpdateFrame();
            
            Profiler.EndSample();

        }
        
        private void OnDestroy()
        {
            m_Rasterizer.Release();
            Destroy(m_LightCamera);
        }
    }
}