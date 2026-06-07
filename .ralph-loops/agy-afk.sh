#!/bin/zsh
set -e

if [ -z "$1" ]; then
  echo "Usage: $0 <iterations>"
  exit 1
fi

# Get script directory and read the prompt ONCE before the loop starts
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROMPT_PATH="$SCRIPT_DIR/prompt.md"
PROMPT=$(cat "$PROMPT_PATH")

# Robust C-style loop for dynamic variables in Zsh
for ((i=1; i<=$1; i++)); do
  echo "====== Starting Antigravity (agy) iteration $i of $1... ======"
  
  # Captured output from non-interactive print mode
  result=$(agy --dangerously-skip-permissions -p "$PROMPT" < /dev/null)

  echo "$result"

  # Zsh native substring matching
  if [[ "$result" == *"<promise>COMPLETE</promise>"* ]]; then
    echo "PRD complete after $i iterations."
    exit 0
  fi
done

echo "Reached maximum iterations ($1) without encountering completion promise tag."