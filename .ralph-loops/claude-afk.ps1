$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($args[0])) {
    Write-Error "Usage: $($MyInvocation.MyCommand.Name) <iterations>"
    exit 1
}

$Iterations = [int]$args[0]
$PromptPath = Join-Path $PSScriptRoot "prompt.md"
$Prompt = Get-Content -Raw -Path $PromptPath

for ($i = 1; $i -le $Iterations; $i++) {
    Write-Host "====== Starting Claude iteration $i of $Iterations in isolated sandbox... ======"

    $Result = claude --permission-mode bypassPermissions --model claude-sonnet-4-6 -p "$Prompt"

    Write-Host $Result

    if ($Result -like "*<promise>COMPLETE</promise>*") {
        Write-Host "PRD complete after $i iterations."
        exit 0
    }
}