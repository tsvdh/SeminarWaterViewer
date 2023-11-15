using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCell : MonoBehaviour
{
    private float _width;
    private float _height;

    public void Init(float width, float height)
    {
        _width = width;
        _height = height;

        Transform cellTransform = transform;
        Vector3 scale = cellTransform.localScale;
        scale.x = width;
        scale.z = height;
        cellTransform.localScale = scale;
    }
}
