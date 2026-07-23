using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-9f, 16f, -9f);
    public float smoothSpeed = 7f;

    private float fixedHeight;

    void Start()
    {
        fixedHeight = offset.y;

        // İlk karede kamerayı doğrudan doğru yere ışınla (kaymasın)
        if (target != null)
        {
            Vector3 flatTargetPos = new Vector3(target.position.x, 0f, target.position.z);
            transform.position = flatTargetPos + new Vector3(offset.x, fixedHeight, offset.z);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 flatTargetPos = new Vector3(target.position.x, 0f, target.position.z);
        Vector3 desiredPosition = flatTargetPos + new Vector3(offset.x, fixedHeight, offset.z);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}