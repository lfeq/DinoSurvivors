$PromptPath = Join-Path $PSScriptRoot "prompt.md"
$Prompt = Get-Content -Raw -Path $PromptPath
claude --permission-mode acceptEdits --model claude-sonnet-4-6 "$Prompt"