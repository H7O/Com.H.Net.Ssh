#if NETSTANDARD2_0
using System.Threading;
using System.Threading.Tasks;

// Each shim below is declared in the same namespace as the BCL type it extends, so that every
// file which already imports that namespace picks it up without an extra using. They are
// internal, so they never leak into Com.H.Net.Ssh's public surface or collide with a consumer's
// own polyfills, and the whole file compiles out on net8.0+ where the BCL provides the real
// implementations.

namespace System.IO
{
    /// <summary>
    /// netstandard2.0 shims for <see cref="Stream"/> members that only exist on the modern
    /// target frameworks.
    /// </summary>
    internal static class StreamPolyfills
    {
        /// <summary>
        /// Approximates <c>Stream.DisposeAsync()</c> (available in .NET Core 3.0+). Falls back to
        /// the synchronous <see cref="Stream.Dispose()"/>, which still flushes; the only thing lost
        /// is the asynchronous flush.
        /// </summary>
        /// <remarks>
        /// This covers explicit <c>await stream.DisposeAsync()</c> calls only. It cannot satisfy an
        /// <c>await using</c> statement: the C# compiler does not consider extension methods when
        /// binding the pattern-based <c>DisposeAsync</c> of <c>await using</c> (CS8417), so those
        /// call sites use <c>#if NETSTANDARD2_0</c> and a plain <c>using</c> instead.
        /// </remarks>
        public static ValueTask DisposeAsync(this Stream stream)
        {
            stream?.Dispose();
            return default;
        }
    }

    /// <summary>
    /// netstandard2.0 shims for the asynchronous <see cref="File"/> members that only exist on the
    /// modern target frameworks. <see cref="File"/> is a static class and so cannot be extended;
    /// call sites use <c>FilePolyfills.ReadAllTextAsync</c> under <c>#if NETSTANDARD2_0</c>.
    /// </summary>
    internal static class FilePolyfills
    {
        // Matches the buffer size StreamReader uses for File.ReadAllText* on the modern targets.
        private const int DefaultBufferSize = 4096;

        /// <summary>
        /// Approximates <c>File.ReadAllTextAsync(string, CancellationToken)</c> (available in
        /// .NET Core 2.1+), detecting the encoding from the byte order mark as that overload does.
        /// </summary>
        public static Task<string> ReadAllTextAsync(
            string path, CancellationToken cancellationToken = default)
            => ReadAllTextAsync(path, Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, cancellationToken);

        /// <summary>
        /// Approximates <c>File.ReadAllTextAsync(string, Encoding, CancellationToken)</c>
        /// (available in .NET Core 2.1+).
        /// </summary>
        public static Task<string> ReadAllTextAsync(
            string path, Text.Encoding encoding, CancellationToken cancellationToken = default)
            => ReadAllTextAsync(path, encoding, detectEncodingFromByteOrderMarks: true, cancellationToken);

        private static async Task<string> ReadAllTextAsync(
            string path,
            Text.Encoding encoding,
            bool detectEncodingFromByteOrderMarks,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read,
                DefaultBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var reader = new StreamReader(
                stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, leaveOpen: true))
            {
                // StreamReader has no cancellable ReadToEndAsync on netstandard2.0, so the token is
                // observed between chunks instead.
                var builder = new Text.StringBuilder();
                var buffer = new char[DefaultBufferSize];
                int read;
                while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    builder.Append(buffer, 0, read);
                }
                return builder.ToString();
            }
        }
    }
}
#endif
