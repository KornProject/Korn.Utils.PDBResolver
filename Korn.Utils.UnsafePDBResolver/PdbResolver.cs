using System.Runtime.InteropServices;
using System.Text;

namespace Korn.Utils.UnsafePDBResolver;
public unsafe class PdbResolver : IDisposable
{
    public PdbResolver(string path) : this(File.ReadAllBytes(path)) { }

    public PdbResolver(byte[] bytes)
    {
        handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        Data = (byte*)handle.AddrOfPinnedObject();
        Length = bytes.Length;
    }

    readonly GCHandle handle;
    public readonly byte* Data;
    public readonly int Length;

    public unsafe FieldSymbol* ResolveField(string fieldName, string declaringType)
        => (FieldSymbol*)(Data + new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes($"?{fieldName}@{declaringType}@")) - 0x06);

    public unsafe MethodSymbol* ResolveMethod(string methodName)
        => (MethodSymbol*)(Data + new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes($"{methodName}\0")) - 0x07);

    bool disposed;
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;

        handle.Free();
    }

    ~PdbResolver() => Dispose();
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FieldSymbol
{
    public uint SegmentOffset;
    public ushort Segment;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MethodSymbol
{
    public uint HeaderOffset;
}