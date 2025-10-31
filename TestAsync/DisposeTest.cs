using Com.H.Net.Ssh;

namespace TestAsync;

public static class DisposeTest
{
    public static async Task TestDisposalAsync()
    {
        Console.WriteLine("\n===========================================");
        Console.WriteLine("  Testing Dispose Pattern");
        Console.WriteLine("===========================================\n");

        // Test 1: Dispose after operations
        Console.WriteLine("Test 1: Dispose after operations");
        Console.WriteLine("-----------------------------------");
        using (var client = new SFtpClient("192.168.50.196", 2222, "t", "123"))
        {
            var exists = await client.ExistAsync("/test_async");
            Console.WriteLine($"  Connected and performed operation: {exists}");
        } // Dispose called here
        Console.WriteLine("  ✅ Disposed successfully\n");

        // Test 2: Multiple dispose calls (should be safe)
        Console.WriteLine("Test 2: Multiple Dispose calls (idempotent)");
        Console.WriteLine("-----------------------------------");
        var client2 = new SFtpClient("192.168.50.196", 2222, "t", "123");
        await client2.ExistAsync("/test_async");
        client2.Dispose();
        Console.WriteLine("  First Dispose() called");
        client2.Dispose(); // Should be safe to call again
        Console.WriteLine("  Second Dispose() called");
        Console.WriteLine("  ✅ Multiple disposes handled safely\n");

        // Test 3: Dispose with KeepConnectionOpen = true
        Console.WriteLine("Test 3: Dispose with KeepConnectionOpen = true");
        Console.WriteLine("-----------------------------------");
        using (var client3 = new SFtpClient("192.168.50.196", 2222, "t", "123"))
        {
            client3.KeepConnectionOpen = true;
            await client3.ExistAsync("/test_async");
            await client3.ExistAsync("/test_async"); // Should reuse connection
            Console.WriteLine("  Multiple operations with kept connection");
        } // Dispose should clean up properly
        Console.WriteLine("  ✅ Disposed even with KeepConnectionOpen\n");

        Console.WriteLine("===========================================");
        Console.WriteLine("  ✅ ALL DISPOSE TESTS PASSED!");
        Console.WriteLine("===========================================");
    }
}
