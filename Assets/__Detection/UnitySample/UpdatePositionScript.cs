using UnityEngine;

public class UpdatePositionScript : MonoBehaviour
{
    [SerializeField]
    private GameObject EmoticonChanger;
    [SerializeField] private GameObject centerEyeAnchor; // Reference to the center eye anchor in the CameraRig

    public void UpdatePosition(FaceDetectionResult result)
    {
        if (result.isEmpty)
            ;
        //     HideFaceMarker();
        else
        {
            //  ShowFaceMarker();
            this.transform.position = result.worldPosition;
            Debug.Log("Result y: " + result.worldPosition.y + " size " + result.imageSize.y);
            EmoticonChanger.transform.position = result.worldPosition + new Vector3(0, (result.worldPosition.y + result.imageSize.y / 2), 0);
            Vector3 direction = EmoticonChanger.transform.position - centerEyeAnchor.transform.position ;
            EmoticonChanger.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    void Update()
    {
        Vector3 direction = EmoticonChanger.transform.position - centerEyeAnchor.transform.position ;
        EmoticonChanger.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
