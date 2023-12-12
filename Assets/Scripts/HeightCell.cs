using System;
using TMPro;
using UnityEngine;

public class HeightCell : MonoBehaviour
{
    private Cell _cell;
    private TextMeshProUGUI _text;

    public void Init(TextMeshProUGUI text)
    {
        _text = text;
    }

    public void SetCell(Cell cell, bool showLow)
    {
        _cell = cell;

        bool belowLowThreshold = cell.H < Math.Pow(10, -2);
        bool belowMinThreshold = cell.H < Math.Pow(10, -6);
        bool shouldShow = !belowLowThreshold || (showLow && !belowMinThreshold);
        gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        Transform cellTransform = transform;
        
        Vector3 scale = cellTransform.localScale;
        scale.y = cell.H;
        cellTransform.localScale = scale;

        Vector3 pos = cellTransform.position;
        pos.y = cell.H / 2;
        cellTransform.position = pos;
    }

    private void OnMouseOver()
    {
        _text.text = $"({_cell.X + 1}, {_cell.Y + 1}): h: {_cell.H}, qx: {_cell.Qx}, qy: {_cell.Qy}";
    }
}
