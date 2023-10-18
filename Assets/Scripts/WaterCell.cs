using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
    public void SetHeight(float height)
    {
        Transform cellTransform = transform;
        
        Vector3 scale = cellTransform.localScale;
        scale.y = height;
        cellTransform.localScale = scale;

        Vector3 pos = cellTransform.position;
        pos.y = height / 2;
        cellTransform.position = pos;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
