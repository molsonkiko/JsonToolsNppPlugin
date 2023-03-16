: this script requires 7-zip to be installed on your computer
: sync testfiles with ones in repo
: ONLY EXECUTE THIS BATCH FILE FROM THE SAME DIRECTORY WHERE IT LIVES IN THE REPO!!!!
xcopy .\testfiles .\JsonToolsNppPlugin\bin\Release-x64\testfiles\ /s /y
copy ".\DSON UDL.xml" ".\JsonToolsNppPlugin\bin\Release-x64\DSON UDL.xml" /y
xcopy .\testfiles .\JsonToolsNppPlugin\bin\Release\testfiles\ /s /y
copy ".\DSON UDL.xml" ".\JsonToolsNppPlugin\bin\Release\DSON UDL.xml" /y
: zip testfiles and dlls to release zipfiles
: also copy directories to Downloads for easy access later
cd JsonToolsNppPlugin\bin\Release-x64
xcopy . "%userprofile%\Downloads\JsonTools NEWEST x64\" /s /y
7z -r a ..\..\Release_x64.zip JsonTools.dll testfiles "DSON UDL.xml"
cd ..\Release
xcopy . "%userprofile%\Downloads\JsonTools NEWEST x86\" /s /y
7z -r a ..\..\Release_x86.zip JsonTools.dll testfiles "DSON UDL.xml"
cd ..\..\..