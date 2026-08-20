using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Storage in the macOS keychain, the counterpart to
/// <see cref="WindowsCredentialStore"/>. Entries are encrypted by the system and
/// bound to the login keychain of the user; the application never writes
/// plaintext to disk.
/// </summary>
/// <remarks>
/// <para>
/// Through the Security framework rather than the <c>security</c> command line
/// tool. Reading would work either way - the value comes back on standard
/// output, which nobody else can see. Writing would not: the password is passed
/// to that tool as an argument, and arguments of a running process are readable
/// by every account on the machine. A token must not stand in the process list,
/// however briefly.
/// </para>
/// <para>
/// The constants of the framework (<c>kSecClass</c> and its like) are exported
/// as pointers to CFString objects, not as functions. They are therefore looked
/// up by symbol and dereferenced once, when this class is first used.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed class MacOsKeychainStore : ISecretStore
{
    /// <summary>
    /// The account the entries are filed under. The key becomes the service, so
    /// that one glance at Keychain Access shows what belongs to whom.
    /// </summary>
    private const string Account = "ClaudeUsageChecker";

    private const int ErrSecSuccess = 0;
    private const int ErrSecItemNotFound = -25300;

    public bool IsSupported => OperatingSystem.IsMacOS();

    public string? Read(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var query = Native.Query(key, withData: true);

        try
        {
            var status = Native.SecItemCopyMatching(query, out var result);
            if (status == ErrSecItemNotFound)
            {
                return null;
            }

            if (status != ErrSecSuccess)
            {
                throw new InvalidOperationException(T.SecretReadFailed(key, status));
            }

            if (result == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Native.ReadUtf8(result);
            }
            finally
            {
                Native.CFRelease(result);
            }
        }
        finally
        {
            Native.CFRelease(query);
        }
    }

    public void Write(string key, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var bytes = Encoding.UTF8.GetBytes(secret);
        var query = Native.Query(key, withData: false);
        var data = Native.CreateData(bytes);

        try
        {
            // Update first, add second. The other way round means asking for a
            // duplicate and treating the refusal as the normal case.
            var attributes = Native.Dictionary([Native.SecValueData], [data]);
            int status;

            try
            {
                status = Native.SecItemUpdate(query, attributes);
            }
            finally
            {
                Native.CFRelease(attributes);
            }

            if (status == ErrSecItemNotFound)
            {
                var complete = Native.Query(key, withData: false, value: data);
                try
                {
                    status = Native.SecItemAdd(complete, out _);
                }
                finally
                {
                    Native.CFRelease(complete);
                }
            }

            if (status != ErrSecSuccess)
            {
                throw new InvalidOperationException(T.SecretWriteFailed(key, status));
            }
        }
        finally
        {
            Native.CFRelease(data);
            Native.CFRelease(query);
            Array.Clear(bytes);
        }
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var query = Native.Query(key, withData: false);

        try
        {
            var status = Native.SecItemDelete(query);

            // A missing entry is not an error - the caller wanted it gone.
            if (status is not (ErrSecSuccess or ErrSecItemNotFound))
            {
                throw new InvalidOperationException(T.SecretDeleteFailed(key, status));
            }
        }
        finally
        {
            Native.CFRelease(query);
        }
    }

    [SupportedOSPlatform("macos")]
    private static class Native
    {
        private const string SecurityFramework =
            "/System/Library/Frameworks/Security.framework/Security";

        private const string CoreFoundation =
            "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        private const uint Utf8 = 0x08000100;

        // Looked up once. Every one of these is a pointer to a CFString living
        // in the framework, so the symbol has to be dereferenced rather than
        // called.
        private static readonly IntPtr SecurityHandle = NativeLibrary.Load(SecurityFramework);
        private static readonly IntPtr CoreFoundationHandle = NativeLibrary.Load(CoreFoundation);

        internal static readonly IntPtr SecClass = Constant(SecurityHandle, "kSecClass");
        internal static readonly IntPtr SecClassGenericPassword =
            Constant(SecurityHandle, "kSecClassGenericPassword");
        internal static readonly IntPtr SecAttrService = Constant(SecurityHandle, "kSecAttrService");
        internal static readonly IntPtr SecAttrAccount = Constant(SecurityHandle, "kSecAttrAccount");
        internal static readonly IntPtr SecValueData = Constant(SecurityHandle, "kSecValueData");
        internal static readonly IntPtr SecReturnData = Constant(SecurityHandle, "kSecReturnData");
        internal static readonly IntPtr SecMatchLimit = Constant(SecurityHandle, "kSecMatchLimit");
        internal static readonly IntPtr SecMatchLimitOne = Constant(SecurityHandle, "kSecMatchLimitOne");

        private static readonly IntPtr KeyCallBacks =
            NativeLibrary.GetExport(CoreFoundationHandle, "kCFTypeDictionaryKeyCallBacks");
        private static readonly IntPtr ValueCallBacks =
            NativeLibrary.GetExport(CoreFoundationHandle, "kCFTypeDictionaryValueCallBacks");
        private static readonly IntPtr True = Constant(CoreFoundationHandle, "kCFBooleanTrue");

        private static IntPtr Constant(IntPtr library, string symbol) =>
            Marshal.ReadIntPtr(NativeLibrary.GetExport(library, symbol));

        /// <summary>
        /// The dictionary that names one entry: this class, this service, this
        /// account - optionally asking for the value, or carrying it.
        /// </summary>
        internal static IntPtr Query(string key, bool withData, IntPtr value = default)
        {
            var service = CreateString(key);
            var account = CreateString(Account);

            try
            {
                List<IntPtr> keys = [SecClass, SecAttrService, SecAttrAccount];
                List<IntPtr> values = [SecClassGenericPassword, service, account];

                if (withData)
                {
                    keys.Add(SecReturnData);
                    values.Add(True);
                    keys.Add(SecMatchLimit);
                    values.Add(SecMatchLimitOne);
                }

                if (value != IntPtr.Zero)
                {
                    keys.Add(SecValueData);
                    values.Add(value);
                }

                return Dictionary([.. keys], [.. values]);
            }
            finally
            {
                // The dictionary retains what it holds, so the two strings can
                // go as soon as it exists.
                CFRelease(service);
                CFRelease(account);
            }
        }

        internal static IntPtr Dictionary(IntPtr[] keys, IntPtr[] values) =>
            CFDictionaryCreate(IntPtr.Zero, keys, values, keys.Length, KeyCallBacks, ValueCallBacks);

        internal static IntPtr CreateData(byte[] bytes) =>
            CFDataCreate(IntPtr.Zero, bytes, bytes.Length);

        private static IntPtr CreateString(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text + '\0');
            return CFStringCreateWithCString(IntPtr.Zero, bytes, Utf8);
        }

        /// <summary>Copies the bytes out of a CFData and reads them as UTF-8.</summary>
        internal static string? ReadUtf8(IntPtr data)
        {
            var length = (int)CFDataGetLength(data);
            if (length <= 0)
            {
                return null;
            }

            var pointer = CFDataGetBytePtr(data);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);

            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                Array.Clear(bytes);
            }
        }

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, byte[] cStr, uint encoding);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFDataCreate(IntPtr allocator, byte[] bytes, nint length);

        [DllImport(CoreFoundation)]
        private static extern nint CFDataGetLength(IntPtr data);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFDataGetBytePtr(IntPtr data);

        [DllImport(CoreFoundation)]
        private static extern IntPtr CFDictionaryCreate(
            IntPtr allocator, IntPtr[] keys, IntPtr[] values, nint count,
            IntPtr keyCallBacks, IntPtr valueCallBacks);

        [DllImport(CoreFoundation)]
        internal static extern void CFRelease(IntPtr reference);

        [DllImport(SecurityFramework)]
        internal static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

        [DllImport(SecurityFramework)]
        internal static extern int SecItemAdd(IntPtr attributes, out IntPtr result);

        [DllImport(SecurityFramework)]
        internal static extern int SecItemUpdate(IntPtr query, IntPtr attributesToUpdate);

        [DllImport(SecurityFramework)]
        internal static extern int SecItemDelete(IntPtr query);
    }
}
