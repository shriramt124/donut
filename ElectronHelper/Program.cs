
using System;
using System.IO;  // File/stream operations
using System.IO.Pipes;  // Named pipe communication
using System.Text;  // Text encoding/decoding
using System.Threading.Tasks; // Async programming
using System.Reflection; // For loading classes dynamically
using System.Management.Automation;/ PowerShell execution
using System.Collections.Generic;  // Lists, dictionaries, etc.
using Newtonsoft.Json;  // JSON serialization
using Newtonsoft.Json.Linq;// JSON manipulation

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
            JToken paramsJsonToken = command["paramsJson"];//always be object 
            object[] parameters = null;

            try
            {
                // type module lets us find all the methods in the class
                //it helps us create an instance of the class
                //it can find the method with 
                Type moduleType = FindModuleType(moduleName);
                //WINDOW.pathname 
                if (moduleType == null)
                {
                    return new { 
                        status = "error", 
                        error = $"Module not found: {moduleName}" 
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
                    //jo mthod info uper mila hai usko preprare karo pramatere se 
                    parameters = PrepareParameters(methodInfo, paramsJsonToken);
                }

                // Create an instance of the module class
                object moduleInstance = Activator.CreateInstance(moduleType);

                // Invoke the method (operation) on the module instance
                object result;
                
                // Check if the method is async (returns Task or Task<T>)
                if (IsAsyncMethod(methodInfo))
                {
                    // For async methods, we need to await the Task
                    dynamic task = methodInfo.Invoke(moduleInstance, parameters);
                    result = await task;
                }
                else
                {
                    // For synchronous methods, we can invoke directly
                    result = methodInfo.Invoke(moduleInstance, parameters);
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