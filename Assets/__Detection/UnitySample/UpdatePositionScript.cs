using UnityEngine;

public class UpdatePositionScript : MonoBehaviour
{
    [SerializeField]
    private GameObject EmoticonChanger;

    public void UpdatePosition(FaceDetectionResult result)
    {
        this.transform.position = result.worldPosition;
        EmoticonChanger.transform.position = result.worldPosition + new Vector3(0, (result.worldPosition.y+result.imageSize.y/2),0);
    }

}
