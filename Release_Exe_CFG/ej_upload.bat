@echo off
set file="inputfile_list"
set zerosize=0
set _date=%DATE:/=-%
cd /d d:
SET LogfileName="EJLoadRun_%_date%.log"
echo %LogfileName%
dir d:\To_Inject\EJFiles /a:-d /b >%file%

FOR /F "usebackq" %%A IN ('%file%') DO set size=%%~zA
if %size% GTR %zerosize% (
   echo.move the previous zip file in D:\EJ\Loading\log\BackUp...Start Processing.
    echo file name is %file%  
		move D:\EJ\Loading\bin\log\*.zip D:\EJ\Loading\log\BackUp\.
   echo.There are files in D:\To_Inject\EJFiles...Start Processing.
  echo file name process is %file%  
	d:\EJ\Loading\bin\NCR_EJ_Load.exe %file% > D:\EJ\Loading\log\%LogfileName%
		del /q D:\To_Inject\EJFiles\Uploaded\*.*
		move D:\To_Inject\EJFiles\*.* D:\To_Inject\EJFiles\Uploaded\.
			cd /d c:
			cd %PROGRAMFILES%\
			 zip -m D:\EJ\Loading\log\%LogfileName% D:\EJ\Loading\log\%LogfileName%.zip
			  erase /f D:\EJ\Loading\bin\log\%LogfileName%
			 echo "Done archiving the EJ Uploaded log file.."
) ELSE (
    echo.There are no files to process in D:\To_Inject\EJFiles...Quiting.
)