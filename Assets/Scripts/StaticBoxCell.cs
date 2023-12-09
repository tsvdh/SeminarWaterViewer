using UnityEngine;

public class StaticBoxCell : MonoBehaviour
{
    private float _width;
    private float _height;

    public void Init(float width, float depth, float height, Vector3 worldCoors)
    {
        Transform cellTransform = transform;
        
        Vector3 scale = cellTransform.localScale;
        scale.x = width;
        scale.z = depth;
        scale.y = height;
        cellTransform.localScale = scale;
        
        worldCoors.y = height / 2;
        cellTransform.position = worldCoors;
    }
}
