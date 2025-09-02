# ElectronHelper PowerShell Integration

This document explains how the ElectronHelper application integrates with PowerShell scripts.

## Overview

The ElectronHelper application can execute PowerShell scripts that contain classes with methods, allowing dynamic module loading and execution based on requests from an Electron app.

## How It Works

1. **Script Location**: PowerShell scripts are stored in the `scripts/` directory
2. **Naming Convention**: Script files should be named `{ModuleName}.ps1`
3. **Class Structure**: Each script should contain a PowerShell class with the same name as the file (without extension)

## Example Request

From Electron app:
```json
{
    "module": "UserModule",
    "operation": "GetStatus", 
    "paramsJson": {
        "userId": "12345"
    }
}
```

## Example PowerShell Script (`scripts/UserModule.ps1`)

```powershell
class UserModule {
    [string] GetStatus([string] $userId) {
        return "Status for user ${userId}: Active"
    }

    [int] Add([int] $a, [int] $b) {
        return $a + $b
    }
}
```

## Important Notes

- **Variable Interpolation**: Use `${variableName}` syntax when variables are followed by colons or other characters
- **Synchronous Methods**: Work perfectly with the current implementation
- **Asynchronous Methods**: Have limitations due to PowerShell runspace context requirements
- **Parameter Binding**: Parameters are automatically mapped from JSON to PowerShell method parameters
- **Error Handling**: Comprehensive error handling for missing scripts, classes, methods, and parameters

## Response Format

Success response:
```json
{
    "status": "ok",
    "module": "UserModule", 
    "operation": "GetStatus",
    "result": "Status for user 12345: Active"
}
```

Error response:
```json
{
    "status": "error",
    "error": "Operation not found: InvalidMethod in module UserModule"
}
```

## Testing

The PowerShell integration has been tested with:
- ✅ String return values (`GetStatus` method)
- ✅ Integer calculations (`Add` method)
- ✅ Parameter binding from JSON
- ✅ Error handling for missing methods/parameters

## Known Limitations

- Asynchronous PowerShell methods (returning `Task` or `Task<T>`) have runspace context issues
- PowerShell classes must have parameterless constructors
- Only public instance methods are accessible