using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using System.Linq;
using System.Text.RegularExpressions;

// ver 1.2
// created by gatosyocora

public class EmoteSwitchV3Editor : EditorWindow {

    private GameObject targetObject = null;
    private List<GameObject> m_props;
    private List<bool> propStartStates;

    private AnimationClip emoteAnimClip = null;
    
    private bool isSettingAvatar = false;
    private bool isSettingProp = false;

    private const int EMOTE_ON = 0;
    private const int EMOTE_OFF = 1;

    private const int TO_ACTIVE = 1;
    private const int TO_INACTIVE = 0;

    /***** 必要に応じてここからの値を変更する *****/

    private const string OBJECT_PATH_IN_PREFAB = "Joint/Toggle1/Object"; // Prefab内のObjectまでのパス
    private const string TOOGLE1_PATH_IN_PREFAB = "Joint/Toggle1"; // Prefab内のToogle1までのパス

    private const string PREFAB1_PATH = "/EmoteSwitchV3Editor/EmoteSwitch V3_Editor.prefab"; // Prefabのファイルパス

    private const string EMOTE_OFF_ANIMFILE_PATH = "/V3 Prefab/Emote_OFF/[1]Emote_OFF.anim"; // OFFにするキーが入ったコピー元のAnimationファイルのパス
    private const string EMOTE_ON_ANIMFILE_PATH = "/V3 Prefab/Emote_ON/[1]Emote_ON.anim"; // ONにするキーが入ったコピー元のAnimationファイルのパス

    private const string EMOTESWITCH_CONTROLLER_PATH = "/ToggleSwitch/Switch.controller"; // Toggle1のAnimatorに設定するAnimatorControllerまでのパス

    private const string SAVE_FOLDER_PATH = "/EmoteSwitchV3Editor/Animations/"; // 生成されるAnimationファイルが保存されるフォルダ

    private const string IDLE_ANIMATION_NAME = "IDLE"; // Emoteアニメーションとして参照するアニメーションの名前

    /***** 必要に応じてここまでの値を変更する *****/

    private string emoteSwitchV3EditorFolderPath;
    private string idleAniamtionFbxPath;

    private VRC_AvatarDescriptor m_avatar = null;
    private AnimatorOverrideController standingAnimController = null;
    private AnimatorOverrideController sittingAnimController = null;

    private readonly string[] EMOTE_NAMES = { "EMOTE1", "EMOTE2", "EMOTE3", "EMOTE4", "EMOTE5", "EMOTE6", "EMOTE7", "EMOTE8" };

    private enum EMOTES
    {
        EMOTE1and2,
        EMOTE3and4,
        EMOTE5and6,
        EMOTE7ands8,
    };

    private EMOTES selectedOnOffEmote = EMOTES.EMOTE1and2;

    //  Advanced Setting
    private bool isOpeningAdvancedSetting = false;

    private enum SWITCH_TIMING
    {
        BEFORE,
        AFTER,
    };
    private SWITCH_TIMING timing = SWITCH_TIMING.AFTER;

    private bool useIdleAnim = true;

    private const string UNDO_TEXT = "SetEmoteSwitchV3 to ";


    [MenuItem("EmoteSwitch/EmoteSwitchV3 Editor")]
    private static void Create()
    {
        GetWindow<EmoteSwitchV3Editor>("EmoteSwitchV3 Editor");
    }

    private void OnEnable()
    {
        m_props = new List<GameObject>();
        m_props.Add(null);

        propStartStates = new List<bool>();
        propStartStates.Add(false);

        emoteSwitchV3EditorFolderPath = GetEmoteSwitchV3EditorFolderPath();
        idleAniamtionFbxPath = GetIdleAnimationFbxPath();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            targetObject = EditorGUILayout.ObjectField(
                "Avatar",
                targetObject,
                typeof(GameObject),
                true
            ) as GameObject;
        }
        if (EditorGUI.EndChangeCheck())
        {
            isSettingAvatar = (targetObject != null);
            GetAvatarInfo(targetObject);
        }

        // VRC_AvatarDescripterが設定されていない時の例外処理
        if (targetObject != null && m_avatar == null)
            EditorGUILayout.HelpBox("Set VRC_AvatarDescripter to Avatar object", MessageType.Warning);

        // Props
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Prop Objects", EditorStyles.boldLabel);
            if (GUILayout.Button("+"))
            {
                m_props.Add(null);
                propStartStates.Add(false);
            }
            if (GUILayout.Button("-"))
            {
                if (m_props.Count > 1)
                {
                    m_props.RemoveAt(m_props.Count - 1);
                    propStartStates.RemoveAt(m_props.Count - 1);
                }
            }
        }
        EditorGUILayout.LabelField("Default State");
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        {
            for (int i = 0; i < m_props.Count; i++)
            {

                using (new EditorGUILayout.HorizontalScope())
                {
                    propStartStates[i] = EditorGUILayout.Toggle(propStartStates[i], GUILayout.MinWidth(30), GUILayout.MaxWidth(30));

                    m_props[i] = EditorGUILayout.ObjectField(
                        "Prop " + (i + 1),
                        m_props[i],
                        typeof(GameObject),
                        true
                    ) as GameObject;
                }

            }
        }
        if (EditorGUI.EndChangeCheck())
        {
            isSettingProp = CheckSettingProp(m_props);
        }
        EditorGUI.indentLevel--;


        if (targetObject != null)
        {
            // VRC_AvatarDescripterに設定してあるAnimator
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Standing Anims", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                {
                    standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Anims",
                        standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_avatar.CustomStandingAnims = standingAnimController;
                }
                EditorGUI.indentLevel--;
            }

            // CustomStandingAnimsが設定されていない時の例外処理
            if (targetObject != null && m_avatar != null && standingAnimController == null)
            {
                EditorGUILayout.HelpBox("Set Custom Standing Anims in VRC_AvatarDescripter", MessageType.Warning);
            }

            // Standing AnimatorのEmoteに設定してあるAnimationファイル
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Emote(Standing Anims)", EditorStyles.boldLabel);
            {
                // どのEmoteに設定するか選ぶ
                selectedOnOffEmote = (EMOTES)EditorGUILayout.EnumPopup("ON & OFF Emote", selectedOnOffEmote);

                EditorGUI.indentLevel++;
                for (int i = 0; i < EMOTE_NAMES.Length; i++)
                {
                    if (standingAnimController == null || standingAnimController[EMOTE_NAMES[i]].name == EMOTE_NAMES[i])
                    {
                        AnimationClip dummyAnim;
                        dummyAnim = EditorGUILayout.ObjectField(
                            EMOTE_NAMES[i],
                            null,
                            typeof(AnimationClip),
                            true
                        ) as AnimationClip;
                    }
                    else
                    {
                        standingAnimController[EMOTE_NAMES[i]] = EditorGUILayout.ObjectField(
                            EMOTE_NAMES[i],
                            standingAnimController[EMOTE_NAMES[i]],
                            typeof(AnimationClip),
                            true
                        ) as AnimationClip;
                    }
                }
                EditorGUI.indentLevel--;
            }

            isOpeningAdvancedSetting = EditorGUILayout.Foldout(isOpeningAdvancedSetting, "Advanced Setting");
            if (isOpeningAdvancedSetting)
            {
                EditorGUI.indentLevel++;
                useIdleAnim = EditorGUILayout.Toggle("Use IDLE Animation", useIdleAnim);
                EditorGUI.indentLevel--;
            }

        }

        EditorGUI.BeginDisabledGroup(!(isSettingAvatar && isSettingProp && (standingAnimController != null) && (m_avatar != null)));
        {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Set EmoteSwitch"))
                {
                    SetEmoteSwitchV3(targetObject, m_props);
                }
            }
        }
        EditorGUI.EndDisabledGroup();

        using (new EditorGUI.DisabledGroupScope(!Undo.GetCurrentGroupName().StartsWith(UNDO_TEXT)))
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Undo before Set EmoteSwitch"))
            {
                Undo.PerformUndo();
            }
        }
    }

    /// <summary>
    /// EmoteSwitchを作成する
    /// </summary>
    /// <param name="avatarObj"></param>
    /// <param name="props"></param>
    private void SetEmoteSwitchV3(GameObject avatarObj, List<GameObject> props)
    {
        Undo.RegisterCompleteObjectUndo(avatarObj, UNDO_TEXT + avatarObj.name);

        AnimationClip emoteOnAnimClip = null, emoteOffAnimClip = null;
        float emoteAnimTime = 0f;

        if (emoteAnimClip != null)
        {
            var objName = avatarObj.name;

            foreach (var prop in props)
            {
                if (prop != null)
                {
                    objName = prop.name;
                    break;
                }
            }

            emoteOnAnimClip = CreateAnimationClip(objName, EMOTE_ON);
            emoteOffAnimClip = CreateAnimationClip(objName, EMOTE_OFF);
            CopyAnimationKeys(emoteAnimClip, emoteOnAnimClip);
            CopyAnimationKeys(emoteAnimClip, emoteOffAnimClip);

            emoteAnimTime = GetAnimationTime(emoteAnimClip);
        }

        // PrefabのInstanceに対してSetParentする場合はUnpackする必要がある
        // 2018からのPrefabシステムからSetParent時に自動的にUnPackしなくなった
        if (PrefabUtility.IsPartOfPrefabInstance(avatarObj))
        {
            PrefabUtility.UnpackPrefabInstance(avatarObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        for (int i = 0; i < props.Count; i++)
        {
            var propObj = props[i];

            // Propが未設定なら次へ
            if (propObj == null) continue;

            var propStartState = propStartStates[i];

            // propObjと同じ位置にEmoteSwitchV3を作成する
            var parentTrans = propObj.transform.parent;
            var emoteSwitchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(emoteSwitchV3EditorFolderPath + PREFAB1_PATH);
            var emoteSwitchObj = Instantiate(emoteSwitchPrefab, propObj.transform.position, Quaternion.identity) as GameObject;
            emoteSwitchObj.name = "EmoteSwitch V3_" + propObj.name;
            Undo.RegisterCreatedObjectUndo(emoteSwitchObj, "Instantiate " + emoteSwitchObj.name);
            Undo.SetTransformParent(emoteSwitchObj.transform, parentTrans, emoteSwitchObj.name + " SetParent to " + parentTrans.name);
            emoteSwitchObj.transform.SetParent(parentTrans);

            // 名前の重複を防ぐ
            var maxNum = GetSameNameObjectsMaxNumInBrother(emoteSwitchObj);
            if (maxNum >= 0)
                emoteSwitchObj.name += " "+(maxNum+1);

            // EmoteSwitchV3にpropObjを設定する
            var objectTrans = emoteSwitchObj.transform.Find(OBJECT_PATH_IN_PREFAB);
            Undo.SetTransformParent(propObj.transform, objectTrans, propObj.name + " SetParent to " + objectTrans.name);
            objectTrans.gameObject.SetActive(propStartState);

            // EmoteAnimationを作成する
            var toggleObj = emoteSwitchObj.transform.Find(TOOGLE1_PATH_IN_PREFAB).gameObject;
            var animator = toggleObj.GetComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(emoteSwitchV3EditorFolderPath + EMOTESWITCH_CONTROLLER_PATH);
            animator.runtimeAnimatorController = controller;
            // 初期状態がActiveなら非Activeにする(propStartState==Active(true) -> TO_INACTIVE)
            if (emoteOnAnimClip == null)
                emoteOnAnimClip = CreateEmoteAnimClip(EMOTE_ON, propObj.name, toggleObj, (propStartState)?TO_INACTIVE:TO_ACTIVE);
            else
                AddEmoteAnimClip(ref emoteOnAnimClip, propObj.name, toggleObj, (propStartState) ? TO_INACTIVE : TO_ACTIVE, emoteAnimTime);
            // 初期状態がActiveならActiveにする(propStartState==Active(true) -> TO_ACTIVE)
            if (emoteOffAnimClip == null)
                emoteOffAnimClip = CreateEmoteAnimClip(EMOTE_OFF, propObj.name, toggleObj, (propStartState) ? TO_ACTIVE : TO_INACTIVE);
            else
                AddEmoteAnimClip(ref emoteOffAnimClip, propObj.name, toggleObj, (propStartState) ? TO_ACTIVE : TO_INACTIVE, emoteAnimTime);

        }

        // EmoteAnimationを設定する
        standingAnimController[EMOTE_NAMES[(int)selectedOnOffEmote * 2]] = emoteOnAnimClip;
        standingAnimController[EMOTE_NAMES[(int)selectedOnOffEmote * 2 + 1]] = emoteOffAnimClip;

        Undo.SetCurrentGroupName(UNDO_TEXT + avatarObj.name);
    }

    /// <summary>
    /// アバターの情報を取得する
    /// </summary>
    private void GetAvatarInfo(GameObject obj)
    {
        if (obj == null) return;

        m_avatar = obj.GetComponent<VRC_AvatarDescriptor>();
        if (m_avatar == null) return;

        standingAnimController = m_avatar.CustomStandingAnims;
        sittingAnimController = m_avatar.CustomSittingAnims;
    }

    /// <summary>
    /// Propsに設定されているか調べる
    /// </summary>
    /// <param name="props"></param>
    /// <returns></returns>
    private bool CheckSettingProp(List<GameObject> props)
    {
        if (props == null) return false;

        for (int i = 0; i < props.Count; i++)
            if (props[i] != null)
               return true;

        return false;
    }

    /// <summary>
    /// EmoteSwitch用のEmoteAnimationClipを作成する
    /// </summary>
    /// <param name="fileType">ON用のAnimationファイルなのかOFF用のAnimationファイルなのか</param>
    /// <param name="propName">出し入れするオブジェクトの名前</param>
    /// <param name="targetObj">Emoteで操作するオブジェクト(Toggle1)</param>
    /// <param name="emoteType">EmoteAnimationでON状態にするのかOFF状態にするのか</param>
    /// <returns></returns>
    private AnimationClip CreateEmoteAnimClip(int fileType, string propName, GameObject targetObj, int emoteType)
    {
        var animClip = CreateAnimationClip(propName, fileType);

        if (useIdleAnim)
        {
            var idleAnimClip = GetAnimationClipFromFbx(idleAniamtionFbxPath, IDLE_ANIMATION_NAME);
            CopyAnimationKeys(idleAnimClip, animClip);
        }

        string path = GetHierarchyPath(targetObj);

        string animFilePath = emoteSwitchV3EditorFolderPath;
        if (emoteType == TO_INACTIVE)
        {
            animFilePath += EMOTE_OFF_ANIMFILE_PATH;
        }
        else
        {
            animFilePath += EMOTE_ON_ANIMFILE_PATH;
        }

        var originClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFilePath);
        var bindings = AnimationUtility.GetCurveBindings(originClip).ToArray();

        for (int i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];

            binding.path = path;

            // AnimationClipよりAnimationCurveを取得
            var curve = AnimationUtility.GetEditorCurve(originClip, bindings[i]);
            // AnimationClipにキーリダクションを行ったAnimationCurveを設定
            AnimationUtility.SetEditorCurve(animClip, binding, curve);
        }
        
        return animClip;
    }

    /// <summary>
    /// 特定のAnimationファイルにEmoteSwitch用のアニメーションキーを追加する
    /// </summary>
    /// <param name="animClip"></param>
    /// <param name="propName"></param>
    /// <param name="targetObj"></param>
    /// <param name="emoteType"></param>
    private void AddEmoteAnimClip(ref AnimationClip animClip, string propName, GameObject targetObj, int emoteType, float offsetTime)
    {

        string path = GetHierarchyPath(targetObj);

        string animFilePath = emoteSwitchV3EditorFolderPath;
        if (emoteType == TO_INACTIVE)
        {
            animFilePath += EMOTE_OFF_ANIMFILE_PATH;
        }
        else
        {
            animFilePath += EMOTE_ON_ANIMFILE_PATH;
        }

        var originClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animFilePath);

        float animTime = 0f;
        if (offsetTime > 0f)
        {
            animTime = GetAnimationTime(originClip);
        }

        var diffTime = offsetTime - animTime;

        var bindings = AnimationUtility.GetCurveBindings(originClip).ToArray();
        for (int i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];

            binding.path = path;

            // AnimationClipよりAnimationCurveを取得
            var curve = AnimationUtility.GetEditorCurve(originClip, bindings[i]);

            if (timing == SWITCH_TIMING.AFTER && offsetTime > 0f)
            {
                // Offset分だけずらす
                var keysNum = curve.keys.Length;
                int index = 0;
                for (int j = keysNum-1; j >= 0; j--)
                {
                    var key = curve.keys[j];
                    key.time += diffTime;
                    index = curve.MoveKey(j, key);
                }

                var tangentKey = new Keyframe(curve.keys[index].time, curve.keys[index].value, 1.5708f, -1.5708f);
                index = curve.MoveKey(index, tangentKey);
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Free);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Free);

                index = curve.AddKey(0f, curve.keys[index].value);
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Free);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Free);
            }

            // AnimationClipにキーリダクションを行ったAnimationCurveを設定
            AnimationUtility.SetEditorCurve(animClip, binding, curve);
        }
    }

    /// <summary>
    /// originClipに設定されたAnimationKeyをすべてtargetclipにコピーする
    /// </summary>
    /// <param name="originClip"></param>
    /// <param name="targetClip"></param>
    public static void CopyAnimationKeys(AnimationClip originClip, AnimationClip targetClip)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(originClip).ToArray())
        {
            // AnimationClipよりAnimationCurveを取得
            AnimationCurve curve = AnimationUtility.GetEditorCurve(originClip, binding);
            // AnimationClipにキーリダクションを行ったAnimationCurveを設定
            AnimationUtility.SetEditorCurve(targetClip, binding, curve);
        }
    }

    /// <summary>
    /// 特定のオブジェクトまでのパスを取得する
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (parent.parent == null) return path;

            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    /// <summary>
    /// アニメーションの長さを取得する
    /// </summary>
    /// <param name="anim"></param>
    /// <returns></returns>
    private float GetAnimationTime(AnimationClip anim)
    {
        float animTime = 0f;

        var bindings = AnimationUtility.GetCurveBindings(anim).ToArray();

        foreach (var binding in bindings)
        {
            var keys = AnimationUtility.GetEditorCurve(anim, binding).keys;

            var lastKey = keys[keys.Length - 1];
            if (animTime < lastKey.time)
                animTime = lastKey.time;
        }

        return animTime;
    }

    /// <summary>
    /// fbxから特定のAnimationClipの情報を取得する
    /// </summary>
    /// <param name="fbxPath"></param>
    /// <param name="animName"></param>
    /// <returns></returns>
    private AnimationClip GetAnimationClipFromFbx(string fbxPath, string animName)
    {
        var anims = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        var animObj = System.Array.Find<Object>(anims, item => item is AnimationClip && item.name == animName);

        var animClip = Object.Instantiate(animObj) as AnimationClip;

        return animClip;
    }

    /// <summary>
    /// アニメーションファイルを作成する
    /// </summary>
    /// <param name="objName"></param>
    /// <param name="fileType"></param>
    /// <returns></returns>
    private AnimationClip CreateAnimationClip(string objName, int fileType)
    {
        AnimationClip animClip = new AnimationClip();

        string fileName = objName + ((fileType == EMOTE_ON) ? "_ON" : "_OFF");

        AssetDatabase.CreateAsset(animClip, 
            AssetDatabase.GenerateUniqueAssetPath(emoteSwitchV3EditorFolderPath + SAVE_FOLDER_PATH + fileName + ".anim"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return animClip;
    }

    /// <summary>
    /// 兄弟にある同じ名称のオブジェクトの後ろの数字の最大値を取得する
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private int GetSameNameObjectsMaxNumInBrother(GameObject obj)
    {
        var pattern = @"^"+obj.name+" ?[0-9]*";
        var parentTrans = obj.transform.parent;
        var brotherTrans = parentTrans.gameObject.transform;

        int num = -1;

        foreach (Transform brother in brotherTrans)
        {
            var brotherObjName = brother.gameObject.name;

            if (Regex.IsMatch(brotherObjName, pattern) && (obj.transform != brother))
            {
                var matchNumbers = Regex.Matches(brotherObjName, @"[0-9]+");
                if (matchNumbers.Count >= 2)
                {
                    var lastNum = int.Parse(matchNumbers[matchNumbers.Count - 1].Value);
                    if (num < lastNum) num = lastNum;
                }
                else if (num < 0)
                {
                    num = 0;
                }
            }
        }

        return num;

    }

    private string GetAssetPathForSearch(string filter)
    {
        var guid = AssetDatabase.FindAssets(filter).FirstOrDefault();
        return AssetDatabase.GUIDToAssetPath(guid);
    }

    /// <summary>
    /// EmoteSwitchV3のフォルダパスを取得する（Assets/...）
    /// </summary>
    /// <returns></returns>
    private string GetEmoteSwitchV3EditorFolderPath()
    {
        return GetAssetPathForSearch("EmoteSwitch V3 t:Folder");
    }

    /// <summary>
    /// Idleアニメーションを参照するFbxのパスを取得する（Assets/...）
    /// </summary>
    /// <returns></returns>
    private string GetIdleAnimationFbxPath()
    {
        return GetAssetPathForSearch("Male_Standing_Pose t:Model");
    }
}
