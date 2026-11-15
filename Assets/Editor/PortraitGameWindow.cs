using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PixelMoments.Editor
{
    // PlayerSettings does not set the editor's Game View render size.
    [InitializeOnLoad]
    public static class PortraitGameWindow
    {
        static PortraitGameWindow()
        {
            EditorApplication.delayCall += Apply;
            EditorUserBuildSettings.activeBuildTargetChanged += QueueApply;
            EditorApplication.playModeStateChanged += state => {
                if(state == PlayModeStateChange.ExitingEditMode) Apply();
            };
        }
        private static void QueueApply() { EditorApplication.delayCall += Apply; }
        [MenuItem("Pixel Moments/Set entire Game View to 9 x 16")]
        public static void Apply()
        {
            if(Application.isBatchMode) return;
            try {
                // Unity 2022.3 has no public API for Game View resolution. This method
                // sets its actual render target, selected preset and zoom together.
                var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView",true);
                var method=type.GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic,
                    null,new[]{typeof(Vector2),typeof(string)},null);
                if(method==null) throw new MissingMethodException(type.FullName,"SetCustomResolution");
                var window=EditorWindow.GetWindow(type,false,"Game",false);
                method.Invoke(window,new object[]{new Vector2(900,1600),"Pixel Moments Portrait 9:16"});
                window.Repaint();
            } catch(Exception error) {
                Debug.LogWarning("Could not set the entire Game View to 900 x 1600. Select that resolution in the Game toolbar. " + error.GetBaseException().Message);
            }
        }
    }
}
