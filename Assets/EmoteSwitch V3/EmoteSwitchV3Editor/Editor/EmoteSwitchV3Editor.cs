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

namespace Gatosyocora.EmoteSwitchV3Editor
{
    public class EmoteSwitchV3Editor : EditorWindow
    {
        public class Prop
        {
            public GameObject obj { get; set; }
            public bool defaultState { get; set; } = false;
            public bool isLocalEmoteSwitch { get; set; } = false;

            public Prop(GameObject obj)
            {
                this.obj = obj;
                defaultState = false;
                isLocalEmoteSwitch = true;
            }
        }

        private GameObject targetObject = null;
        private List<Prop> propList;

        private enum EMOTE_TYPE
        {
            ON, OFF
        }

        private enum TO_STATE
        {
            ACTIVE, INACTIVE
        }

        /***** 必要に応じてここからの値を変更する *****/

        private static readonly string OBJECT_PATH_IN_PREFAB = "Joint/Toggle1/Object"; // Prefab内のObjectまでのパス
        private static readonly string TOOGLE1_PATH_IN_PREFAB = "Joint/Toggle1"; // Prefab内のToogle1までのパス
        private static readonly string JOINT_PATH_IN_PREFAB = "Joint"; // Prefab内のJointまでのパス

        private static readonly string PREFAB1_PATH = "/EmoteSwitchV3Editor/EmoteSwitch V3_Editor.prefab"; // Prefabのファイルパス

        private static readonly string EMOTE_OFF_ANIMFILE_PATH = "/V3 Prefab/Emote_OFF/[1]Emote_OFF.anim"; // OFFにするキーが入ったコピー元のAnimationファイルのパス
        private static readonly string EMOTE_ON_ANIMFILE_PATH = "/V3 Prefab/Emote_ON/[1]Emote_ON.anim"; // ONにするキーが入ったコピー元のAnimationファイルのパス

        private static readonly string EMOTESWITCH_CONTROLLER_PATH = "/ToggleSwitch/Switch.controller"; // Toggle1のAnimatorに設定するAnimatorControllerまでのパス

        private static readonly string IDLE_ANIMATION_NAME = "IDLE"; // Emoteアニメーションとして参照するアニメーションの名前

        private static readonly string LOCAL_SYSTEM_PREFAB_PATH = "/Local_System/Prefab/Local_system.prefab"; // LocalSystemのPrefabのファイルパス
        private static readonly string LOCAL_ROOT_BELOW_OBJECT_NAME = "Local_On_Switch"; // LocalSystem内にあるアバター直下に置くオブジェクトの名前
        private static readonly string OBJECT_PATH_IN_LOCAL_SYSTEM = "On_Animation_Particle/On_Object/After_On/Object"; // LocalSystem内のObjectまでのパス

        /***** 必要に応じてここまでの値を変更する *****/

        private string emoteSwitchV3EditorFolderPath;
        private string idleAniamtionFbxPath;

        private VRC_AvatarDescriptor avatar = null;
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

        private static readonly string UNDO_TEXT = "SetEmoteSwitchV3 to ";

        private bool useLocal = false;

        private string savedFolderPath;

        private static readonly char BSLASH = '\\';
        private static readonly string EMOTE_SWITCH_SAVED_FOLDER = "ESV3Animation";

        [MenuItem("EmoteSwitch/EmoteSwitchV3 Editor")]
        private static void Create()
        {
            GetWindow<EmoteSwitchV3Editor>("EmoteSwitchV3 Editor");
        }

        private void OnEnable()
        {
            propList = new List<Prop>()
            {
                new Prop(null)
            };

            emoteSwitchV3EditorFolderPath = GetEmoteSwitchV3EditorFolderPath();
            idleAniamtionFbxPath = GetIdleAnimationFbxPath();
        }

        private void OnGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                avatar = EditorGUILayout.ObjectField(
                    "Avatar",
                    avatar,
                    typeof(VRC_AvatarDescriptor),
                    true
                ) as VRC_AvatarDescriptor;

                if (check.changed && avatar != null)
                {
                    GetAvatarInfo(avatar);
                    savedFolderPath = GetSavedFolderPath(avatar.CustomStandingAnims);
                }
            }

            // VRC_AvatarDescripterが設定されていない時の例外処理
            if (targetObject != null && avatar == null)
                EditorGUILayout.HelpBox("Set VRC_AvatarDescripter to Avatar object", MessageType.Warning);

            // Props
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Prop Objects", EditorStyles.boldLabel);
                if (GUILayout.Button("+"))
                {
                    propList.Add(new Prop(null));
                }
                if (GUILayout.Button("-"))
                {
                    if (propList.Count > 1)
                    {
                        propList.RemoveAt(propList.Count - 1);
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

            using (new EditorGUI.IndentLevelScope())
            {
                var index = 0;
                foreach (var prop in propList)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        prop.defaultState = EditorGUILayout.Toggle(prop.defaultState, GUILayout.MinWidth(30), GUILayout.MaxWidth(30));

                        prop.obj = EditorGUILayout.ObjectField(
                            "Prop " + (index + 1),
                            prop.obj,
                            typeof(GameObject),
                            true
                        ) as GameObject;

                        if (useLocal)
                        {
                            prop.isLocalEmoteSwitch = EditorGUILayout.ToggleLeft("", prop.isLocalEmoteSwitch, GUILayout.Width(30f));
                        }
                    }
                }
            }

            if (targetObject != null)
            {
                EditorGUILayout.Space();

                // VRC_AvatarDescripterに設定してあるAnimator
                EditorGUILayout.LabelField("Custom Standing Anims", EditorStyles.boldLabel);

                using (new EditorGUI.IndentLevelScope())
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Anims",
                        standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;

                    if (check.changed)
                    {
                        avatar.CustomStandingAnims = standingAnimController;
                    }
                }

                // CustomStandingAnimsが設定されていない時の例外処理
                if (targetObject != null && avatar != null && standingAnimController == null)
                {
                    EditorGUILayout.HelpBox("Set Custom Standing Anims in VRC_AvatarDescripter", MessageType.Warning);
                }

                EditorGUILayout.Space();

                // Standing AnimatorのEmoteに設定してあるAnimationファイル
                EditorGUILayout.LabelField("Emote(Standing Anims)", EditorStyles.boldLabel);

                // どのEmoteに設定するか選ぶ
                selectedOnOffEmote = (EMOTES)EditorGUILayout.EnumPopup("ON & OFF Emote", selectedOnOffEmote);

                using (new EditorGUI.IndentLevelScope())
                {
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
                    if (savedFolderPath == "/" && avatar != null)
                    {
                        savedFolderPath = GetSavedFolderPath(avatar.CustomStandingAnims);
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
                using (new EditorGUI.IndentLevelScope())
                {
                    useIdleAnim = EditorGUILayout.Toggle("Use IDLE Animation", useIdleAnim);
                    useLocal = EditorGUILayout.Toggle("Use Local EmoteSwitch", useLocal);
                }
            }

            using (new EditorGUI.DisabledGroupScope(avatar == null || standingAnimController == null || !CheckSettingProp(propList)))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set EmoteSwitch"))
                {
                    SetEmoteSwitchV3(targetObject, propList, savedFolderPath);
                }
            }

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
        /// <param name="avatarObj">アバターのルートオブジェクト</param>
        /// <param name="propList">EmoteSwitchで追加するオブジェクトのリスト</param>
        /// <param name="savedFolderPath">EmoteSwitchV3で作成するAnimationClipを保存するフォルダパス</param>
        private void SetEmoteSwitchV3(GameObject avatarObj, List<Prop> propList, string savedFolderPath)
        {
            Undo.RegisterCompleteObjectUndo(avatarObj, UNDO_TEXT + avatarObj.name);

            AnimationClip emoteOnAnimClip = null, emoteOffAnimClip = null;
            float emoteAnimTime = 0f;

            // PrefabのInstanceに対してSetParentする場合はUnpackする必要がある
            // 2018からのPrefabシステムからSetParent時に自動的にUnPackしなくなった
            if (PrefabUtility.IsPartOfPrefabInstance(avatarObj))
            {
                PrefabUtility.UnpackPrefabInstance(avatarObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            foreach (var prop in propList)
            {
                var propObj = prop.obj;
                var propDefaultState = prop.defaultState;

                // Propが未設定なら次へ
                if (propObj == null) continue;

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
                objectTrans.gameObject.SetActive(propDefaultState);

                GameObject localSystemObj = null;
                if (useLocal && prop.isLocalEmoteSwitch)
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
                    if (useLocal && prop.isLocalEmoteSwitch)
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
                    emoteOnAnimClip = CreateEmoteAnimClip(
                                            EMOTE_TYPE.ON, 
                                            savedFolderPath + propObj.name, 
                                            toggleObj.transform, 
                                            avatar.transform, 
                                            (propDefaultState) ? TO_STATE.INACTIVE : TO_STATE.ACTIVE);
                }
                else
                {
                    AddEmoteAnimClip(ref emoteOnAnimClip, 
                                        toggleObj.transform, 
                                        avatar.transform, 
                                        (propDefaultState) ? TO_STATE.INACTIVE : TO_STATE.ACTIVE, 
                                        emoteAnimTime);
                }
                // 初期状態がActiveならActiveにする(propStartState==Active(true) -> TO_ACTIVE)
                if (emoteOffAnimClip == null)
                {
                    emoteOffAnimClip = CreateEmoteAnimClip(
                                            EMOTE_TYPE.OFF, 
                                            savedFolderPath + propObj.name, 
                                            toggleObj.transform, 
                                            avatar.transform, 
                                            (propDefaultState) ? TO_STATE.ACTIVE : TO_STATE.INACTIVE);
                }
                else
                {
                    AddEmoteAnimClip(ref emoteOffAnimClip, 
                                        toggleObj.transform, 
                                        avatar.transform, 
                                        (propDefaultState) ? TO_STATE.ACTIVE : TO_STATE.INACTIVE, 
                                        emoteAnimTime);
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
        private bool CheckSettingProp(List<Prop> propList)
        {
            if (propList == null) return false;
            return propList.Any(x => x.obj != null);
        }

        /// <summary>
        /// EmoteSwitch用のEmoteAnimationClipを作成する
        /// </summary>
        /// <param name="fileType">ON用のAnimationファイルなのかOFF用のAnimationファイルなのか</param>
        /// <param name="propName">出し入れするオブジェクトの名前</param>
        /// <param name="targetObj">Emoteで操作するオブジェクト(Toggle1)</param>
        /// <param name="emoteType">EmoteAnimationでON状態にするのかOFF状態にするのか</param>
        /// <returns></returns>
        private AnimationClip CreateEmoteAnimClip(EMOTE_TYPE type, string savedFilePath, Transform targetTrans, Transform rootTrans, TO_STATE emoteType)
        {
            var animClip = CreateAnimationClip(savedFilePath, type);

            if (useIdleAnim)
            {
                var idleAnimClip = GetAnimationClipFromFbx(idleAniamtionFbxPath, IDLE_ANIMATION_NAME);
                CopyAnimationKeys(idleAnimClip, animClip);
            }

            string path = AnimationUtility.CalculateTransformPath(targetTrans, rootTrans);

            string animFilePath = emoteSwitchV3EditorFolderPath;
            if (emoteType == TO_STATE.INACTIVE)
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
        private void AddEmoteAnimClip(ref AnimationClip animClip, Transform targetTrans, Transform rootTrans, TO_STATE emoteType, float offsetTime)
        {
            string path = AnimationUtility.CalculateTransformPath(targetTrans, rootTrans);

            string animFilePath = emoteSwitchV3EditorFolderPath;
            if (emoteType == TO_STATE.INACTIVE)
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
                    for (int j = keysNum - 1; j >= 0; j--)
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
        private AnimationClip CreateAnimationClip(string savedFilePath, EMOTE_TYPE type)
        {
            AnimationClip animClip = new AnimationClip();

            savedFilePath += ((type == EMOTE_TYPE.ON) ? "_ON" : "_OFF") + ".anim";

            CreateFolderIfNeeded(savedFilePath);

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
            var pattern = @"^" + obj.name + " ?[0-9]*";
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
            if (controller == null) return "Assets/" + EMOTE_SWITCH_SAVED_FOLDER + "/";

            var filePath = AssetDatabase.GetAssetPath(controller);
            return Path.GetDirectoryName(filePath).Replace('\\', '/') + "/" + EMOTE_SWITCH_SAVED_FOLDER + "/";
        }

        /// <summary>
        /// パス内で存在しないフォルダを作成する
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <returns>新しいフォルダを作成したかどうか</returns>
        public static bool CreateFolderIfNeeded(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }
            
            return false;
        }
    }
}