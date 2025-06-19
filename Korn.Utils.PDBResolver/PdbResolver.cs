using Korn.Logger;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Korn.Utils;
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

    public SymbolFullName* Resolve(string name, string declaringType)
    {
        var searchString = $"?{name}@{declaringType}@";
        var index = new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes(searchString));

        if (index == -1)
            throw new KornError(
                "PdbResolver->Resolve:",
                $"Unable resolve full name \"{searchString}\"."
            );

        return (SymbolFullName*)(Data + index - 0x06);
    }

    public SymbolName* Resolve(string name)
    {
        var index = new Span<byte>(Data, Length).IndexOf(Encoding.UTF8.GetBytes(name));

        if (index == -1)
            throw new KornError(
                "PdbResolver->Resolve:",
                $"Unable resolve name \"{name}\"."
            );

        return (SymbolName*)(Data + index - 0x07);
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
public unsafe struct SymbolFullName
{
    public uint SegmentOffset;
    public ushort Segment;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SymbolName
{
    public uint HeaderOffset;
}