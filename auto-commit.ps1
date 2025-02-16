while ($true) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    git add .
    git commit -m "Auto-commit: $timestamp"
    git push origin main
    Write-Host "Committed and pushed changes at $timestamp"
    Start-Sleep -Seconds 600  # Wait for 10 minutes
} 