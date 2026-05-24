
using Unity.Netcode;
using System;
using Map.Infrastructure;

using TileIdPrimitive = System.Int32;
using EdgeIdPrimitive = System.Int32;
using StructureIndexPrimitive = System.Byte;
using VehicleIndexPrimitive = System.Byte;

using PlayerIdPrimitive = System.Byte;
using ClientIdPrimitive = System.UInt64;

[System.Serializable]
public struct TileId : INetworkSerializeByMemcpy, IEquatable<TileId>, IComparable<TileId>
{
    [UnityEngine.SerializeField]
    private TileIdPrimitive value;
    public TileIdPrimitive Value => value;

    public static readonly TileId NONE = new TileId { value = -1 };

    public static bool operator ==(TileId left, TileId right) => left.value == right.value;
    public static bool operator !=(TileId left, TileId right) => left.value != right.value;

    public override bool Equals(object obj) => obj is TileId id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(TileId other) => this == other;
    public int CompareTo(TileId other) => value.CompareTo(other.value);

    public static implicit operator TileIdPrimitive(TileId value) => value.value;

    public TileId(TileIdPrimitive value) => this.value = value;
}

[System.Serializable]
public struct EdgeId : INetworkSerializeByMemcpy, IEquatable<EdgeId>, IComparable<EdgeId>
{
    [UnityEngine.SerializeField]
    private EdgeIdPrimitive value;
    public EdgeIdPrimitive Value => value;

    public static readonly EdgeId NONE = new EdgeId { value = -1 };

    public static bool operator ==(EdgeId left, EdgeId right) => left.value == right.value;
    public static bool operator !=(EdgeId left, EdgeId right) => left.value != right.value;

    public override bool Equals(object obj) => obj is EdgeId id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(EdgeId other) => this == other;
    public int CompareTo(EdgeId other) => value.CompareTo(other.value);

    public static implicit operator EdgeIdPrimitive(EdgeId value) => value.value;

    public EdgeId(EdgeIdPrimitive value) => this.value = value;
}


[System.Serializable]
public struct StructureIndex : INetworkSerializeByMemcpy, IEquatable<StructureIndex>, IComparable<StructureIndex>
{
    [UnityEngine.SerializeField]
    private StructureIndexPrimitive value;
    public StructureIndexPrimitive Value => value;

    public static readonly StructureIndex NONE = new StructureIndex { value = StructureIndexPrimitive.MaxValue };

    public static bool operator ==(StructureIndex left, StructureIndex right) => left.value == right.value;
    public static bool operator !=(StructureIndex left, StructureIndex right) => left.value != right.value;

    public override bool Equals(object obj) => obj is StructureIndex id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(StructureIndex other) => this == other;
    public int CompareTo(StructureIndex other) => value.CompareTo(other.value);

    public static implicit operator StructureIndexPrimitive(StructureIndex value) => value.value;

    public StructureIndex(StructureIndexPrimitive value) => this.value = value;
}

[System.Serializable]
public struct VehicleIndex : INetworkSerializeByMemcpy, IEquatable<VehicleIndex>, IComparable<VehicleIndex>
{
    [UnityEngine.SerializeField]
    private VehicleIndexPrimitive value;
    public VehicleIndexPrimitive Value => value;

    public static readonly VehicleIndex NONE = new VehicleIndex { value = VehicleIndexPrimitive.MaxValue };

    public static bool operator ==(VehicleIndex left, VehicleIndex right) => left.value == right.value;
    public static bool operator !=(VehicleIndex left, VehicleIndex right) => left.value != right.value;

    public override bool Equals(object obj) => obj is VehicleIndex id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(VehicleIndex other) => this == other;
    public int CompareTo(VehicleIndex other) => value.CompareTo(other.value);

    public static implicit operator VehicleIndexPrimitive(VehicleIndex value) => value.value;

    public VehicleIndex(VehicleIndexPrimitive value) => this.value = value;
}

[System.Serializable]
public struct PlayerId : INetworkSerializeByMemcpy, IEquatable<PlayerId>, IComparable<PlayerId>
{
    [UnityEngine.SerializeField]
    private PlayerIdPrimitive value;
    public PlayerIdPrimitive Value => value;

    public static readonly PlayerId NONE = new PlayerId { value = PlayerIdPrimitive.MaxValue };

    public static bool operator ==(PlayerId left, PlayerId right) => left.value == right.value;
    public static bool operator !=(PlayerId left, PlayerId right) => left.value != right.value;

    public override bool Equals(object obj) => obj is PlayerId id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(PlayerId other) => this == other;
    public int CompareTo(PlayerId other) => value.CompareTo(other.value);

    public static implicit operator PlayerIdPrimitive(PlayerId value) => value.value;

    public PlayerId(PlayerIdPrimitive value) => this.value = value;
}

[System.Serializable]
public struct ClientId : INetworkSerializeByMemcpy, IEquatable<ClientId>, IComparable<ClientId>
{
    [UnityEngine.SerializeField]
    private ClientIdPrimitive value;
    public ClientIdPrimitive Value => value;

    public static readonly ClientId NONE = new ClientId { value = ClientIdPrimitive.MaxValue };

    public static bool operator ==(ClientId left, ClientId right) => left.value == right.value;
    public static bool operator !=(ClientId left, ClientId right) => left.value != right.value;

    public override bool Equals(object obj) => obj is ClientId id && value == id.value;
    public override int GetHashCode() => HashCode.Combine(value);

    public bool Equals(ClientId other) => this == other;
    public int CompareTo(ClientId other) => value.CompareTo(other.value);

    public static implicit operator ClientIdPrimitive(ClientId value) => value.value;

    public ClientId(ClientIdPrimitive value) => this.value = value;
}