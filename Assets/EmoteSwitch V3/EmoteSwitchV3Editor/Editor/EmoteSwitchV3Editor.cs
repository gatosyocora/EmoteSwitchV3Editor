using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using System.IO;

// ver 1.3
// created by gatosyocora

public class EmoteSwitchV3Editor : EditorWindow {

    private GameObject targetObject = null;
    private List<GameObject> m_props;
    private List<bool> propStartStates;
    private List<bool> isLocal;

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
    private const string JOINT_PATH_IN_PREFAB = "Joint"; // Prefab内のJointまでのパス

    private const string PREFAB1_PATH = "/EmoteSwitchV3Editor/EmoteSwitch V3_Editor.prefab"; // Prefabのファイルパス

    private const string EMOTE_OFF_ANIMFILE_PATH = "/V3 Prefab/Emote_OFF/[1]Emote_OFF.anim"; // OFFにするキーが入ったコピー元のAnimationファイルのパス
    private const string EMOTE_ON_ANIMFILE_PATH = "/V3 Prefab/Emote_ON/[1]Emote_ON.anim"; // ONにするキーが入ったコピー元のAnimationファイルのパス

    private const string EMOTESWITCH_CONTROLLER_PATH = "/ToggleSwitch/Switch.controller"; // Toggle1のAnimatorに設定するAnimatorControllerまでのパス

    private const string IDLE_ANIMATION_NAME = "IDLE"; // Emoteアニメーションとして参照するアニメーションの名前

    private const string LOCAL_SYSTEM_PREFAB_PATH = "/Local_System/Prefab/Local_system.prefab"; // LocalSystemのPrefabのファイルパス
    private const string LOCAL_ROOT_BELOW_OBJECT_NAME = "Local_On_Switch"; // LocalSystem内にあるアバター直下に置くオブジェクトの名前
    private const string OBJECT_PATH_IN_LOCAL_SYSTEM = "On_Animation_Particle/On_Object/After_On/Object"; // LocalSystem内のObjectまでのパス

    /***** 必要に応じてここまでの値を変更する *****/

    private string emoteSwitchV3EditorFolderPath;
    private string idleAniamtionFbxPath;

    private VRC_AvatarDescriptor m_avatar = null;
    private AnimatorOverrideController standingAnimController = null;

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

    private bool useLocal = false;

    private string savedFolderPath;

    private const char BSLASH = '\\';
    private const string EMOTE_SWITCH_SAVED_FOLDER = "ESV3Animation";

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

        isLocal = new List<bool>();
        isLocal.Add(true);

        emoteSwitchV3EditorFolderPath = GetEmoteSwitchV3EditorFolderPath();
        idleAniamtionFbxPath = GetIdleAnimationFbxPath();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            m_avatar = EditorGUILayout.ObjectField(
                "Avatar",
                m_avatar,
                typeof(VRC_AvatarDescriptor),
                true
            ) as VRC_AvatarDescriptor;
        }
        if (EditorGUI.EndChangeCheck())
        {
            isSettingAvatar = (m_avatar != null);
            GetAvatarInfo(m_avatar);
            savedFolderPath = GetSavedFolderPath(m_avatar.CustomStandingAnims);
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
                isLocal.Add(true);
            }
            if (GUILayout.Button("-"))
            {
                if (m_props.Count > 1)
                {
                    m_props.RemoveAt(m_props.Count - 1);
                    propStartStates.RemoveAt(m_props.Count - 1);
                    isLocal.RemoveAt(m_props.Count - 1);
                }
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Default State");
            GUILayout.FlexibleSpace();

            if (useLocal)
            {
                EditorGUILayout.LabelField("Local", GUILayout.Width(35f));
            }
        }
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

                    if (useLocal)
                    {
                        isLocal[i] = EditorGUILayout.ToggleLeft("", isLocal[i], GUILayout.Width(30f));
                    }
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
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("SaveFolder", savedFolderPath);

            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                savedFolderPath = EditorUtility.OpenFolderPanel("Select saved folder", "Assets/", string.Empty);
                savedFolderPath = FileUtil.GetProjectRelativePath(savedFolderPath) + "/";
                if (savedFolderPath == "/" && m_avatar != null)
                {
                    savedFolderPath = GetSavedFolderPath(m_avatar.CustomStandingAnims);
                }
                else if (savedFolderPath == "/")
                {
                    savedFolderPath = GetSavedFolderPath(null);
                }
            }
        }

        isOpeningAdvancedSetting = EditorGUILayout.Foldout(isOpeningAdvancedSetting, "Advanced Setting");
        if (isOpeningAdvancedSetting)
        {
            EditorGUI.indentLevel++;
            useIdleAnim = EditorGUILayout.Toggle("Use IDLE Animation", useIdleAnim);
            useLocal = EditorGUILayout.Toggle("Use Local EmoteSwitch", useLocal);
            EditorGUI.indentLevel--;
        }


        EditorGUI.BeginDisabledGroup(!(isSettingAvatar && isSettingProp && (standingAnimController != null) && (m_avatar != null)));
        {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Set EmoteSwitch"))
                {
                    SetEmoteSwitchV3(targetObject, m_props, savedFolderPath);
                }
            }
        }
        EditorGUI.EndDisabledGroup();

        using (new EditorGUI.DisabledGroupScope(!Undo.GetCurrentGroupName().StartsWith(UNDO_TEXT)))
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Rever before Set EmoteSwitch"))
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
    private void SetEmoteSwitchV3(GameObject avatarObj, List<GameObject> props, string savedFolderPath)
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

            var savedFilePath = savedFolderPath + objName;
            emoteOnAnimClip = CreateAnimationClip(savedFilePath, EMOTE_ON);
            emoteOffAnimClip = CreateAnimationClip(savedFilePath, EMOTE_OFF);
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

            // 名前の重複を防ぐ
            var maxNum = GetSameNameObjectsMaxNumInBrother(emoteSwitchObj);
            if (maxNum >= 0)
            {
                emoteSwitchObj.name += " " + (maxNum + 1);
            }

            // EmoteSwitchV3にpropObjを設定する
            var objectTrans = emoteSwitchObj.transform.Find(OBJECT_PATH_IN_PREFAB);
            Undo.SetTransformParent(propObj.transform, objectTrans, propObj.name + " SetParent to " + objectTrans.name);
            objectTrans.gameObject.SetActive(propStartState);

            GameObject localSystemObj = null;
            if (useLocal && isLocal[i])
            {
                // LocalSystemを設定する
                var localSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(emoteSwitchV3EditorFolderPath + LOCAL_SYSTEM_PREFAB_PATH);
                localSystemObj = Instantiate(localSystemPrefab, propObj.transform.position, Quaternion.identity) as GameObject;
                localSystemObj.name = "EmoteSwitchV3_Local_" + propObj.name;
                Undo.RegisterCreatedObjectUndo(localSystemObj, "Instantiate " + localSystemObj.name);
                Undo.SetTransformParent(localSystemObj.transform, parentTrans, localSystemObj.name + " SetParent to " + parentTrans.name);

                if (PrefabUtility.IsPartOfPrefabInstance(localSystemObj))
                {
                    PrefabUtility.UnpackPrefabInstance(localSystemObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                var localOnSwitchTrans = localSystemObj.transform.Find(LOCAL_ROOT_BELOW_OBJECT_NAME);
                Undo.SetTransformParent(localOnSwitchTrans, avatarObj.transform, localOnSwitchTrans.name + " SetParent to " + avatarObj.name);
                localOnSwitchTrans.localPosition = Vector3.zero;
                localOnSwitchTrans.localRotation = Quaternion.identity;
                localOnSwitchTrans.localScale = Vector3.one;

                var objectInLocalSystemTrans = localSystemObj.transform.Find(OBJECT_PATH_IN_LOCAL_SYSTEM);
                Undo.SetTransformParent(emoteSwitchObj.transform, objectInLocalSystemTrans, emoteSwitchObj.name + " SetParent to " + objectInLocalSystemTrans.name);
            }

            // JointやIK_Followerがついたオブジェクトを非アクティブにすると壊れるので回避
            var joints = propObj.GetComponents<Joint>();
            var followers = propObj.GetComponents<VRC_IKFollower>();
            var jointTrans = emoteSwitchObj.transform.Find(JOINT_PATH_IN_PREFAB);

            if (joints.Length > 0 || followers.Length > 0)
            {
                if (useLocal && isLocal[i])
                {
                    var jointObj = new GameObject("EmoteSwitchV3_Local_" + propObj.name + "_Joint");
                    Undo.RegisterCreatedObjectUndo(jointObj, "Create " + jointObj.name);
                    jointTrans = jointObj.transform;
                    var localSystemParent = localSystemObj.transform.parent;
                    jointTrans.SetPositionAndRotation(localSystemParent.position, localSystemParent.rotation);
                    Undo.SetTransformParent(jointTrans, localSystemParent, jointTrans.name + " SetParent to " + localSystemParent.name);
                    Undo.SetTransformParent(localSystemObj.transform, jointTrans, localSystemObj.name + " SetParent to " + jointTrans.name);
                }

                if (joints.Length > 0)
                {
                    var rigidBody = propObj.GetComponent<Rigidbody>();
                    CopyComponent(jointTrans.gameObject, rigidBody);
                    Undo.DestroyObjectImmediate(rigidBody);
                }

                foreach (var joint in joints)
                {
                    CopyComponent(jointTrans.gameObject, joint);
                    Undo.DestroyObjectImmediate(joint);
                }

                foreach (var follower in followers)
                {
                    CopyComponent(jointTrans.gameObject, follower);
                    Undo.DestroyObjectImmediate(follower);
                }
            }

            // EmoteAnimationを作成する
            var toggleObj = emoteSwitchObj.transform.Find(TOOGLE1_PATH_IN_PREFAB).gameObject;
            var animator = toggleObj.GetComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(emoteSwitchV3EditorFolderPath + EMOTESWITCH_CONTROLLER_PATH);
            animator.runtimeAnimatorController = controller;
            // 初期状態がActiveなら非Activeにする(propStartState==Active(true) -> TO_INACTIVE)
            if (emoteOnAnimClip == null)
            {
                emoteOnAnimClip = CreateEmoteAnimClip(EMOTE_ON, savedFolderPath + propObj.name, toggleObj, (propStartState) ? TO_INACTIVE : TO_ACTIVE);
            }
            else
            {
                AddEmoteAnimClip(ref emoteOnAnimClip, toggleObj, (propStartState) ? TO_INACTIVE : TO_ACTIVE, emoteAnimTime);
            }
            // 初期状態がActiveならActiveにする(propStartState==Active(true) -> TO_ACTIVE)
            if (emoteOffAnimClip == null)
            {
                emoteOffAnimClip = CreateEmoteAnimClip(EMOTE_OFF, savedFolderPath + propObj.name, toggleObj, (propStartState) ? TO_ACTIVE : TO_INACTIVE);
            }
            else
            {
                AddEmoteAnimClip(ref emoteOffAnimClip, toggleObj, (propStartState) ? TO_ACTIVE : TO_INACTIVE, emoteAnimTime);
            }
        }

        // EmoteAnimationを設定する
        Undo.RecordObject(standingAnimController, "Set Animation For EmoteSwitch to Controller");
        standingAnimController[EMOTE_NAMES[(int)selectedOnOffEmote * 2]] = emoteOnAnimClip;
        standingAnimController[EMOTE_NAMES[(int)selectedOnOffEmote * 2 + 1]] = emoteOffAnimClip;

        Undo.SetCurrentGroupName(UNDO_TEXT + avatarObj.name);
    }

    /// <summary>
    /// アバターの情報を取得する
    /// </summary>
    private void GetAvatarInfo(VRC_AvatarDescriptor avatar)
    {
        if (avatar == null) return;

        targetObject = avatar.gameObject;

        standingAnimController = avatar.CustomStandingAnims;
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
        {
            if (props[i] != null)
            {
                return true;
            }
        }

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
    private AnimationClip CreateEmoteAnimClip(int fileType, string savedFilePath, GameObject targetObj, int emoteType)
    {
        var animClip = CreateAnimationClip(savedFilePath, fileType);

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
    private void AddEmoteAnimClip(ref AnimationClip animClip, GameObject targetObj, int emoteType, float offsetTime)
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
    private AnimationClip CreateAnimationClip(string savedFilePath, int fileType)
    {
        AnimationClip animClip = new AnimationClip();

        savedFilePath += ((fileType == EMOTE_ON) ? "_ON" : "_OFF") + ".anim";

        CreateNoExistFolders(savedFilePath);

        AssetDatabase.CreateAsset(animClip, 
            AssetDatabase.GenerateUniqueAssetPath(savedFilePath));
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

    // 特定のオブジェクトにコンポーネントを値ごとコピーする
    private void CopyComponent(GameObject targetObj, Component fromComp)
    {
        ComponentUtility.CopyComponent(fromComp);
        ComponentUtility.PasteComponentAsNew(targetObj);
    }

    /// <summary>
    /// 保存先のフォルダのパスを取得する
    /// </summary>
    /// <param name="controller"></param>
    /// <returns></returns>
    private string GetSavedFolderPath(AnimatorOverrideController controller)
    {
        if (controller == null) return "Assets/"+EMOTE_SWITCH_SAVED_FOLDER+"/";

        var filePath = AssetDatabase.GetAssetPath(controller);
        return Path.GetDirectoryName(filePath).Replace('\\', '/') + "/"+EMOTE_SWITCH_SAVED_FOLDER+"/";
    }

    /// <summary>
    /// パス内で存在しないフォルダを作成する
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool CreateNoExistFolders(string path)
    {
        string directoryPath;
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directoryPath = path;
        }
        else
        {
            directoryPath = Path.GetDirectoryName(path);
        }

        if (!Directory.Exists(directoryPath))
        {
            var directories = directoryPath.Split(BSLASH);

            directoryPath = "Assets";
            for (int i = 1; i < directories.Length; i++)
            {
                if (!Directory.Exists(directoryPath + BSLASH + directories[i]))
                {
                    AssetDatabase.CreateFolder(directoryPath, directories[i]);
                }

                directoryPath += BSLASH + directories[i];
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        return false;
    }
}
