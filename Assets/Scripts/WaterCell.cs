using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
    private Cell _cell;
    private TextMeshProUGUI _text;
    
    public void Init(Cell cell, TextMeshProUGUI text)
    {
        _cell = cell;
        _text = text;
        
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
        _text.text = $"({_cell.X}, {_cell.Y}): h: {_cell.H}, qx: {_cell.Qx}, qy: {_cell.Qy}";
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
