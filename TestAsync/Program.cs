using Com.H.Net.Ssh;
using System.Text;
using System.Threading;

namespace TestAsync;

class Program
{
    // SFTP Connection Settings
    private const string Server = "192.168.50.196";
    private const int Port = 2222;
    private const string Username = "t";
    private const string Password = "123";
    
    // Test paths
    private const string RemoteTestDir = "/test_async";
    private const string LocalTestDir = @"C:\Temp\SftpAsyncTest";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  SFTP Async Methods Comprehensive Test");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Server: {Server}:{Port}");
        Console.WriteLine($"User: {Username}");
        Console.WriteLine();
        
        // Ensure local test directory exists
        if (!Directory.Exists(LocalTestDir))
            Directory.CreateDirectory(LocalTestDir);
        
        using var client = new SFtpClient(Server, Port, Username, Password);
        
        try
        {
            await RunAllTestsAsync(client);
            
            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("  ✅ ALL TESTS COMPLETED SUCCESSFULLY!");
            Console.WriteLine("===========================================");
            
            // Test the Dispose pattern
            await DisposeTest.TestDisposalAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("  ❌ TEST FAILED!");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return;
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static async Task RunAllTestsAsync(SFtpClient client)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // Global timeout
        var ct = cts.Token;
        
        // Test 1: ExistAsync (should not exist yet)
        await Test1_ExistAsync_NonExistent(client, ct);
        
        // Test 2: Upload a test file
        await Test2_UploadAsync_FromLocalPath(client, ct);
        
        // Test 3: ExistAsync (should exist now)
        await Test3_ExistAsync_Existing(client, ct);
        
        // Test 4: GetFileInfoAsync
        await Test4_GetFileInfoAsync(client, ct);
        
        // Test 5: DownloadAsync to local path
        await Test5_DownloadAsync_ToLocalPath(client, ct);
        
        // Test 6: DownloadAsStringAsync
        await Test6_DownloadAsStringAsync(client, ct);
        
        // Test 7: UploadAsync from Stream
        await Test7_UploadAsync_FromStream(client, ct);
        
        // Test 8: DownloadAsync to Stream
        await Test8_DownloadAsync_ToStream(client, ct);
        
        // Test 9: Upload directory (multiple files)
        await Test9_UploadAsync_Directory(client, ct);
        
        // Test 10: ListFilesAsync
        await Test10_ListFilesAsync(client, ct);
        
        // Test 11: Download directory
        await Test11_DownloadAsync_Directory(client, ct);
        
        // Test 12: DeleteAsync single file
        await Test12_DeleteAsync_SingleFile(client, ct);
        
        // Test 13: ExistsAsync (alias method)
        await Test13_ExistsAsync_Alias(client, ct);
        
        // Cleanup
        await TestCleanup(client, ct);
    }
    
    static async Task Test1_ExistAsync_NonExistent(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 1: ExistAsync - Non-existent file");
        Console.WriteLine("---------------------------------------");
        
        var testFile = $"{RemoteTestDir}/nonexistent.txt";
        var exists = await client.ExistAsync(testFile, ct);
        
        Console.WriteLine($"  File: {testFile}");
        Console.WriteLine($"  Exists: {exists}");
        
        if (exists)
            throw new Exception("File should not exist!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test2_UploadAsync_FromLocalPath(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 2: UploadAsync - From Local Path");
        Console.WriteLine("---------------------------------------");
        
        var localFile = Path.Combine(LocalTestDir, "test_upload.txt");
        var remoteFile = $"{RemoteTestDir}/test_upload.txt";
        
        // Create test file
        var content = $"Test file created at {DateTime.Now}\nThis is a test for UploadAsync method.";
        await File.WriteAllTextAsync(localFile, content, ct);
        
        Console.WriteLine($"  Local: {localFile}");
        Console.WriteLine($"  Remote: {remoteFile}");
        Console.Write("  Uploading... ");
        
        await client.UploadAsync(localFile, remoteFile, cancellationToken: ct);
        
        Console.WriteLine("Done!");
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test3_ExistAsync_Existing(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 3: ExistAsync - Existing file");
        Console.WriteLine("---------------------------------------");
        
        var testFile = $"{RemoteTestDir}/test_upload.txt";
        var exists = await client.ExistAsync(testFile, ct);
        
        Console.WriteLine($"  File: {testFile}");
        Console.WriteLine($"  Exists: {exists}");
        
        if (!exists)
            throw new Exception("File should exist!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test4_GetFileInfoAsync(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 4: GetFileInfoAsync");
        Console.WriteLine("---------------------------------------");
        
        var testFile = $"{RemoteTestDir}/test_upload.txt";
        var fileInfo = await client.GetFileInfoAsync(testFile, ct);
        
        Console.WriteLine($"  File: {testFile}");
        Console.WriteLine($"  Name: {fileInfo.Name}");
        Console.WriteLine($"  Full Path: {fileInfo.FullPath}");
        Console.WriteLine($"  Is Directory: {fileInfo.IsDirectory}");
        Console.WriteLine($"  Last Modified: {fileInfo.LastModified}");
        
        if (fileInfo.IsDirectory)
            throw new Exception("Should be a file, not a directory!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test5_DownloadAsync_ToLocalPath(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 5: DownloadAsync - To Local Path");
        Console.WriteLine("---------------------------------------");
        
        var remoteFile = $"{RemoteTestDir}/test_upload.txt";
        var localFile = Path.Combine(LocalTestDir, "test_download.txt");
        
        // Delete if exists
        if (File.Exists(localFile))
            File.Delete(localFile);
        
        Console.WriteLine($"  Remote: {remoteFile}");
        Console.WriteLine($"  Local: {localFile}");
        Console.Write("  Downloading... ");
        
        await client.DownloadAsync(remoteFile, localFile, ct);
        
        Console.WriteLine("Done!");
        
        if (!File.Exists(localFile))
            throw new Exception("Downloaded file does not exist!");
        
        var content = await File.ReadAllTextAsync(localFile, ct);
        Console.WriteLine($"  Content length: {content.Length} chars");
        Console.WriteLine($"  First 50 chars: {content[..Math.Min(50, content.Length)]}...");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test6_DownloadAsStringAsync(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 6: DownloadAsStringAsync");
        Console.WriteLine("---------------------------------------");
        
        var remoteFile = $"{RemoteTestDir}/test_upload.txt";
        
        Console.WriteLine($"  Remote: {remoteFile}");
        Console.Write("  Downloading... ");
        
        var content = await client.DownloadAsStringAsync(remoteFile, cancellationToken: ct);
        
        Console.WriteLine("Done!");
        Console.WriteLine($"  Content length: {content.Length} chars");
        Console.WriteLine($"  Content: {content}");
        
        if (string.IsNullOrEmpty(content))
            throw new Exception("Downloaded content is empty!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test7_UploadAsync_FromStream(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 7: UploadAsync - From Stream");
        Console.WriteLine("---------------------------------------");
        
        var remoteFile = $"{RemoteTestDir}/test_stream_upload.bin";
        var testData = Encoding.UTF8.GetBytes($"Stream upload test at {DateTime.Now}\n" +
            "This tests uploading from a MemoryStream.\n" +
            "Binary data: " + string.Join("", Enumerable.Range(0, 256).Select(i => ((byte)i).ToString("X2"))));
        
        using var stream = new MemoryStream(testData);
        
        Console.WriteLine($"  Remote: {remoteFile}");
        Console.WriteLine($"  Data size: {testData.Length} bytes");
        Console.Write("  Uploading... ");
        
        await client.UploadAsync(stream, remoteFile, closeStream: false, cancellationToken: ct);
        
        Console.WriteLine("Done!");
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test8_DownloadAsync_ToStream(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 8: DownloadAsync - To Stream");
        Console.WriteLine("---------------------------------------");
        
        var remoteFile = $"{RemoteTestDir}/test_stream_upload.bin";
        
        using var stream = new MemoryStream();
        
        Console.WriteLine($"  Remote: {remoteFile}");
        Console.Write("  Downloading... ");
        
        await client.DownloadAsync(remoteFile, stream, closeStream: false, cancellationToken: ct);
        
        Console.WriteLine("Done!");
        
        stream.Position = 0;
        var data = stream.ToArray();
        
        Console.WriteLine($"  Downloaded size: {data.Length} bytes");
        Console.WriteLine($"  First 50 bytes: {string.Join(" ", data.Take(50).Select(b => b.ToString("X2")))}");
        
        if (data.Length == 0)
            throw new Exception("Downloaded data is empty!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test9_UploadAsync_Directory(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 9: UploadAsync - Directory (Multiple Files)");
        Console.WriteLine("---------------------------------------");
        
        var localDir = Path.Combine(LocalTestDir, "test_folder");
        Directory.CreateDirectory(localDir);
        
        // Create multiple test files
        for (int i = 1; i <= 3; i++)
        {
            var filePath = Path.Combine(localDir, $"file{i}.txt");
            await File.WriteAllTextAsync(filePath, $"Test file {i} content\nCreated at {DateTime.Now}", ct);
        }
        
        // Create a subdirectory with a file
        var subDir = Path.Combine(localDir, "subfolder");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "Nested file content", ct);
        
        Console.WriteLine($"  Local: {localDir}");
        Console.WriteLine($"  Remote: {RemoteTestDir}");
        Console.Write("  Uploading directory... ");
        
        await client.UploadAsync(localDir + Path.DirectorySeparatorChar, RemoteTestDir + "/", cancellationToken: ct);
        
        Console.WriteLine("Done!");
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test10_ListFilesAsync(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 10: ListFilesAsync");
        Console.WriteLine("---------------------------------------");
        
        Console.WriteLine($"  Directory: {RemoteTestDir}");
        Console.Write("  Listing files... ");
        
        var files = await client.ListFilesAsync(RemoteTestDir, ct);
        
        Console.WriteLine($"Done! ({files.Count} items)");
        Console.WriteLine();
        
        foreach (var file in files)
        {
            var type = file.IsDirectory ? "[DIR]" : "[FILE]";
            Console.WriteLine($"    {type} {file.Name,-30} {file.LastModified:yyyy-MM-dd HH:mm:ss}");
        }
        
        if (files.Count == 0)
            throw new Exception("No files found!");
        
        Console.WriteLine();
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test11_DownloadAsync_Directory(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 11: DownloadAsync - Directory");
        Console.WriteLine("---------------------------------------");
        
        var remoteDir = $"{RemoteTestDir}/test_folder";
        var localDir = Path.Combine(LocalTestDir, "downloaded_folder");
        
        // Clean up if exists
        if (Directory.Exists(localDir))
            Directory.Delete(localDir, true);
        
        Console.WriteLine($"  Remote: {remoteDir}");
        Console.WriteLine($"  Local: {localDir}");
        Console.Write("  Downloading directory... ");
        
        await client.DownloadAsync(remoteDir, localDir, ct);
        
        Console.WriteLine("Done!");
        
        if (!Directory.Exists(localDir))
            throw new Exception("Downloaded directory does not exist!");
        
        var downloadedFiles = Directory.GetFiles(localDir, "*", SearchOption.AllDirectories);
        Console.WriteLine($"  Downloaded files: {downloadedFiles.Length}");
        
        foreach (var file in downloadedFiles)
        {
            Console.WriteLine($"    {Path.GetFileName(file)}");
        }
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test12_DeleteAsync_SingleFile(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 12: DeleteAsync - Single File");
        Console.WriteLine("---------------------------------------");
        
        var remoteFile = $"{RemoteTestDir}/test_stream_upload.bin";
        
        Console.WriteLine($"  File: {remoteFile}");
        
        // Verify it exists first
        var existsBefore = await client.ExistAsync(remoteFile, ct);
        Console.WriteLine($"  Exists before: {existsBefore}");
        
        if (!existsBefore)
            throw new Exception("File should exist before deletion!");
        
        Console.Write("  Deleting... ");
        await client.DeleteAsync(remoteFile, ct);
        Console.WriteLine("Done!");
        
        // Verify it's gone
        var existsAfter = await client.ExistAsync(remoteFile, ct);
        Console.WriteLine($"  Exists after: {existsAfter}");
        
        if (existsAfter)
            throw new Exception("File should not exist after deletion!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task Test13_ExistsAsync_Alias(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Test 13: ExistsAsync - Alias Method");
        Console.WriteLine("---------------------------------------");
        
        var testFile = $"{RemoteTestDir}/test_upload.txt";
        
        Console.WriteLine($"  File: {testFile}");
        Console.Write("  Checking with ExistsAsync... ");
        
        var exists = await client.ExistsAsync(testFile, ct);
        
        Console.WriteLine($"{exists}");
        
        if (!exists)
            throw new Exception("File should exist!");
        
        Console.WriteLine("  ✅ PASSED");
        Console.WriteLine();
    }
    
    static async Task TestCleanup(SFtpClient client, CancellationToken ct)
    {
        Console.WriteLine("Cleanup: Deleting remote test directory");
        Console.WriteLine("---------------------------------------");
        
        try
        {
            Console.Write("  Deleting... ");
            
            // Delete all files first (recursive)
            var files = await client.ListFilesAsync(RemoteTestDir, ct);
            foreach (var file in files)
            {
                await client.DeleteAsync(file.FullPath, ct);
            }
            
            // Delete the directory itself
            await client.DeleteAsync(RemoteTestDir, ct);
            
            Console.WriteLine("Done!");
            Console.WriteLine("  ✅ Cleanup complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Cleanup failed - {ex.Message}");
            Console.WriteLine("  ⚠️  You may need to manually clean up test files");
        }
        
        Console.WriteLine();
    }
}
