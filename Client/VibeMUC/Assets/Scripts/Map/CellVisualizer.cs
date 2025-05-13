using UnityEngine;

namespace VibeMUC.Map
{
    /// <summary>
    /// Handles the visual representation of a cell and its walls
    /// </summary>
    public class CellVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Cell _cell;
        [SerializeField] private MeshRenderer _floorRenderer;
        [SerializeField] private Transform _wallContainer;
        
        [Header("Wall Prefabs")]
        [SerializeField] private GameObject _wallPrefab;
        
        [Header("Wall Settings")]
        [SerializeField] private float _wallThickness = 0.1f;
        [SerializeField] private float _wallHeight = 1f;
        // Offset walls slightly to prevent z-fighting
        [SerializeField] private float _wallOffset = 0.001f;

        [Header("Colors")]
        [SerializeField] private Color _passableColor = Color.white;
        [SerializeField] private Color _impassableColor = Color.gray;
        [SerializeField] private Color _exploredColor = Color.gray;
        [SerializeField] private Color _hiddenColor = Color.black;

        private GameObject _northWall;
        private GameObject _eastWall;
        private GameObject _southWall;
        private GameObject _westWall;
        private GameObject _neCorner;
        private GameObject _nwCorner;
        private GameObject _seCorner;
        private GameObject _swCorner;

        private void Awake()
        {
            if (_cell == null)
                _cell = GetComponent<Cell>();

            InitializeWalls();
            UpdateVisuals();
        }

        private void OnEnable()
        {
            if (_cell != null)
            {
                _cell.OnVisibilityChanged += UpdateVisuals;
                _cell.OnWallsChanged += UpdateWalls;
                _cell.OnPassabilityChanged += UpdateVisuals;
            }
        }

        private void OnDisable()
        {
            if (_cell != null)
            {
                _cell.OnVisibilityChanged -= UpdateVisuals;
                _cell.OnWallsChanged -= UpdateWalls;
                _cell.OnPassabilityChanged -= UpdateVisuals;
            }
        }

        private void InitializeWalls()
        {
            if (_wallContainer == null)
            {
                _wallContainer = new GameObject("Walls").transform;
                _wallContainer.SetParent(transform);
                _wallContainer.localPosition = Vector3.zero;
            }

            // Create main walls
            if (_wallPrefab != null)
            {
                float wallLength = 1f + _wallThickness; // Extend walls to cover corners
                float cornerSize = _wallThickness;

                // North Wall
                if (_northWall == null)
                {
                    _northWall = CreateWall("North", new Vector3(0, _wallHeight/2, 0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }

                // East Wall
                if (_eastWall == null)
                {
                    _eastWall = CreateWall("East", new Vector3(0.5f, _wallHeight/2, 0), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }

                // South Wall
                if (_southWall == null)
                {
                    _southWall = CreateWall("South", new Vector3(0, _wallHeight/2, -0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }

                // West Wall
                if (_westWall == null)
                {
                    _westWall = CreateWall("West", new Vector3(-0.5f, _wallHeight/2, 0), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }

                // Create corner pieces
                if (_neCorner == null)
                {
                    _neCorner = CreateCorner("NE_Corner", new Vector3(0.5f, _wallHeight/2, 0.5f));
                }
                if (_nwCorner == null)
                {
                    _nwCorner = CreateCorner("NW_Corner", new Vector3(-0.5f, _wallHeight/2, 0.5f));
                }
                if (_seCorner == null)
                {
                    _seCorner = CreateCorner("SE_Corner", new Vector3(0.5f, _wallHeight/2, -0.5f));
                }
                if (_swCorner == null)
                {
                    _swCorner = CreateCorner("SW_Corner", new Vector3(-0.5f, _wallHeight/2, -0.5f));
                }
            }
        }

        private GameObject CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = Instantiate(_wallPrefab, _wallContainer);
            wall.name = name;
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            return wall;
        }

        private GameObject CreateCorner(string name, Vector3 position)
        {
            GameObject corner = Instantiate(_wallPrefab, _wallContainer);
            corner.name = name;
            corner.transform.localPosition = position;
            corner.transform.localScale = new Vector3(_wallThickness, _wallHeight, _wallThickness);
            return corner;
        }

        private void UpdateVisuals()
        {
            if (_floorRenderer == null) return;

            Color floorColor;
            
            if (!_cell.IsExplored)
            {
                floorColor = _hiddenColor;
            }
            else if (!_cell.IsVisible)
            {
                floorColor = _exploredColor;
            }
            else
            {
                floorColor = _cell.IsPassable ? _passableColor : _impassableColor;
            }

            _floorRenderer.material.color = floorColor;
        }

        private void UpdateWalls()
        {
            bool hasNorth = _cell.HasNorthWall;
            bool hasEast = _cell.HasEastWall;
            bool hasSouth = _cell.HasSouthWall;
            bool hasWest = _cell.HasWestWall;

            if (_northWall != null) _northWall.SetActive(hasNorth);
            if (_eastWall != null) _eastWall.SetActive(hasEast);
            if (_southWall != null) _southWall.SetActive(hasSouth);
            if (_westWall != null) _westWall.SetActive(hasWest);

            // Update corners based on adjacent walls
            if (_neCorner != null) _neCorner.SetActive(hasNorth && hasEast);
            if (_nwCorner != null) _nwCorner.SetActive(hasNorth && hasWest);
            if (_seCorner != null) _seCorner.SetActive(hasSouth && hasEast);
            if (_swCorner != null) _swCorner.SetActive(hasSouth && hasWest);
        }
    }
} 