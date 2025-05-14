using System;

namespace VibeMUC.Map
{
    /// <summary>
    /// Base class for dungeon generators that provides common functionality.
    /// </summary>
    public abstract class DungeonGeneratorBase : IDungeonGenerator
    {
        protected Random Random { get; private set; }

        public abstract string Name { get; }
        public abstract string Description { get; }
        
        // Default size constraints that can be overridden
        public virtual int MinWidth => 10;
        public virtual int MinHeight => 10;
        public virtual int MaxWidth => 100;
        public virtual int MaxHeight => 100;

        /// <summary>
        /// Generates a new dungeon map with the specified parameters.
        /// </summary>
        public DungeonMapData Generate(int width, int height, int? seed = null)
        {
            ValidateParameters(width, height);
            InitializeRandom(seed);
            return GenerateInternal(width, height);
        }

        /// <summary>
        /// Internal generation method that must be implemented by derived classes.
        /// </summary>
        protected abstract DungeonMapData GenerateInternal(int width, int height);

        /// <summary>
        /// Validates the input parameters for map generation.
        /// </summary>
        protected virtual void ValidateParameters(int width, int height)
        {
            if (width < MinWidth || width > MaxWidth)
                throw new ArgumentOutOfRangeException(nameof(width), 
                    $"Width must be between {MinWidth} and {MaxWidth}");

            if (height < MinHeight || height > MaxHeight)
                throw new ArgumentOutOfRangeException(nameof(height), 
                    $"Height must be between {MinHeight} and {MaxHeight}");
        }

        /// <summary>
        /// Initializes the random number generator with an optional seed.
        /// </summary>
        protected virtual void InitializeRandom(int? seed)
        {
            Random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Creates an empty map with the specified dimensions.
        /// </summary>
        protected DungeonMapData CreateEmptyMap(int width, int height)
        {
            var map = new DungeonMapData
            {
                Width = width,
                Height = height,
                MapName = $"{Name} {width}x{height}",
                Cells = new CellData[width * height]
            };

            // Initialize all cells as empty
            for (int i = 0; i < map.Cells.Length; i++)
            {
                map.Cells[i] = new CellData { IsEmpty = true };
            }

            return map;
        }

        /// <summary>
        /// Gets a cell from the map at the specified coordinates.
        /// Returns null if the coordinates are out of bounds.
        /// </summary>
        protected CellData GetCell(DungeonMapData map, int x, int y)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                return null;

            return map.Cells[y * map.Width + x];
        }

        /// <summary>
        /// Sets a cell in the map at the specified coordinates.
        /// </summary>
        protected void SetCell(DungeonMapData map, int x, int y, CellData cell)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                throw new ArgumentOutOfRangeException("Coordinates out of bounds");

            map.Cells[y * map.Width + x] = cell ?? throw new ArgumentNullException(nameof(cell));
        }
    }
} 