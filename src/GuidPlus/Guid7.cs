using System;
using System.Security.Cryptography;

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
#if !NETSTANDARD2_0
            Span<byte> node = stackalloc byte[8];
            var span = node;
#else
            using var nodeScope = ArrayScope.Rent<byte>(8);
            var node = nodeScope.Array;
            var span = nodeScope.AsSpan();
#endif
            RandomBytes.GetBytes(node, 8);

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

#if !NETSTANDARD2_0
            Span<byte> guidBytes = stackalloc byte[16];
#else
            using var guidBytesScope = ArrayScope.Rent<byte>(16);
            var guidBytes = guidBytesScope.Array;
#endif

            guidBytes[0] = (byte)(unixTs >> 4);
            guidBytes[1] = (byte)(unixTs >> 12);
            guidBytes[2] = (byte)(unixTs >> 20);
            guidBytes[3] = (byte)(unixTs >> 28);
            guidBytes[4] = (byte)(msec);
            guidBytes[5] = (byte)((unixTs << 4) | (msec >> 8));
            guidBytes[6] = (byte)(clockSeq);
            guidBytes[7] = (byte)(clockSeq >> 8);
            guidBytes[8] = (byte)(node[0] & 0x3f | 0x80);
            guidBytes[9] = node[1];
            guidBytes[10] = node[2];
            guidBytes[11] = node[3];
            guidBytes[12] = node[4];
            guidBytes[13] = node[5];
            guidBytes[14] = node[6];
            guidBytes[15] = node[7];

            return new Guid(guidBytes);
        }
    }
}
