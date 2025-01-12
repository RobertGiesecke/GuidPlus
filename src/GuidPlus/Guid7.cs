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

        /// <summary>
        /// Generates a version 7 UUID.
        /// The node bytes are filled with with a cryptographically strong random sequence of bytes.
        /// </summary>
        public static Guid NewGuid()
        {
            const int requiredNodeSize = 8;
#if !NETSTANDARD2_0
            Span<byte> node = stackalloc byte[requiredNodeSize];
            var span = node;
#else
            using var nodeScope = ArrayScope.Rent<byte>(requiredNodeSize);
            var node = nodeScope.Array;
            var span = nodeScope.AsSpan();
#endif
            RandomBytes.GetBytes(node, requiredNodeSize);

            return NewGuid(span);
        }

        /// <summary>
        /// Generates a version 7 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">
        /// 8 node bytes to add to the end of the GUID.
        /// The first two bits of the first byte will by overwritten with <c>0b10</c>.
        /// </param>
        public static Guid NewGuid(byte[] node) => NewGuid(node.AsSpan());

        /// <summary>
        /// Generates a version 7 UUID with the specified node bytes.
        /// </summary>
        /// <param name="node">
        /// 8 node bytes to add to the end of the GUID.
        /// The first two bits of the first byte will by overwritten with <c>0b10</c>.
        /// </param>
        public static Guid NewGuid(Span<byte> node)
        {
            if (node.Length != 8)
            {
                throw new ArgumentException("Node length must be 8 bytes.", nameof(node));
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
