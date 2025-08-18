using UnityEngine;

[ExecuteAlways]
public class ShaderScreenCenterSetter : MonoBehaviour
{
    public Material targetMaterial;         
    public string propertyName = "_Center";  
    public Transform centerTransform;        
    public Camera targetCamera;              
    public bool expectsNormalized = true;    
    public bool invertY = false;             
    public bool clampToScreen = true;        

    void Update()
    {
        if (targetMaterial == null || centerTransform == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 vp = targetCamera.WorldToViewportPoint(centerTransform.position);

        if (vp.z < 0f)
        {
            if (clampToScreen)
            {
                vp.x = Mathf.Clamp01(vp.x);
                vp.y = Mathf.Clamp01(vp.y);
            }
            else
            {

            }
        }

        float x = vp.x;
        float y = invertY ? 1f - vp.y : vp.y;

        if (!expectsNormalized)
        {
            x *= Screen.width;
            y *= Screen.height;
        }

        if (targetMaterial.HasProperty(propertyName))
        {
            targetMaterial.SetVector(propertyName, new Vector4(x, y, 0f, 0f));
        }
        else
        {

#if UNITY_EDITOR
            Debug.LogWarning($"Material does not have property '{propertyName}'. Available shader: {targetMaterial.shader.name}");
#endif
        }
    }
}
