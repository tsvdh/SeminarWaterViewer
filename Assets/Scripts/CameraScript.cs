using System;
using System.IO;
using Configs;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    private Vector3 _prevMousePos;

    private int _width;
    
    // Start is called before the first frame update
    private void Start()
    {
        var gridManager = GameObject.Find("Grid").GetComponent<GridManager>();
        var simConfigPath = $@"{gridManager.path}\input\{gridManager.simName}\config.json";
        var simConfig = JsonUtility.FromJson<SimConfig>(File.ReadAllText(simConfigPath));

        _width = simConfig.width;
        
        _prevMousePos = Input.mousePosition;
    }

    // Update is called once per frame
    private void Update()
    {
        Transform curTransform = transform;

        Vector3 forward = curTransform.forward;
        forward.y = 0;
        forward.Normalize();
        
        // full grid in 2 seconds
        float speedMultiplier = Time.deltaTime * _width / 2;
        
        if (Input.GetKey(KeyCode.W))
        {
            curTransform.position += forward * speedMultiplier;
        }
        if (Input.GetKey(KeyCode.S))
        {
            curTransform.position += forward * -speedMultiplier;
        }
        if (Input.GetKey(KeyCode.A))
        {
            curTransform.position += curTransform.right * -speedMultiplier;
        }
        if (Input.GetKey(KeyCode.D))
        {
            curTransform.position += curTransform.right * speedMultiplier;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            // 1 second of scrolling for full range
            float scrollMultiplier = Time.deltaTime * _width / 1;

            if (scroll > 0)
                curTransform.position += Vector3.down * scrollMultiplier;
            else
                curTransform.position += Vector3.up * scrollMultiplier;
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
