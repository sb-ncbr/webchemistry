rmdir bin /s /q
mkdir bin
copy UpdateSvc_*.bat bin\
copy Extract.bat bin\

mkdir bin\Bootstrapper
copy ..\Plaftorm\bin\Bootstrapper\*.dll bin\Bootstrapper
copy ..\Plaftorm\bin\Bootstrapper\*.exe bin\Bootstrapper
copy ..\Plaftorm\bin\Bootstrapper\*.config bin\Bootstrapper
copy configs\Bootstrapper\*.cfg bin\Bootstrapper
del bin\ChargesService\*.vshost.*

mkdir bin\MQService
copy ..\AppsAndServices\Queries\bin\Service\*.dll bin\MQService
copy ..\AppsAndServices\Queries\bin\Service\*.exe bin\MQService
copy ..\AppsAndServices\Queries\bin\Service\*.config bin\MQService
copy ..\AppsAndServices\Queries\bin\Service\*.txt bin\MQService
del bin\MQService\*.vshost.*

cd bin
"C:\Program Files\7-Zip\7z" a Platform.7z Bootstrapper\
"C:\Program Files\7-Zip\7z" a Platform.7z MQService\

REM rmdir Bootstrapper /s /q
REM rmdir MQService /s /q
REM rmdir ValidatorService /s /q
REM rmdir AtlasAnalyzer /s /q
REM rmdir ChargesService /s /q
REM rmdir ChargesAnalyzerService /s /q