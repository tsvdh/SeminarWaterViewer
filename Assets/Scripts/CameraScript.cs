using UnityEngine;

public class CameraScript : MonoBehaviour
{
    private Vector3 _prevMousePos;
    
    // Start is called before the first frame update
    private void Start()
    {
        _prevMousePos = Input.mousePosition;
    }

    // Update is called once per fixed frame
    private void FixedUpdate()
    {
        Transform curTransform = transform;

        Vector3 forward = curTransform.forward;
        forward.y = 0;
        forward.Normalize();
        
        if (Input.GetKey(KeyCode.W))
        {
            curTransform.position += forward * 0.2f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            curTransform.position += forward * -0.2f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            curTransform.position += curTransform.right * -0.2f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            curTransform.position += curTransform.right * 0.2f;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            if (scroll > 0)
                curTransform.position += Vector3.down;
            else
                curTransform.position += Vector3.up;
        }

        Vector3 newMousePos = Input.mousePosition;
        Vector3 mousePosDelta = newMousePos - _prevMousePos;
        _prevMousePos = newMousePos;
        
        if (Input.GetMouseButton(1))
        {
            Vector3 eulerAngles = curTransform.eulerAngles;
            eulerAngles.x -= mousePosDelta.y / 2;
            eulerAngles.y += mousePosDelta.x / 2;
            curTransform.eulerAngles = eulerAngles;
        }
    }
}
