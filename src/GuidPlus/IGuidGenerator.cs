using System;

namespace GuidPlus
{
    /// <summary>
    /// Generator for UUIDs (GUIDs).
    /// </summary>
    public interface IGuidGenerator
    {
        /// <summary>
        /// Generates a UUID.
        /// </summary>
        /// <param name="randomizeNode">randomize the potentially cached random components?</param>
        Guid NewGuid(bool randomizeNode = false);
    }
}
