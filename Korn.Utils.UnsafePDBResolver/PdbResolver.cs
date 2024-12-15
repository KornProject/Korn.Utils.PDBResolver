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

    public FieldSymbol* ResolveField(string fieldName, string declaringType) => ResolveField($"{fieldName}@{declaringType}");

    public FieldSymbol* ResolveField(string fieldName)
    {
        var index = new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes($"?{fieldName}@"));

        if (index == -1)
            throw new KornError([
                "PdbResolver->ResolveField:",
                $"Unable resolve field with name \"{fieldName}\"."
            ]);

        return (FieldSymbol*)(Data + index - 0x06);
    }

    public MethodSymbol* ResolveMethod(string methodName)
    {
        var index = new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes($"{methodName}\0"));

        if (index == -1)
            throw new KornError([
                "PdbResolver->ResolveMethod:", 
                $"Unable resolve method with name \"{methodName}\"."
            ]);
        
        return (MethodSymbol*)(Data + index - 0x07);
    }

    public static string GetDebugSymbolsPathForExecutable(string executablePath)
        => Path.Combine(Path.GetDirectoryName(executablePath)!, $"{Path.GetFileNameWithoutExtension(executablePath)}.pdb");

    public static bool IsExecutableContainsDebugSymbols(string executablePath) 
        => File.Exists(GetDebugSymbolsPathForExecutable(executablePath));

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