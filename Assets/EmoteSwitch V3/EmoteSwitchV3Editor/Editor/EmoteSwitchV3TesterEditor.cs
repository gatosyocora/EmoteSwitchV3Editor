using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EmoteSwitchV3Tester))]
public class EmoteSwitchV3TesterEditor : Editor
{
    private bool startWaiting = false;

    public override void OnInspectorGUI()
    {
        var tester = target as EmoteSwitchV3Tester;

        base.OnInspectorGUI();
        using (new EditorGUI.DisabledGroupScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Reset"))
            {
                tester.animator.SetInteger("Emote", 0);
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emote1&2");
                if (GUILayout.Button("ON"))
                {
                    tester.animator.SetInteger("Emote", 1);
                }
                if (GUILayout.Button("OFF"))
                {
                    tester.animator.SetInteger("Emote", 2);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emote3&4");
                if (GUILayout.Button("ON"))
                {
                    tester.animator.SetInteger("Emote", 3);
                }
                if (GUILayout.Button("OFF"))
                {
                    tester.animator.SetInteger("Emote", 4);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emote5&6");
                if (GUILayout.Button("ON"))
                {
                    tester.animator.SetInteger("Emote", 5);
                }
                if (GUILayout.Button("OFF"))
                {
                    tester.animator.SetInteger("Emote", 6);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emote7&8");
                if (GUILayout.Button("ON"))
                {
                    tester.animator.SetInteger("Emote", 7);
                }
                if (GUILayout.Button("OFF"))
                {
                    tester.animator.SetInteger("Emote", 8);
                }
            }
        }
    }
}
