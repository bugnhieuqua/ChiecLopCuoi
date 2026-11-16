#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Environment;

namespace ChiecLopCuoi.EditorTools
{
    public static class CowLopAnimationBuilder
    {
        public const string SpritesFolder = "Assets/Art/Characters/CowLop/Sprites";
        public const string RightSheetPath = "Assets/Art/Characters/CowLop/cowlop_right.png";
        public const string BaseActionsSheetPath = "Assets/Art/Characters/CowLop/CowLop_Base_Actions.png";
        public const string SkillsSheetPath = "Assets/Art/Characters/CowLop/CowLop_Skills.png";
        public const string KOSheetPath = "Assets/Art/Characters/CowLop/CowLop_KO.png";
        public const string AnimFolder = "Assets/Animations/CowLop";
        public const string PrefabPath = "Assets/Prefabs/Units/CowLop.prefab";

        [MenuItem("Tools/CowLop/Apply KO Animation Only")]
        public static string ApplyKOOnly()
        {
            if (!AssetDatabase.IsValidFolder(AnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "CowLop");

            EnsureKOSheetSliced();
            var koSprites = LoadSpritesFromPath(KOSheetPath);
            if (koSprites.Count < 4)
            {
                Debug.LogError($"[CowLopAnimationBuilder] Missing CowLop_KO sprites ({koSprites.Count}).");
                return "Failed: Missing KO sprites";
            }

            var koClip = CreateSequenceClip("CowLop_KO", new[]
            {
                GetSprite(koSprites, "CowLop_KO_0"),
                GetSprite(koSprites, "CowLop_KO_1"),
                GetSprite(koSprites, "CowLop_KO_2"),
                GetSprite(koSprites, "CowLop_KO_3")
            }, 8f, false);

            string controllerPath = $"{AnimFolder}/CowLopAnimatorController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller != null)
            {
                EnsureKOStateInController(controller, koClip);
            }
            else
            {
                return BuildAll();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                var ctrl = prefab.GetComponent<CowLopPlayerController>();
                if (ctrl != null)
                {
                    ctrl.AutoLoadSpritesInEditor();
                    EditorUtility.SetDirty(prefab);
                }
            }

            string currentScene = EditorSceneManager.GetActiveScene().path;
            string[] scenes = { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/level2.unity", "Assets/Scenes/level3.unity" };
            foreach (string sPath in scenes)
            {
                if (!File.Exists(sPath)) continue;
                var sc = EditorSceneManager.OpenScene(sPath, OpenSceneMode.Single);
                var existingCtrl = Object.FindFirstObjectByType<CowLopPlayerController>();
                if (existingCtrl != null)
                {
                    existingCtrl.AutoLoadSpritesInEditor();
                    var anim = existingCtrl.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.runtimeAnimatorController = controller;
                        EditorUtility.SetDirty(anim);
                    }
                    EditorUtility.SetDirty(existingCtrl);
                    EditorSceneManager.MarkSceneDirty(sc);
                    EditorSceneManager.SaveScene(sc);
                }
            }

            if (!string.IsNullOrEmpty(currentScene) && File.Exists(currentScene))
            {
                EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "[CowLopAnimationBuilder] Applied CowLop_KO (4 frames, 8 FPS) + KO Animator state + Prefab + 3 scenes.";
        }

        [MenuItem("Tools/CowLop/Build All Animations, Prefab and Place in Scenes")]
        public static string BuildAll()
        {
            return BuildInternal(level1Only: false);
        }

        [MenuItem("Tools/CowLop/Build Animations & Apply to Level 1 Only")]
        public static string BuildLevel1Only()
        {
            return BuildInternal(level1Only: true);
        }

        private static string BuildInternal(bool level1Only)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder(AnimFolder))
                AssetDatabase.CreateFolder("Assets/Animations", "CowLop");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Units"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Units");

            // 1. Re-import cả 4 sprite sheets chuẩn (cowlop_right.png, CowLop_Base_Actions.png, CowLop_Skills.png, CowLop_KO.png)
            AssetDatabase.ImportAsset(RightSheetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(BaseActionsSheetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(SkillsSheetPath, ImportAssetOptions.ForceUpdate);
            EnsureKOSheetSliced();

            // 2. Tải toàn bộ sub-sprites từ 4 sheets
            var rightSprites = LoadSpritesFromPath(RightSheetPath);
            var baseSprites = LoadSpritesFromPath(BaseActionsSheetPath);
            var skillSprites = LoadSpritesFromPath(SkillsSheetPath);
            var koSprites = LoadSpritesFromPath(KOSheetPath);

            if (rightSprites.Count == 0 || baseSprites.Count == 0 || skillSprites.Count == 0)
            {
                Debug.LogError($"[CowLopAnimationBuilder] Thiếu sprites! Right={rightSprites.Count}, Base={baseSprites.Count}, Skills={skillSprites.Count}");
                return "Failed: Missing sprites";
            }

            // 3. Tạo đầy đủ 11 AnimationClips (.anim) cho Animator:
            // - Đứng yên (CowLop_Idle): 4 frames thở & chuyển trọng tâm 2 chân (CowLop_Base_Actions Row 0)
            var idleClip = CreateSequenceClip("CowLop_Idle", new[]
            {
                GetSprite(baseSprites, "CowLop_Idle_0"),
                GetSprite(baseSprites, "CowLop_Idle_1"),
                GetSprite(baseSprites, "CowLop_Idle_2"),
                GetSprite(baseSprites, "CowLop_Idle_3")
            }, 6f, true);

            // - Bước sang phải (CowLop_WalkRight): 8 frames đầy đủ 2 bước Phải - Trái - Phải - Trái (cowlop_right_0..7)
            var walkRightClip = CreateSequenceClip("CowLop_WalkRight", new[]
            {
                GetSprite(rightSprites, "cowlop_right_0"),
                GetSprite(rightSprites, "cowlop_right_1"),
                GetSprite(rightSprites, "cowlop_right_2"),
                GetSprite(rightSprites, "cowlop_right_3"),
                GetSprite(rightSprites, "cowlop_right_4"),
                GetSprite(rightSprites, "cowlop_right_5"),
                GetSprite(rightSprites, "cowlop_right_6"),
                GetSprite(rightSprites, "cowlop_right_7")
            }, 12f, true);

            // - Bước sang trái (CowLop_WalkLeft): 8 frames đầy đủ 2 bước (kết hợp flipX = true trong CowLopPlayerController)
            var walkLeftClip = CreateSequenceClip("CowLop_WalkLeft", new[]
            {
                GetSprite(rightSprites, "cowlop_right_0"),
                GetSprite(rightSprites, "cowlop_right_1"),
                GetSprite(rightSprites, "cowlop_right_2"),
                GetSprite(rightSprites, "cowlop_right_3"),
                GetSprite(rightSprites, "cowlop_right_4"),
                GetSprite(rightSprites, "cowlop_right_5"),
                GetSprite(rightSprites, "cowlop_right_6"),
                GetSprite(rightSprites, "cowlop_right_7")
            }, 12f, true);

            // - Chạy nhanh (CowLop_Run): 8 frames sải chân nâng gối luân phiên 2 chân (cowlop_right_8..15)
            var runClip = CreateSequenceClip("CowLop_Run", new[]
            {
                GetSprite(rightSprites, "cowlop_right_8"),
                GetSprite(rightSprites, "cowlop_right_9"),
                GetSprite(rightSprites, "cowlop_right_10"),
                GetSprite(rightSprites, "cowlop_right_11"),
                GetSprite(rightSprites, "cowlop_right_12"),
                GetSprite(rightSprites, "cowlop_right_13"),
                GetSprite(rightSprites, "cowlop_right_14"),
                GetSprite(rightSprites, "cowlop_right_15")
            }, 15f, true);

            // - Đánh thường cận chiến (CowLop_Attack): 6 frames đấm + đá cao giữ lốp trên lưng (CowLop_Skills Row 0)
            var attackClip = CreateSequenceClip("CowLop_Attack", new[]
            {
                GetSprite(skillSprites, "CowLop_Attack_0"),
                GetSprite(skillSprites, "CowLop_Attack_1"),
                GetSprite(skillSprites, "CowLop_Attack_2"),
                GetSprite(skillSprites, "CowLop_Attack_3"),
                GetSprite(skillSprites, "CowLop_Attack_4"),
                GetSprite(skillSprites, "CowLop_Attack_5")
            }, 12f, false);

            // - Ném lốp tầm xa (CowLop_TireThrow): 6 frames đầy đủ tháo lốp - lấy đà - bước tới ném - thu thế (CowLop_Skills Row 2)
            var tireThrowClip = CreateSequenceClip("CowLop_TireThrow", new[]
            {
                GetSprite(skillSprites, "CowLop_TireThrow_0"),
                GetSprite(skillSprites, "CowLop_TireThrow_1"),
                GetSprite(skillSprites, "CowLop_TireThrow_2"),
                GetSprite(skillSprites, "CowLop_TireThrow_3"),
                GetSprite(skillSprites, "CowLop_TireThrow_4"),
                GetSprite(skillSprites, "CowLop_TireThrow_5")
            }, 12f, false);

            // - Nhảy (CowLop_Jump): 4 frames riêng (khuỵu lấy đà - bật nhảy - lơ lửng - tiếp đất) từ CowLop_Base_Actions Row 2
            var jumpClip = CreateSequenceClip("CowLop_Jump", new[]
            {
                GetSprite(baseSprites, "CowLop_Jump_0"),
                GetSprite(baseSprites, "CowLop_Jump_1"),
                GetSprite(baseSprites, "CowLop_Jump_2"),
                GetSprite(baseSprites, "CowLop_Jump_3")
            }, 8f, false);

            // - Đập lốp nhảy cao (CowLop_JumpSlam): 6 frames khuỵu - nhảy - đỉnh - lao xuống - đập nổ bụi - bật dậy (CowLop_Skills Row 3)
            var jumpSlamClip = CreateSequenceClip("CowLop_JumpSlam", new[]
            {
                GetSprite(skillSprites, "CowLop_JumpSlam_0"),
                GetSprite(skillSprites, "CowLop_JumpSlam_1"),
                GetSprite(skillSprites, "CowLop_JumpSlam_2"),
                GetSprite(skillSprites, "CowLop_JumpSlam_3"),
                GetSprite(skillSprites, "CowLop_JumpSlam_4"),
                GetSprite(skillSprites, "CowLop_JumpSlam_5")
            }, 11f, false);

            // - Tuyệt kỹ Xoay lốp 360 (CowLop_TireSpin): 6 frames xoay lốp kèm chuyển chân linh hoạt (CowLop_Skills Row 1)
            var tireSpinClip = CreateSequenceClip("CowLop_TireSpin", new[]
            {
                GetSprite(skillSprites, "CowLop_TireSpin_0"),
                GetSprite(skillSprites, "CowLop_TireSpin_1"),
                GetSprite(skillSprites, "CowLop_TireSpin_2"),
                GetSprite(skillSprites, "CowLop_TireSpin_3"),
                GetSprite(skillSprites, "CowLop_TireSpin_4"),
                GetSprite(skillSprites, "CowLop_TireSpin_5")
            }, 12f, false);

            // - Đỡ đòn (CowLop_Block): 4 frames khiên lốp gồng đỡ & phản lực từ CowLop_Base_Actions Row 1
            var blockClip = CreateSequenceClip("CowLop_Block", new[]
            {
                GetSprite(baseSprites, "CowLop_Block_0"),
                GetSprite(baseSprites, "CowLop_Block_1"),
                GetSprite(baseSprites, "CowLop_Block_2"),
                GetSprite(baseSprites, "CowLop_Block_3")
            }, 8f, true);

            // - Bị thương / Đẩy lùi (CowLop_Hurt): 4 frames riêng từ CowLop_Base_Actions Row 3
            var hurtClip = CreateSequenceClip("CowLop_Hurt", new[]
            {
                GetSprite(baseSprites, "CowLop_Hurt_0"),
                GetSprite(baseSprites, "CowLop_Hurt_1"),
                GetSprite(baseSprites, "CowLop_Hurt_2"),
                GetSprite(baseSprites, "CowLop_Hurt_3")
            }, 10f, false);

            // - Thua / Gục ngã (CowLop_KO): 4 frames ngã xuống nền từ CowLop_KO.png (không lặp)
            var koClip = koSprites.Count >= 4
                ? CreateSequenceClip("CowLop_KO", new[]
                {
                    GetSprite(koSprites, "CowLop_KO_0"),
                    GetSprite(koSprites, "CowLop_KO_1"),
                    GetSprite(koSprites, "CowLop_KO_2"),
                    GetSprite(koSprites, "CowLop_KO_3")
                }, 8f, false)
                : null;

            // 4. Tạo/Cập nhật AnimatorController duy nhất tại Assets/Animations/CowLop/
            var controller = CreateAnimatorControllerAtPath(
                $"{AnimFolder}/CowLopAnimatorController.controller",
                idleClip, walkRightClip, walkLeftClip, runClip,
                attackClip, tireThrowClip, jumpClip,
                jumpSlamClip, tireSpinClip, blockClip, hurtClip, koClip);

            // 5. Cập nhật Prefab CowLop
            var defaultSprite = GetSprite(baseSprites, "CowLop_Idle_0");
            var prefab = CreateOrUpdatePrefab(controller, defaultSprite);

            // 6. Áp dụng vào Scene Level 1 trước (hoặc cả 3 scene khi BuildAll)
            if (!level1Only)
            {
                UpdateSceneCowLop(prefab, controller, "Assets/Scenes/level3.unity", new Vector3(-50.0f, -3.76f, 0f), -3.76f, 165f);
                UpdateSceneCowLop(prefab, controller, "Assets/Scenes/level2.unity", new Vector3(-50.0f, -3.76f, 0f), -3.76f, 150f);
            }
            UpdateSceneCowLop(prefab, controller, "Assets/Scenes/SampleScene.unity", new Vector3(-39.60f, -3.56f, 0f), -3.56f, 150f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int totalFrames = rightSprites.Count + baseSprites.Count + skillSprites.Count;
            string sceneScope = level1Only ? "Level 1 (SampleScene)" : "3 màn chơi";
            string msg = $"[CowLopAnimationBuilder] Đã cập nhật cowlop_right.png ({rightSprites.Count}f) + CowLop_Base_Actions.png ({baseSprites.Count}f) + CowLop_Skills.png ({skillSprites.Count}f) = {totalFrames} frames, tạo 11 AnimationClips + AnimatorController và đồng bộ vào {sceneScope}!";
            Debug.Log("<color=green>" + msg + "</color>");
            return msg;
        }

        public static Dictionary<string, Sprite> LoadSpritesFromPath(string assetPath)
        {
            var dict = new Dictionary<string, Sprite>();
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (allAssets != null)
            {
                foreach (Object asset in allAssets)
                {
                    if (asset is Sprite sp) dict[sp.name] = sp;
                }
            }
            return dict;
        }

        private static Sprite GetSprite(Dictionary<string, Sprite> dict, string key)
        {
            if (dict.TryGetValue(key, out Sprite sp) && sp != null) return sp;
            Debug.LogWarning($"[CowLopAnimationBuilder] Missing sprite '{key}'");
            foreach (var kv in dict)
            {
                if (kv.Value != null) return kv.Value;
            }
            return null;
        }

        private static AnimationClip CreateSequenceClip(string clipName, Sprite[] frames, float frameRate, bool isLoop)
        {
            string clipPath = $"{AnimFolder}/{clipName}.anim";
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
            settings.stopTime = frames.Length / frameRate;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void EnsureKOSheetSliced()
        {
            if (!File.Exists(KOSheetPath)) return;
            AssetDatabase.ImportAsset(KOSheetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(KOSheetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.spritePixelsPerUnit = 165f;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(KOSheetPath);
            if (tex == null) return;
            int cellW = tex.width / 4;
            int cellH = tex.height;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var rects = new SpriteRect[4];
            for (int i = 0; i < 4; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"CowLop_KO_{i}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(i * cellW, 0, cellW, cellH),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f)
                };
            }
            dataProvider.SetSpriteRects(rects);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static void EnsureKOStateInController(AnimatorController controller, AnimationClip koClip)
        {
            if (controller == null || koClip == null) return;

            bool hasParam = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == "KO") { hasParam = true; break; }
            }
            if (!hasParam)
            {
                controller.AddParameter("KO", AnimatorControllerParameterType.Trigger);
            }

            var sm = controller.layers[0].stateMachine;
            AnimatorState koState = null;
            foreach (var cs in sm.states)
            {
                if (cs.state.name == "KO")
                {
                    koState = cs.state;
                    break;
                }
            }

            if (koState == null)
            {
                koState = sm.AddState("KO", new Vector3(580f, 340f, 0f));
            }
            koState.motion = koClip;

            bool hasAnyTransition = false;
            foreach (var tr in sm.anyStateTransitions)
            {
                if (tr.destinationState == koState)
                {
                    hasAnyTransition = true;
                    break;
                }
            }

            if (!hasAnyTransition)
            {
                var tr = sm.AddAnyStateTransition(koState);
                tr.hasExitTime = false;
                tr.duration = 0f;
                tr.canTransitionToSelf = false;
                tr.AddCondition(AnimatorConditionMode.If, 0f, "KO");
            }

            // Đảm bảo không có transition quay về Idle sau khi KO
            for (int i = koState.transitions.Length - 1; i >= 0; i--)
            {
                koState.RemoveTransition(koState.transitions[i]);
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorController CreateAnimatorControllerAtPath(
            string controllerPath,
            AnimationClip idleClip, AnimationClip walkRightClip, AnimationClip walkLeftClip, AnimationClip runClip,
            AnimationClip attackClip, AnimationClip tireThrowClip, AnimationClip jumpClip,
            AnimationClip jumpSlamClip, AnimationClip tireSpinClip, AnimationClip hurtBlockClip, AnimationClip hurtClip,
            AnimationClip koClip = null)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("MoveDir", AnimatorControllerParameterType.Int);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TireThrow", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("JumpSlam", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TireSpin", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var idleState = sm.AddState("Idle", new Vector3(240f, 0f, 0f));
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            var walkRightState = sm.AddState("WalkRight", new Vector3(240f, 80f, 0f));
            walkRightState.motion = walkRightClip;

            var walkLeftState = sm.AddState("WalkLeft", new Vector3(240f, 160f, 0f));
            walkLeftState.motion = walkLeftClip;

            var runState = sm.AddState("Run", new Vector3(240f, 240f, 0f));
            runState.motion = runClip;

            var blockState = sm.AddState("Block", new Vector3(240f, -80f, 0f));
            blockState.motion = hurtBlockClip;

            var attackState = sm.AddState("Attack", new Vector3(580f, -80f, 0f));
            attackState.motion = attackClip;

            var tireThrowState = sm.AddState("TireThrow", new Vector3(580f, -10f, 0f));
            tireThrowState.motion = tireThrowClip;

            var jumpState = sm.AddState("Jump", new Vector3(580f, 60f, 0f));
            jumpState.motion = jumpClip;

            var jumpSlamState = sm.AddState("JumpSlam", new Vector3(580f, 130f, 0f));
            jumpSlamState.motion = jumpSlamClip;

            var tireSpinState = sm.AddState("TireSpin", new Vector3(580f, 200f, 0f));
            tireSpinState.motion = tireSpinClip;

            var hurtState = sm.AddState("Hurt", new Vector3(580f, 270f, 0f));
            hurtState.motion = hurtClip;

            // Locomotion Transitions
            var tIdleToWalkRight = idleState.AddTransition(walkRightState);
            tIdleToWalkRight.hasExitTime = false;
            tIdleToWalkRight.duration = 0f;
            tIdleToWalkRight.AddCondition(AnimatorConditionMode.Equals, 1f, "MoveDir");
            tIdleToWalkRight.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            var tIdleToWalkLeft = idleState.AddTransition(walkLeftState);
            tIdleToWalkLeft.hasExitTime = false;
            tIdleToWalkLeft.duration = 0f;
            tIdleToWalkLeft.AddCondition(AnimatorConditionMode.Equals, -1f, "MoveDir");
            tIdleToWalkLeft.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            var tIdleToRun = idleState.AddTransition(runState);
            tIdleToRun.hasExitTime = false;
            tIdleToRun.duration = 0f;
            tIdleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIdleToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            var tWalkRightToIdle = walkRightState.AddTransition(idleState);
            tWalkRightToIdle.hasExitTime = false;
            tWalkRightToIdle.duration = 0f;
            tWalkRightToIdle.AddCondition(AnimatorConditionMode.Equals, 0f, "MoveDir");

            var tWalkRightToWalkLeft = walkRightState.AddTransition(walkLeftState);
            tWalkRightToWalkLeft.hasExitTime = false;
            tWalkRightToWalkLeft.duration = 0f;
            tWalkRightToWalkLeft.AddCondition(AnimatorConditionMode.Equals, -1f, "MoveDir");
            tWalkRightToWalkLeft.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            var tWalkRightToRun = walkRightState.AddTransition(runState);
            tWalkRightToRun.hasExitTime = false;
            tWalkRightToRun.duration = 0f;
            tWalkRightToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            var tWalkLeftToIdle = walkLeftState.AddTransition(idleState);
            tWalkLeftToIdle.hasExitTime = false;
            tWalkLeftToIdle.duration = 0f;
            tWalkLeftToIdle.AddCondition(AnimatorConditionMode.Equals, 0f, "MoveDir");

            var tWalkLeftToWalkRight = walkLeftState.AddTransition(walkRightState);
            tWalkLeftToWalkRight.hasExitTime = false;
            tWalkLeftToWalkRight.duration = 0f;
            tWalkLeftToWalkRight.AddCondition(AnimatorConditionMode.Equals, 1f, "MoveDir");
            tWalkLeftToWalkRight.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            var tWalkLeftToRun = walkLeftState.AddTransition(runState);
            tWalkLeftToRun.hasExitTime = false;
            tWalkLeftToRun.duration = 0f;
            tWalkLeftToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            var tRunToIdle = runState.AddTransition(idleState);
            tRunToIdle.hasExitTime = false;
            tRunToIdle.duration = 0f;
            tRunToIdle.AddCondition(AnimatorConditionMode.Equals, 0f, "MoveDir");

            var tRunToWalkRight = runState.AddTransition(walkRightState);
            tRunToWalkRight.hasExitTime = false;
            tRunToWalkRight.duration = 0f;
            tRunToWalkRight.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            tRunToWalkRight.AddCondition(AnimatorConditionMode.Equals, 1f, "MoveDir");

            var tRunToWalkLeft = runState.AddTransition(walkLeftState);
            tRunToWalkLeft.hasExitTime = false;
            tRunToWalkLeft.duration = 0f;
            tRunToWalkLeft.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            tRunToWalkLeft.AddCondition(AnimatorConditionMode.Equals, -1f, "MoveDir");

            var tIdleToBlock = idleState.AddTransition(blockState);
            tIdleToBlock.hasExitTime = false;
            tIdleToBlock.duration = 0f;
            tIdleToBlock.AddCondition(AnimatorConditionMode.If, 0f, "Block");

            var tBlockToIdle = blockState.AddTransition(idleState);
            tBlockToIdle.hasExitTime = false;
            tBlockToIdle.duration = 0f;
            tBlockToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");

            void AddSkillTrigger(string triggerName, AnimatorState state)
            {
                var tr = sm.AddAnyStateTransition(state);
                tr.hasExitTime = false;
                tr.duration = 0f;
                tr.canTransitionToSelf = false;
                tr.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

                var toIdle = state.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 0.95f;
                toIdle.duration = 0.02f;
            }

            AddSkillTrigger("Attack", attackState);
            AddSkillTrigger("TireThrow", tireThrowState);
            AddSkillTrigger("Jump", jumpState);
            AddSkillTrigger("JumpSlam", jumpSlamState);
            AddSkillTrigger("TireSpin", tireSpinState);
            AddSkillTrigger("Hurt", hurtState);

            if (koClip != null)
            {
                EnsureKOStateInController(controller, koClip);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateOrUpdatePrefab(RuntimeAnimatorController controller, Sprite defaultSprite)
        {
            var go = new GameObject("CowLop");
            go.transform.localScale = new Vector3(2.81f, 2.81f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingLayerName = "Near_Layer";
            sr.sortingOrder = 10;

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.enabled = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.45f);
            col.offset = new Vector2(0f, 0.725f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var ctrl = go.AddComponent<CowLopPlayerController>();
            ctrl.groundY = -3.56f;
            ctrl.AutoLoadSpritesInEditor();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void UpdateSceneCowLop(GameObject prefab, RuntimeAnimatorController controller, string scenePath, Vector3 spawnPos, float groundY, float maxHp)
        {
            if (!File.Exists(scenePath)) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject instance = null;
            var existingCtrl = Object.FindFirstObjectByType<CowLopPlayerController>();
            if (existingCtrl != null)
            {
                instance = existingCtrl.gameObject;
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "CowLop";
            }

            instance.transform.position = spawnPos;
            instance.transform.localScale = new Vector3(2.81f, 2.81f, 1f);

            var sr = instance.GetComponent<SpriteRenderer>();
            if (sr == null) sr = instance.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Near_Layer";
            sr.sortingOrder = 10;
            sr.flipX = false;

            var anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.enabled = true;
            EditorUtility.SetDirty(anim);

            var ctrl = instance.GetComponent<CowLopPlayerController>();
            if (ctrl == null) ctrl = instance.AddComponent<CowLopPlayerController>();
            ctrl.groundY = groundY;
            ctrl.maxHealth = maxHp;
            ctrl.currentHealth = maxHp;
            ctrl.AutoLoadSpritesInEditor();
            EditorUtility.SetDirty(ctrl);
            EditorUtility.SetDirty(instance.transform);

            var cam = Object.FindFirstObjectByType<DragCamera2D>();
            if (cam != null)
            {
                cam.followTarget = instance.transform;
                EditorUtility.SetDirty(cam);
            }

            var duel = Object.FindFirstObjectByType<DuelEncounterManager>();
            if (duel != null)
            {
                duel.cowLop = ctrl;
                EditorUtility.SetDirty(duel);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
