using System.Collections;
using UnityEngine;

public class UpdatePositionScript : MonoBehaviour
{
    [SerializeField]
    private GameObject EmoticonChanger;
    [SerializeField] private GameObject centerEyeAnchor; // Reference to the center eye anchor in the CameraRig


    [SerializeField]
    public float lerpSpeed = 5f;


    Coroutine positionLerpCoroutine;


    Vector3 oldPosition;


    //
    [SerializeField]
    Vector3 emojiOffset = new Vector3(0f,0.1f,0f);


// just for debug purpose
    public static UpdatePositionScript Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void UpdatePosition(FaceDetectionResult result)
    {
        if (result.isEmpty)
        {
            Debug.Log("Result is empty");
        }
        //     HideFaceMarker();
        else
        {
            //  ShowFaceMarker();


            //positioning for first time
            if (oldPosition == Vector3.zero)
            {
                EmoticonChanger.transform.position = result.worldPosition + emojiOffset;
                oldPosition = EmoticonChanger.transform.position;
            }
            else
            {
                //if we already have a target, then stop the old coroutine 
                if (positionLerpCoroutine != null)
                    StopCoroutine(positionLerpCoroutine);

                // start a new coroutine and keep lerping towards target
                positionLerpCoroutine = StartCoroutine(UpdatePositionLerped(result.worldPosition + emojiOffset));
            }

            Debug.Log("Result y: " + result.worldPosition.y + " size " + result.imageSize.y);

            //rotation
            Vector3 direction = EmoticonChanger.transform.position - centerEyeAnchor.transform.position;
            EmoticonChanger.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }


    IEnumerator UpdatePositionLerped(Vector3 targetPosition)
    {
        while (Vector3.Distance(EmoticonChanger.transform.position, targetPosition) > 0.01f)
        {
            EmoticonChanger.transform.position = Vector3.Lerp(oldPosition, targetPosition, Time.deltaTime * lerpSpeed);
            yield return null;
        }
    }


    void HideFaceMarker()
    {
        this.gameObject.SetActive(false);
    }

    void ShowFaceMarker()
    {
        this.gameObject.SetActive(true);
    }

}
