using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using System;
using System.Text;
using System.Net;
//using UnityEngine.UIElements;

public class WebRequester : MonoBehaviour
{
    // Konstante für den "HelloWorld" Endpoint
    private const string ENDPOINT_HELLOWORLD = "/api/helloworld";
    private const string ENDPOINT_PROCESS_FACE = "/api/processFace";


    bool readyForNewChatGPTPrompt = true;

    [SerializeField] WebcamDebug webcamDebug;


    [SerializeField] private string baseWebAddress; // https://vercel-backend-khuk1nas7-tillomaticus-projects.vercel.app
                                                    // http://localhost:3000

    //[SerializeField]
    // private WebcamScreenshotCapture webcamCaptureControl;

    // Dictionary to store header key-value pairs
    [SerializeField] private List<Header> headers = new List<Header>();//x-vercel-protection-bypass=EfZ0njB8c1xEm1XXGAWpklmv6060rwA6

    [System.Serializable]
    public class Header
    {
        public string key;
        public string value;
    }

    // UI Elemente
    [SerializeField] private TMPro.TMP_InputField inputField;
    [SerializeField] private Button sendButton;      // Button zum Absenden
    [SerializeField] private TMP_Text resultTextPanel;   // TextPanel für das Ergebnis

    [System.Serializable]
    public class ImageRequest
    {
        public string image_base64;
    }


    void Awake()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public void OnSendButtonClicked()
    {
        // Text aus der Eingabe holen und senden
        string userInput = inputField.text;
        Debug.Log("input text:" + userInput);
        StartCoroutine(SendChatMessage(userInput));
    }

    public void TestEndpoint()
    {
        StartCoroutine(TestGetEndpoint());
    }


    IEnumerator TestGetEndpoint()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseWebAddress + ENDPOINT_PROCESS_FACE))
        {
            AddHeaders(www); // include x-vercel-protection-bypass if needed

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("GET Error: " + www.error);
            }
            else
            {
                Debug.Log("GET Response: " + www.downloadHandler.text);
            }
        }
    }

    public void DebugSendImage()
    {
        Texture2D texture = webcamDebug.GetCurrentFrame();

        StartCoroutine(ContinousSend());


        // StartCoroutine(StartSendImageProcess(texture));
    }

    IEnumerator ContinousSend()
    {
        while (true)
        {

            yield return null;
            if (readyForNewChatGPTPrompt)
            {
                readyForNewChatGPTPrompt = false;
                Texture2D texture = webcamDebug.GetCurrentFrame();
                StartCoroutine(StartSendImageProcess(texture));
            }
        }
    }

    public void OnCaptureImage(Texture2D texture)
    {

        if (!readyForNewChatGPTPrompt)
            return;

        readyForNewChatGPTPrompt = false;
        StartCoroutine(StartSendImageProcess(texture));

    }
    public static async Task<string> TextureToBase64Async(Texture2D texture, bool asJPG = false)
    {
        if (texture == null) return null;

        // Run encoding on a background thread to avoid freezing Unity
        byte[] imageBytes = await Task.Run(() =>
        {
            return asJPG ? texture.EncodeToJPG() : texture.EncodeToPNG();
        });

        return "data:image/" + (asJPG ? "jpeg" : "png") + ";base64," + Convert.ToBase64String(imageBytes);
    }

    IEnumerator StartSendImageProcess(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null!");
            yield break;
        }

        byte[] imageBytes = null;
        imageBytes = texture.EncodeToJPG();
        string base64 = null;

        yield return null;

        base64 = Convert.ToBase64String(imageBytes);
        yield return StartCoroutine(GetExpressionFromImage(base64));
    }

    IEnumerator GetExpressionFromImage(string _imageBase64Encoded)
    {

        Debug.Log("image64 " + _imageBase64Encoded);

        // Create a JSON object manually in the same style that worked before
        string jsonMessage = $"{{ \"image_base64\": \"{_imageBase64Encoded}\" }}";
        Debug.Log("jsonMessage: " + jsonMessage);

        using (UnityWebRequest www = UnityWebRequest.Post(baseWebAddress + ENDPOINT_PROCESS_FACE, jsonMessage, "application/json"))
        {
            AddHeaders(www); // custom headers

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                resultTextPanel.text = "Error: " + www.error;
                readyForNewChatGPTPrompt = true;
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Server: " + jsonResponse);
                resultTextPanel.text = "Server: " + jsonResponse;
                readyForNewChatGPTPrompt = true;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }


    IEnumerator SendChatMessage(string _message)
    {
        // Create a JSON object with the message
        string jsonMessage = $"{{ \"message\": \"{_message}\" }}";
        Debug.Log(JsonUtility.ToJson(_message));
        Debug.Log("jsonMessage: " + jsonMessage);

        using (UnityWebRequest www = UnityWebRequest.Post(baseWebAddress + ENDPOINT_HELLOWORLD, jsonMessage, "application/json"))
        {
            AddHeaders(www); // Header hinzufügen

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                resultTextPanel.text = "Error: " + www.error;
            }
            else
            {
                // Ergebnis im TextPanel anzeigen
                resultTextPanel.text = "Server: " + www.downloadHandler.text;
            }
        }
    }



    public void CallHelloWorld()
    {
        Debug.Log("Start: Fuck (- where is) the hammer?!");
        StartCoroutine(GetRequest(baseWebAddress + ENDPOINT_HELLOWORLD));
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            Debug.Log("GetRequest 1: " + uri);

            AddHeaders(webRequest); // Add headers to the request

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;
            Debug.Log("GetRequest 2: " + uri);

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    private void AddHeaders(UnityWebRequest request)
    {
        foreach (Header header in headers)
        {
            request.SetRequestHeader(header.key, header.value);
        }
    }
}
