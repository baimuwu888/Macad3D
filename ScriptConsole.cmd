@echo off

rem Find MSBuild
setlocal enabledelayedexpansion
rem 找到vswhere.exe的地址 
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
rem 找到csi.exe的地址 csi是用来执行C#脚本的
for /f "usebackq tokens=*" %%i in (`%VSWHERE% -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\Roslyn\csi.exe`) do (
  set CSIPATH=%%i
)

if not exist "%CSIPATH%" (
	echo Cannot find CSI.EXE, please install VisualStudio with MSBuild.
	pause
	exit /b 1
)

rem Init environment variables
rem 设置.csx为可执行的脚本
set PATHEXT=%PATHEXT%;.CSX
rem设置当前文件夹路径
set MMROOT=%~dp0
rem 进入到Build文件夹
cd %MMROOT%\Build
rem 再启动一个新的窗口
rem Start
if not %1.==. (
	set CMD=%*
	call:execute
	echo Script returned with error code !ERRORLEVEL!.
	exit /b !ERRORLEVEL!
)

echo - 
echo - Macad3D build script console.
echo - Enter 'help' for available commands.
echo - 

:loopstart
	set CMD=
	set /P CMD=">  " 
	if /I "%CMD%"=="exit" goto loopend
	if /I "%CMD%"=="quit" goto loopend
	call:execute
	goto loopstart

:loopend
exit /b 0
rem 根据命理执行对应的c#脚本
:execute
	for /f "tokens=1*" %%a in ("%CMD%") do (
		echo.%%a| find /I ".csx">nul && ( set SCR=%%a ) || ( set SCR=%%a.csx )
		if not exist "!SCR!" (
			echo Invalid script name.
			goto loopstart
		)
		"%CSIPATH%" !SCR! %%b
	)
goto:eof
