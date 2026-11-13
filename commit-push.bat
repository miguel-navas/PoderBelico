@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === Status ===
git status --short
echo.
set /p MSG=Mensagem do commit (Enter = "dados: conferencia Wikipedia"): 
if "%MSG%"=="" set MSG=dados: conferencia Wikipedia
git add -A
git commit -m "%MSG%" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_011g1Zexja8Gixn5N46nQkh7"
if errorlevel 1 (
  echo.
  echo Nada para commitar ou erro no commit. Tentando push mesmo assim...
)
git push
echo.
git log --oneline -3
pause
