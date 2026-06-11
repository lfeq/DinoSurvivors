#!/bin/zsh
set -e

if [ -z "$1" ]; then
  echo "Usage: $0 <iterations>"
  exit 1
fi

# Stop the .NET SDK from leaving persistent build-server daemons running after a
# build/test (VBCSCompiler + MSBuild node-reuse nodes). Those daemons inherit
# agy's stdout fd; if it is captured through a pipe they keep it open forever and
# hang the loop between iterations. Disabling reuse makes them exit with the build.
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
export DOTNET_CLI_TELEMETRY_OPTOUT=1

# Get script directory and read the prompt ONCE before the loop starts
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROMPT_PATH="$SCRIPT_DIR/prompt.md"
PROMPT=$(cat "$PROMPT_PATH")

# Robust C-style loop for dynamic variables in Zsh
for ((i=1; i<=$1; i++)); do
  echo "====== Starting Antigravity (agy) iteration $i of $1... ======"
  
  # Non-interactive print mode. Capture to a FILE, not a pipe: if agy spawns
  # long-lived children (e.g. .NET build servers) they would inherit a capture
  # pipe and wedge it open, hanging the loop. A file fd is harmless, and `wait`
  # returns as soon as agy itself exits regardless of orphaned grandchildren.
  # --print-timeout 30m: the 5m default truncates a full TDD iteration.
  LOG="$(mktemp -t agy-afk-XXXXXX)"
  agy --dangerously-skip-permissions --print-timeout 30m -p "$PROMPT" < /dev/null > "$LOG" 2>&1 &
  AGY_PID=$!
  tail -f "$LOG" &              # live view while agy works
  TAIL_PID=$!
  wait "$AGY_PID" || true       # don't let one bad iteration kill the AFK loop
  sleep 0.3                     # let tail flush the final lines
  kill "$TAIL_PID" 2>/dev/null || true
  result="$(cat "$LOG")"
  rm -f "$LOG"

  # Zsh native substring matching
  if [[ "$result" == *"<promise>COMPLETE</promise>"* ]]; then
    echo "PRD complete after $i iterations."
    exit 0
  fi
done

echo "Reached maximum iterations ($1) without encountering completion promise tag."