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
    public GameObject waterCellPrefab;
    public GameObject wallCellPrefab;
    public GameObject groundCellPrefab;
    
    private TextMeshProUGUI _cellText;
    private TextMeshProUGUI _frameText;
    private Toggle _heightToggle;

    private int _width;
    private int _height;
    
    private int _frame;
    
    // Start is called before the first frame update
    private void Start()
    {
        _cellText = GameObject.Find("Cell Info").GetComponent<TextMeshProUGUI>();
        _frameText = GameObject.Find("Frame Info").GetComponent<TextMeshProUGUI>();
        _heightToggle = GameObject.Find("Height Toggle").GetComponent<Toggle>();
        
        _heightToggle.onValueChanged.AddListener(_ => DrawGrid());

        ClearChildren(false);
        
        var inputPath = $@"{path}\input\{simName}.txt";
        string text = File.ReadAllText(inputPath).Split("-")[1];
        
        var heightGrid = new Grid(text);
        _width = heightGrid.Width;
        _height = heightGrid.Height;

        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                GameObject ground = Instantiate(groundCellPrefab, transform, true);
                Vector3 pos = GridToWorldCoors(x, y);
                pos.y = -0.5f;
                ground.transform.position = pos;

                if (heightGrid.GetCell(x, y).H > 0)
                {
                    GameObject wall = Instantiate(wallCellPrefab, transform, true);
                    wall.transform.position = GridToWorldCoors(x, y);
                    wall.GetComponent<HeightCell>().Init(heightGrid.GetCell(x, y), _cellText);
                }
            }
        }

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

    private void ClearChildren(bool keepSolid)
    {
        foreach (Transform child in transform)
        {
            if (!(keepSolid && child.CompareTag("Solid")))
                Destroy(child.gameObject);
        }
    }

    private bool DrawGrid()
    {
        var outputPath = $@"{path}\output\{simName}\{_frame}.txt";

        if (!File.Exists(outputPath))
            return false;

        ClearChildren(true);

        var grid = new Grid(File.ReadAllText(outputPath));

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                float height = grid.GetCell(x, y).H;
                if (_heightToggle.isOn ? height == 0 : height < 1 / Math.Pow(10, 4))
                    continue;
                
                GameObject cell = Instantiate(waterCellPrefab, transform, true);
                cell.transform.position = GridToWorldCoors(x, y);
                cell.GetComponent<HeightCell>().Init(grid.GetCell(x, y), _cellText);
            }
        }

        _frameText.text = $"Frame {_frame}";

        return true;
    }

    private Vector3 GridToWorldCoors(int x, int y)
    {
        return new Vector3(x - _width / 2f, 0, -(y - _height / 2f));
    }
}
