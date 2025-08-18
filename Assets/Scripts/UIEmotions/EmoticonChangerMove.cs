using UnityEngine;

public class EmoticonChangerMove : MonoBehaviour
{
    [SerializeField] private GameObject centerEyeAnchor; // Reference to the center eye anchor in the CameraRig
    [SerializeField] private GameObject gui; // Reference to the GUI GameObject

    [SerializeField] private float X = 0;
    [SerializeField] private float Y = 0;

    // Update is called once per frame
    void Update()
    {
        gui.transform.position = centerEyeAnchor.transform.position + centerEyeAnchor.transform.forward * 0.6f;
        gui.transform.rotation = Quaternion.LookRotation(gui.transform.position - centerEyeAnchor.transform.position);
    }
}
