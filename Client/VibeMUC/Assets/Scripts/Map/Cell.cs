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

        [SerializeField]
        private bool _isPassable = true;

        [SerializeField]
        private bool _isExplored = false;

        [SerializeField]
        private bool _isVisible = false;

        // Walls are stored as flags for each direction
        [SerializeField]
        private bool _hasNorthWall = false;

        [SerializeField]
        private bool _hasEastWall = false;

        [SerializeField]
        private bool _hasSouthWall = false;

        [SerializeField]
        private bool _hasWestWall = false;

        public bool IsPassable
        {
            get => _isPassable;
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

        // Check if there's a wall in a specific direction
        public bool HasWallInDirection(Direction direction) => direction switch
        {
            Direction.North => HasNorthWall,
            Direction.East => HasEastWall,
            Direction.South => HasSouthWall,
            Direction.West => HasWestWall,
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