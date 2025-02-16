using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;  
using System.Collections.Generic;

namespace MobileMonetizationPro
{
    public class ScreenshotTool : EditorWindow
    {
        string name = "Screenshot Name";

        [MenuItem("Tools/Mobile Monetization Pro/Open Screenshot Tool")]

        static void Init()
        {
            ScreenshotTool window = (ScreenshotTool)EditorWindow.GetWindow(typeof(ScreenshotTool), false);

            window.maxSize = new Vector2(512, 155);
            window.minSize = window.maxSize;
            window.title = ("Screenshot Tool!");
            window.Show();

        }

        void OnGUI()
        {
            GUILayout.Label("Name of the Screenshot", EditorStyles.boldLabel);
            name = EditorGUILayout.TextField("Name: ", name);

            if (GUILayout.Button("TAKE SCREENSHOT!"))
            {
                Action();
            }
        }

        void Action()
        {
            ScreenCapture.CaptureScreenshot(name + ".png");
        }
    }
}