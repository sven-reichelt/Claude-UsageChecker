using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Storage in the Windows Credential Manager. Entries are encrypted by Windows
/// through DPAPI and bound to the user account; the application never writes
/// plaintext to disk.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCredentialStore : ISecretStore
{
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public bool IsSupported => OperatingSystem.IsWindows();

    public string? Read(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!NativeMethods.CredRead(key, CredTypeGeneric, 0, out var handle))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return null;
            }

            // Localised: the message surfaces in the interface when signing out fails.
            throw new InvalidOperationException(T.SecretReadFailed(key, error));
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeMethods.Credential>(handle);
            if (credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                return null;
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            try
            {
                return Encoding.Unicode.GetString(bytes);
            }
            finally
            {
                Array.Clear(bytes);
            }
        }
        finally
        {
            NativeMethods.CredFree(handle);
        }
    }

    public unsafe void Write(string key, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var blob = Encoding.Unicode.GetBytes(secret);
        var blobHandle = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobHandle, blob.Length);

            var credential = new NativeMethods.Credential
            {
                Type = CredTypeGeneric,
                TargetName = key,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobHandle,
                Persist = CredPersistLocalMachine,
                UserName = Environment.UserName
            };

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException(
                    T.SecretWriteFailed(key, Marshal.GetLastWin32Error()));
            }
        }
        finally
        {
            // Overwrite first, then free: the token value must not linger in
            // released memory. ZeroFreeGlobalAllocUnicode would be wrong here -
            // it expects a pointer from SecureStringToGlobalAllocUnicode and
            // works out the length itself.
            NativeMemory.Clear((void*)blobHandle, (nuint)blob.Length);
            Marshal.FreeHGlobal(blobHandle);
            Array.Clear(blob);
        }
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (NativeMethods.CredDelete(key, CredTypeGeneric, 0))
        {
            return;
        }

        var error = Marshal.GetLastWin32Error();
        if (error != ErrorNotFound)
        {
            throw new InvalidOperationException(T.SecretDeleteFailed(key, error));
        }
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct Credential
        {
            public uint Flags;
            public uint Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string? TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string? UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredWrite(ref Credential userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredDelete(string target, int type, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        internal static extern void CredFree(IntPtr cred);
    }
}
