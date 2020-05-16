using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using System.IO;
using System;

// ver 1.3
// created by gatosyocora

namespace Gatosyocora.EmoteSwitchV3Editor
{
    public class EmoteSwitchV3Editor : EditorWindow
    {
        /// <summary>
        /// EmoteSwitchV3で操作するオブジェクト
        /// </summary>
        public class Prop
        {
            public GameObject Obj { get; set; }
            public bool DefaultState { get; set; } = false;
            public bool IsLocalEmoteSwitch { get; set; } = false;

            public Prop(GameObject obj = null)
            {
                this.Obj = obj;
                DefaultState = false;
                IsLocalEmoteSwitch = false;
            }
        }

        private VRC_AvatarDescriptor avatar = null;
        private AnimatorOverrideController standingAnimController = null;
        private List<Prop> propList;

        private string outputFolderPath;

        #region StaticReadOnly
        /***** 必要に応じてここからの値を変更する *****/

        /// <summary>
        /// Prefab内のObjectまでのパス
        /// </summary>
        private static readonly string OBJECT_PATH_IN_PREFAB = "Joint/Toggle1/Object";

        /// <summary>
        /// Prefab内のToogle1までのパス
        /// </summary>
        private static readonly string TOOGLE1_PATH_IN_PREFAB = "Joint/Toggle1";

        /// <summary>
        /// Prefab内のJointまでのパス
        /// </summary>
        private static readonly string JOINT_PATH_IN_PREFAB = "Joint";

        /// <summary>
        /// Prefabのファイルパス
        /// </summary>
        private static readonly string PREFAB1_PATH = "/EmoteSwitchV3Editor/EmoteSwitch V3_Editor.prefab";

        /// <summary>
        /// OFFにするキーが入ったコピー元のAnimationファイルのパス
        /// </summary>
        private static readonly string EMOTE_OFF_ANIMFILE_PATH = "/V3 Prefab/Emote_OFF/[1]Emote_OFF.anim";

        /// <summary>
        /// ONにするキーが入ったコピー元のAnimationファイルのパス
        /// </summary>
        private static readonly string EMOTE_ON_ANIMFILE_PATH = "/V3 Prefab/Emote_ON/[1]Emote_ON.anim";

        /// <summary>
        /// Toggle1のAnimatorに設定するAnimatorControllerまでのパス
        /// </summary>
        private static readonly string EMOTESWITCH_CONTROLLER_PATH = "/ToggleSwitch/Switch.controller";

        /// <summary>
        /// Emoteアニメーションとして参照するアニメーションの名前
        /// </summary>
        private static readonly string IDLE_ANIMATION_NAME = "IDLE";

        /// <summary>
        /// LocalSystemのPrefabのファイルパス
        /// </summary>
        private static readonly string LOCAL_SYSTEM_PREFAB_PATH = "/Local_System/Prefab/Local_system.prefab";

        /// <summary>
        /// LocalSystem内にあるアバター直下に置くオブジェクトの名前
        /// </summary>
        private static readonly string LOCAL_ROOT_BELOW_OBJECT_NAME = "Local_On_Switch";

        /// <summary>
        /// LocalSystem内のObjectまでのパス
        /// </summary>
        private static readonly string OBJECT_PATH_IN_LOCAL_SYSTEM = "On_Animation_Particle/On_Object/After_On/Object";

        /// <summary>
        /// SetEmoteSwitchV3に対するUNDOのテキスト
        /// </summary>
        private static readonly string UNDO_TEXT = "SetEmoteSwitchV3 to ";

        /// <summary>
        /// EmoteSwitchV3Editorで作成されるAssetを保存するフォルダの名称
        /// </summary>
        private static readonly string EMOTE_SWITCH_SAVED_FOLDER = "ESV3Animation";

        /***** 必要に応じてここまでの値を変更する *****/
        #endregion

        private string EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH;
        private string IDLE_ANIAMTION_FBX_PATH;


        /// <summary>
        /// EmoteSwitchV3のアニメーションファイルを設定するEmoteの場所
        /// </summary>
        private enum EmotePair
        {
            EMOTE1and2,
            EMOTE3and4,
            EMOTE5and6,
            EMOTE7and8,
        };
        private EmotePair selectedOnOffEmote = EmotePair.EMOTE1and2;

        /// <summary>
        /// AnimatorOverrideControllerのEmoteの名称
        /// </summary>
        private enum Emote
        { 
            EMOTE1, 
            EMOTE2, 
            EMOTE3, 
            EMOTE4, 
            EMOTE5, 
            EMOTE6, 
            EMOTE7, 
            EMOTE8 
        };

        private enum SwitchTiming
        {
            BEFORE,
            AFTER,
        };
        private SwitchTiming timing = SwitchTiming.AFTER;

        private bool useIdleAnim = true;

        #region GUI
        private bool useLocal = false;
        private bool isOpeningAdvancedSetting = false;
        #endregion


        [MenuItem("EmoteSwitch/EmoteSwitchV3 Editor")]
        private static void Create()
        {
            GetWindow<EmoteSwitchV3Editor>("EmoteSwitchV3 Editor");
        }

        private void OnEnable()
        {
            propList = new List<Prop>()
            {
                new Prop()
            };

            EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH = GetEmoteSwitchV3EditorFolderPath();
            IDLE_ANIAMTION_FBX_PATH = GetIdleAnimationFbxPath();
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
                    SetAvatarInfo(avatar);
                    outputFolderPath = GetSavedFolderPath(avatar.CustomStandingAnims);
                }
            }

            // VRC_AvatarDescripterが設定されていない時の例外処理
            if (avatar == null)
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
                foreach (var prop in propList)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        prop.DefaultState = EditorGUILayout.Toggle(prop.DefaultState, GUILayout.MinWidth(30), GUILayout.MaxWidth(30));

                        prop.Obj = EditorGUILayout.ObjectField(
                            "Prop " + (propList.IndexOf(prop)+1),
                            prop.Obj,
                            typeof(GameObject),
                            true
                        ) as GameObject;

                        if (useLocal)
                        {
                            prop.IsLocalEmoteSwitch = EditorGUILayout.ToggleLeft("", prop.IsLocalEmoteSwitch, GUILayout.Width(30f));
                        }
                    }
                }
            }

            if (avatar != null)
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
                if (standingAnimController == null)
                {
                    EditorGUILayout.HelpBox("Set Custom Standing Anims in VRC_AvatarDescripter", MessageType.Warning);
                }

                EditorGUILayout.Space();

                // Standing AnimatorのEmoteに設定してあるAnimationファイル
                EditorGUILayout.LabelField("Emote(Standing Anims)", EditorStyles.boldLabel);

                // どのEmoteに設定するか選ぶ
                selectedOnOffEmote = (EmotePair)EditorGUILayout.EnumPopup("ON & OFF Emote", selectedOnOffEmote);

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var emoteName in Enum.GetNames(typeof(Emote)))
                    {
                        if (standingAnimController == null || standingAnimController[emoteName].name == emoteName)
                        {
                            AnimationClip dummyAnim;
                            dummyAnim = EditorGUILayout.ObjectField(
                                emoteName,
                                null,
                                typeof(AnimationClip),
                                true
                            ) as AnimationClip;
                        }
                        else
                        {
                            standingAnimController[emoteName] = EditorGUILayout.ObjectField(
                                emoteName,
                                standingAnimController[emoteName],
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
                EditorGUILayout.LabelField("SaveFolder", outputFolderPath);

                if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                {
                    outputFolderPath = EditorUtility.OpenFolderPanel("Select saved folder", "Assets/", string.Empty);
                    outputFolderPath = FileUtil.GetProjectRelativePath(outputFolderPath) + "/";
                    if (outputFolderPath == "/" && avatar != null)
                    {
                        outputFolderPath = GetSavedFolderPath(avatar.CustomStandingAnims);
                    }
                    else if (outputFolderPath == "/")
                    {
                        outputFolderPath = GetSavedFolderPath(null);
                    }
                }
            }

            isOpeningAdvancedSetting = EditorGUILayout.Foldout(isOpeningAdvancedSetting, "Advanced Setting");
            if (isOpeningAdvancedSetting)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    useIdleAnim = EditorGUILayout.Toggle("Use IDLE Animation", useIdleAnim);

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        useLocal = EditorGUILayout.Toggle("Use Local EmoteSwitch", useLocal);
                        if (check.changed)
                        {
                            propList.ForEach(x => x.IsLocalEmoteSwitch = useLocal);
                        }
                    }
                }
            }

            using (new EditorGUI.DisabledGroupScope(avatar == null || standingAnimController == null || !CheckSettingProp(propList)))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set EmoteSwitch"))
                {
                    var onEmote = (Emote)((int)selectedOnOffEmote * 2);
                    var offEmote = (Emote)((int)selectedOnOffEmote * 2 + 1);

                    SetEmoteSwitchV3(avatar, propList, outputFolderPath, onEmote, offEmote, standingAnimController);
                }
            }

            using (new EditorGUI.DisabledGroupScope(!Undo.GetCurrentGroupName().StartsWith(UNDO_TEXT)))
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Revert before Set EmoteSwitch"))
                {
                    Undo.PerformUndo();
                }
            }
        }

        /// <summary>
        /// EmoteSwitchを作成する
        /// </summary>
        /// <param name="avatar">アバターのVRC_AvatarDescriptor</param>
        /// <param name="propList">EmoteSwitchで追加するオブジェクトのリスト</param>
        /// <param name="outputFolderPath">EmoteSwitchV3で作成するAnimationClipを保存するフォルダパス</param>
        /// <param name="avatarController">EmoteSwitchV3で作成するAnimationClipを設定するAnimatorOverrideController</param>
        private void SetEmoteSwitchV3(VRC_AvatarDescriptor avatar, IList<Prop> propList, string outputFolderPath, Emote OnEmote, Emote OffEmote, AnimatorOverrideController avatarController = null)
        {
            if (avatar == null || propList == null || string.IsNullOrEmpty(outputFolderPath))
                return;

            var avatarObj = avatar.gameObject;
            var avatarTrans = avatar.transform;
            var avatarName = avatar.name;

            if (avatarController == null)
            {
                avatarController = avatar.CustomStandingAnims;
            }

            Undo.RegisterCompleteObjectUndo(avatarObj, UNDO_TEXT + avatarName);

            AnimationClip emoteOnAnimClip = null, emoteOffAnimClip = null;

            // PrefabのInstanceに対してSetParentする場合はUnpackする必要がある
            // 2018からのPrefabシステムからSetParent時に自動的にUnPackしなくなった
            if (PrefabUtility.IsPartOfPrefabInstance(avatarObj))
            {
                PrefabUtility.UnpackPrefabInstance(avatarObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            AnimationClip poseAnimClip = null;
            if (useIdleAnim)
            {
                poseAnimClip = GetAnimationClipFromFbx(IDLE_ANIAMTION_FBX_PATH, IDLE_ANIMATION_NAME);
            }

            foreach (var prop in propList)
            {
                // Propが未設定なら次へ
                if (prop == null) continue;

                var propObj = prop.Obj;
                var propDefaultState = prop.DefaultState;

                // propObjと同じ位置にEmoteSwitchV3を作成する
                var parentTrans = propObj.transform.parent;
                var emoteSwitchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH + PREFAB1_PATH);
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
                if (prop.IsLocalEmoteSwitch)
                {
                    // LocalSystemを設定する
                    var localSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH + LOCAL_SYSTEM_PREFAB_PATH);
                    localSystemObj = Instantiate(localSystemPrefab, propObj.transform.position, Quaternion.identity) as GameObject;
                    localSystemObj.name = "EmoteSwitchV3_Local_" + propObj.name;
                    Undo.RegisterCreatedObjectUndo(localSystemObj, "Instantiate " + localSystemObj.name);
                    Undo.SetTransformParent(localSystemObj.transform, parentTrans, localSystemObj.name + " SetParent to " + parentTrans.name);

                    if (PrefabUtility.IsPartOfPrefabInstance(localSystemObj))
                    {
                        PrefabUtility.UnpackPrefabInstance(localSystemObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                    var localOnSwitchTrans = localSystemObj.transform.Find(LOCAL_ROOT_BELOW_OBJECT_NAME);
                    Undo.SetTransformParent(localOnSwitchTrans, avatarTrans, localOnSwitchTrans.name + " SetParent to " + avatarName);
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
                    if (prop.IsLocalEmoteSwitch)
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
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH + EMOTESWITCH_CONTROLLER_PATH);
                animator.runtimeAnimatorController = controller;

                // 初期状態がActiveなら非Activeにする(propDefaultState==Active(true) -> Active(false))
                if (emoteOnAnimClip == null)
                {
                    emoteOnAnimClip = CreateEmoteAnimClip(
                                            outputFolderPath + propObj.name + "_ON.anim", 
                                            toggleObj.transform,
                                            avatarTrans, 
                                            !propDefaultState,
                                            poseAnimClip);
                }
                else
                {
                    AddEmoteAnimClip(ref emoteOnAnimClip, 
                                        toggleObj.transform,
                                        avatarTrans, 
                                        !propDefaultState);
                }

                // 初期状態が非ActiveならActiveにする(propDefaultState==Active(false) -> Active(true))
                if (emoteOffAnimClip == null)
                {
                    emoteOffAnimClip = CreateEmoteAnimClip(
                                            outputFolderPath + propObj.name + "_OFF.anim", 
                                            toggleObj.transform,
                                            avatarTrans,
                                            !propDefaultState,
                                            poseAnimClip);
                }
                else
                {
                    AddEmoteAnimClip(ref emoteOffAnimClip, 
                                        toggleObj.transform,
                                        avatarTrans, 
                                        !propDefaultState);
                }
            }

            // EmoteAnimationを設定する
            Undo.RecordObject(avatarController, "Set Animation For EmoteSwitch to Controller");
            avatarController[Enum.GetName(typeof(Emote), OnEmote)] = emoteOnAnimClip;
            avatarController[Enum.GetName(typeof(Emote), OffEmote)] = emoteOffAnimClip;

            Undo.SetCurrentGroupName(UNDO_TEXT + avatarName);
        }

        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        private void SetAvatarInfo(VRC_AvatarDescriptor avatar)
        {
            if (avatar == null) return;
            standingAnimController = avatar.CustomStandingAnims;
        }

        /// <summary>
        /// Propsに設定されているか調べる
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        private bool CheckSettingProp(IReadOnlyList<Prop> propList)
        {
            if (propList == null) return false;
            return propList.Any(x => x.Obj != null);
        }

        /// <summary>
        /// EmoteSwitch用のEmoteAnimationClipを作成する
        /// </summary>
        /// <param name="savedFilePath">作成するAnimationClipの保存先ファイル名</param>
        /// <param name="propTrans">EmoteSwitchV3で操作するオブジェクトのTransform</param>
        /// <param name="rootTrans">VRC_AvatarDescriptorが設定されたアバターのルートオブジェクトのTransform</param>
        /// <param name="toState">EmoteSwitchV3によって変更されたあとのPropの状態</param>
        /// <param name="poseAnimClip">Emote中のアニメーション</param>
        /// <returns>EmoteSwitchV3を実行させるAnimaionClip</returns>
        private AnimationClip CreateEmoteAnimClip(string savedFilePath, Transform propTrans, Transform rootTrans, bool toState, AnimationClip poseAnimClip = null)
        {
            var animClip = new AnimationClip();

            CreateFolderIfNeeded(savedFilePath);

            AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(savedFilePath));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (poseAnimClip != null)
            {
                CopyAnimationKeys(poseAnimClip, animClip);
            }

            string path = AnimationUtility.CalculateTransformPath(propTrans, rootTrans);

            string animFilePath = EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH;
            if (toState)
            {
                animFilePath += EMOTE_ON_ANIMFILE_PATH;
            }
            else
            {
                animFilePath += EMOTE_OFF_ANIMFILE_PATH;
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
        /// <param name="animClip">EmoteSwitchV3に必要なキーを追加するAnimationClip</param>
        /// <param name="propTrans">EmoteSwitchV3で操作するオブジェクトのTransform</param>
        /// <param name="rootTrans">VRC_AvatarDescriptorが設定されたアバターのルートオブジェクトのTransform</param>
        /// <param name="toState">EmoteSwitchV3によって変更されたあとのPropの状態</param>
        /// <param name="offsetTime">EmoteSwitchV3の発動開始タイミングのoffset</param>
        private void AddEmoteAnimClip(ref AnimationClip animClip, Transform propTrans, Transform rootTrans, bool toState, float offsetTime = 0f)
        {
            string path = AnimationUtility.CalculateTransformPath(propTrans, rootTrans);

            string animFilePath = EMOTE_SWITCH_V3_EDITOR_FOLDER_PATH;
            if (toState)
            {
                animFilePath += EMOTE_ON_ANIMFILE_PATH;
            }
            else
            {
                animFilePath += EMOTE_OFF_ANIMFILE_PATH;
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

                if (timing == SwitchTiming.AFTER && offsetTime > 0f)
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

            var animObj = System.Array.Find<UnityEngine.Object>(anims, item => item is AnimationClip && item.name == animName);

            var animClip = UnityEngine.Object.Instantiate(animObj) as AnimationClip;

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