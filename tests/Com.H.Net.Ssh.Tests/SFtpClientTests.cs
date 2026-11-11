using System.Text;
using Renci.SshNet.Common;

namespace Com.H.Net.Ssh.Tests;

/// <summary>SFtpClient against a real SFTP server. Each test works in a remote folder of its own.</summary>
public class SFtpClientTests(SftpServer server) : IClassFixture<SftpServer>
{
    private static MemoryStream Text(string text) => new(Encoding.UTF8.GetBytes(text));

    private SFtpClient Client(bool keepOpen = true)
    {
        Skip.If(server.Unavailable is not null, server.Unavailable);
        var client = server.Client();
        client.KeepConnectionOpen = keepOpen;
        return client;
    }

    /// <summary>The requests of one kind the server logged, as the paths they were for.</summary>
    private static List<string> Requests(IEnumerable<string> log, string operation)
        => log.Where(l => l.Contains($" op={operation} "))
            .Select(l => l[(l.IndexOf(" path=", StringComparison.Ordinal) + 6)..].Split(' ')[0])
            .ToList();

    #region the folders above an upload

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_upload_creates_the_folders_above_the_file(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();

        if (async) await client.UploadAsync(Text("hello"), $"/{folder}/a/b/c/file.txt");
        else client.Upload(Text("hello"), $"/{folder}/a/b/c/file.txt");

        Assert.Equal("hello", File.ReadAllText(server.OnDisk($"{folder}/a/b/c/file.txt")));
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_upload_into_a_folder_that_exists_asks_the_server_about_that_folder_only(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        await client.UploadAsync(Text("one"), $"/{folder}/a/b/c/one.txt");

        var log = await server.LogDuringAsync(async () =>
        {
            if (async) await client.UploadAsync(Text("two"), $"/{folder}/a/b/c/two.txt");
            else client.Upload(Text("two"), $"/{folder}/a/b/c/two.txt");
        });

        // One question, whatever the depth. Before 11.0.0 it was one for each of the four folders.
        Assert.Equal([$"/{folder}/a/b/c"], Requests(log, "stat"));
        Assert.Empty(Requests(log, "mkdir"));
        Assert.Equal("two", File.ReadAllText(server.OnDisk($"{folder}/a/b/c/two.txt")));
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_upload_creates_only_the_folders_that_are_missing(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        await client.UploadAsync(Text("one"), $"/{folder}/a/one.txt");

        var log = await server.LogDuringAsync(async () =>
        {
            if (async) await client.UploadAsync(Text("two"), $"/{folder}/a/b/c/two.txt");
            else client.Upload(Text("two"), $"/{folder}/a/b/c/two.txt");
        });

        // It looks upwards from the file's folder and stops at the first one that is there.
        Assert.Equal([$"/{folder}/a/b/c", $"/{folder}/a/b", $"/{folder}/a"], Requests(log, "stat"));
        Assert.Equal([$"/{folder}/a/b", $"/{folder}/a/b/c"], Requests(log, "mkdir"));
        Assert.Equal("two", File.ReadAllText(server.OnDisk($"{folder}/a/b/c/two.txt")));
    }

    [SkippableFact]
    public async Task An_upload_to_a_path_without_a_leading_slash_works_the_same()
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();

        await client.UploadAsync(Text("relative"), $"{folder}/a/file.txt");
        await client.UploadAsync(Text("top"), $"{folder}-top.txt");

        Assert.Equal("relative", File.ReadAllText(server.OnDisk($"{folder}/a/file.txt")));
        Assert.Equal("top", File.ReadAllText(server.OnDisk($"{folder}-top.txt")));
    }

    [SkippableFact]
    public async Task Two_uploads_into_the_same_new_folder_at_once_both_arrive()
    {
        Skip.If(server.Unavailable is not null, server.Unavailable);
        var folder = SftpServer.NewFolder();

        await Task.WhenAll(Enumerable.Range(0, 6).Select(async i =>
        {
            using var client = server.Client();
            await client.UploadAsync(Text("file " + i), $"/{folder}/a/b/c/d/{i}.txt");
        }));

        Assert.Equal(6, Directory.GetFiles(server.OnDisk($"{folder}/a/b/c/d")).Length);
    }

    [SkippableFact]
    public async Task A_local_folder_is_uploaded_with_its_subfolders()
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        var local = Directory.CreateTempSubdirectory("com-h-net-ssh-up-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(local, "site", "css"));
            File.WriteAllText(Path.Combine(local, "site", "index.html"), "<html>");
            File.WriteAllText(Path.Combine(local, "site", "css", "main.css"), "body{}");

            await client.UploadAsync(Path.Combine(local, "site"), $"/{folder}/");

            Assert.Equal("<html>", File.ReadAllText(server.OnDisk($"{folder}/site/index.html")));
            Assert.Equal("body{}", File.ReadAllText(server.OnDisk($"{folder}/site/css/main.css")));
        }
        finally
        {
            Directory.Delete(local, recursive: true);
        }
    }

    #endregion

    #region file sizes

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_listing_gives_the_size_of_each_file(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        await client.UploadAsync(Text("12345"), $"/{folder}/five.txt");
        await client.UploadAsync(new MemoryStream(new byte[70_000]), $"/{folder}/big.bin");
        await client.UploadAsync(Text(""), $"/{folder}/empty.txt");
        await client.UploadAsync(Text("x"), $"/{folder}/sub/x.txt");

        var files = (async ? await client.ListFilesAsync($"/{folder}") : client.ListFiles($"/{folder}")).ToDictionary(f => f.Name);

        Assert.Equal(5, files["five.txt"].Size);
        Assert.Equal(70_000, files["big.bin"].Size);
        Assert.Equal(0, files["empty.txt"].Size);
        Assert.True(files["sub"].IsDirectory);
        Assert.False(files["five.txt"].IsDirectory);
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_information_about_one_file_gives_its_size(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        await client.UploadAsync(Text("1234567"), $"/{folder}/seven.txt");

        var info = async ? await client.GetFileInfoAsync($"/{folder}/seven.txt") : client.GetFileInfo($"/{folder}/seven.txt");

        Assert.Equal(7, info.Size);
        Assert.Equal("seven.txt", info.Name);
    }

    #endregion

    #region the server's host key

    [SkippableFact]
    public async Task The_servers_host_key_is_known_after_connecting()
    {
        using var client = Client();
        Assert.Null(client.ServerHostKeyFingerprint);

        await client.ExistsAsync("/");

        Assert.Equal(server.HostKeyFingerprint, client.ServerHostKeyFingerprint);
    }

    [SkippableTheory]
    [InlineData("as printed")]
    [InlineData("without the prefix")]
    [InlineData("with padding")]
    [InlineData("with spaces around")]
    public async Task The_expected_host_key_lets_its_server_in(string written)
    {
        using var client = Client();
        var hash = server.HostKeyFingerprint["SHA256:".Length..];
        client.HostKeyFingerprint = written switch
        {
            "without the prefix" => hash,
            "with padding" => "SHA256:" + hash + "=",
            "with spaces around" => "  " + server.HostKeyFingerprint + " ",
            _ => server.HostKeyFingerprint,
        };

        Assert.True(await client.ExistsAsync("/"));
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_server_with_another_host_key_is_refused_before_the_login_is_sent(bool async)
    {
        using var client = Client();
        var folder = SftpServer.NewFolder();
        client.HostKeyFingerprint = "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        SshConnectionException? refusal = null;
        var log = await server.LogDuringAsync(async () =>
        {
            refusal = async
                ? await Assert.ThrowsAsync<SshConnectionException>(() => client.UploadAsync(Text("secret"), $"/{folder}/secret.txt"))
                : Assert.Throws<SshConnectionException>(() => client.Upload(Text("secret"), $"/{folder}/secret.txt"));
        });

        Assert.Contains(server.HostKeyFingerprint, refusal!.Message);
        Assert.Contains("SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA was expected", refusal.Message);
        // The password never reached the server, and neither did the file.
        Assert.DoesNotContain(log, line => line.Contains("msg=authenticated"));
        Assert.False(Directory.Exists(server.OnDisk(folder)));
        Assert.Equal(server.HostKeyFingerprint, client.ServerHostKeyFingerprint);
    }

    [SkippableFact]
    public async Task A_refused_client_connects_once_it_is_given_the_right_key()
    {
        using var client = Client();
        client.HostKeyFingerprint = "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        await Assert.ThrowsAsync<SshConnectionException>(() => client.ExistsAsync("/"));

        // The fingerprint the server showed is what a trust-on-first-use caller would store.
        client.HostKeyFingerprint = client.ServerHostKeyFingerprint;

        Assert.True(await client.ExistsAsync("/"));
    }

    [SkippableFact]
    public async Task Without_an_expected_host_key_any_server_is_accepted_as_before()
    {
        using var client = Client();
        Assert.Null(client.HostKeyFingerprint);

        Assert.True(await client.ExistsAsync("/"));
    }

    [SkippableFact]
    public async Task A_wrong_password_is_still_reported_as_a_failed_login()
    {
        Skip.If(server.Unavailable is not null, server.Unavailable);
        using var client = new SFtpClient("127.0.0.1", server.Port, SftpServer.User, "not the password")
        {
            HostKeyFingerprint = server.HostKeyFingerprint,
        };

        await Assert.ThrowsAsync<SshAuthenticationException>(() => client.ExistsAsync("/"));
    }

    #endregion
}
