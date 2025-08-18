using System.Collections;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;

public class DebugCamera : MonoBehaviour
{
    [SerializeField] WebCamTextureManager webCamTextureManager;
    [SerializeField] RawImage imageDisplay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //   Debug.Log("Start DebugCamera");
        //  StartCoroutine(initCoroutine());
    }

    private IEnumerator initCoroutine()
    {
        Debug.Log("initCoroutine started");

        // Wait for webcam to be ready
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;

        if (imageDisplay != null && webCamTextureManager.WebCamTexture != null)
        {
            Debug.Log("Image set");
            imageDisplay.texture = webCamTextureManager.WebCamTexture;
        }
        else
        {
            Debug.Log("Problem setting image");
        }
    }

    public void UpdateDebugTexture(Texture texture)
    {
        Debug.Log("In Update Debug Texture");
        if (texture == null)
            return;

        imageDisplay.texture = texture;
    }
}
