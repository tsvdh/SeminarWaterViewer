using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TimeUtils;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Logger = TimeUtils.Logger;

public class GridManager : MonoBehaviour
{
    private struct ConfigFile
    {
        public int fps;
        public int seconds;
        public bool separateFiles;
    }
    
    private enum LoadingEvent 
    {
        All,
        DiskReading,
        GridProcessing,
        MeshProcessing
    }
    
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
    private TextMeshProUGUI _loadingInfo;

    private int _frame;
    private int _simFPS;
    private int _numFrames;
    private float _timeSinceLastFrame;
    private bool _forwardCache;
    private bool _backwardsCache;
    
    // Grid method variables
    private static int _width;
    private static int _height;
    private static int _cornerVertices;
    private static int _centerVertices;
    private static int _triangleVertices;

    // Multithreading input variables
    private static string _pathCopy;
    private static string _simNameCopy;
    private static Grid _heightGrid;
    private static bool _useSeparateFiles;

    // Multithreading output variables
    private static string[] _inputGrids;
    private static Grid[] _grids;
    private static (Vector3[], int[], Vector3[])[] _meshComponents;
    private static Mesh[] _meshes;
    
    // Start is called before the first frame update
    private void Start()
    {
        _pathCopy = path;
        _simNameCopy = simName;
        _cellText = GameObject.Find("Cell Info").GetComponent<TextMeshProUGUI>();
        _frameText = GameObject.Find("Frame Info").GetComponent<TextMeshProUGUI>();
        _heightToggle = GameObject.Find("Height Toggle").GetComponent<Toggle>();
        _meshToggle = GameObject.Find("Mesh Toggle").GetComponent<Toggle>();
        _loadingInfo = GameObject.Find("Loading Info").GetComponent<TextMeshProUGUI>();
        
        _heightToggle.onValueChanged.AddListener(_ => DrawGrid());
        _meshToggle.onValueChanged.AddListener(_ => DrawGrid());

        ClearChildren(false);

        var configPath = $@"{path}\input\{simName}\config.json";
        var inputPath = $@"{path}\input\{simName}\data.txt";

        var config = JsonUtility.FromJson<ConfigFile>(File.ReadAllText(configPath));

        _simFPS = config.fps;
        _numFrames = config.seconds * _simFPS + 1;
        _useSeparateFiles = config.separateFiles;
        
        string data = File.ReadAllText(inputPath).Split("-")[1];
        _heightGrid = new Grid(data);
        _width = _heightGrid.Width;
        _height = _heightGrid.Height;
        
        _cornerVertices = (_width + 1) * (_height + 1);
        _centerVertices = _width * _height;
        _triangleVertices = 12 * _centerVertices;

        GameObject ground = Instantiate(groundCellPrefab, transform, true);
        Vector3 groundPos = ground.transform.position;
        groundPos.y = -0.5f;
        groundPos.x -= 0.5f;
        groundPos.z += 0.5f;
        ground.transform.position = groundPos;
        ground.GetComponent<GroundCell>().Init(_width, _height);
        
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                if (_heightGrid.GetCell(x, y).H > 0)
                {
                    GameObject wall = Instantiate(wallCellPrefab, transform, true);
                    wall.transform.position = GridToWorldCoors(x, y);
                    wall.GetComponent<HeightCell>().Init(_heightGrid.GetCell(x, y), _cellText);
                }
            }
        }

        _inputGrids = new string[_numFrames];
        _grids = new Grid[_numFrames];
        _meshComponents = new (Vector3[], int[], Vector3[])[_numFrames];
        _meshes = new Mesh[_numFrames];
        
        Logger.StartEvent(LoadingEvent.All);
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

        UpdateLoadingProcess();
        
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

    private void UpdateLoadingProcess()
    {
        if (Logger.IsEventDone(LoadingEvent.All))
            return;
        
        if (!_useSeparateFiles && !Logger.IsEventStarted(LoadingEvent.DiskReading))
        {
            Logger.StartEvent(LoadingEvent.DiskReading);
            Task.Run(() =>
            {
                var builder = new StringBuilder();
                var index = 0;
                
                foreach (string line in File.ReadLines($@"{_pathCopy}\output\{_simNameCopy}\full.txt"))
                {
                    if (line.StartsWith("--"))
                    {
                        _inputGrids[index] = builder.ToString();
                        builder.Clear();
                        index++;
                    }
                    else
                    {
                        builder.AppendLine(line);
                    }
                }
                Logger.EndEvent(LoadingEvent.DiskReading, Format.Seconds);
            });
            return;
        }
        
        if (!Logger.IsEventRunning(LoadingEvent.DiskReading) && !Logger.IsEventStarted(LoadingEvent.GridProcessing))
        {
            Logger.StartEvent(LoadingEvent.GridProcessing);
            Task.Run(() => Parallel.For(0, _numFrames, 
                                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 }, 
                                        LoadFrame));
            return;
        }
        
        if (Logger.IsEventRunning(LoadingEvent.GridProcessing))
        {
            var framesLoaded = 0;
            for (var i = 0; i < _numFrames; i++)
            {
                if (_grids[i] != null)
                    framesLoaded++;
            }

            _loadingInfo.text = $"Loading ({(float) framesLoaded / _numFrames:P})";

            if (framesLoaded == _numFrames)
            {
                Logger.EndEvent(LoadingEvent.GridProcessing, Format.Seconds);
                _loadingInfo.text = "";
            }
            else
                return;
        }

        if (!Logger.IsEventStarted(LoadingEvent.MeshProcessing))
        {
            Logger.StartEvent(LoadingEvent.MeshProcessing);

            for (var i = 0; i < _numFrames; i++)
            {
                _meshes[i] = new Mesh
                {
                    indexFormat = IndexFormat.UInt32,
                    vertices = _meshComponents[i].Item1,
                    triangles = _meshComponents[i].Item2,
                    normals = _meshComponents[i].Item3
                };
            }
            
            Logger.EndEvent(LoadingEvent.MeshProcessing, Format.Milliseconds);
        }

        if (!Logger.IsEventDone(LoadingEvent.All))
        {
            Logger.EndEvent(LoadingEvent.All, Format.Seconds);
            DrawGrid();
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
        if (_frame < 0 || _frame >= _grids.Length)
            return false;
        
        ClearChildren(true);
        
        if (_meshToggle.isOn)
        {
            Mesh mesh = _meshes[_frame];
            
            var surface = new GameObject("Surface", typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.parent = transform;
            surface.GetComponent<MeshFilter>().mesh = mesh;
            surface.GetComponent<MeshRenderer>().material = waterMaterial;
        }
        else
        {
            Grid grid = _grids[_frame];
            
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
        
        return true;
    }

    private static Vector3 GridToWorldCoors(int x, int y)
    {
        return new Vector3(x - _width / 2f, 0, -(y - _height / 2f));
    }

    private static int CoorsToVertexIndex(int x, int y, bool cellCenter)
    {
        return !cellCenter 
                   ? y * (_width + 1) + x 
                   : _cornerVertices + y * _width + x;
    }

    private static int CoorsToTriangleIndex(int x, int y)
    {
        return 12 * (y * _width + x);
    }

    private static float BiLerp(float x, float y, Grid grid)
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
    
    private static void LoadFrame(int index)
    {
        string fileName = _useSeparateFiles ? index.ToString() : "full";
        var outputPath = $@"{_pathCopy}\output\{_simNameCopy}\{fileName}.txt";

        if (!File.Exists(outputPath))
            return;
        
        Grid grid = _useSeparateFiles 
                        ? new Grid(File.ReadLines(outputPath)) 
                        : new Grid(_inputGrids[index]);
        
        var vertices = new Vector3[_triangleVertices];
        var triangles = new int[_triangleVertices];
        var normals = new Vector3[_triangleVertices];

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
                
                var cellNormals = new Vector3[4];
                for (var i = 0; i < 4; i++)
                {
                    int triangleStart = startIndex + 3 * i;
                    cellNormals[i] = Vector3.Cross(vertices[triangleStart + 1] - vertices[triangleStart], 
                                                   vertices[triangleStart + 2] - vertices[triangleStart])
                                            .normalized;
                }
                for (var i = 0; i < 12; i++)
                {
                    normals[startIndex + i] = cellNormals[i / 3];
                }
            }
        }

        _grids[index] = grid;
        _meshComponents[index].Item1 = vertices;
        _meshComponents[index].Item2 = triangles;
        _meshComponents[index].Item3 = normals;
    }
}
