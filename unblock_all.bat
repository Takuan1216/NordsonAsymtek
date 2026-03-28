@echo on
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -Recurse | Unblock-File"
echo.
echo 已解除目前資料夾及子資料夾所有檔案的封鎖
pause