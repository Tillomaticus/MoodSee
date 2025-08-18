using UnityEngine;

public class UpdatePositionScript : MonoBehaviour
{
    [SerializeField]
    private GameObject EmoticonChanger;
    [SerializeField] private GameObject centerEyeAnchor; // Reference to the center eye anchor in the CameraRig

    public void UpdatePosition(FaceDetectionResult result)
    {
        this.transform.position = result.worldPosition;
        EmoticonChanger.transform.position = result.worldPosition + new Vector3(0, (result.worldPosition.y+result.imageSize.y/2),0);
        EmoticonChanger.transform.rotation = Quaternion.LookRotation(centerEyeAnchor.transform.position - EmoticonChanger.transform.position);
    }

}
