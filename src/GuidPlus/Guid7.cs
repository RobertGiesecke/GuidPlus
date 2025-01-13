using System;

namespace GuidPlus
{
    /// <summary>
    /// Generator for version 7 UUIDs (GUIDs).
    /// </summary>
    public static partial class Guid7
    {
        internal static Func<DateTime> _getTime = () => DateTime.UtcNow;
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly LockClass _sequenceLock = new();
        private static DateTime _lastClock;
        private static int _sequence;

        const int RequiredNodeSize = 8;

        /// <summary>
        /// Generates a version 7 UUID.
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
        /// Generates a version 7 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">
        /// 8 node bytes to add to the end of the GUID.
        /// The first two bits of the first byte will by overwritten with <c>0b10</c>.
        /// </param>
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
        /// Generates a version 7 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">
        /// 8 node bytes to add to the end of the GUID.
        /// The first two bits of the first byte will by overwritten with <c>0b10</c>.
        /// </param>
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
                // Round to milliseconds
                clock = clock.AddTicks(-clock.Ticks % TimeSpan.TicksPerMillisecond);
                _sequence = clock > _lastClock ? 0 : _sequence + 1;
                sequence = _sequence;
                _lastClock = clock;
            }

            var unixDiff = clock - _unixEpoch;
            var unixTs = (ulong)unixDiff.TotalSeconds;
            var msec = (uint)unixDiff.Milliseconds;
            var clockSeq = sequence & 0x3fff | 0x7000;

            var a = (uint)(unixTs >> 4);

            var b = (ushort)(msec | ((unixTs & 0xF) << 12));
            var c = (ushort) clockSeq;

            if (randomizeNode)
            {
                RandomBytes.GetBytes(node);
            }

            return new Guid(a, b, c,
                d: (byte)(node[0] & 0x3f | 0x80),
                e: node[1],
                f: node[2],
                g: node[3],
                h: node[4],
                i: node[5],
                j: node[6],
                k: node[7]);
        }
    }
}
