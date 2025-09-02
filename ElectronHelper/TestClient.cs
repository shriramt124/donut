using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectronHelper
{
    /// <summary>
    /// Simple test client that simulates an Electron app connecting to the ElectronHelper
    /// </summary>
    public class TestClient
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== ElectronHelper Test Client ===");
            Console.WriteLine("This simulates an Electron app connecting to the helper.");
            Console.WriteLine();

            // Give the user instructions
            Console.WriteLine("Instructions:");
            Console.WriteLine("1. Run 'dotnet run' in another terminal to start ElectronHelper");
            Console.WriteLine("2. Then run this test client");
            Console.WriteLine("3. Or run both by choosing option in the menu below");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Choose test option:");
                Console.WriteLine("1. Test UserModule.GetStatus");
                Console.WriteLine("2. Test UserModule.Add");
                Console.WriteLine("3. Test UserModule.GetStatusAsync");
                Console.WriteLine("4. Custom test (enter your own JSON)");
                Console.WriteLine("5. Exit");
                Console.Write("Enter choice (1-5): ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await TestGetStatus();
                            break;
                        case "2":
                            await TestAdd();
                            break;
                        case "3":
                            await TestGetStatusAsync();
                            break;
                        case "4":
                            await TestCustom();
                            break;
                        case "5":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.WriteLine();
                Console.WriteLine("=".PadRight(50, '='));
                Console.WriteLine();
            }
        }

        static async Task TestGetStatus()
        {
            var request = new
            {
                module = "UserModule",
                operation = "GetStatus",
                paramsJson = new { userId = "12345" }
            };

            await SendTestRequest("UserModule.GetStatus Test", request);
        }

        static async Task TestAdd()
        {
            var request = new
            {
                module = "UserModule",
                operation = "Add",
                paramsJson = new { a = 15, b = 27 }
            };

            await SendTestRequest("UserModule.Add Test", request);
        }

        static async Task TestGetStatusAsync()
        {
            var request = new
            {
                module = "UserModule",
                operation = "GetStatusAsync",
                paramsJson = new { userId = "98765" }
            };

            await SendTestRequest("UserModule.GetStatusAsync Test", request);
        }

        static async Task TestCustom()
        {
            Console.WriteLine("Enter JSON request (or press Enter for example):");
            Console.WriteLine("Example: {\"module\":\"UserModule\",\"operation\":\"GetStatus\",\"paramsJson\":{\"userId\":\"custom123\"}}");
            Console.Write("JSON: ");
            
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                input = "{\"module\":\"UserModule\",\"operation\":\"GetStatus\",\"paramsJson\":{\"userId\":\"custom123\"}}";
            }

            try
            {
                var request = JsonConvert.DeserializeObject(input);
                await SendTestRequest("Custom Test", request);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ Invalid JSON: {ex.Message}");
            }
        }

        static async Task SendTestRequest(string testName, object request)
        {
            Console.WriteLine($"🧪 Running: {testName}");
            
            string requestJson = JsonConvert.SerializeObject(request);
            Console.WriteLine($"📤 Sending: {requestJson}");

            try
            {
                using var pipeClient = new NamedPipeClientStream(".", "electron-helper-pipe", PipeDirection.InOut);
                
                Console.WriteLine("🔌 Connecting to ElectronHelper...");
                await pipeClient.ConnectAsync(5000); // 5 second timeout
                
                Console.WriteLine("✅ Connected!");

                using var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };
                using var reader = new StreamReader(pipeClient, Encoding.UTF8);

                // Send the request
                await writer.WriteLineAsync(requestJson);
                Console.WriteLine("📤 Request sent!");

                // Read the response
                Console.WriteLine("⏳ Waiting for response...");
                string response = await reader.ReadLineAsync();

                if (response != null)
                {
                    Console.WriteLine($"📥 Response: {response}");
                    
                    // Try to pretty-print the JSON response
                    try
                    {
                        var responseObj = JsonConvert.DeserializeObject(response);
                        var prettyJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);
                        Console.WriteLine("📋 Pretty Response:");
                        Console.WriteLine(prettyJson);
                    }
                    catch
                    {
                        // If it's not valid JSON, just show the raw response
                        Console.WriteLine("📋 Raw Response:");
                        Console.WriteLine(response);
                    }

                    Console.WriteLine("✅ Test completed successfully!");
                }
                else
                {
                    Console.WriteLine("❌ No response received.");
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("❌ Connection timeout. Make sure ElectronHelper is running.");
                Console.WriteLine("💡 Start it with: dotnet run");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                Console.WriteLine("💡 Make sure ElectronHelper is running in another terminal.");
            }
        }
    }
}