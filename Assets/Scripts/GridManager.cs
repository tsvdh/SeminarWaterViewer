using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public string path;
    public string simName;
    public GameObject waterCellPrefab;
    public GameObject wallCellPrefab;
    public GameObject groundCellPrefab;
    public Material waterMaterial;
    
    private TextMeshProUGUI _cellText;
    private TextMeshProUGUI _frameText;
    private Toggle _heightToggle;
    private Toggle _meshToggle;

    private int _frame;
    private int _simFPS;
    private float _timeSinceLastFrame;
    private bool _forwardCache;
    private bool _backwardsCache;
    
    private int _width;
    private int _height;
    private int _cornerVertices;
    private int _centerVertices;
    private int _triangles;

    private Grid _heightGrid;
    
    // Start is called before the first frame update
    private void Start()
    {
        _cellText = GameObject.Find("Cell Info").GetComponent<TextMeshProUGUI>();
        _frameText = GameObject.Find("Frame Info").GetComponent<TextMeshProUGUI>();
        _heightToggle = GameObject.Find("Height Toggle").GetComponent<Toggle>();
        _meshToggle = GameObject.Find("Mesh Toggle").GetComponent<Toggle>();
        
        _heightToggle.onValueChanged.AddListener(_ => DrawGrid());
        _meshToggle.onValueChanged.AddListener(_ => DrawGrid());

        ClearChildren(false);

        var configPath = $@"{path}\input\{simName}\config.txt";
        var inputPath = $@"{path}\input\{simName}\data.txt";

        string[] args = File.ReadAllText(configPath).Trim().Split("\n");
        _simFPS = int.Parse(args[0]);
        
        string data = File.ReadAllText(inputPath).Split("-")[1];
        _heightGrid = new Grid(data);
        _width = _heightGrid.Width;
        _height = _heightGrid.Height;
        
        _cornerVertices = (_width + 1) * (_height + 1);
        _centerVertices = _width * _height;
        _triangles = 12 * _centerVertices;
        
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                GameObject ground = Instantiate(groundCellPrefab, transform, true);
                Vector3 pos = GridToWorldCoors(x, y);
                pos.y = -0.5f;
                ground.transform.position = pos;

                if (_heightGrid.GetCell(x, y).H > 0)
                {
                    GameObject wall = Instantiate(wallCellPrefab, transform, true);
                    wall.transform.position = GridToWorldCoors(x, y);
                    wall.GetComponent<HeightCell>().Init(_heightGrid.GetCell(x, y), _cellText);
                }
            }
        }

        DrawGrid();
    }

    // Update is called once per frame
    private void Update()
    {
        _timeSinceLastFrame += Time.deltaTime;
        
        if (Input.GetKeyDown(KeyCode.RightArrow)
            || (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.RightShift)))
            _forwardCache = true;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow)
            || (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightShift)))
            _backwardsCache = true;
        
        if (_timeSinceLastFrame < 1f / _simFPS)
            return;
        
        if (_forwardCache)
        {
            _frame++;
            if (!DrawGrid())
                _frame--;
        }
        if (_backwardsCache)
        {
            _frame--;
            if (!DrawGrid())
                _frame++;
        }
        
        _timeSinceLastFrame = 0;
        _forwardCache = false;
        _backwardsCache = false;
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
        // long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        
        var outputPath = $@"{path}\output\{simName}\{_frame}.txt";

        if (!File.Exists(outputPath))
            return false;

        ClearChildren(true);

        var grid = new Grid(File.ReadAllText(outputPath));

        // long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        // Debug.Log($"Disk access: {now - start}ms");
        // start = now;
        
        if (_meshToggle.isOn)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            var vertices = new Vector3[_triangles];
            var triangles = new int[_triangles];

            var interpGrid = new Vector3[_cornerVertices];

            for (var y = 0; y <= grid.Height; y++)
            {
                for (var x = 0; x <= grid.Width; x++)
                {
                    Vector3 cornerPos = GridToWorldCoors(x, y);
                    cornerPos.y = ShiftNearZeroHeight(BiLerp(x, y, grid));
                    cornerPos.x -= 0.5f;
                    cornerPos.z += 0.5f;

                    interpGrid[CoorsToVertexIndex(x, y, false)] = cornerPos;
                }
            }
            
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    if (_heightGrid.GetCell(x, y).H > 0)
                        continue;
                    
                    int startIndex = CoorsToTriangleIndex(x, y);

                    Vector3 topLeftPos = interpGrid[CoorsToVertexIndex(x, y, false)];
                    Vector3 topRightPos = interpGrid[CoorsToVertexIndex(x + 1, y, false)];
                    Vector3 bottomLeftPos = interpGrid[CoorsToVertexIndex(x, y + 1, false)];
                    Vector3 bottomRightPos = interpGrid[CoorsToVertexIndex(x + 1, y + 1, false)];
                    Vector3 centerPos = GridToWorldCoors(x, y);
                    centerPos.y = ShiftNearZeroHeight(grid.GetCell(x, y).H);

                    Vector3[] posArray =
                    {
                        centerPos, topLeftPos, topRightPos, // top
                        centerPos, bottomLeftPos, topLeftPos, // left
                        centerPos, bottomRightPos, bottomLeftPos, // bottom
                        centerPos, topRightPos, bottomRightPos // right
                    };
                    
                    for (var i = 0; i < 12; i++)
                    {
                        vertices[startIndex + i] = posArray[i];
                        triangles[startIndex + i] = startIndex + i;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            var surface = new GameObject("Surface", typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.parent = transform;
            surface.GetComponent<MeshFilter>().mesh = mesh;
            surface.GetComponent<MeshRenderer>().material = waterMaterial;
        }
        else
        {
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
        }

        _frameText.text = $"Frame {_frame}";

        // now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        // Debug.Log($"Mesh construction: {now - start}ms");
        
        return true;
    }

    private Vector3 GridToWorldCoors(int x, int y)
    {
        return new Vector3(x - _width / 2f, 0, -(y - _height / 2f));
    }

    private int CoorsToVertexIndex(int x, int y, bool cellCenter)
    {
        return !cellCenter 
                   ? y * (_width + 1) + x 
                   : _cornerVertices + y * _width + x;
    }

    private int CoorsToTriangleIndex(int x, int y)
    {
        return 12 * (y * _width + x);
    }

    private float BiLerp(float x, float y, Grid grid)
    {
        // shift cell coors to grid center coors
        float xGrid = x - 0.5f;
        float yGrid = y - 0.5f;

        int xMin = Math.Max(0, (int)Math.Floor(xGrid));
        int xMax = Math.Min(_width - 1, (int)Math.Ceiling(xGrid));
        
        int yMin = Math.Max(0, (int)Math.Floor(yGrid));
        int yMax = Math.Min(_width - 1, (int)Math.Ceiling(yGrid));

        float topLeft = grid.GetCell(xMin, yMin).H;
        float topRight = grid.GetCell(xMax, yMin).H;
        float bottomLeft = grid.GetCell(xMin, yMax).H;
        float bottomRight = grid.GetCell(xMax, yMax).H;

        bool topLeftWall = _heightGrid.GetCell(xMin, yMin).H > 0;
        bool topRightWall = _heightGrid.GetCell(xMax, yMin).H > 0;
        bool bottomLeftWall = _heightGrid.GetCell(xMin, yMax).H > 0;
        bool bottomRightWall = _heightGrid.GetCell(xMax, yMax).H > 0;

        float xW = xGrid - (float)Math.Truncate(xGrid);
        float yW = yGrid - (float)Math.Truncate(yGrid);

        float top = topLeftWall ? topRight 
                    : topRightWall ? topLeft 
                    : Lerp(topLeft, topRight, xW);
        float bottom = bottomLeftWall ? bottomRight 
                       : bottomRightWall ? bottomLeft
                       : Lerp(bottomLeft, bottomRight, xW);

        return topLeftWall && topRightWall ? bottom 
                   : bottomLeftWall && bottomRightWall ? top 
                   : Lerp(top, bottom, yW);
    }

    private static float Lerp(float a, float b, float w)
    {
        return a * (1 - w) + b * w;
    }

    private static float ShiftNearZeroHeight(float height)
    {
        return height < Math.Pow(10, -4) 
                   ? -0.01f 
                   : height;
    }
}
