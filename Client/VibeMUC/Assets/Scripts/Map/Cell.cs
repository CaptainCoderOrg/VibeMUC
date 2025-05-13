using UnityEngine;
using System;

namespace VibeMUC.Map
{
    /// <summary>
    /// Represents a single cell in the dungeon grid.
    /// </summary>
    public class Cell : MonoBehaviour
    {
        // Events for property changes
        public event Action OnVisibilityChanged;
        public event Action OnWallsChanged;
        public event Action OnPassabilityChanged;
        public event Action OnDoorsChanged;

        [SerializeField]
        private bool _isPassable = false;

        [SerializeField]
        private bool _isExplored = false;

        [SerializeField]
        private bool _isVisible = false;

        [SerializeField]
        private bool _isEmpty = true;

        // Walls are stored as flags for each direction
        [SerializeField]
        private bool _hasNorthWall = false;

        [SerializeField]
        private bool _hasEastWall = false;

        [SerializeField]
        private bool _hasSouthWall = false;

        [SerializeField]
        private bool _hasWestWall = false;

        // Doors are stored as flags for each direction
        [SerializeField]
        private bool _hasNorthDoor = false;

        [SerializeField]
        private bool _hasEastDoor = false;

        [SerializeField]
        private bool _hasSouthDoor = false;

        [SerializeField]
        private bool _hasWestDoor = false;

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (_isEmpty != value)
                {
                    _isEmpty = value;
                    // Empty cells are never passable
                    if (value)
                    {
                        IsPassable = false;
                    }
                    OnVisibilityChanged?.Invoke();
                }
            }
        }

        public bool IsPassable
        {
            get => !_isEmpty && _isPassable;
            set
            {
                if (_isPassable != value)
                {
                    _isPassable = value;
                    OnPassabilityChanged?.Invoke();
                }
            }
        }

        public bool IsExplored
        {
            get => _isExplored;
            set => _isExplored = value;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnVisibilityChanged?.Invoke();
                }
            }
        }

        public Vector2Int GridPosition { get; set; }

        // Wall properties
        public bool HasNorthWall
        {
            get => _hasNorthWall;
            set
            {
                if (_hasNorthWall != value)
                {
                    _hasNorthWall = value;
                    Debug.Log($"Cell {GridPosition}: North wall {(value ? "added" : "removed")}");
                    OnWallsChanged?.Invoke();
                }
            }
        }

        public bool HasEastWall
        {
            get => _hasEastWall;
            set
            {
                if (_hasEastWall != value)
                {
                    _hasEastWall = value;
                    Debug.Log($"Cell {GridPosition}: East wall {(value ? "added" : "removed")}");
                    OnWallsChanged?.Invoke();
                }
            }
        }

        public bool HasSouthWall
        {
            get => _hasSouthWall;
            set
            {
                if (_hasSouthWall != value)
                {
                    _hasSouthWall = value;
                    Debug.Log($"Cell {GridPosition}: South wall {(value ? "added" : "removed")}");
                    OnWallsChanged?.Invoke();
                }
            }
        }

        public bool HasWestWall
        {
            get => _hasWestWall;
            set
            {
                if (_hasWestWall != value)
                {
                    _hasWestWall = value;
                    Debug.Log($"Cell {GridPosition}: West wall {(value ? "added" : "removed")}");
                    OnWallsChanged?.Invoke();
                }
            }
        }

        // Door properties
        public bool HasNorthDoor
        {
            get => _hasNorthDoor;
            set
            {
                if (_hasNorthDoor != value)
                {
                    _hasNorthDoor = value;
                    Debug.Log($"Cell {GridPosition}: North door {(value ? "added" : "removed")}");
                    OnDoorsChanged?.Invoke();
                }
            }
        }

        public bool HasEastDoor
        {
            get => _hasEastDoor;
            set
            {
                if (_hasEastDoor != value)
                {
                    _hasEastDoor = value;
                    Debug.Log($"Cell {GridPosition}: East door {(value ? "added" : "removed")}");
                    OnDoorsChanged?.Invoke();
                }
            }
        }

        public bool HasSouthDoor
        {
            get => _hasSouthDoor;
            set
            {
                if (_hasSouthDoor != value)
                {
                    _hasSouthDoor = value;
                    Debug.Log($"Cell {GridPosition}: South door {(value ? "added" : "removed")}");
                    OnDoorsChanged?.Invoke();
                }
            }
        }

        public bool HasWestDoor
        {
            get => _hasWestDoor;
            set
            {
                if (_hasWestDoor != value)
                {
                    _hasWestDoor = value;
                    Debug.Log($"Cell {GridPosition}: West door {(value ? "added" : "removed")}");
                    OnDoorsChanged?.Invoke();
                }
            }
        }

        // Helper method to set all walls at once
        public void SetWalls(bool north, bool east, bool south, bool west)
        {
            bool changed = _hasNorthWall != north || _hasEastWall != east || 
                         _hasSouthWall != south || _hasWestWall != west;

            _hasNorthWall = north;
            _hasEastWall = east;
            _hasSouthWall = south;
            _hasWestWall = west;

            if (changed)
            {
                Debug.Log($"Cell {GridPosition}: Walls updated - N:{north} E:{east} S:{south} W:{west}");
                OnWallsChanged?.Invoke();
            }
        }

        // Helper method to set all doors at once
        public void SetDoors(bool north, bool east, bool south, bool west)
        {
            bool changed = _hasNorthDoor != north || _hasEastDoor != east || 
                         _hasSouthDoor != south || _hasWestDoor != west;

            _hasNorthDoor = north;
            _hasEastDoor = east;
            _hasSouthDoor = south;
            _hasWestDoor = west;

            if (changed)
            {
                Debug.Log($"Cell {GridPosition}: Doors updated - N:{north} E:{east} S:{south} W:{west}");
                OnDoorsChanged?.Invoke();
            }
        }

        // Check if there's a wall in a specific direction
        public bool HasWallInDirection(Direction direction) => direction switch
        {
            Direction.North => HasNorthWall,
            Direction.East => HasEastWall,
            Direction.South => HasSouthWall,
            Direction.West => HasWestWall,
            _ => false
        };

        // Check if there's a door in a specific direction
        public bool HasDoorInDirection(Direction direction) => direction switch
        {
            Direction.North => HasNorthDoor,
            Direction.East => HasEastDoor,
            Direction.South => HasSouthDoor,
            Direction.West => HasWestDoor,
            _ => false
        };

        private void UpdateVisuals()
        {
            // TODO: Update the visual representation of the cell and its walls
            // This will be implemented when we add sprites/models
        }
    }

    public enum Direction
    {
        North,
        East,
        South,
        West
    }
} 