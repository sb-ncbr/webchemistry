rmdir Bootstrapper /s /q
rmdir ..\Bootstrapper /s /q

rmdir MQService /s /q
rmdir ..\MQService /s /q

rmdir ValidatorService /s /q
rmdir ..\ValidatorService /s /q

rmdir AtlasAnalyzer /s /q
rmdir ..\AtlasAnalyzer /s /q

rmdir ChargesService /s /q
rmdir ..\ChargesService /s /q

rmdir ChargesAnalyzerService /s /q
rmdir ..\ChargesAnalyzerService /s /q

"C:\Program Files\7-Zip\7z.exe" x Platform.7z

mkdir ..\Bootstrapper
mkdir ..\MQService
mkdir ..\ValidatorService
mkdir ..\AtlasAnalyzer
mkdir ..\ChargesService
mkdir ..\ChargesAnalyzerService

copy Bootstrapper\*.* ..\Bootstrapper
copy MQService\*.* ..\MQService
copy ValidatorService\*.* ..\ValidatorService
copy AtlasAnalyzer\*.* ..\AtlasAnalyzer
copy ChargesService\*.* ..\ChargesService
copy ChargesAnalyzerService\*.* ..\ChargesAnalyzerService

rmdir Bootstrapper /s /q
rmdir MQService /s /q
rmdir ValidatorService /s /q
rmdir AtlasAnalyzer /s /q
rmdir ChargesService /s /q
rmdir ChargesAnalyzerService /s /q