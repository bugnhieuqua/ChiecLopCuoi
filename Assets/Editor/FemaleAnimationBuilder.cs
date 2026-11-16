#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Environment;
using ChiecLopCuoi.UI;
using ChiecLopCuoi.Editor;

namespace ChiecLopCuoi.EditorTools
{
    public static class FemaleAnimationBuilder
    {
        public const string SheetPath = "Assets/Art/Characters/NPC_Female/char_female_gift_bag_sheet.png";
        public const string AnimFolder = "Assets/Animations/NPC_Female";
        public const string PrefabPath = "Assets/Prefabs/Units/NPC_Female.prefab";

        public const string DemonsSheetPath = "Assets/Art/Enemies/enemy_demons_pack_spritesheet.png";
        public const string DemonsWalkSheetPath = "Assets/Art/Enemies/enemy_demons_walk.png";
        public const string EnemiesAnimFolder = "Assets/Animations/Enemies";

        [MenuItem("Tools/Setup Level 1 All Animations & Test")]
        public static void BuildLevel1ForTesting()
        {
            BuildAllInternal(syncLevel2And3: false);
        }

        [MenuItem("Tools/Setup Level 1 All Animations & Sync To Level 2-3")]
        public static void BuildAllAndSyncLevels()
        {
            BuildAllInternal(syncLevel2And3: true);
        }

        private static void BuildAllInternal(bool syncLevel2And3)
        {
            // 1. Tạo đầy đủ 11 AnimationClips & AnimatorController cho CowLop (chỉ áp dụng Level 1 nếu chưa đồng bộ 2-3)
            if (syncLevel2And3)
                CowLopAnimationBuilder.BuildAll();
            else
                CowLopAnimationBuilder.BuildLevel1Only();

            // 2. Tạo đầy đủ 8 AnimationClips & AnimatorController cho NPC_Female (từ char_female_gift_bag_sheet.png)
            var femaleController = BuildAnimationsAndController();
            var femaleSprites = LoadAllFemaleSprites();
            femaleSprites.TryGetValue("Female_Idle_0", out Sprite defaultFemaleSprite);
            var femalePrefab = CreateOrUpdateFemalePrefab(femaleController, defaultFemaleSprite);

            // 3. Tạo đầy đủ 4 AnimationClips (IdleWalk, Attack, Hurt, Die) & AnimatorController cho 4 loài Quỷ
            var demonControllers = BuildAllEnemyDemonAnimations();

            // 4. Cập nhật LoreCanvas.prefab (cỡ chữ 26pt dễ đọc)
            RebuildLoreCanvasPrefab();

            // 5. Căn chỉnh Map Level 1 (SampleScene.unity): 2 Tượng sát mặt đất, 5 Xác xe hỏng trang trí, CowLop + NPC_Female + 8 Quỷ + LoreCanvas tắt tự mở
            SetupAndAlignLevel1(femalePrefab, femaleController, demonControllers);

            // 6. Chỉ đồng bộ sang Level 2 & Level 3 khi gọi BuildAllAndSyncLevels
            if (syncLevel2And3)
            {
                LoginSceneBuilder.BuildLoginScene();
                SyncToLevel2AndLevel3(femaleController, demonControllers);
            }

            // Mở lại SampleScene làm scene hiện hành sau khi hoàn tất
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string scopeMsg = syncLevel2And3 ? "cả 3 Level" : "riêng Level 1 (SampleScene) để kiểm thử";
            Debug.Log($"<color=green>[Setup Level 1 & Animator]</color> Hoàn tất 100% ({scopeMsg}): Đã tạo mới toàn bộ Animator cho CowLop (11 clips), NPC_Female (8 clips), 4 loài Quỷ (Idle/Attack/Hurt/Die), 2 Tượng, rải 5 Xác xe hỏng và căn chỉnh bản đồ!");
        }

        [MenuItem("Tools/NPC Female/Build Animations and Controller")]
        public static AnimatorController BuildAnimationsAndController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(AnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "NPC_Female");

            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate);
            var sprites = LoadAllFemaleSprites();

            // 1. Female_Idle: Đủ 4 frames đứng thủ thế tự nhiên (Female_Idle_0..3)
            var idleClip = CreateRangeClip(AnimFolder, sprites, "Female_Idle", 4, 5f, true);

            // 2. Female_Walk: 8 frames bước trái-phải xen kẽ, 12 fps, loop
            var walkClip = CreateRangeClip(AnimFolder, sprites, "Female_Walk", 8, 12f, true);

            // 3. Female_Run: 8 frames chạy hai chân luân phiên hoàn chỉnh, 14 fps, loop
            var runClip = CreateRangeClip(AnimFolder, sprites, "Female_Run", 8, 14f, true);

            // 4. Female_Jump: 8 frames, 12 fps, non-loop
            var jumpClip = CreateRangeClip(AnimFolder, sprites, "Female_Jump", 8, 12f, false);

            // 5. Female_ThrowGift (Attack 1): 6 frames, 10 fps, non-loop
            var throwGiftClip = CreateRangeClip(AnimFolder, sprites, "Female_ThrowGift", 6, 10f, false);

            // 6. Female_ThrowBag (Attack 2): 8 frames, 12 fps, non-loop
            var throwBagClip = CreateRangeClip(AnimFolder, sprites, "Female_ThrowBag", 8, 12f, false);

            // 7. Female_Block: tách riêng hoàn toàn khỏi Hurt
            var blockClip = CreateSequenceClip(AnimFolder, "Female_Block", new[]
            {
                GetSub(sprites, "Female_Idle_0"),
                GetSub(sprites, "Female_Idle_1")
            }, 6f, true);

            // 8. Female_Hurt: tách riêng hoàn toàn khỏi Block
            var hurtClip = CreateSequenceClip(AnimFolder, "Female_Hurt", new[]
            {
                GetSub(sprites, "Female_Jump_1"),
                GetSub(sprites, "Female_Idle_3")
            }, 10f, false);

            var controller = CreateFemaleAnimatorController(
                idleClip, walkClip, runClip, jumpClip, throwGiftClip, throwBagClip, blockClip, hurtClip);

            return controller;
        }

        public static Dictionary<string, AnimatorController> BuildAllEnemyDemonAnimations()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(EnemiesAnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "Enemies");

            AssetDatabase.ImportAsset(DemonsSheetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(DemonsWalkSheetPath, ImportAssetOptions.ForceUpdate);

            var dict = new Dictionary<string, Sprite>();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(DemonsSheetPath);
            if (allAssets != null)
            {
                foreach (var a in allAssets)
                {
                    if (a is Sprite sp) dict[sp.name] = sp;
                }
            }

            var walkDict = new Dictionary<string, Sprite>();
            var walkAssets = AssetDatabase.LoadAllAssetsAtPath(DemonsWalkSheetPath);
            if (walkAssets != null)
            {
                foreach (var a in walkAssets)
                {
                    if (a is Sprite sp) walkDict[sp.name] = sp;
                }
            }

            var controllers = new Dictionary<string, AnimatorController>();
            string[] prefixes = { "quyghen", "quydau", "quytuti", "quygian" };
            int[] idleCounts = { 6, 6, 6, 6 };
            int[] atkCounts = { 5, 7, 7, 8 };

            for (int p = 0; p < prefixes.Length; p++)
            {
                string prefix = prefixes[p];
                var idleFrames = new List<Sprite>();
                for (int i = 0; i < idleCounts[p]; i++)
                {
                    if (dict.TryGetValue($"{prefix}_idle_{i}", out var sp)) idleFrames.Add(sp);
                }

                var walkFrames = new List<Sprite>();
                for (int i = 0; i < 6; i++)
                {
                    if (walkDict.TryGetValue($"{prefix}_walk_{i}", out var sp)) walkFrames.Add(sp);
                }
                if (walkFrames.Count == 0) walkFrames.AddRange(idleFrames);

                var atkFrames = new List<Sprite>();
                for (int i = 0; i < atkCounts[p]; i++)
                {
                    if (dict.TryGetValue($"{prefix}_attack_{i}", out var sp)) atkFrames.Add(sp);
                }

                var hurtFrames = new List<Sprite>();
                if (atkFrames.Count > 1) hurtFrames.Add(atkFrames[1]);
                if (idleFrames.Count > 3) hurtFrames.Add(idleFrames[3]);
                if (idleFrames.Count > 0) hurtFrames.Add(idleFrames[0]);

                var dieFrames = new List<Sprite>();
                if (idleFrames.Count > 3) dieFrames.Add(idleFrames[3]);
                if (idleFrames.Count > 4) dieFrames.Add(idleFrames[4]);
                if (idleFrames.Count > 5) dieFrames.Add(idleFrames[5]);
                else if (idleFrames.Count > 2) dieFrames.Add(idleFrames[2]);

                string legacyIdleWalkPath = $"{EnemiesAnimFolder}/{prefix}_IdleWalk.anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(legacyIdleWalkPath) != null)
                {
                    AssetDatabase.DeleteAsset(legacyIdleWalkPath);
                }

                var idleClip = CreateSequenceClip(EnemiesAnimFolder, $"{prefix}_Idle", idleFrames.ToArray(), 8f, true);
                var walkClip = CreateSequenceClip(EnemiesAnimFolder, $"{prefix}_Walk", walkFrames.ToArray(), 10f, true);
                var attackClip = CreateSequenceClip(EnemiesAnimFolder, $"{prefix}_Attack", atkFrames.ToArray(), 11f, false);
                var hurtClip = CreateSequenceClip(EnemiesAnimFolder, $"{prefix}_Hurt", hurtFrames.ToArray(), 10f, false);
                var dieClip = CreateSequenceClip(EnemiesAnimFolder, $"{prefix}_Die", dieFrames.ToArray(), 8f, false);

                string ctrlPath = $"{EnemiesAnimFolder}/{prefix}_Animator.controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                {
                    AssetDatabase.DeleteAsset(ctrlPath);
                }
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
                ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

                var sm = ctrl.layers[0].stateMachine;
                var idleState = sm.AddState("Idle", new Vector3(240f, 0f, 0f));
                idleState.motion = idleClip;
                sm.defaultState = idleState;

                var walkState = sm.AddState("Walk", new Vector3(240f, 90f, 0f));
                walkState.motion = walkClip;

                var attackState = sm.AddState("Attack", new Vector3(520f, 0f, 0f));
                attackState.motion = attackClip;

                var hurtState = sm.AddState("Hurt", new Vector3(520f, 90f, 0f));
                hurtState.motion = hurtClip;

                var dieState = sm.AddState("Die", new Vector3(520f, 180f, 0f));
                dieState.motion = dieClip;

                var tIdleToWalk = idleState.AddTransition(walkState);
                tIdleToWalk.hasExitTime = false;
                tIdleToWalk.duration = 0f;
                tIdleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

                var tWalkToIdle = walkState.AddTransition(idleState);
                tWalkToIdle.hasExitTime = false;
                tWalkToIdle.duration = 0f;
                tWalkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

                var tAnyAttack = sm.AddAnyStateTransition(attackState);
                tAnyAttack.hasExitTime = false;
                tAnyAttack.duration = 0f;
                tAnyAttack.canTransitionToSelf = false;
                tAnyAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

                var tAtkBack = attackState.AddTransition(idleState);
                tAtkBack.hasExitTime = true;
                tAtkBack.exitTime = 0.95f;
                tAtkBack.duration = 0.02f;

                var tAnyHurt = sm.AddAnyStateTransition(hurtState);
                tAnyHurt.hasExitTime = false;
                tAnyHurt.duration = 0f;
                tAnyHurt.canTransitionToSelf = false;
                tAnyHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

                var tHurtBack = hurtState.AddTransition(idleState);
                tHurtBack.hasExitTime = true;
                tHurtBack.exitTime = 0.95f;
                tHurtBack.duration = 0.02f;

                var tAnyDie = sm.AddAnyStateTransition(dieState);
                tAnyDie.hasExitTime = false;
                tAnyDie.duration = 0f;
                tAnyDie.canTransitionToSelf = false;
                tAnyDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");

                EditorUtility.SetDirty(ctrl);
                controllers[prefix] = ctrl;
            }

            return controllers;
        }

        public static Dictionary<string, Sprite> LoadAllFemaleSprites()
        {
            var dict = new Dictionary<string, Sprite>();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
            if (allAssets != null)
            {
                foreach (var a in allAssets)
                {
                    if (a is Sprite sp) dict[sp.name] = sp;
                }
            }
            return dict;
        }

        private static Sprite GetSub(Dictionary<string, Sprite> dict, string key)
        {
            return dict.TryGetValue(key, out Sprite s) ? s : null;
        }

        private static AnimationClip CreateRangeClip(string folder, Dictionary<string, Sprite> dict, string clipName, int count, float fps, bool isLoop)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                if (dict.TryGetValue($"{clipName}_{i}", out var s) && s != null) list.Add(s);
            }
            return CreateSequenceClip(folder, clipName, list.ToArray(), fps, isLoop);
        }

        private static AnimationClip CreateSequenceClip(string folder, string clipName, Sprite[] frames, float frameRate, bool isLoop)
        {
            string clipPath = $"{folder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName, frameRate = frameRate };
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                clip.frameRate = frameRate;
            }

            var keyframes = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = frames[i]
                };
            }

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            settings.stopTime = Mathf.Max(1, frames.Length) / frameRate;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateFemaleAnimatorController(
            AnimationClip idleClip, AnimationClip walkClip, AnimationClip runClip,
            AnimationClip jumpClip, AnimationClip throwGiftClip, AnimationClip throwBagClip,
            AnimationClip blockClip, AnimationClip hurtClip)
        {
            string controllerPath = $"{AnimFolder}/FemaleAnimatorController.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ThrowGift", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ThrowBag", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var idleState = sm.AddState("Idle", new Vector3(240f, 0f, 0f));
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk", new Vector3(240f, 80f, 0f));
            walkState.motion = walkClip;

            var runState = sm.AddState("Run", new Vector3(240f, 160f, 0f));
            runState.motion = runClip;

            var blockState = sm.AddState("Block", new Vector3(240f, -80f, 0f));
            blockState.motion = blockClip;

            var jumpState = sm.AddState("Jump", new Vector3(540f, -40f, 0f));
            jumpState.motion = jumpClip;

            var throwGiftState = sm.AddState("ThrowGift", new Vector3(540f, 40f, 0f));
            throwGiftState.motion = throwGiftClip;

            var throwBagState = sm.AddState("ThrowBag", new Vector3(540f, 120f, 0f));
            throwBagState.motion = throwBagClip;

            var hurtState = sm.AddState("Hurt", new Vector3(540f, 200f, 0f));
            hurtState.motion = hurtClip;

            // Locomotion transitions
            var tIdleToWalk = idleState.AddTransition(walkState);
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0f;
            tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tWalkToIdle = walkState.AddTransition(idleState);
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0f;
            tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var tWalkToRun = walkState.AddTransition(runState);
            tWalkToRun.hasExitTime = false;
            tWalkToRun.duration = 0f;
            tWalkToRun.AddCondition(AnimatorConditionMode.Greater, 2.0f, "Speed");

            var tRunToWalk = runState.AddTransition(walkState);
            tRunToWalk.hasExitTime = false;
            tRunToWalk.duration = 0f;
            tRunToWalk.AddCondition(AnimatorConditionMode.Less, 2.0f, "Speed");

            var tRunToIdle = runState.AddTransition(idleState);
            tRunToIdle.hasExitTime = false;
            tRunToIdle.duration = 0f;
            tRunToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Block
            var tIdleToBlock = idleState.AddTransition(blockState);
            tIdleToBlock.hasExitTime = false;
            tIdleToBlock.duration = 0f;
            tIdleToBlock.AddCondition(AnimatorConditionMode.If, 0f, "Block");

            var tBlockToIdle = blockState.AddTransition(idleState);
            tBlockToIdle.hasExitTime = false;
            tBlockToIdle.duration = 0f;
            tBlockToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");

            // Combat & Jump triggers
            void AddAnyTrigger(string triggerName, AnimatorState targetState)
            {
                var tr = sm.AddAnyStateTransition(targetState);
                tr.hasExitTime = false;
                tr.duration = 0f;
                tr.canTransitionToSelf = false;
                tr.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

                var back = targetState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.duration = 0.02f;
            }

            AddAnyTrigger("Jump", jumpState);
            AddAnyTrigger("ThrowGift", throwGiftState);
            AddAnyTrigger("ThrowBag", throwBagState);
            AddAnyTrigger("Hurt", hurtState);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateOrUpdateFemalePrefab(RuntimeAnimatorController controller, Sprite defaultSprite)
        {
            var go = new GameObject("NPC_Female");
            go.transform.localScale = new Vector3(-1.85f, 1.85f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingLayerName = "Near_Layer";
            sr.sortingOrder = 10;

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.enabled = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.5f);
            col.offset = new Vector2(0f, 0.75f);

            var ctrl = go.AddComponent<NpcCombatController>();
            ctrl.groundY = -3.56f;
            ctrl.AutoLoadSpritesInEditor();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void RebuildLoreCanvasPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoreCanvas.prefab");
            if (prefab == null)
            {
                LoreCanvasBuilder.BuildLoreCanvas();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoreCanvas.prefab");
            }

            if (prefab != null)
            {
                var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/LoreCanvas.prefab");
                var mgr = root.GetComponent<StoryBookManager>();
                if (mgr != null)
                {
                    mgr.storyFontSize = 26f;
                    mgr.autoOpenOnStartInGameplayScene = true;
                    if (mgr.storyText != null)
                    {
                        mgr.storyText.fontSize = 26f;
                        mgr.storyText.fontStyle = FontStyles.Bold;
                        mgr.storyText.lineSpacing = 10f;
                        var rt = mgr.storyText.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(65f, 325f);
                            rt.sizeDelta = new Vector2(530f, 560f);
                        }
                    }
                    var btnTf = root.transform.Find("LorePanel/BG_Notebook/Btn_Skip_NextPage");
                    if (btnTf != null)
                    {
                        mgr.nextOrSkipButton = btnTf.GetComponent<Button>();
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/LoreCanvas.prefab");
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupAndAlignLevel1(
            GameObject femalePrefab,
            RuntimeAnimatorController femaleController,
            Dictionary<string, AnimatorController> demonControllers)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // 1. Căn chỉnh nền Map Level 1 (Mở rộng BG_Mid, Layer3_Ground thành 180m để lấp kín viền đen 2 đầu màn hình)
            var bgMidGo = GameObject.Find("BG_Mid");
            if (bgMidGo != null)
            {
                bgMidGo.transform.position = new Vector3(15.0f, 4.10f, 10.0f);
                var sr = bgMidGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(180.0f, 10.80f);
                    EditorUtility.SetDirty(sr);
                }
                EditorUtility.SetDirty(bgMidGo.transform);
            }

            var groundGo = GameObject.Find("Layer3_Ground");
            SpriteRenderer groundSr = null;
            if (groundGo != null)
            {
                groundGo.transform.position = new Vector3(15.0f, -2.30f, 0.0f);
                groundSr = groundGo.GetComponent<SpriteRenderer>();
                if (groundSr != null)
                {
                    groundSr.drawMode = SpriteDrawMode.Tiled;
                    groundSr.size = new Vector2(180.0f, 10.83f);
                    EditorUtility.SetDirty(groundSr);
                }
                var col = groundGo.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(180.0f, 4.0f);
                    col.offset = new Vector2(0f, -1.13f);
                    EditorUtility.SetDirty(col);
                }

                var leftWall = groundGo.transform.Find("Left_Wall_Barrier");
                if (leftWall != null) leftWall.localPosition = new Vector3(-72.0f, 1.3f, 0f);
                var rightWall = groundGo.transform.Find("Right_Wall_Barrier");
                if (rightWall != null) rightWall.localPosition = new Vector3(72.0f, 1.3f, 0f);
                EditorUtility.SetDirty(groundGo.transform);
            }

            // Mở rộng dải nền bầu trời sao BG_Far trong Main Camera
            var bgFarGo = GameObject.Find("Main Camera/BG_Far");
            if (bgFarGo != null)
            {
                var rootSr = bgFarGo.GetComponent<SpriteRenderer>();
                if (rootSr != null && rootSr.sprite != null)
                {
                    for (int i = bgFarGo.transform.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(bgFarGo.transform.GetChild(i).gameObject);
                    }
                    float tileWidth = 19.2f;
                    for (int idx = -1; idx <= 1; idx++)
                    {
                        if (idx == 0) continue;
                        var tile = new GameObject($"BG_Far_Tile_{idx}");
                        tile.transform.SetParent(bgFarGo.transform, false);
                        tile.transform.localPosition = new Vector3(idx * tileWidth, 0f, 0f);
                        tile.transform.localScale = Vector3.one;
                        var tsr = tile.AddComponent<SpriteRenderer>();
                        tsr.sprite = rootSr.sprite;
                        tsr.material = rootSr.sharedMaterial;
                        tsr.sortingLayerID = rootSr.sortingLayerID;
                        tsr.sortingOrder = rootSr.sortingOrder;
                        tsr.color = rootSr.color;
                    }
                }
            }

            // 2. Đặt lại vị trí 2 Tượng Đầu - Cuối nâng cao vừa tầm (không để bị chìm thấp xuống dưới) & gắn Animator mới
            var leftStatue = GameObject.Find("prop_statue_girl_0");
            if (leftStatue != null)
            {
                leftStatue.transform.position = new Vector3(-46.0f, -1.30f, 0f);
                EditorUtility.SetDirty(leftStatue.transform);
            }

            var rightStatue = GameObject.Find("prop_statue_girl_gift_left_0");
            if (rightStatue != null)
            {
                rightStatue.transform.position = new Vector3(75.0f, -1.30f, 0f);
                EditorUtility.SetDirty(rightStatue.transform);
            }

            StatueAnimationSetup.CreateAssetsAndWireCurrentScene();

            // 2b. Rải 5 Xác xe hỏng trang trí dọc 3 chặng sa mạc Level 1 (props_broken_vehicles)
            ScatterBrokenVehiclesOnLevel1();

            // 3. Cập nhật CowLop trên Level 1 (Đứng tại X = -39.60, Y = -3.56, đầy đủ Animator)
            var cowLop = Object.FindFirstObjectByType<CowLopPlayerController>();
            if (cowLop != null)
            {
                cowLop.transform.position = new Vector3(-39.60f, -3.56f, 0f);
                cowLop.transform.localScale = new Vector3(2.81f, 2.81f, 1f);
                cowLop.groundY = -3.56f;
                cowLop.minX = -54.0f;
                cowLop.maxX = 82.0f;
                cowLop.maxHealth = 150f;
                cowLop.currentHealth = 150f;
                cowLop.AutoLoadSpritesInEditor();

                var cowAnim = cowLop.GetComponent<Animator>();
                if (cowAnim == null) cowAnim = cowLop.gameObject.AddComponent<Animator>();
                var cowCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/CowLop/CowLopAnimatorController.controller");
                if (cowCtrl != null) cowAnim.runtimeAnimatorController = cowCtrl;
                cowAnim.enabled = true;
                EditorUtility.SetDirty(cowLop);
                EditorUtility.SetDirty(cowAnim);
            }

            // 4. Cập nhật Boss Nữ NPC_Female trên Level 1 (Đứng tại X = 68.5, Y = -3.56 chạm đất, đầy đủ Animator)
            var existingNpc = Object.FindFirstObjectByType<NpcCombatController>();
            GameObject npcGo = existingNpc != null ? existingNpc.gameObject : GameObject.Find("NPC_Female");
            if (npcGo == null)
            {
                npcGo = (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab);
                npcGo.name = "NPC_Female";
            }
            npcGo.transform.position = new Vector3(68.5f, -3.56f, 0f);
            npcGo.transform.localScale = new Vector3(-1.85f, 1.85f, 1f);

            var npcAnim = npcGo.GetComponent<Animator>();
            if (npcAnim == null) npcAnim = npcGo.AddComponent<Animator>();
            npcAnim.runtimeAnimatorController = femaleController;
            npcAnim.enabled = true;
            EditorUtility.SetDirty(npcAnim);

            var npcCtrl = npcGo.GetComponent<NpcCombatController>();
            if (npcCtrl == null) npcCtrl = npcGo.AddComponent<NpcCombatController>();
            npcCtrl.groundY = -3.56f;
            npcCtrl.minStageX = -52.0f;
            npcCtrl.maxStageX = 80.0f;
            npcCtrl.arenaTriggerPlayerX = 48.0f;
            npcCtrl.maxHealth = 160f;
            npcCtrl.currentHealth = 160f;
            npcCtrl.combatUnlocked = false;
            if (cowLop != null) npcCtrl.playerTarget = cowLop.transform;
            npcCtrl.AutoLoadSpritesInEditor();
            EditorUtility.SetDirty(npcCtrl);
            EditorUtility.SetDirty(npcGo.transform);

            // 5. Xoá toàn bộ Monsters cũ (Monster_1_DarkSwordsman, Monster_2_DemonBeast...) và đặt 8 Quỷ có Animator
            Spawn8Level1EnemyDemons(demonControllers);

            // 6. Căn chỉnh Camera & GameManager / BattleManager / DuelEncounterManager
            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(-36.4f, 0f, -10f);
                var dragCam = mainCam.GetComponent<DragCamera2D>();
                if (dragCam != null)
                {
                    if (cowLop != null) dragCam.followTarget = cowLop.transform;
                    if (groundSr != null) dragCam.groundRenderer = groundSr;
                    dragCam.mapMinX = -56.0f;
                    dragCam.mapMaxX = 84.0f;
                    dragCam.UpdateCameraLimits();
                    EditorUtility.SetDirty(dragCam);
                }
            }

            var battleMgr = Object.FindFirstObjectByType<BattleManager>();
            if (battleMgr != null)
            {
                battleMgr.playerStatueX = -46.0f;
                battleMgr.enemyStatueX = 75.0f;
                battleMgr.roundTimeSeconds = 180f;
                battleMgr.p1MaxHealth = 150f;
                battleMgr.p1CurrentHealth = 150f;
                battleMgr.p2MaxHealth = 160f;
                battleMgr.p2CurrentHealth = 160f;
                battleMgr.cowLop = cowLop;
                battleMgr.npcFemale = npcCtrl;
                EditorUtility.SetDirty(battleMgr);
            }

            var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
            if (duelMgr != null)
            {
                duelMgr.cowLop = cowLop;
                duelMgr.npcFemale = npcCtrl;
                duelMgr.nextSceneName = "level2";
                duelMgr.BindResultKOPanel();
                EditorUtility.SetDirty(duelMgr);
            }

            // 7. Cập nhật LoreCanvas (tạm tắt tự mở khi vào Level 1 để test thẳng chuyển động & đánh nhau, vẫn mở được qua nút Sổ Tay)
            var existingLore = GameObject.Find("LoreCanvas");
            if (existingLore != null) Object.DestroyImmediate(existingLore);
            var lorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoreCanvas.prefab");
            if (lorePrefab != null)
            {
                var loreInst = (GameObject)PrefabUtility.InstantiatePrefab(lorePrefab);
                loreInst.name = "LoreCanvas";
                var storyMgr = loreInst.GetComponent<StoryBookManager>();
                if (storyMgr != null)
                {
                    storyMgr.loadSceneOnComplete = false;
                    storyMgr.autoOpenOnStartInGameplayScene = false;
                    storyMgr.storyFontSize = 26f;
                    var hudCanvas = GameObject.Find("HUD_Canvas");
                    if (hudCanvas != null) storyMgr.gameplayUI = hudCanvas;
                    if (storyMgr.lorePanel != null) storyMgr.lorePanel.SetActive(false);
                    EditorUtility.SetDirty(storyMgr);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ScatterBrokenVehiclesOnLevel1()
        {
            const string sheetPath = "Assets/Art/Props/props_broken_vehicles.png";
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);

            var spriteDict = new Dictionary<string, Sprite>();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            if (allAssets != null)
            {
                foreach (var a in allAssets)
                {
                    if (a is Sprite sp) spriteDict[sp.name] = sp;
                }
            }

            var propsRoot = GameObject.Find("Props");
            if (propsRoot == null) propsRoot = new GameObject("Props");

            var oldVehicles = propsRoot.transform.Find("Level1_BrokenVehicles");
            if (oldVehicles != null) Object.DestroyImmediate(oldVehicles.gameObject);

            var container = new GameObject("Level1_BrokenVehicles");
            container.transform.SetParent(propsRoot.transform, false);
            container.transform.position = Vector3.zero;

            var bgMid = GameObject.Find("BG_Mid");
            Material sharedMat = bgMid != null && bgMid.GetComponent<SpriteRenderer>() != null
                ? bgMid.GetComponent<SpriteRenderer>().sharedMaterial
                : null;

            (string name, string spriteKey, float x, float scaleX, float scaleY)[] placements = new[]
            {
                ("Prop_Moto_Ch1",  "props_broken_vehicles_2", -27.5f,  2.15f, 2.15f),
                ("Prop_Car_Ch2",   "props_broken_vehicles_3",  -3.0f,  2.35f, 2.35f),
                ("Prop_Moto_Ch2",  "props_broken_vehicles_2",  14.5f, -2.15f, 2.15f),
                ("Prop_Plane_Ch3", "props_broken_vehicles_4",  32.0f,  2.40f, 2.40f),
                ("Prop_Car_Boss",  "props_broken_vehicles_3",  56.0f, -2.25f, 2.25f)
            };

            foreach (var p in placements)
            {
                if (!spriteDict.TryGetValue(p.spriteKey, out var sp) || sp == null) continue;
                var go = new GameObject(p.name);
                go.transform.SetParent(container.transform, false);
                go.transform.position = new Vector3(p.x, -3.56f, 0f);
                go.transform.localScale = new Vector3(p.scaleX, p.scaleY, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingLayerName = "Mid_Layer";
                sr.sortingOrder = 2;
                if (sharedMat != null) sr.sharedMaterial = sharedMat;
                EditorUtility.SetDirty(go);
            }
        }

        private static void Spawn8Level1EnemyDemons(Dictionary<string, AnimatorController> demonControllers)
        {
            // Xóa toàn bộ Monsters cũ trên Level 1
            var oldMonsters = Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            Material sharedMat = null;
            foreach (var old in oldMonsters)
            {
                if (old == null) continue;
                var oldSr = old.GetComponent<SpriteRenderer>();
                if (oldSr != null && sharedMat == null) sharedMat = oldSr.sharedMaterial;
                Object.DestroyImmediate(old.gameObject);
            }

            var oldContainer = GameObject.Find("Level1_Enemies_Container");
            if (oldContainer != null) Object.DestroyImmediate(oldContainer);
            var container = new GameObject("Level1_Enemies_Container");

            // Cấu hình 3 Chặng Chốt Chặn cho Level 1 (-54 đến +80)
            var gameMgr = GameObject.Find("GameManager");
            if (gameMgr != null)
            {
                var gateMgr = gameMgr.GetComponent<StageZoneGateManager>();
                if (gateMgr == null) gateMgr = gameMgr.AddComponent<StageZoneGateManager>();
                gateMgr.zones = new List<StageZoneGateManager.StageZone>
                {
                    new StageZoneGateManager.StageZone { zoneName = "CHẶNG 1/3: THUNG LŨNG CÁT", minX = -55f, maxX = -2f, gateLimitX = -4f },
                    new StageZoneGateManager.StageZone { zoneName = "CHẶNG 2/3: ĐỒI XƯƠNG RỒNG", minX = -2f, maxX = 24f, gateLimitX = 22f },
                    new StageZoneGateManager.StageZone { zoneName = "CHẶNG 3/3: TIỀN ĐỒN BÌNH MINH", minX = 24f, maxX = 52f, gateLimitX = 48f }
                };
                EditorUtility.SetDirty(gateMgr);
            }

            (string objName, MonsterCombatController.MonsterType type, float x, float scale)[] spawns = new[]
            {
                // Chặng 1 (-22 .. -8) — Tỉ lệ nhỉnh hơn CowLop (2.98m): ~3.15m - 3.35m
                ("Enemy_01_QuyGhen", MonsterCombatController.MonsterType.JealousyDemon, -20.0f, 4.35f),
                ("Enemy_02_QuyDau",  MonsterCombatController.MonsterType.PainDemon,     -10.0f, 3.92f),

                // Chặng 2 (2 .. 18)
                ("Enemy_03_QuyTuTi", MonsterCombatController.MonsterType.InsecurityDemon, 3.0f, 4.10f),
                ("Enemy_04_QuyGian", MonsterCombatController.MonsterType.AngerDemon,     11.5f, 3.78f),
                ("Enemy_05_QuyGhen", MonsterCombatController.MonsterType.JealousyDemon,  18.0f, 4.38f),

                // Chặng 3 (27 .. 43)
                ("Enemy_06_QuyDau",  MonsterCombatController.MonsterType.PainDemon,      28.0f, 3.96f),
                ("Enemy_07_QuyTuTi", MonsterCombatController.MonsterType.InsecurityDemon,35.5f, 4.14f),
                ("Enemy_08_QuyGian", MonsterCombatController.MonsterType.AngerDemon,     42.5f, 3.84f)
            };

            foreach (var sp in spawns)
            {
                var mGo = new GameObject(sp.objName);
                mGo.transform.SetParent(container.transform, false);
                mGo.transform.position = new Vector3(sp.x, -3.56f, 0f);
                mGo.transform.localScale = new Vector3(-sp.scale, sp.scale, 1f);

                var sr = mGo.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Near_Layer";
                sr.sortingOrder = 8;
                if (sharedMat != null) sr.sharedMaterial = sharedMat;

                var anim = mGo.AddComponent<Animator>();
                var ctrl = mGo.AddComponent<MonsterCombatController>();
                ctrl.monsterType = sp.type;
                ctrl.groundY = -3.56f;
                ctrl.ApplyPresetStatsByType();
                ctrl.LoadMonsterSpritesInEditor();
                mGo.AddComponent<MemeDeathSound>();

                string prefix = ctrl.GetDemonPrefix();
                if (demonControllers.TryGetValue(prefix, out var animCtrl) && animCtrl != null)
                {
                    anim.runtimeAnimatorController = animCtrl;
                    anim.enabled = true;
                }

                EditorUtility.SetDirty(ctrl);
                EditorUtility.SetDirty(anim);
            }
        }

        private static void SyncToLevel2AndLevel3(
            RuntimeAnimatorController femaleController,
            Dictionary<string, AnimatorController> demonControllers)
        {
            string[] targetScenes = { "Assets/Scenes/level2.unity", "Assets/Scenes/level3.unity" };
            var cowCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/CowLop/CowLopAnimatorController.controller");

            foreach (string scenePath in targetScenes)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // 1. Copy nguyên bản HUD_Canvas (cùng Bảng Hướng Dẫn mà người dùng đã chỉnh ở Level 1) sang Level 2 & 3
                Level2MapBuilder.CopyHUDCanvasFromLevel1(scene);

                // 2. Đồng bộ CowLop (nhận đủ Animator từ Level 1, đứng tại groundY = -3.76f)
                var cowLop = Object.FindFirstObjectByType<CowLopPlayerController>();
                if (cowLop != null)
                {
                    cowLop.transform.position = new Vector3(-50.0f, -3.76f, 0f);
                    cowLop.transform.localScale = new Vector3(2.81f, 2.81f, 1f);
                    cowLop.groundY = -3.76f;
                    cowLop.minX = -58.0f;
                    cowLop.maxX = 218.0f;
                    cowLop.AutoLoadSpritesInEditor();

                    var cowAnim = cowLop.GetComponent<Animator>();
                    if (cowAnim == null) cowAnim = cowLop.gameObject.AddComponent<Animator>();
                    if (cowCtrl != null) cowAnim.runtimeAnimatorController = cowCtrl;
                    cowAnim.enabled = true;
                    EditorUtility.SetDirty(cowLop);
                    EditorUtility.SetDirty(cowAnim);
                }

                // 3. Đồng bộ Boss Nữ NPC_Female (nhận đủ FemaleAnimatorController từ Level 1, đứng chạm đất tại groundY = -3.76f)
                var npc = Object.FindFirstObjectByType<NpcCombatController>();
                if (npc != null)
                {
                    npc.transform.position = new Vector3(208.0f, -3.76f, 0f);
                    npc.transform.localScale = new Vector3(-1.85f, 1.85f, 1f);
                    npc.groundY = -3.76f;
                    npc.minStageX = -56.0f;
                    npc.maxStageX = 216.0f;
                    npc.arenaTriggerPlayerX = 175.0f;
                    if (cowLop != null) npc.playerTarget = cowLop.transform;
                    npc.AutoLoadSpritesInEditor();

                    var npcAnim = npc.GetComponent<Animator>();
                    if (npcAnim == null) npcAnim = npc.gameObject.AddComponent<Animator>();
                    npcAnim.runtimeAnimatorController = femaleController;
                    npcAnim.enabled = true;
                    EditorUtility.SetDirty(npc);
                    EditorUtility.SetDirty(npcAnim);
                }

                // 4. Đồng bộ AnimatorController cho toàn bộ các Quỷ (Enemies) trên Level 2 & Level 3
                var allMonsters = Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
                foreach (var m in allMonsters)
                {
                    if (m == null) continue;
                    m.groundY = -3.76f;
                    Vector3 mp = m.transform.position;
                    mp.y = -3.76f;
                    m.transform.position = mp;
                    m.ApplyPresetStatsByType();
                    m.LoadMonsterSpritesInEditor();

                    var mAnim = m.GetComponent<Animator>();
                    if (mAnim == null) mAnim = m.gameObject.AddComponent<Animator>();
                    string prefix = m.GetDemonPrefix();
                    if (demonControllers.TryGetValue(prefix, out var dCtrl) && dCtrl != null)
                    {
                        mAnim.runtimeAnimatorController = dCtrl;
                        mAnim.enabled = true;
                    }
                    EditorUtility.SetDirty(m);
                    EditorUtility.SetDirty(mAnim);
                }

                var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
                if (duelMgr != null)
                {
                    duelMgr.cowLop = cowLop;
                    duelMgr.npcFemale = npc;
                    duelMgr.BindResultKOPanel();
                    EditorUtility.SetDirty(duelMgr);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
#endif
