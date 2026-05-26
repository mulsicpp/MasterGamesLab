
using System;
using Unity.Netcode;
using UnityEngine;

using TimestampPrimitive = System.UInt32;

namespace Map
{
    [Serializable]
    public struct Timestamp : INetworkSerializeByMemcpy, IEquatable<Timestamp>, IComparable<Timestamp>
    {
        [SerializeField]
        public TimestampPrimitive Value;

        public Timestamp(TimestampPrimitive value) => Value = value;

        public Timestamp Next() => new Timestamp(Value + 1);

        public static bool operator ==(Timestamp left, Timestamp right) => left.Value == right.Value;
        public static bool operator !=(Timestamp left, Timestamp right) => left.Value != right.Value;

        public static bool operator <(Timestamp left, Timestamp right) => left.Value < right.Value;
        public static bool operator >(Timestamp left, Timestamp right) => left.Value > right.Value;

        public static bool operator <=(Timestamp left, Timestamp right) => left.Value <= right.Value;
        public static bool operator >=(Timestamp left, Timestamp right) => left.Value >= right.Value;

        public override bool Equals(object obj) => obj is Timestamp stamp && Value == stamp.Value;
        public override int GetHashCode() => HashCode.Combine(Value);

        public bool Equals(Timestamp other) => this == other;
        public int CompareTo(Timestamp other) => Value.CompareTo(other.Value);

        public static explicit operator TimestampPrimitive(Timestamp value) => value.Value;
        public static explicit operator Timestamp(TimestampPrimitive value) => new Timestamp { Value = value };
    }
}