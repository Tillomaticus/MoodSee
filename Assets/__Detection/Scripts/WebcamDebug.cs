using UnityEngine;
using UnityEngine.UI; // Only if using UI RawImage

public class WebcamDebug : MonoBehaviour
{
    public RawImage rawImage; // Drag a RawImage from your Canvas here (optional)
    private WebCamTexture webcamTexture;

    void Start()
    {
        // Pick the first available webcam
        string camName = WebCamTexture.devices.Length > 0 ? WebCamTexture.devices[0].name : null;
        if (camName == null)
        {
            Debug.LogError("No webcam detected!");
            return;
        }

        webcamTexture = new WebCamTexture(camName);
        webcamTexture.Play();

        // Optionally show it on a RawImage
        if (rawImage != null)
        {
            rawImage.texture = webcamTexture;
            rawImage.material.mainTexture = webcamTexture;
        }
    }

    public Texture2D GetCurrentFrame()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return null;

        Texture2D tex = new Texture2D(webcamTexture.width, webcamTexture.height);
        tex.SetPixels(webcamTexture.GetPixels());
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (webcamTexture != null)
            webcamTexture.Stop();
    }
}