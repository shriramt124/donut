class UserModule {
    [string] GetStatus([string] $userId) {
        return "Status for user ${userId}: Active"
    }

    [int] Add([int] $a, [int] $b) {
        return $a + $b
    }

    [System.Threading.Tasks.Task[string]] GetStatusAsync([string] $userId) {
        return [System.Threading.Tasks.Task]::Run([Func[string]]{
            Start-Sleep -Seconds 1
            "Async status for user ${userId}: Active"
        })
    }
}
