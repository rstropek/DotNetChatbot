# --- USER SETTINGS ---
$LocalPath   = "C:\Code\github\DotNetChatbot"
$HostName    = "dev-01"
$User        = "rainer"
$KeyPath     = "C:\Users\RainerStropek\.ssh\id_rsa.ppk"   # PuTTY key is fine
$Fingerprint = "EpVbjjkSy2IukqmR+9amlVgXTP1h/x2AzXej5eDlYKA"
$RemotePath  = "/home/rainer/live/2025-09-22-basta"

# If you use a non-standard SSH port, uncomment the next line and set it:
# $PortNumber = 22

# --- LOAD WINSCP .NET API ---
$winScpDll = "C:\Program Files (x86)\WinSCP\WinSCPnet.dll"
Add-Type -Path $winScpDll

# --- SESSION OPTIONS ---
$sessionOptions = New-Object WinSCP.SessionOptions -Property @{
  Protocol              = [WinSCP.Protocol]::Sftp
  HostName              = $HostName
  UserName              = $User
  SshPrivateKeyPath     = $KeyPath
  SshHostKeyFingerprint = $Fingerprint
}
# If you set $PortNumber above, apply it:
if ($PSBoundParameters.ContainsKey('PortNumber') -or $PortNumber) {
  $sessionOptions.PortNumber = $PortNumber
}

$session = New-Object WinSCP.Session

try {
  $session.Open($sessionOptions)

  # --- TRANSFER/EXCLUDES ---
  $transferOptions = New-Object WinSCP.TransferOptions
  # Exclude folders and patterns (| = exclude in WinSCP masks)
  # Fixed: Use */ for directories and proper wildcards
  $transferOptions.FileMask = "| bin/; obj/; .git/; node_modules/; *.tmp; *.log; *.suo; *.user; .vs/"
  $transferOptions.TransferMode = [WinSCP.TransferMode]::Binary
  $transferOptions.PreserveTimestamp = $true

  # --- FIRST FULL SYNC (mirror local -> remote, delete removed files on remote) ---
  $null = $session.SynchronizeDirectories(
    [WinSCP.SynchronizationMode]::Remote,
    $LocalPath, $RemotePath,
    $true,     # remove remote files that no longer exist locally
    $true,     # mirror (copy new/changed)
    # Use Time + Size to be robust across editors/filesystems
    ([WinSCP.SynchronizationCriteria]::Time -bor [WinSCP.SynchronizationCriteria]::Size),
    $transferOptions
  )

  Write-Host "Initial sync complete. Watching for changes..."

  # --- CONTINUOUS SYNC: events set flags; main thread performs sync after debounce ---
  $fsw = New-Object System.IO.FileSystemWatcher $LocalPath
  $fsw.IncludeSubdirectories = $true
  $fsw.EnableRaisingEvents = $true
  $fsw.Filter = "*.*"

  # Use global scope for proper sharing between event handlers and main thread
  $global:pending   = $false
  $global:lastEvent = Get-Date
  $debounceMs       = 800
  $syncing          = $false

  # Event actions: only set flags/time (no WinSCP calls here!)
  $onChange = {
    param($sender, $e)
    
    # Filter out changes in excluded directories
    $excludePatterns = @('\\bin\\', '\\obj\\', '\\.git\\', '\\node_modules\\', '\\.vs\\')
    $shouldExclude = $false
    foreach ($pattern in $excludePatterns) {
      if ($e.FullPath -match $pattern) {
        $shouldExclude = $true
        break
      }
    }
    
    if (-not $shouldExclude) {
      Write-Host "File change detected: $($e.FullPath) ($($e.ChangeType))"
      $global:pending   = $true
      $global:lastEvent = Get-Date
    }
  }

  $handlers = @()
  $handlers += Register-ObjectEvent $fsw Changed -Action $onChange
  $handlers += Register-ObjectEvent $fsw Created -Action $onChange
  $handlers += Register-ObjectEvent $fsw Deleted -Action $onChange
  $handlers += Register-ObjectEvent $fsw Renamed -Action $onChange

  try {
    Write-Host "File system watcher configured for: $LocalPath"
    Write-Host "Sync running. Press Ctrl+C to stop."
    while ($true) {
      Start-Sleep -Milliseconds 200

      if ($global:pending -and -not $syncing) {
        $elapsed = (Get-Date) - $global:lastEvent
        Write-Host "Changes pending for $($elapsed.TotalMilliseconds)ms (debounce: ${debounceMs}ms)"
        
        if ($elapsed.TotalMilliseconds -ge $debounceMs) {
          $syncing = $true
          Write-Host "Starting sync operation..."
          try {
            $result = $session.SynchronizeDirectories(
              [WinSCP.SynchronizationMode]::Remote,
              $LocalPath, $RemotePath,
              $true,  # remove remote files not present locally
              $true,  # mirror/copy new+changed
              ([WinSCP.SynchronizationCriteria]::Time -bor [WinSCP.SynchronizationCriteria]::Size),
              $transferOptions
            )
            Write-Host ("[{0}] Incremental sync completed - {1} files transferred" -f (Get-Date), $result.Transfers.Count)
            $global:pending = $false
          } catch {
            Write-Warning "Sync failed: $_"
            # keep pending=true so we retry next loop
            Start-Sleep -Seconds 2  # Brief pause before retry
          } finally {
            $syncing = $false
          }
        }
      }
    }
  }
  finally {
    foreach ($h in $handlers) {
      Unregister-Event -SourceIdentifier $h.Name
    }
    $fsw.EnableRaisingEvents = $false
    $fsw.Dispose()
  }
}
finally {
  if ($session -and $session.Opened) { $session.Dispose() }
}
