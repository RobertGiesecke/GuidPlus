using System;

namespace GuidPlus
{
    /// <summary>
    /// Generator for version 6 UUIDs (GUIDs).
    /// </summary>
    public static partial class Guid6
    {
        internal static Func<DateTime> _getTime = () => DateTime.UtcNow;
        private static readonly DateTime _gregorianEpoch = new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        private static readonly LockClass _sequenceLock = new();
        private static DateTime _lastClock;
        private static int _sequence;

        const int RequiredNodeSize = 6;

        /// <summary>
        /// Generates a version 6 UUID.
        /// The node bytes are filled with with a cryptographically strong random sequence of bytes.
        /// </summary>
        public static Guid NewGuid()
        {
#if !NETSTANDARD2_0
            Span<byte> node = stackalloc byte[RequiredNodeSize];
            var span = node;
#else
            using var nodeScope = ArrayScope.Rent<byte>(RequiredNodeSize);
            var node = nodeScope.Array;
            var span = nodeScope.AsSpan();
#endif
            RandomBytes.GetBytes(node, RequiredNodeSize);

            return NewGuid(span);
        }

        /// <summary>
        /// Generates a version 6 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">6 node bytes to add to the end of the GUID.</param>
        /// <param name="randomizeNode">should the contents of <paramref name="node"/> be randomized?</param>
        public static Guid NewGuid(byte[] node, bool randomizeNode = false)
        {
            if (randomizeNode)
            {
                RandomBytes.GetBytes(node, RequiredNodeSize);
            }

            return NewGuid(node.AsSpan(), randomizeNode: false);
        }

        /// <summary>
        /// Generates a version 6 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">6 node bytes to add to the end of the GUID.</param>
        /// <param name="randomizeNode">should the contents of <paramref name="node"/> be randomized?</param>
        public static Guid NewGuid(Span<byte> node, bool randomizeNode = false)
        {
            if (node.Length != RequiredNodeSize)
            {
                throw new ArgumentException($"Node length must be {RequiredNodeSize} bytes.", nameof(node));
            }

            DateTime clock;
            int sequence;
            lock (_sequenceLock)
            {
                clock = _getTime();
                _sequence = clock > _lastClock ? 0 : _sequence + 1;
                sequence = _sequence;
                _lastClock = clock;
            }

            var timestamp = (clock - _gregorianEpoch).Ticks;
            var timeHigh = (int)(timestamp >> 28);
            var timeMid = (short)(timestamp >> 12);
            var timeLow = (short)(timestamp & 0x0fff | 0x6000);
            var clockSeq = sequence & 0x3fff | 0x8000;

            if (randomizeNode)
            {
                RandomBytes.GetBytes(node);
            }

            return new Guid(
                timeHigh,
                timeMid,
                timeLow,
                (byte)(clockSeq >> 8),
                (byte)clockSeq,
                node[0],
                node[1],
                node[2],
                node[3],
                node[4],
                node[5]
            );
        }
    }
}
