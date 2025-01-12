using System;

namespace GuidPlus
{
    /// <summary>
    /// Generator for version 8 UUIDs (GUIDs).
    /// </summary>
    public static class Guid8
    {
        /// <summary>
        /// Preconfigured generator for version 8 UUIDs (GUIDs).
        /// </summary>
        public class Generator : IGuidGenerator
        {
            private readonly Func<ulong> _timeSource;
            private readonly int _timeSize;
            private readonly byte[] _nodeBuffer;
            private readonly LockClass _sequenceLock = new();
            private ulong _lastClock;
            private int _sequence;

            /// <summary>
            /// Initializes a new generator for version 8 UUIDs using the given timestamp provider.
            /// </summary>
            /// <param name="timeSource">Timestamp provider function.</param>
            /// <param name="timeSize">Size of provided timestamp in bit (up to 60).</param>
            public Generator(Func<ulong> timeSource, int timeSize)
                : this(timeSource, timeSize, null) { }

            /// <summary>
            /// Initializes a new generator for version 8 UUIDs using the given timestamp provider
            /// and node bytes.
            /// </summary>
            /// <param name="timeSource">Timestamp provider function.</param>
            /// <param name="timeSize">Size of provided timestamp in bit (up to 60).</param>
            /// <param name="node">
            /// Node bytes to add to the end of the GUID. Timestamps using up to 48 bits require 7
            /// node bytes, larger timestamps require 8 node bytes. The first two bits of the first
            /// byte will be overwritten.
            /// </param>
            public Generator(Func<ulong> timeSource, int timeSize, byte[] node)
            {
                if (timeSize > 60)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(timeSize),
                        "Timestamps larger than 60 bits are not supported."
                    );
                }

                if (timeSize < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(timeSize),
                        "Timestamps cannot be smaller than 1 bit."
                    );
                }

                _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
                _timeSize = timeSize;

                if (node == null)
                {
                    return;
                }

                _nodeBuffer = node;

                if (timeSize > 48 && node.Length != 7)
                {
                    throw new ArgumentException(
                        "Node length must be 7 bytes for timestamps using up to 48 bits.",
                        nameof(node)
                    );
                }

                if (timeSize <= 48 && node.Length != 8)
                {
                    throw new ArgumentException(
                        "Node length must be 8 bytes for timestamps larger than 48 bits.",
                        nameof(node)
                    );
                }
            }

            /// <summary>
            /// returns the required node size for a given timestamp size.
            /// </summary>
            /// <param name="timeSize">Size of provided timestamp in bit (up to 60).</param>
            private static int GenerateRandomNodeSize(int timeSize) => timeSize > 48 ? 7 : 8;

            /// <summary>
            /// Generates random node bytes for the given timestamp size.
            /// </summary>
            /// <param name="nodeSize">usable size of provide node buffer.</param>
            /// <param name="node">node buffer, must be at least <paramref name="nodeSize"/> bytes long.</param>
            private static void GenerateRandomNode(int nodeSize, byte[] node)
            {
                RandomBytes.GetBytes(node, nodeSize);
            }

#if !NETSTANDARD2_0
            /// <summary>
            /// Generates random node bytes
            /// </summary>
            /// <param name="node">bytes to be randomized.</param>
            private static void GenerateRandomNode(Span<byte> node)
            {
                RandomBytes.GetBytes(node);
            }
#endif

            /// <inheritdoc />
            public Guid NewGuid()
            {
                ulong clock;
                int sequence;
                lock (_sequenceLock)
                {
                    clock = _timeSource();
                    _sequence = clock > _lastClock ? 0 : _sequence + 1;
                    sequence = _sequence;
                    _lastClock = clock;
                }

                clock = clock << 64 - _timeSize;
                var timestamp32 = (int)(clock >> 32);
                var timestamp48 = (short)(clock >> 16);
                var timeOrSeq = _timeSize <= 48
                    ? (short)(sequence & 0x0fff | 0x8000)
                    : (short)(clock >> 4 & 0x0fff | 0x8000);

                if (_nodeBuffer != null)
                {
                    var node = _nodeBuffer.AsSpan();
                    return GuidFromNode(node, timestamp32, timestamp48, timeOrSeq, sequence);
                }

                var nodeSize = GenerateRandomNodeSize(_timeSize);
#if !NETSTANDARD2_0
                Span<byte> usedNode = stackalloc byte[nodeSize];
                GenerateRandomNode(usedNode);
                return GuidFromNode(usedNode, timestamp32, timestamp48, timeOrSeq, sequence);
#else
                using var buffer = ArrayScope.Rent<byte>(nodeSize);
                GenerateRandomNode(nodeSize, buffer.Array);

                return GuidFromNode(buffer.AsSpan(), timestamp32, timestamp48, timeOrSeq, sequence);
#endif
            }

            /// <summary>
            /// create a guid V8 from a node buffer and component values
            /// </summary>
            private Guid GuidFromNode(Span<byte> node, int timestamp32, short timestamp48, short timeOrSeq, int sequence)
            {
                if (_timeSize <= 48)
                {
                    return new Guid(
                        timestamp32,
                        timestamp48,
                        timeOrSeq,
                        (byte)(node[0] & 0x3f | 0x80),
                        node[1],
                        node[2],
                        node[3],
                        node[4],
                        node[5],
                        node[6],
                        node[7]
                    );
                }
                else
                {
                    return new Guid(
                        timestamp32,
                        timestamp48,
                        timeOrSeq,
                        (byte)(sequence >> 2 & 0x3f | 0x80),
                        (byte)(node[0] & 0x3f | sequence << 6),
                        node[1],
                        node[2],
                        node[3],
                        node[4],
                        node[5],
                        node[6]
                    );
                }
            }
        }
    }
}
