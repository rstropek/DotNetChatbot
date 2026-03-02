#!/bin/bash

# One-way sync script using rsync over SSH
# Usage: ./sync-to-server.sh [config_file]

set -euo pipefail

# Default configuration
SOURCE_DIR="${SOURCE_DIR:-}"
REMOTE_USER="${REMOTE_USER:-}"
REMOTE_HOST="${REMOTE_HOST:-}"
REMOTE_PATH="${REMOTE_PATH:-}"
SSH_PORT="${SSH_PORT:-22}"
SSH_KEY="${SSH_KEY:-}"
DRY_RUN="${DRY_RUN:-false}"
WATCH_MODE="${WATCH_MODE:-false}"
WATCH_INTERVAL="${WATCH_INTERVAL:-30}"

# Default exclusion patterns
DEFAULT_EXCLUDES=(
    "node_modules/"
    ".git/"
    ".DS_Store"
    "*.swp"
    "*.swo"
    "*~"
    ".env"
    ".env.local"
    "__pycache__/"
    "*.pyc"
    ".venv/"
    "venv/"
    "dist/"
    "build/"
    ".idea/"
    ".vscode/"
    "*.log"
    ".cache/"
    "bin"
    "obj"
)

# Function to display usage
usage() {
    cat << EOF
Usage: $0 [options]

Options:
    -s, --source DIR        Source directory to sync (required)
    -u, --user USER         Remote SSH user (required)
    -h, --host HOST         Remote SSH host (required)
    -p, --path PATH         Remote destination path (required)
    -P, --port PORT         SSH port (default: 22)
    -k, --key FILE          SSH private key file
    -e, --exclude PATTERN   Add exclusion pattern (can be used multiple times)
    -f, --exclude-from FILE Load exclusion patterns from file (one per line)
    -c, --config FILE       Load configuration from file
    -d, --dry-run           Perform a dry run (show what would be synced)
    -w, --watch             Watch mode: continuously sync at regular intervals
    -i, --interval SECONDS  Watch mode interval in seconds (default: 30)
    --no-delete             Don't delete files on remote (default: deletions are synced)
    --help                  Show this help message

Environment variables:
    SOURCE_DIR, REMOTE_USER, REMOTE_HOST, REMOTE_PATH, SSH_PORT, SSH_KEY, DRY_RUN

Example:
    $0 -s ~/myproject -u deploy -h example.com -p /var/www/app
    $0 -c sync-config.conf --dry-run
    $0 -c sync-config.conf --watch --interval 60

Config file format (bash syntax):
    SOURCE_DIR="~/myproject"
    REMOTE_USER="deploy"
    REMOTE_HOST="example.com"
    REMOTE_PATH="/var/www/app"
    SSH_PORT=22
    EXCLUDES=("node_modules/" "*.log")
EOF
    exit 1
}

# Parse command line arguments
DELETE_FLAG="--delete"  # Default: sync deletions
EXCLUDES=()
CONFIG_FILE=""
EXCLUDE_FILE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--source)
            SOURCE_DIR="$2"
            shift 2
            ;;
        -u|--user)
            REMOTE_USER="$2"
            shift 2
            ;;
        -h|--host)
            REMOTE_HOST="$2"
            shift 2
            ;;
        -p|--path)
            REMOTE_PATH="$2"
            shift 2
            ;;
        -P|--port)
            SSH_PORT="$2"
            shift 2
            ;;
        -k|--key)
            SSH_KEY="$2"
            shift 2
            ;;
        -e|--exclude)
            EXCLUDES+=("$2")
            shift 2
            ;;
        -f|--exclude-from)
            EXCLUDE_FILE="$2"
            shift 2
            ;;
        -c|--config)
            CONFIG_FILE="$2"
            shift 2
            ;;
        -d|--dry-run)
            DRY_RUN=true
            shift
            ;;
        -w|--watch)
            WATCH_MODE=true
            shift
            ;;
        -i|--interval)
            WATCH_INTERVAL="$2"
            shift 2
            ;;
        --no-delete)
            DELETE_FLAG=""
            shift
            ;;
        --help)
            usage
            ;;
        *)
            echo "Unknown option: $1"
            usage
            ;;
    esac
done

# Load config file if specified
if [[ -n "$CONFIG_FILE" ]]; then
    if [[ -f "$CONFIG_FILE" ]]; then
        echo "Loading configuration from: $CONFIG_FILE"
        # shellcheck source=/dev/null
        source "$CONFIG_FILE"
    else
        echo "Error: Config file not found: $CONFIG_FILE"
        exit 1
    fi
fi

# Validate required parameters
if [[ -z "$SOURCE_DIR" ]] || [[ -z "$REMOTE_USER" ]] || [[ -z "$REMOTE_HOST" ]] || [[ -z "$REMOTE_PATH" ]]; then
    echo "Error: Missing required parameters"
    usage
fi

# Expand tilde in source directory
SOURCE_DIR="${SOURCE_DIR/#\~/$HOME}"

# Validate source directory exists
if [[ ! -d "$SOURCE_DIR" ]]; then
    echo "Error: Source directory does not exist: $SOURCE_DIR"
    exit 1
fi

# Combine default excludes with user-specified ones
ALL_EXCLUDES=("${DEFAULT_EXCLUDES[@]}")
if [[ ${#EXCLUDES[@]} -gt 0 ]]; then
    ALL_EXCLUDES+=("${EXCLUDES[@]}")
fi

# Load exclusions from file if specified
if [[ -n "$EXCLUDE_FILE" ]]; then
    if [[ -f "$EXCLUDE_FILE" ]]; then
        echo "Loading exclusions from: $EXCLUDE_FILE"
        while IFS= read -r line || [[ -n "$line" ]]; do
            # Skip empty lines and comments
            [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
            ALL_EXCLUDES+=("$line")
        done < "$EXCLUDE_FILE"
    else
        echo "Warning: Exclude file not found: $EXCLUDE_FILE"
    fi
fi

# Also check for .syncignore in source directory
SYNCIGNORE="$SOURCE_DIR/.syncignore"
if [[ -f "$SYNCIGNORE" ]]; then
    echo "Found .syncignore file in source directory"
    while IFS= read -r line || [[ -n "$line" ]]; do
        # Skip empty lines and comments
        [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
        ALL_EXCLUDES+=("$line")
    done < "$SYNCIGNORE"
fi

# Build rsync exclude arguments
EXCLUDE_ARGS=()
for pattern in "${ALL_EXCLUDES[@]}"; do
    EXCLUDE_ARGS+=(--exclude="$pattern")
done

# Build SSH command
SSH_CMD="ssh -p $SSH_PORT"
if [[ -n "$SSH_KEY" ]]; then
    SSH_CMD="$SSH_CMD -i $SSH_KEY"
fi

# Build rsync command
RSYNC_OPTS=(
    -avz                          # archive mode, verbose, compress
    -e "$SSH_CMD"                 # specify SSH command
    "${EXCLUDE_ARGS[@]}"          # exclusion patterns
)

# Add progress flag only if not in watch mode (to reduce noise)
if [[ "$WATCH_MODE" != "true" ]]; then
    RSYNC_OPTS+=(--progress)
fi

if [[ "$DRY_RUN" == "true" ]]; then
    RSYNC_OPTS+=(--dry-run)
    echo "=== DRY RUN MODE ==="
fi

if [[ -n "$DELETE_FLAG" ]]; then
    RSYNC_OPTS+=("$DELETE_FLAG")
    echo "=== Deletions will be synced (files removed locally will be removed on remote) ==="
else
    echo "=== Deletions will NOT be synced (use default behavior or remove --no-delete) ==="
fi

# Display sync information
echo "========================================="
echo "Sync Configuration"
echo "========================================="
echo "Source:      $SOURCE_DIR"
echo "Destination: $REMOTE_USER@$REMOTE_HOST:$REMOTE_PATH"
echo "SSH Port:    $SSH_PORT"
echo "Exclusions:  ${#ALL_EXCLUDES[@]} patterns"
if [[ "$WATCH_MODE" == "true" ]]; then
    echo "Watch Mode:  Enabled (interval: ${WATCH_INTERVAL}s)"
fi
echo "========================================="
echo

# Function to perform a single sync
perform_sync() {
    local sync_count=$1
    
    if [[ "$WATCH_MODE" == "true" ]]; then
        echo "--- Sync #$sync_count at $(date '+%Y-%m-%d %H:%M:%S') ---"
    else
        echo "Starting sync..."
    fi
    echo
    
    # Add trailing slash to source to sync contents, not the directory itself
    rsync "${RSYNC_OPTS[@]}" "$SOURCE_DIR/" "$REMOTE_USER@$REMOTE_HOST:$REMOTE_PATH"
    
    local rsync_exit=$?
    echo
    
    if [[ $rsync_exit -eq 0 ]]; then
        if [[ "$DRY_RUN" == "true" ]]; then
            echo "Dry run completed. No files were actually transferred."
        else
            echo "Sync completed successfully!"
        fi
    else
        echo "Sync failed with exit code: $rsync_exit"
        if [[ "$WATCH_MODE" != "true" ]]; then
            exit $rsync_exit
        fi
    fi
    
    return $rsync_exit
}

# Signal handler for clean shutdown in watch mode
trap 'echo -e "\n\nReceived interrupt signal. Shutting down..."; exit 0' INT TERM

# Perform the sync
if [[ "$WATCH_MODE" == "true" ]]; then
    echo "Watch mode enabled. Press Ctrl+C to stop."
    echo
    
    sync_count=1
    while true; do
        perform_sync $sync_count
        
        if [[ $sync_count -eq 1 ]]; then
            echo
            echo "Watching for changes. Next sync in ${WATCH_INTERVAL} seconds..."
        else
            echo "Next sync in ${WATCH_INTERVAL} seconds..."
        fi
        
        sleep "$WATCH_INTERVAL"
        sync_count=$((sync_count + 1))
        echo
    done
else
    perform_sync 1
fi