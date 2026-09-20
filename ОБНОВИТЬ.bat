@echo off
cd /d "%~dp0"
git add -A
git commit -m "update"
git push origin main
echo Done! Check: https://github.com/artemykoryagin-cyber/HomeworkGate/actions
pause
