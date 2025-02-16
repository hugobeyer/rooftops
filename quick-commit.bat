@echo off
echo Quick Git Commit and Push
echo ========================

:: Get current date and time for commit message
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set TIMESTAMP=%datetime:~0,4%-%datetime:~4,2%-%datetime:~6,2% %datetime:~8,2%:%datetime:~10,2%:%datetime:~12,2%

:: Add all changes
git add .

:: Commit with timestamp
git commit -m "Quick commit: %TIMESTAMP%"

:: Push to GitHub
git push origin main

echo.
echo Changes committed and pushed successfully!
echo Timestamp: %TIMESTAMP%
echo.
pause 