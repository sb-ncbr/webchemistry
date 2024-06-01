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

mkdir bin\ValidatorService
copy ..\AppsAndServices\MotiveValidator\bin\Service\*.dll bin\ValidatorService
copy ..\AppsAndServices\MotiveValidator\bin\Service\*.exe bin\ValidatorService
copy ..\AppsAndServices\MotiveValidator\bin\Service\*.config bin\ValidatorService
copy ..\AppsAndServices\MotiveValidator\bin\Service\*.csv bin\ValidatorService
copy ..\AppsAndServices\MotiveValidator\bin\Service\*.txt bin\ValidatorService
del bin\ValidatorService\*.vshost.*

mkdir bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.dll bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.exe bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.config bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.tsv bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.dat bin\AtlasAnalyzer
copy ..\AppsAndServices\MotiveAtlas\bin\Analyzer\*.txt bin\AtlasAnalyzer
copy configs\AtlasAnalyzer\*.json bin\AtlasAnalyzer
del bin\AtlasAnalyzer\*.vshost.*

mkdir bin\ChargesService
copy ..\AppsAndServices\Charges\bin\Service\*.dll bin\ChargesService
copy ..\AppsAndServices\Charges\bin\Service\*.txt bin\ChargesService
copy ..\AppsAndServices\Charges\bin\Service\*.exe bin\ChargesService
copy ..\AppsAndServices\Charges\bin\Service\*.config bin\ChargesService
del bin\ChargesService\*.vshost.*

mkdir bin\ChargesAnalyzerService
copy ..\AppsAndServices\Charges\bin\Service.Analyzer\*.dll bin\ChargesAnalyzerService
copy ..\AppsAndServices\Charges\bin\Service.Analyzer\*.txt bin\ChargesAnalyzerService
copy ..\AppsAndServices\Charges\bin\Service.Analyzer\*.exe bin\ChargesAnalyzerService
copy ..\AppsAndServices\Charges\bin\Service.Analyzer\*.config bin\ChargesAnalyzerService
copy ..\AppsAndServices\Charges\bin\Service.Analyzer\*.wxml bin\ChargesAnalyzerService
del bin\ChargesAnalyzerService\*.vshost.*

cd bin
"C:\Program Files\7-Zip\7z" a Platform.7z Bootstrapper\
"C:\Program Files\7-Zip\7z" a Platform.7z MQService\
"C:\Program Files\7-Zip\7z" a Platform.7z ValidatorService\
"C:\Program Files\7-Zip\7z" a Platform.7z AtlasAnalyzer\
"C:\Program Files\7-Zip\7z" a Platform.7z ChargesService\
"C:\Program Files\7-Zip\7z" a Platform.7z ChargesAnalyzerService\

REM rmdir Bootstrapper /s /q
REM rmdir MQService /s /q
REM rmdir ValidatorService /s /q
REM rmdir AtlasAnalyzer /s /q
REM rmdir ChargesService /s /q
REM rmdir ChargesAnalyzerService /s /q