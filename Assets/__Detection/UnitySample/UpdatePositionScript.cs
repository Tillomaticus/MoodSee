using UnityEngine;

public class UpdatePositionScript : MonoBehaviour
{

    public void UpdatePosition(FaceDetectionResult result)
    {
        this.transform.position = result.worldPosition;
    }
}
