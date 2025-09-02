
using System;
using System.IO;  // File/stream operations
using System.IO.Pipes;  // Named pipe communication
using System.Text;  // Text encoding/decoding
using System.Threading.Tasks; // Async programming
using System.Reflection; // For loading classes dynamically
using System.Collections; // Hashtable
using System.Collections.Generic;  // Lists, dictionaries, etc.
using Newtonsoft.Json;  // JSON serialization
using Newtonsoft.Json.Linq;// JSON manipulation
using System.Management.Automation; // PowerShell execution
using System.Management.Automation.Runspaces;

namespace ElectronHelper
{
    class Program
    {
        public void sessionIntiitalize(){
            //runspace to execute for powershell c#
        }
       
        static async Task Main(string[] args)
        {
               //immidiately session intialize karna hai
               //1.session intitalize
               //2.session management
               //3.login nahi hai to direct execute kar le
               //4.jisme instance banna hai usme session manage karna hi padega
                
            const string pipeName = "electron-helper-pipe";  //same pipename shouel present in the electronjs
          
            while (true)
            {
                // Create a named pipe server for communication
                using var pipeServer = new NamedPipeServerStream(
                    pipeName,                    //  clients to connect
                    PipeDirection.InOut,         // Two-way communication
                    1,                           // Maximum connected clients
                    PipeTransmissionMode.Byte,   // Data transmission mode
                    PipeOptions.Asynchronous);   // Non-blocking operations

                Console.WriteLine("waiting electron to connect");
                 
                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine("Electron connected!");

                try
                {
                    // Create readers-writers for the pipe
                    using var reader = new StreamReader(pipeServer, Encoding.UTF8);
                    using var writer = new StreamWriter(pipeServer, Encoding.UTF8) { AutoFlush = true };

                    // Read commands until connection ends
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null && line.Length > 0)
                    {
                        Console.WriteLine($"Received: {line}");
                        
                        try
                        {
                            // Parse the command JSON
                            var command = JObject.Parse(line);
                            
                            // Execute the command and get the result
                            var result = await ExecuteCommand(command);
                            
                            // Send the result back
                            await writer.WriteLineAsync(JsonConvert.SerializeObject(result));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                            // Send error response as JSON
                            await writer.WriteLineAsync(JsonConvert.SerializeObject(new { 
                                status = "error", 
                                error = ex.Message 
                            }));
                        }
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        // Execute a command  
        static async Task<object> ExecuteCommand(JObject command)
        {
            
            string moduleName = command["module"]?.ToString();  // Module name  aaya
            string operation = command["operation"]?.ToString(); // Operation name aaya
            
            //  module and opearations are cumpulsory so throw error if not present
            if (string.IsNullOrEmpty(moduleName))
            {
                return new { status = "error", error = "missing required parameter: module" };
            }
            
            if (string.IsNullOrEmpty(operation))
            {
                return new { status = "error", error = "missing required parameter: operation" };
            }
            
            // Extract optional paramsJson (could be null)
            JToken paramsJsonToken = command["paramsJson"]; // always be object 
            object[] parameters = null;

            try
            {
                // Prefer executing a PowerShell class defined in scripts/<module>.ps1
                string scriptsRoot = Path.Combine(AppContext.BaseDirectory, "scripts");
                string scriptPath = Path.Combine(scriptsRoot, $"{moduleName}.ps1");

                object result;

                if (File.Exists(scriptPath))
                {
                    // Execute the PowerShell class method inside the .ps1 script
                    var psResult = ExecutePowerShellScriptMethod(
                        scriptPath,
                        moduleName,
                        operation,
                        paramsJsonToken as JObject
                    );
                    result = psResult;
                }
                else
                {
                    // Fall back to loading C# modules by reflection if available
                    Type moduleType = FindModuleType(moduleName);
                    if (moduleType == null)
                    {
                        return new {
                            status = "error",
                            error = $"Module not found as PowerShell script or C# type: {moduleName}"
                        };
                    }

                    // Find the method or operation in the module class
                    MethodInfo methodInfo = FindMethodInfo(moduleType, operation);
                    if (methodInfo == null)
                    {
                        return new {
                            status = "error",
                            error = $"Operation not found: {operation} in module {moduleName}"
                        };
                    }

                    // Process parameters if they exist
                    if (paramsJsonToken != null && paramsJsonToken.Type != JTokenType.Null)
                    {
                        parameters = PrepareParameters(methodInfo, paramsJsonToken);
                    }

                    object moduleInstance = Activator.CreateInstance(moduleType);

                    // Check if the method is async (returns Task or Task<T>)
                    if (IsAsyncMethod(methodInfo))
                    {
                        dynamic task = methodInfo.Invoke(moduleInstance, parameters);
                        result = await task;
                    }
                    else
                    {
                        result = methodInfo.Invoke(moduleInstance, parameters);
                    }
                }

                // Return success response with result
                return new { 
                    status = "ok", 
                    module = moduleName,
                    operation = operation,
                    result = result
                };
            }
            catch (Exception ex)
            {
                // Extract the innermost exception for better error reporting
                Exception innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                }

                Console.WriteLine($"Execution error: {innerException.Message}");
                return new { 
                    status = "error", 
                    error = innerException.Message,
                    stackTrace = innerException.StackTrace
                };
            }
        }

        // Execute a PowerShell class method defined in a script file.
        // The script should declare a class whose name matches the file name (moduleName),
        // and we will create an instance and invoke the specified method with paramsJson.
        private static object ExecutePowerShellScriptMethod(string scriptPath, string moduleName, string methodName, JObject? paramsJson)
        {
            // Build a case-insensitive hashtable for parameter lookup in PowerShell
            Hashtable? map = null;
            if (paramsJson != null)
            {
                map = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in paramsJson.Properties())
                {
                    // Convert JToken -> CLR object; Newtonsoft handles primitives and nested structures
                    map[prop.Name] = prop.Value.ToObject<object?>();
                }
            }

            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            using var ps = PowerShell.Create();
            ps.Runspace = runspace;

            // Use a single param-ized script to avoid manual string interpolation issues.
            string invoker = @"
param(
    [string]$scriptPath,
    [string]$className,
    [string]$methodName,
    [System.Collections.IDictionary]$paramsMap
)

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw ""PowerShell module script not found: $scriptPath""
}

# Load the script so that its class becomes available in the session
. $scriptPath

# Create an instance of the class (default constructor)
$instance = New-Object -TypeName $className
if (-not $instance) {
    throw ""Unable to create instance of class '$className' from script '$scriptPath'""
}

$type = $instance.GetType()
$bindingFlags = [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::IgnoreCase
$methods = $type.GetMethods($bindingFlags) | Where-Object { $_.Name -eq $methodName }
if (-not $methods) {
    throw ""Operation not found: $methodName in module $className""
}

# If multiple overloads, prefer the first; advanced: could match on parameter names
$method = $methods[0]
$paramInfos = $method.GetParameters()

$argList = @()
foreach ($pi in $paramInfos) {
    if ($paramsMap -and $paramsMap.Contains($pi.Name)) {
        $val = $paramsMap[$pi.Name]
    }
    elseif (-not $pi.HasDefaultValue) {
        throw ""Required parameter '$($pi.Name)' not found in paramsJson""
    }
    else {
        $val = $pi.DefaultValue
    }
    # Convert to the declared parameter type for reliable invocation
    $converted = [System.Management.Automation.LanguagePrimitives]::ConvertTo($val, $pi.ParameterType)
    $argList += $converted
}

# Invoke the method using reflection to avoid quoting/injection issues
$ret = $method.Invoke($instance, $argList)
if ($ret -is [System.Threading.Tasks.Task]) {
    # Await Task or Task[T]
    $awaiter = $ret.GetAwaiter()
    $awaiter.GetResult()
} else {
    $ret
}
";

            ps.AddScript(invoker)
              .AddArgument(scriptPath)
              .AddArgument(moduleName)
              .AddArgument(methodName)
              .AddArgument(map ?? new Hashtable(StringComparer.OrdinalIgnoreCase));

            var results = ps.Invoke();
            if (ps.Streams.Error.Count > 0)
            {
                // Return the first error for simplicity
                var err = ps.Streams.Error[0];
                throw new Exception(err?.Exception?.Message ?? err?.ToString());
            }

            if (results == null || results.Count == 0)
            {
                return null!;
            }

            // If multiple pipeline outputs, return the last one
            var last = results[^1];
            return last?.BaseObject;
        }

        // Find a module type by name
        private static Type FindModuleType(string moduleName)
        {
            // First try to find the type in the current assembly
            Type type = Type.GetType($"ElectronHelper.{moduleName}");
            if (type != null)
            {
                return type;
            }

            // If not found, try to load the assembly from file
            string assemblyPath = Path.Combine(Environment.CurrentDirectory, $"{moduleName}.dll");
            if (File.Exists(assemblyPath))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            return t;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading assembly {assemblyPath}: {ex.Message}");
                }
            }
            
            // If we still haven't found it, check the current directory for .cs files
            string csFilePath = Path.Combine(Environment.CurrentDirectory, $"{moduleName}.cs");
            if (File.Exists(csFilePath))
            {
                Console.WriteLine($"Found source file {csFilePath}, but we can't compile it at runtime. Please compile it to a DLL first.");
            }

            return null;
        }

        // Find a method by name in a type
        private static MethodInfo FindMethodInfo(Type type, string methodName)
        {
            // Look for public instance methods with the given name (case-insensitive)
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (MethodInfo method in methods)
            {
                if (method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                {
                    return method;
                }
            }
            return null;
        }

        // Check if a method is async (returns Task or Task<T>)
        private static bool IsAsyncMethod(MethodInfo method)
        {
            Type returnType = method.ReturnType;
            return returnType == typeof(Task) || 
                  (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

            // Prepare parameters for a method call based on paramsJson
        private static object[] PrepareParameters(MethodInfo method, JToken paramsJsonToken)
        {
            //this is a class that tells us about the parameters and we can destructure those parameters

            ParameterInfo[] paramInfos = method.GetParameters();
            if (paramInfos.Length == 0)
            {
                return null;
            }

            // Convert JSON to appropriate parameter types
            object[] parameters = new object[paramInfos.Length];
            
            // If paramsJson is an object, extract named parameters
            if (paramsJsonToken.Type == JTokenType.Object)
            {
                JObject paramsObj = (JObject)paramsJsonToken;
                
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    ParameterInfo paramInfo = paramInfos[i];
                    JToken paramValue = paramsObj[paramInfo.Name];
                    
                    if (paramValue != null)
                    {
                        parameters[i] = paramValue.ToObject(paramInfo.ParameterType);
                    }
                    else if (!paramInfo.IsOptional)
                    {
                        throw new ArgumentException($"Required parameter '{paramInfo.Name}' not found in paramsJson");
                    }
                    else
                    {
                        parameters[i] = paramInfo.DefaultValue;
                    }
                }
            }
           
           
            return parameters;
        }
    }
}