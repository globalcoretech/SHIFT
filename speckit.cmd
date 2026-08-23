@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0speckit.ps1" -Command "%1" -Name "%2"
