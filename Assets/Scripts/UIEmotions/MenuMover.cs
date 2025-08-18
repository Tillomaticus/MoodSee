using UnityEngine;

public class MenuMover : MonoBehaviour
{
    [SerializeField] private GameObject centerEyeAnchor; // Reference to the center eye anchor in the CameraRig
    [SerializeField] private GameObject gui; // Reference to the GUI GameObject


    // Update is called once per frame
    void Update()
    {
        gui.transform.position = centerEyeAnchor.transform.position + centerEyeAnchor.transform.forward * 0.6f;
        gui.transform.rotation = Quaternion.LookRotation(gui.transform.position - centerEyeAnchor.transform.position);
    }
}
