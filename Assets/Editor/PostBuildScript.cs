using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;

public class PostBuildScript
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.Android)
        {
            Process process = new Process();
            process.StartInfo.FileName = "adb";
            process.StartInfo.Arguments = $"install -r \"{pathToBuiltProject}\"";
            process.Start();
        }
    }
} 