using UnityEngine;

namespace VibeMUC.Map
{
    /// <summary>
    /// Manages the grid-based dungeon layout.
    /// </summary>
    public class DungeonGrid : MonoBehaviour
    {
        [SerializeField]
        private Vector2Int _gridSize = new(10, 10);

        [SerializeField]
        private GameObject _cellPrefab;

        private Cell[,] _grid;

        private void Awake()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            // Clear existing cells if any
            if (_grid != null)
            {
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
            }

            _grid = new Cell[_gridSize.x, _gridSize.y];

            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    Vector3 worldPos = new(x, 0, y);
                    GameObject cellObj = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
                    cellObj.name = $"Cell_{x}_{y}";

                    Cell cell = cellObj.GetComponent<Cell>();
                    cell.GridPosition = new Vector2Int(x, y);
                    _grid[x, y] = cell;

                    // Initialize with no walls
                    cell.SetWalls(false, false, false, false);
                }
            }
        }

        public Cell GetCell(Vector2Int position)
        {
            if (IsValidPosition(position))
            {
                return _grid[position.x, position.y];
            }
            return null;
        }

        public Cell GetCell(int x, int y)
        {
            return GetCell(new Vector2Int(x, y));
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < _gridSize.x &&
                   position.y >= 0 && position.y < _gridSize.y;
        }

        public bool CanMoveTo(Vector2Int position)
        {
            Cell cell = GetCell(position);
            return cell != null && cell.IsPassable;
        }

        // Add or remove a wall between two adjacent cells
        public void SetWallBetweenCells(Vector2Int pos1, Vector2Int pos2, bool hasWall)
        {
            Cell cell1 = GetCell(pos1);
            Cell cell2 = GetCell(pos2);

            if (cell1 == null || cell2 == null)
                return;

            // Determine the relative position of cell2 to cell1
            Vector2Int diff = pos2 - pos1;
            
            if (diff.x == 1 && diff.y == 0) // cell2 is east of cell1
            {
                cell1.HasEastWall = hasWall;
                cell2.HasWestWall = hasWall;
            }
            else if (diff.x == -1 && diff.y == 0) // cell2 is west of cell1
            {
                cell1.HasWestWall = hasWall;
                cell2.HasEastWall = hasWall;
            }
            else if (diff.x == 0 && diff.y == 1) // cell2 is north of cell1
            {
                cell1.HasNorthWall = hasWall;
                cell2.HasSouthWall = hasWall;
            }
            else if (diff.x == 0 && diff.y == -1) // cell2 is south of cell1
            {
                cell1.HasSouthWall = hasWall;
                cell2.HasNorthWall = hasWall;
            }
        }

        // Check if there's a wall between two adjacent cells
        public bool HasWallBetween(Vector2Int pos1, Vector2Int pos2)
        {
            Cell cell1 = GetCell(pos1);
            Cell cell2 = GetCell(pos2);

            if (cell1 == null || cell2 == null)
                return true; // Treat invalid cells as having walls

            Vector2Int diff = pos2 - pos1;
            
            return diff switch
            {
                { x: 1, y: 0 } => cell1.HasEastWall || cell2.HasWestWall,
                { x: -1, y: 0 } => cell1.HasWestWall || cell2.HasEastWall,
                { x: 0, y: 1 } => cell1.HasNorthWall || cell2.HasSouthWall,
                { x: 0, y: -1 } => cell1.HasSouthWall || cell2.HasNorthWall,
                _ => true // Non-adjacent cells are treated as having walls
            };
        }

        public Vector2Int GridSize => _gridSize;
    }
} 