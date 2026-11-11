using System.Diagnostics;

namespace Com.H.Net.Ssh.Tests;

/// <summary>
/// A server that accepts keys only, and the key pairs to log in with, made by ssh-keygen exactly as
/// a person would make them. So the keys are in OpenSSH's own format, which is what ssh-keygen has
/// written by default since OpenSSH 7.8.
/// </summary>
public sealed class KeyServer : IDisposable
{
    public const string Passphrase = "open sesame";

    private readonly string _folder = Directory.CreateTempSubdirectory("com-h-net-ssh-keys-").FullName;

    public SftpServer? Server { get; }
    public string? Unavailable { get; }

    public string Key(string name) => Path.Combine(_folder, name);

    public KeyServer()
    {
        try
        {
            Generate("ed25519", "ed25519", "");
            Generate("ed25519-locked", "ed25519", Passphrase);
            Generate("rsa", "rsa", "");
            Generate("ecdsa", "ecdsa", "");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Unavailable = "ssh-keygen is needed to make the test keys, and could not be run: " + ex.Message;
            return;
        }

        var authorized = Path.Combine(_folder, "authorized_keys");
        File.WriteAllLines(authorized, Directory.GetFiles(_folder, "*.pub").Select(File.ReadAllText).Select(k => k.Trim()));
        Server = new SftpServer(authorized);
        Unavailable = Server.Unavailable;
    }

    private void Generate(string name, string type, string passphrase)
    {
        var start = new ProcessStartInfo("ssh-keygen") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-q", "-t", type, "-N", passphrase, "-C", "com-h-net-ssh-test", "-f", Key(name) })
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("ssh-keygen did not start.");
        process.WaitForExit(30_000);
        if (process.ExitCode != 0 || !File.Exists(Key(name)))
            throw new InvalidOperationException("ssh-keygen failed: " + process.StandardError.ReadToEnd());
    }

    public void Dispose()
    {
        Server?.Dispose();
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Left for the system's temp clean-up.
        }
    }
}

public class PrivateKeyTests(KeyServer keys) : IClassFixture<KeyServer>
{
    [SkippableTheory]
    [InlineData("ed25519", null)]
    [InlineData("ed25519-locked", KeyServer.Passphrase)]
    [InlineData("rsa", null)]
    [InlineData("ecdsa", null)]
    public async Task A_key_made_by_ssh_keygen_logs_in_as_it_is(string key, string? passphrase)
    {
        Skip.If(keys.Unavailable is not null, keys.Unavailable);
        Assert.StartsWith("-----BEGIN OPENSSH PRIVATE KEY-----", File.ReadAllText(keys.Key(key)));

        using var client = new SFtpClient("127.0.0.1", keys.Server!.Port, SftpServer.User,
            new PrivateKeyFileSettings { Path = keys.Key(key), Passphrase = passphrase });
        var folder = SftpServer.NewFolder();

        await client.UploadAsync(new MemoryStream("signed in with a key"u8.ToArray()), $"/{folder}/proof.txt");

        Assert.Equal("signed in with a key", File.ReadAllText(keys.Server.OnDisk($"{folder}/proof.txt")));
    }

    [SkippableFact]
    public async Task A_key_the_server_does_not_know_is_refused()
    {
        Skip.If(keys.Unavailable is not null, keys.Unavailable);
        // A key pair of the same kind that is not in the server's authorized_keys.
        var stranger = keys.Key("stranger");
        var start = new ProcessStartInfo("ssh-keygen") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in new[] { "-q", "-t", "ed25519", "-N", "", "-f", stranger }) start.ArgumentList.Add(arg);
        using (var process = Process.Start(start)!) process.WaitForExit(30_000);

        using var client = new SFtpClient("127.0.0.1", keys.Server!.Port, SftpServer.User, new PrivateKeyFileSettings { Path = stranger });

        await Assert.ThrowsAsync<Renci.SshNet.Common.SshAuthenticationException>(() => client.ExistsAsync("/"));
    }
}
