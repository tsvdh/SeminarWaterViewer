using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public string path;
    public GameObject cellPrefab;
    
    private TextMeshProUGUI _cellText;
    private TextMeshProUGUI _frameText;
    private int _frame;
    
    // Start is called before the first frame update
    private void Start()
    {
        _cellText = GameObject.Find("Cell Info").GetComponent<TextMeshProUGUI>();
        _frameText = GameObject.Find("Frame Info").GetComponent<TextMeshProUGUI>();
        
        DrawGrid();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _frame++;
            DrawGrid();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _frame--;
            DrawGrid();
        }
    }

    private void DrawGrid()
    {
        var fullPath = $"{path}\\out{_frame}.txt";

        if (!File.Exists(fullPath))
        {
            Debug.Log($"{fullPath} does not exist");
            return;
        }

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
                if (height == 0)
                    continue;
                
                GameObject cell = Instantiate(cellPrefab, transform, true);
                cell.transform.position = new Vector3(x - grid.Width / 2f, 0, -(y - grid.Height / 2f));
                cell.GetComponent<WaterCell>().Init(grid.GetCell(x, y), _cellText);
            }
        }

        _frameText.text = $"Frame {_frame}";
    }
}
