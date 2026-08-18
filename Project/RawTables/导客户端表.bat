@echo off
cd /d %~dp0
Tools\XLSX2CSV.exe ./ ../public/Tables
pause
