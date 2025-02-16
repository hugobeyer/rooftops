using UnityEngine;
using UnityEditor;
using System.IO;

namespace MobileMonetizationPro
{
    public class FixWin32Error : EditorWindow
    {
        private string selectedDirectory;

        [MenuItem("Tools/Mobile Monetization Pro/Solutions/Fix Win32 Error")]
        public static void ShowWindow()
        {
            GetWindow<FixWin32Error>("FixWin32Error");
        }

        private void OnGUI()
        {
            GUILayout.Label("Select Directory:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            selectedDirectory = EditorGUILayout.TextField(selectedDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                selectedDirectory = EditorUtility.OpenFolderPanel("Select Directory", "", "");
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Fix Win32 Error"))
            {
                FixError();
            }

            if (GUILayout.Button("Reimport"))
            {
                ReimportAssets();
            }
        }

        private void FixError()
        {
            if (string.IsNullOrEmpty(selectedDirectory))
            {
                EditorUtility.DisplayDialog("Error", "Please select a directory.", "OK");
                return;
            }

            string filePath = Path.Combine(selectedDirectory, "gradlew.bat");

            try
            {
                // Write gradlew.bat file content
                string gradlewContent = @"
@if ""%DEBUG%"" == """" @echo off
@rem ##########################################################################
@rem
@rem  Gradle startup script for Windows
@rem
@rem ##########################################################################

@rem Set local scope for the variables with windows NT shell
if ""%OS%""==""Windows_NT"" setlocal

set DIRNAME=%~dp0
if ""%DIRNAME%"" == """" set DIRNAME=.
set APP_BASE_NAME=%~n0
set APP_HOME=%DIRNAME%

@rem Add default JVM options here. You can also use JAVA_OPTS and GRADLE_OPTS to pass JVM options to this script.
set DEFAULT_JVM_OPTS=

@rem Find java.exe
if defined JAVA_HOME goto findJavaFromJavaHome

set JAVA_EXE=java.exe
%JAVA_EXE% -version >NUL 2>&1
if ""%ERRORLEVEL%"" == ""0"" goto init

echo.
echo ERROR: JAVA_HOME is not set and no 'java' command could be found in your PATH.
echo.
echo Please set the JAVA_HOME variable in your environment to match the
echo location of your Java installation.

goto fail

:findJavaFromJavaHome
set JAVA_HOME=%JAVA_HOME:""=%
set JAVA_EXE=%JAVA_HOME%/bin/java.exe

if exist ""%JAVA_EXE%"" goto init

echo.
echo ERROR: JAVA_HOME is set to an invalid directory: %JAVA_HOME%
echo.
echo Please set the JAVA_HOME variable in your environment to match the
echo location of your Java installation.

goto fail

:init
@rem Get command-line arguments, handling Windows variants

if not ""%OS%"" == ""Windows_NT"" goto win9xME_args
if ""%@eval[2+2]"" == ""4"" goto 4NT_args

:win9xME_args
@rem Slurp the command line arguments.
set CMD_LINE_ARGS=
set _SKIP=2

:win9xME_args_slurp
if ""x%~1"" == ""x"" goto execute

set CMD_LINE_ARGS=%*
goto execute

:4NT_args
@rem Get arguments from the 4NT Shell from JP Software
set CMD_LINE_ARGS=%$

:execute
@rem Setup the command line

set CLASSPATH=%APP_HOME%\gradle\wrapper\gradle-wrapper.jar

@rem Execute Gradle
""%JAVA_EXE%"" %DEFAULT_JVM_OPTS% %JAVA_OPTS% %GRADLE_OPTS% ""-Dorg.gradle.appname=%APP_BASE_NAME%"" -classpath ""%CLASSPATH%"" org.gradle.wrapper.GradleWrapperMain %CMD_LINE_ARGS%

:end
@rem End local scope for the variables with windows NT shell
if ""%ERRORLEVEL%""==""0"" goto mainEnd

:fail
rem Set variable GRADLE_EXIT_CONSOLE if you need the _script_ return code instead of
rem the _cmd.exe /c_ return code!
if  not "" == ""%GRADLE_EXIT_CONSOLE%"" exit 1
exit /b 1

:mainEnd
if ""%OS%""==""Windows_NT"" endlocal

:omega
";

                // Write gradlew.bat file
                File.WriteAllText(filePath, gradlewContent);

                // Rename the file extension to .bat
                string newFilePath = Path.ChangeExtension(filePath, ".bat");
                File.Move(filePath, newFilePath);

                // Display success message
                EditorUtility.DisplayDialog("Success", "File created successfully at: " + newFilePath, "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", "An error occurred: " + e.Message, "OK");
            }
        }



        private void ReimportAssets()
        {
            EditorApplication.ExecuteMenuItem("Assets/Reimport");
            EditorUtility.DisplayDialog("Reimport", "Assets are being reimported.", "OK");
            Close();
        }
    }
}
