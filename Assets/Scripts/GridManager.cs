using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public string path;
    public string simName;
    public GameObject cellPrefab;
    
    private TextMeshProUGUI _cellText;
    private TextMeshProUGUI _frameText;
    private Toggle _heightToggle;
    
    private int _frame;
    
    // Start is called before the first frame update
    private void Start()
    {
        _cellText = GameObject.Find("Cell Info").GetComponent<TextMeshProUGUI>();
        _frameText = GameObject.Find("Frame Info").GetComponent<TextMeshProUGUI>();
        _heightToggle = GameObject.Find("Height Toggle").GetComponent<Toggle>();
        
        _heightToggle.onValueChanged.AddListener(_ => DrawGrid());
        
        DrawGrid();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) 
            || (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.RightShift)))
        {
            _frame++;
            if (!DrawGrid())
                _frame--;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) 
            || (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightShift)))
        {
            _frame--;
            if (!DrawGrid())
                _frame++;
        }
    }

    private bool DrawGrid()
    {
        var fullPath = $@"{path}\output\{simName}\{_frame}.txt";

        if (!File.Exists(fullPath))
            return false;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        var grid = new Grid(fullPath);

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                float height = grid.GetCell(x, y).H;
                if (_heightToggle.isOn ? height == 0 : height < 1 / Math.Pow(10, 4))
                    continue;
                
                GameObject cell = Instantiate(cellPrefab, transform, true);
                cell.transform.position = new Vector3(x - grid.Width / 2f, 0, -(y - grid.Height / 2f));
                cell.GetComponent<WaterCell>().Init(grid.GetCell(x, y), _cellText);
            }
        }

        _frameText.text = $"Frame {_frame}";

        return true;
    }
}
