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
    
    public void Init(Cell cell, TextMeshProUGUI text)
    {
        _cell = cell;
        _text = text;
        
        SetCell(cell);
    }

    public void SetCell(Cell cell)
    {
        _cell = cell;

        gameObject.SetActive(!(cell.H < Math.Pow(10, -4)));

        Transform cellTransform = transform;
        
        Vector3 scale = cellTransform.localScale;
        scale.y = cell.H;
        cellTransform.localScale = scale;

        Vector3 pos = cellTransform.position;
        pos.y = cell.H / 2;
        cellTransform.position = pos;
    }

    private void OnMouseEnter()
    {
        _text.text = $"({_cell.X + 1}, {_cell.Y + 1}): h: {_cell.H}, qx: {_cell.Qx}, qy: {_cell.Qy}";
    }
}
