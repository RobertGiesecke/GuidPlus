using System;

namespace GuidPlus
{
    /// <summary>
    /// Generator for version 6 UUIDs (GUIDs).
    /// </summary>
    public static partial class Guid6
    {
        /// <summary>
        /// Preconfigured generator for version 6 UUIDs (GUIDs).
        /// </summary>
        public class Generator : IGuidGenerator
        {
            private readonly byte[] _node;

            /// <summary>
            /// Initializes a new generator for version 6 UUIDs with the specified node bytes.
            /// </summary>
            /// <param name="node">6 node bytes to add to the end of the GUID.</param>
            /// <param name="randomizeNode">should the contents of <paramref name="node"/> be randomized?</param>
            public Generator(byte[] node, bool randomizeNode = false)
            {
                if (node.Length != 6)
                {
                    throw new ArgumentException("Node length must be 6 bytes.", nameof(node));
                }

                if (randomizeNode)
                {
                    RandomBytes.GetBytes(node, 6);
                }

                _node = node;
            }

            /// <summary>
            /// Generates a UUID.
            /// </summary>
            /// <param name="randomizeNode">should the contents of <see cref="_node"/> be randomized?</param>
            public Guid NewGuid(bool randomizeNode = false)
            {
                return Guid6.NewGuid(_node, randomizeNode);
            }
        }
    }
}
