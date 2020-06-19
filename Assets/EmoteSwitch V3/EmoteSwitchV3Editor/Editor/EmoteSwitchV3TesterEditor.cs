using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// EmoteSwitchV3Tester v1.0
// created by gatosyocora

namespace Gatosyocora.EmoteSwitchV3Editor
{
    [CustomEditor(typeof(EmoteSwitchV3Tester))]
    public class EmoteSwitchV3TesterEditor : Editor
    {
        private string emote1Name = "EMOTE1";
        private string emote2Name = "EMOTE2";
        private string emote3Name = "EMOTE3";
        private string emote4Name = "EMOTE4";
        private string emote5Name = "EMOTE5";
        private string emote6Name = "EMOTE6";
        private string emote7Name = "EMOTE7";
        private string emote8Name = "EMOTE8";

        public override void OnInspectorGUI()
        {
            var tester = target as EmoteSwitchV3Tester;

            base.OnInspectorGUI();

            if (EditorApplication.isPlaying)
            {
                emote1Name = tester.controller["EMOTE1"].name;
                emote2Name = tester.controller["EMOTE2"].name;
                emote3Name = tester.controller["EMOTE3"].name;
                emote4Name = tester.controller["EMOTE4"].name;
                emote5Name = tester.controller["EMOTE5"].name;
                emote6Name = tester.controller["EMOTE6"].name;
                emote7Name = tester.controller["EMOTE7"].name;
                emote8Name = tester.controller["EMOTE8"].name;
            }

            using (new EditorGUI.DisabledGroupScope(!EditorApplication.isPlaying))
            {
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(emote1Name == "EMOTE1"))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Emote 1 & 2");
                    if (GUILayout.Button(emote1Name))
                    {
                        PlayEmote(1, tester);
                    }
                    if (GUILayout.Button(emote2Name))
                    {
                        PlayEmote(2, tester);
                    }
                }

                using (new EditorGUI.DisabledGroupScope(emote3Name == "EMOTE3"))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Emote 3 & 4");
                    if (GUILayout.Button(emote3Name))
                    {
                        PlayEmote(3, tester);
                    }
                    if (GUILayout.Button(emote4Name))
                    {
                        PlayEmote(4, tester);
                    }
                }

                using (new EditorGUI.DisabledGroupScope(emote5Name == "EMOTE5"))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Emote 5 & 6");
                    if (GUILayout.Button(emote5Name))
                    {
                        PlayEmote(5, tester);
                    }
                    if (GUILayout.Button(emote6Name))
                    {
                        PlayEmote(6, tester);
                    }
                }

                using (new EditorGUI.DisabledGroupScope(emote7Name == "EMOTE7"))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Emote 7 & 8");
                    if (GUILayout.Button(emote7Name))
                    {
                        PlayEmote(7, tester);
                    }
                    if (GUILayout.Button(emote8Name))
                    {
                        PlayEmote(8, tester);
                    }
                }
            }
        }

        private async void PlayEmote(int emoteNumber, EmoteSwitchV3Tester tester)
        {
            float second;
            var emoteClip = tester.controller[$"EMOTE{emoteNumber}"];
            if (!(emoteClip is null) && emoteClip.name != $"EMOTE{emoteNumber}")
            {
                second = tester.controller[$"EMOTE{emoteNumber}"].length;
            }
            else
            {
                second = 1;
            }
            tester.animator.SetInteger("Emote", emoteNumber);
            await Task.Delay((int)(second * 1000));
            tester.animator.SetInteger("Emote", 0);
        }
    }

}

