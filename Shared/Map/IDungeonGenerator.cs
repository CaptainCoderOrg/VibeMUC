using System;

namespace VibeMUC.Map
{
    /// <summary>
    /// Interface for dungeon generation strategies.
    /// </summary>
    public interface IDungeonGenerator
    {
        /// <summary>
        /// Generates a new dungeon map with the specified parameters.
        /// </summary>
        /// <param name="width">The width of the map</param>
        /// <param name="height">The height of the map</param>
        /// <param name="seed">Random seed for generation (optional)</param>
        /// <returns>The generated dungeon map</returns>
        DungeonMapData Generate(int width, int height, int? seed = null);

        /// <summary>
        /// Gets the name of this generator strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a description of how this generator works.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the minimum supported map width.
        /// </summary>
        int MinWidth { get; }

        /// <summary>
        /// Gets the minimum supported map height.
        /// </summary>
        int MinHeight { get; }

        /// <summary>
        /// Gets the maximum supported map width.
        /// </summary>
        int MaxWidth { get; }

        /// <summary>
        /// Gets the maximum supported map height.
        /// </summary>
        int MaxHeight { get; }
    }
} 