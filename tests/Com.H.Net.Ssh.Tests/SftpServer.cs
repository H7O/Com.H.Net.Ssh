using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Com.H.Net.Ssh.Tests;

/// <summary>
/// A real SFTP server for the tests: portable-sftp (github.com/H7O/portable-sftp), serving a scratch
/// folder on this machine on a free local port. So a test can also look at the files the server holds.
/// It runs with -verbose, which logs every request, and a test can count what an operation cost.
/// <para>
/// The program is found through the COM_H_TEST_SFTP_SERVER environment variable (its full path), or
/// on the PATH. Without it, the tests report themselves as skipped.
/// </para>
/// </summary>
public sealed class SftpServer : IDisposable
{
    public const string User = "sftp";
    public const string Password = "com-h-test-password";

    private readonly Process? _process;
    private readonly string? _scratch;
    private readonly ConcurrentQueue<string> _output = new();

    /// <summary>Why the server can't be used, or null when it is running.</summary>
    public string? Unavailable { get; }

    public string ServedFolder { get; } = "";
    public int Port { get; }

    /// <summary>The server's host key, as it printed it when it started: "SHA256:...".</summary>
    public string HostKeyFingerprint { get; } = "";

    public SftpServer() : this(null)
    {
    }

    /// <param name="authorizedKeys">An authorized_keys file. With it, the server accepts those keys and no password.</param>
    internal SftpServer(string? authorizedKeys)
    {
        var program = Find();
        if (program is null)
        {
            Unavailable = "No SFTP test server. Set COM_H_TEST_SFTP_SERVER to the path of portable-sftp (github.com/H7O/portable-sftp).";
            return;
        }

        _scratch = Path.Combine(Path.GetTempPath(), "com-h-net-ssh-tests", Guid.NewGuid().ToString("N")[..8]);
        ServedFolder = Path.Combine(_scratch, "served");
        Directory.CreateDirectory(ServedFolder);
        Port = FreePort();

        var start = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var arg in new[] { "-root", ServedFolder, "-bind", "127.0.0.1", "-port", Port.ToString(), "-user", User,
                     "-hostkey", Path.Combine(_scratch, "host_key"), "-max-conns", "32", "-verbose" })
            start.ArgumentList.Add(arg);
        // The server takes one way of logging in or the other, not both.
        if (authorizedKeys is not null)
        {
            start.ArgumentList.Add("-keyfile");
            start.ArgumentList.Add(authorizedKeys);
            start.Environment.Remove("SFTP_PASSWORD");
        }
        else
        {
            start.Environment["SFTP_PASSWORD"] = Password;
        }

        _process = Process.Start(start)!;
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) _output.Enqueue(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _output.Enqueue(e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (!WaitForPort(Port, TimeSpan.FromSeconds(10)))
        {
            Unavailable = $"portable-sftp did not start listening on port {Port}. It said: {string.Join(" | ", _output)}";
            Dispose();
            return;
        }

        var until = DateTime.UtcNow.AddSeconds(5);
        while (HostKeyFingerprint.Length == 0 && DateTime.UtcNow < until)
        {
            HostKeyFingerprint = _output.Select(l => Regex.Match(l, @"host key\s+(SHA256:\S+)")).FirstOrDefault(m => m.Success)?.Groups[1].Value ?? "";
            if (HostKeyFingerprint.Length == 0) Thread.Sleep(50);
        }
        if (HostKeyFingerprint.Length == 0)
            Unavailable = "portable-sftp did not print its host key.";
    }

    public SFtpClient Client() => new("127.0.0.1", Port, User, Password);

    /// <summary>A folder name no other test uses.</summary>
    public static string NewFolder() => "t" + Guid.NewGuid().ToString("N")[..10];

    /// <summary>The path on this machine of a file or folder the server holds.</summary>
    public string OnDisk(string remotePath)
        => Path.Combine(ServedFolder, remotePath.Trim('/').Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Everything the server has logged so far.</summary>
    public List<string> Log() => [.. _output];

    /// <summary>The lines the server logged while <paramref name="work"/> ran.</summary>
    public async Task<List<string>> LogDuringAsync(Func<Task> work)
    {
        await Task.Delay(150); // let earlier lines arrive
        var before = _output.Count;
        await work();
        await Task.Delay(300);
        return _output.Skip(before).ToList();
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException)
        {
            // It never started.
        }
        _process?.Dispose();

        try
        {
            if (_scratch is not null) Directory.Delete(_scratch, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left for the system's temp clean-up.
        }
    }

    private static string? Find()
    {
        var configured = Environment.GetEnvironmentVariable("COM_H_TEST_SFTP_SERVER");
        if (!string.IsNullOrWhiteSpace(configured))
            return File.Exists(configured) ? configured : null;

        var names = OperatingSystem.IsWindows() ? new[] { "portable-sftp.exe" } : ["portable-sftp"];
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(folder => names.Select(name => Path.Combine(folder, name)))
            .FirstOrDefault(File.Exists);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static bool WaitForPort(int port, TimeSpan patience)
    {
        var until = DateTime.UtcNow + patience;
        while (DateTime.UtcNow < until)
        {
            try
            {
                using var client = new TcpClient();
                client.Connect(IPAddress.Loopback, port);
                return true;
            }
            catch (SocketException)
            {
                Thread.Sleep(100);
            }
        }
        return false;
    }
}
