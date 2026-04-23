using UnityEngine;

public class FollowPlane : MonoBehaviour
{
    public Transform plane;
    public float offsetTranslation;
    public Quaternion offsetRotation;

    // Update is called once per frame
    void Update()
    {
        transform.position = plane.position + (transform.forward * offsetTranslation) + (transform.up * 3);
        transform.rotation = plane.rotation * offsetRotation;
    }
}
