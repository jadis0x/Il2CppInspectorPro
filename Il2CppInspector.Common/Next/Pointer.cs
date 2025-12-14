#nullable enable
using System.Collections.Immutable;
using VersionedSerialization;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next;

public struct Pointer<T>(ulong value = 0) : IReadable, IEquatable<Pointer<T>> where T : struct, IReadable
{
    [NativeInteger]
    private ulong _value = value;

    public readonly ulong PointerValue => _value;
    public readonly bool Null => _value == 0;

    public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct
    {
        _value = reader.ReadNUInt();
    }

    public static int Size(in StructVersion version = default, bool is32Bit = false)
    {
        return is32Bit ? 4 : 8;
    }

    public readonly T Read(ref SpanReader reader, in StructVersion version)
    {
        reader.Offset = (int)PointerValue;
        return reader.ReadVersionedObject<T>(version);
    }

    public readonly ImmutableArray<T> ReadArray(ref SpanReader reader, long count, in StructVersion version)
    {
        reader.Offset = (int)PointerValue;
        return reader.ReadVersionedObjectArray<T>(count, version);
    }

    public static implicit operator Pointer<T>(ulong value) => new(value);
    public static implicit operator ulong(Pointer<T> ptr) => ptr.PointerValue;

    #region Equality operators + ToString

    public static bool operator ==(Pointer<T> left, Pointer<T> right)
        => left._value == right._value;

    public static bool operator !=(Pointer<T> left, Pointer<T> right)
        => !(left == right);

    public readonly override bool Equals(object? obj)
        => obj is Pointer<T> other && Equals(other);

    public readonly bool Equals(Pointer<T> other)
        => this == other;

    public readonly override int GetHashCode()
        => HashCode.Combine(_value);

    public readonly override string ToString() => $"0x{_value:X} <{typeof(T).Name}>";

    #endregion
}