$host.ui.RawUI.WindowTitle = "Quick Git Commit"
Set-Location $PSScriptRoot

Write-Host "Quick Git Commit and Push"
Write-Host "========================"

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Add all changes
git add .

# Commit with timestamp
git commit -m "Quick commit: $timestamp"

# Push to GitHub
git push origin main

Write-Host "`nChanges committed and pushed successfully!"
Write-Host "Timestamp: $timestamp`n"

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 