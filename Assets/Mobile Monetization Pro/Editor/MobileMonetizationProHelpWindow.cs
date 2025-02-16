using UnityEngine;
using UnityEditor;

namespace MobileMonetizationPro
{
    public class MobileMonetizationProHelpWindow : EditorWindow
    {
        [MenuItem("Tools/Mobile Monetization Pro/Help/Open Documentation", false, 6)]
        public static void OpenDocumentation()
        {
            string documentationLink = "https://sites.google.com/view/mobilemonetizationpro/documentation";
            Application.OpenURL(documentationLink);
        }
        [MenuItem("Tools/Mobile Monetization Pro/Help/Open Video Tutorials", false, 6)]
        public static void OpenGettingStartedTutorial()
        {
            string documentationLink = "https://www.youtube.com/playlist?list=PLijV8trSDlm5sVV4rYX5Y6i399DN6_FGp";
            Application.OpenURL(documentationLink);
        }
    }
}
