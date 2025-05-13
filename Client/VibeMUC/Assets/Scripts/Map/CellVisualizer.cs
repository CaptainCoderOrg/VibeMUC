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
        [SerializeField] private GameObject _doorPrefab;
        
        [Header("Wall Settings")]
        [SerializeField] private float _wallThickness = 0.1f;
        [SerializeField] private float _wallHeight = 1f;
        [SerializeField] private float _doorWidth = 0.4f;  // Width of the door opening
        // Offset walls slightly to prevent z-fighting
        [SerializeField] private float _wallOffset = 0.05f;  // Increased offset to prevent clipping

        [Header("Colors")]
        [SerializeField] private Color _passableColor = Color.white;
        [SerializeField] private Color _impassableColor = Color.gray;
        [SerializeField] private Color _exploredColor = Color.gray;
        [SerializeField] private Color _hiddenColor = Color.black;

        private GameObject _northWall;
        private GameObject _northWallLeft;
        private GameObject _northWallRight;
        private GameObject _eastWall;
        private GameObject _eastWallLeft;
        private GameObject _eastWallRight;
        private GameObject _southWall;
        private GameObject _southWallLeft;
        private GameObject _southWallRight;
        private GameObject _westWall;
        private GameObject _westWallLeft;
        private GameObject _westWallRight;
        private GameObject _northDoor;
        private GameObject _eastDoor;
        private GameObject _southDoor;
        private GameObject _westDoor;
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
                _cell.OnDoorsChanged += UpdateWallsAndDoors;
                _cell.OnPassabilityChanged += UpdateVisuals;
            }
        }

        private void OnDisable()
        {
            if (_cell != null)
            {
                _cell.OnVisibilityChanged -= UpdateVisuals;
                _cell.OnWallsChanged -= UpdateWalls;
                _cell.OnDoorsChanged -= UpdateWallsAndDoors;
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

            // Create main walls and doors
            if (_wallPrefab != null && _doorPrefab != null)
            {
                float wallLength = (1f - _doorWidth) / 2f; // Length of each wall segment when split
                float fullWallLength = 1f; // Length of a full wall (no door)
                float cornerSize = _wallThickness;

                // North Walls and Door
                if (_northWall == null)
                {
                    _northWall = CreateWall("North", new Vector3(0, _wallHeight/2, 0.5f), 
                        new Vector3(fullWallLength, _wallHeight, _wallThickness));
                }
                if (_northWallLeft == null)
                {
                    _northWallLeft = CreateWall("North_Left", new Vector3(-(_doorWidth + wallLength)/2f, _wallHeight/2, 0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }
                if (_northWallRight == null)
                {
                    _northWallRight = CreateWall("North_Right", new Vector3((_doorWidth + wallLength)/2f, _wallHeight/2, 0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }
                if (_northDoor == null)
                {
                    _northDoor = CreateDoor("North_Door", new Vector3(0, _wallHeight/2, 0.5f - _wallOffset), Quaternion.identity);
                }

                // East Walls and Door
                if (_eastWall == null)
                {
                    _eastWall = CreateWall("East", new Vector3(0.5f, _wallHeight/2, 0), 
                        new Vector3(_wallThickness, _wallHeight, fullWallLength));
                }
                if (_eastWallLeft == null)
                {
                    _eastWallLeft = CreateWall("East_Left", new Vector3(0.5f, _wallHeight/2, -(_doorWidth + wallLength)/2f), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }
                if (_eastWallRight == null)
                {
                    _eastWallRight = CreateWall("East_Right", new Vector3(0.5f, _wallHeight/2, (_doorWidth + wallLength)/2f), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }
                if (_eastDoor == null)
                {
                    _eastDoor = CreateDoor("East_Door", new Vector3(0.5f - _wallOffset, _wallHeight/2, 0), Quaternion.Euler(0, 90, 0));
                }

                // South Walls and Door
                if (_southWall == null)
                {
                    _southWall = CreateWall("South", new Vector3(0, _wallHeight/2, -0.5f), 
                        new Vector3(fullWallLength, _wallHeight, _wallThickness));
                }
                if (_southWallLeft == null)
                {
                    _southWallLeft = CreateWall("South_Left", new Vector3(-(_doorWidth + wallLength)/2f, _wallHeight/2, -0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }
                if (_southWallRight == null)
                {
                    _southWallRight = CreateWall("South_Right", new Vector3((_doorWidth + wallLength)/2f, _wallHeight/2, -0.5f), 
                        new Vector3(wallLength, _wallHeight, _wallThickness));
                }
                if (_southDoor == null)
                {
                    _southDoor = CreateDoor("South_Door", new Vector3(0, _wallHeight/2, -0.5f + _wallOffset), Quaternion.identity);
                }

                // West Walls and Door
                if (_westWall == null)
                {
                    _westWall = CreateWall("West", new Vector3(-0.5f, _wallHeight/2, 0), 
                        new Vector3(_wallThickness, _wallHeight, fullWallLength));
                }
                if (_westWallLeft == null)
                {
                    _westWallLeft = CreateWall("West_Left", new Vector3(-0.5f, _wallHeight/2, -(_doorWidth + wallLength)/2f), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }
                if (_westWallRight == null)
                {
                    _westWallRight = CreateWall("West_Right", new Vector3(-0.5f, _wallHeight/2, (_doorWidth + wallLength)/2f), 
                        new Vector3(_wallThickness, _wallHeight, wallLength));
                }
                if (_westDoor == null)
                {
                    _westDoor = CreateDoor("West_Door", new Vector3(-0.5f + _wallOffset, _wallHeight/2, 0), Quaternion.Euler(0, 90, 0));
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

        private GameObject CreateDoor(string name, Vector3 position, Quaternion rotation)
        {
            GameObject door = Instantiate(_doorPrefab, _wallContainer);
            door.name = name;
            door.transform.localPosition = position;
            door.transform.localRotation = rotation;
            return door;
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

            // If the cell is empty, hide everything
            if (_cell.IsEmpty)
            {
                _floorRenderer.enabled = false;
                if (_wallContainer != null)
                {
                    _wallContainer.gameObject.SetActive(false);
                }
                return;
            }

            // Show the wall container if it was hidden
            if (_wallContainer != null)
            {
                _wallContainer.gameObject.SetActive(true);
            }
            _floorRenderer.enabled = true;

            // Update walls and doors
            UpdateWalls();
            UpdateDoors();

            // Update floor color
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
            if (_cell == null) return;

            bool hasNorth = _cell.HasNorthWall;
            bool hasEast = _cell.HasEastWall;
            bool hasSouth = _cell.HasSouthWall;
            bool hasWest = _cell.HasWestWall;

            bool hasNorthDoor = _cell.HasNorthDoor && hasNorth;
            bool hasEastDoor = _cell.HasEastDoor && hasEast;
            bool hasSouthDoor = _cell.HasSouthDoor && hasSouth;
            bool hasWestDoor = _cell.HasWestDoor && hasWest;

            Debug.Log($"Updating walls for cell at {_cell.GridPosition}");
            Debug.Log($"Wall states - N:{hasNorth} E:{hasEast} S:{hasSouth} W:{hasWest}");
            Debug.Log($"Door states - N:{hasNorthDoor} E:{hasEastDoor} S:{hasSouthDoor} W:{hasWestDoor}");

            // North wall components
            if (_northWall != null) _northWall.SetActive(hasNorth && !hasNorthDoor);
            if (_northWallLeft != null) _northWallLeft.SetActive(hasNorthDoor);
            if (_northWallRight != null) _northWallRight.SetActive(hasNorthDoor);
            if (_northDoor != null) _northDoor.SetActive(hasNorthDoor);

            // East wall components
            if (_eastWall != null) _eastWall.SetActive(hasEast && !hasEastDoor);
            if (_eastWallLeft != null) _eastWallLeft.SetActive(hasEastDoor);
            if (_eastWallRight != null) _eastWallRight.SetActive(hasEastDoor);
            if (_eastDoor != null) _eastDoor.SetActive(hasEastDoor);

            // South wall components
            if (_southWall != null) _southWall.SetActive(hasSouth && !hasSouthDoor);
            if (_southWallLeft != null) _southWallLeft.SetActive(hasSouthDoor);
            if (_southWallRight != null) _southWallRight.SetActive(hasSouthDoor);
            if (_southDoor != null) _southDoor.SetActive(hasSouthDoor);

            // West wall components
            if (_westWall != null) _westWall.SetActive(hasWest && !hasWestDoor);
            if (_westWallLeft != null) _westWallLeft.SetActive(hasWestDoor);
            if (_westWallRight != null) _westWallRight.SetActive(hasWestDoor);
            if (_westDoor != null) _westDoor.SetActive(hasWestDoor);

            // Update corners - only show if both adjacent walls exist and neither has a door
            if (_neCorner != null) _neCorner.SetActive(hasNorth && hasEast && !hasNorthDoor && !hasEastDoor);
            if (_nwCorner != null) _nwCorner.SetActive(hasNorth && hasWest && !hasNorthDoor && !hasWestDoor);
            if (_seCorner != null) _seCorner.SetActive(hasSouth && hasEast && !hasSouthDoor && !hasEastDoor);
            if (_swCorner != null) _swCorner.SetActive(hasSouth && hasWest && !hasSouthDoor && !hasWestDoor);

            Debug.Log($"North wall visibility - Full:{_northWall?.activeSelf} Left:{_northWallLeft?.activeSelf} Right:{_northWallRight?.activeSelf} Door:{_northDoor?.activeSelf}");
        }

        private void UpdateWallsAndDoors()
        {
            // When doors change, we need to update both walls and doors
            UpdateWalls();
        }

        private void UpdateDoors()
        {
            // This method is now empty since door visibility is handled in UpdateWalls
        }
    }
} 