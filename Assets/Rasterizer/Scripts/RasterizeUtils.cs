using UnityEngine;

namespace Rasterizer
{
    public static class RasterizeUtils
    {
        public const float PI = 3.1415926f;
        public const float D2R = PI / 180.0f;

        public static Matrix4x4 GetViewMatrix(Vector3 eyePosition, Vector3 lookAt, Vector3 up)
        {
            Vector3 cameraZ = -lookAt.normalized;
            Vector3 cameraY = up.normalized;
            Vector3 cameraX = Vector3.Cross(cameraY, cameraZ);
            cameraY = Vector3.Cross(cameraZ, cameraX);
            
            Matrix4x4 rotationMat = Matrix4x4.identity;
            rotationMat.SetColumn(0, cameraX);
            rotationMat.SetColumn(1, cameraY);
            rotationMat.SetColumn(2, cameraZ);
            
            Matrix4x4 translateMat = Matrix4x4.identity;
            translateMat.SetColumn(3, new Vector4(-eyePosition.x, -eyePosition.y, -eyePosition.z));

            return rotationMat.transpose * translateMat;
        }
        
        public static Matrix4x4 GetOrthographicProjectionMatrix(float l, float r, float b, float t, float f, float n)
        {
            Matrix4x4 translate = Matrix4x4.identity;
            translate.SetColumn(3, new Vector4(-(r + l) * 0.5f, -(t + b) * 0.5f, -(n + f) * 0.5f, 1f));
            Matrix4x4 scale = Matrix4x4.identity;
            scale.m00 = 2f / (r - l);
            scale.m11 = 2f / (t - b);
            scale.m22 = 2f / (n - f);

            return scale * translate;
        }

        public static Matrix4x4 GetPerspectiveProjectionMatrix(float l, float r, float b, float t, float f, float n)
        {
            Matrix4x4 persp2Ortho = Matrix4x4.identity;
            persp2Ortho.m00 = n;
            persp2Ortho.m11 = n;
            persp2Ortho.m22 = n + f;
            persp2Ortho.m23 = -n * f;
            persp2Ortho.m32 = 1;
            persp2Ortho.m33 = 0;

            return GetOrthographicProjectionMatrix(l, r, b, t, f, n) * persp2Ortho;
        }
        public static Matrix4x4 GetPerspectiveProjectionMatrix(float eye_fov, float aspect_ratio, float zNear, float zFar)
        {
            float t = zNear * Mathf.Tan(eye_fov * D2R * 0.5f);
            float b = -t;
            float r = t * aspect_ratio;
            float l = -r;
            float n = -zNear;
            float f = -zFar;
            return GetPerspectiveProjectionMatrix(l, r, b, t, f, n);
        }
        
        public static void SetViewProjectionMatrix(Camera camera, float aspect, out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix)
        {
            //Transform from left hand coordinates to right hand coordinates:
            //z *= -1
            Vector3 cameraPos = camera.transform.position;
            cameraPos.z *= -1;
            Vector3 lookAt = camera.transform.forward;
            lookAt.z *= -1;
            Vector3 up = camera.transform.up;
            up.z *= -1;

            viewMatrix = GetViewMatrix(cameraPos, lookAt, up);
            if (camera.orthographic)
            {
                float halfHeight = camera.orthographicSize;
                float halfWidth = halfHeight * aspect;
                float f = -camera.farClipPlane;
                float n = -camera.nearClipPlane;
                projMatrix = GetOrthographicProjectionMatrix(-halfWidth, halfWidth, -halfHeight, halfHeight, f, n);
            }
            else
            {
                projMatrix = GetPerspectiveProjectionMatrix(camera.fieldOfView, aspect, camera.nearClipPlane,
                    camera.farClipPlane);
            }
        }
        
        
        //define show panel delegate

    }
}