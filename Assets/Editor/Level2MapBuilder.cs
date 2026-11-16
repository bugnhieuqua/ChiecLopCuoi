using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using ChiecLopCuoi.Environment;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.NPC;

namespace ChiecLopCuoi.Editor
{
    public static class Level2MapBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/level2.unity";
        private const string MID_PATH = "Assets/Art/Environments/Level2_Village/02_mid_terrain/bg_mid_village.png";
        private const string GROUND_PATH = "Assets/Art/Environments/Level2_Village/03_near_ground/layer_03_grass_ground.png";

        private struct MonsterSpawnSpec
        {
            public string objectName;
            public MonsterCombatController.MonsterType type;
            public float worldX;
            public float scale;

            public MonsterSpawnSpec(string name, MonsterCombatController.MonsterType t, float x, float s = 1.65f)
            {
                objectName = name;
                type = t;
                worldX = x;
                scale = s;
            }
        }

        [MenuItem("Tools/Build Level 2 Map")]
        public static void BuildMap()
        {
            Debug.Log("<color=cyan>[Level2MapBuilder]</color> Bắt đầu dựng Map Level 2 mở rộng 280m (-60 đến +220) + 14 Quái 4 Chặng...");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            AssetDatabase.ImportAsset(MID_PATH, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(GROUND_PATH, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Art/Enemies/enemy_demons_pack_spritesheet.png", ImportAssetOptions.ForceUpdate);
            ConfigureDemonSpritesPivot();

            Sprite midSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MID_PATH);
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_PATH);

            if (midSprite == null || groundSprite == null)
            {
                Debug.LogError("<color=red>[Level2MapBuilder]</color> Không tải được Sprite mới! Kiểm tra lại thư mục Level2_Village.");
                return;
            }

            // -------------------------------------------------------------
            // 0. Bê nguyên HUD_Canvas (HUD_Menu, Result_KO_Panel, HUD_Notebook) từ Level 1 qua
            // -------------------------------------------------------------
            CopyHUDCanvasFromLevel1(activeScene);

            // -------------------------------------------------------------
            // 0b. Mở rộng Nền Xa Mặt Trăng: BG_Far phủ kín từ -120 đến +280
            // -------------------------------------------------------------
            ExpandFarMoonBackground();

            // -------------------------------------------------------------
            // 1. Cấu hình Nền Giữa: BG_Mid (Dài 420m, tâm tại X = 80 -> [-130, +290])
            // Hạ Y = -1.08f để chân đình làng & bụi cây chạm sát mặt đường đá [-3.50, -3.82]
            // -------------------------------------------------------------
            GameObject bgMidGo = GameObject.Find("BG_Mid");
            if (bgMidGo == null) bgMidGo = new GameObject("BG_Mid");

            bgMidGo.transform.position = new Vector3(80f, -1.08f, 10f);
            bgMidGo.transform.rotation = Quaternion.identity;
            bgMidGo.transform.localScale = Vector3.one;

            for (int i = bgMidGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bgMidGo.transform.GetChild(i).gameObject);
            }

            SpriteRenderer bgMidSr = bgMidGo.GetComponent<SpriteRenderer>();
            if (bgMidSr == null) bgMidSr = bgMidGo.AddComponent<SpriteRenderer>();
            bgMidSr.enabled = true;
            bgMidSr.sprite = midSprite;
            bgMidSr.drawMode = SpriteDrawMode.Tiled;
            bgMidSr.tileMode = SpriteTileMode.Continuous;
            bgMidSr.size = new Vector2(420f, 5.00f);
            bgMidSr.sortingLayerName = "Mid_Layer";
            bgMidSr.sortingOrder = 0;

            ParallaxLayer bgMidParallax = bgMidGo.GetComponent<ParallaxLayer>();
            if (bgMidParallax == null) bgMidParallax = bgMidGo.AddComponent<ParallaxLayer>();
            bgMidParallax.parallaxSpeed = 0.5f;
            bgMidParallax.isLooping = true;

            // -------------------------------------------------------------
            // 2. Cấu hình Nền Đất Đi Lại: Layer3_Ground (Dài 420m, tâm X = 80)
            // -------------------------------------------------------------
            GameObject groundGo = GameObject.Find("Layer3_Ground");
            if (groundGo == null) groundGo = new GameObject("Layer3_Ground");

            groundGo.transform.position = new Vector3(80f, -4.92f, 0f);
            groundGo.transform.rotation = Quaternion.identity;
            groundGo.transform.localScale = Vector3.one;

            Transform foreGrass = groundGo.transform.Find("Layer4_ForeGrass");
            if (foreGrass != null) Object.DestroyImmediate(foreGrass.gameObject);

            // Rào chắn vật lý tại X = -60 (localX = -140) và X = +220 (localX = +140)
            Transform topBarrier = groundGo.transform.Find("Top_Barrier");
            if (topBarrier != null) topBarrier.localPosition = new Vector3(0f, 7.56f, 0f);
            Transform leftBarrier = groundGo.transform.Find("Left_Wall_Barrier");
            if (leftBarrier != null) leftBarrier.localPosition = new Vector3(-140f, 3.92f, 0f);
            Transform rightBarrier = groundGo.transform.Find("Right_Wall_Barrier");
            if (rightBarrier != null) rightBarrier.localPosition = new Vector3(140f, 3.92f, 0f);

            SpriteRenderer groundSr = groundGo.GetComponent<SpriteRenderer>();
            if (groundSr == null) groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.enabled = true;
            groundSr.sprite = groundSprite;
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.tileMode = SpriteTileMode.Continuous;
            groundSr.size = new Vector2(420f, 3.00f);
            groundSr.sortingLayerName = "Near_Layer";
            groundSr.sortingOrder = 0;

            ParallaxLayer groundParallax = groundGo.GetComponent<ParallaxLayer>();
            if (groundParallax == null) groundParallax = groundGo.AddComponent<ParallaxLayer>();
            groundParallax.parallaxSpeed = 1.0f;
            groundParallax.isLooping = true;

            BoxCollider2D groundCol = groundGo.GetComponent<BoxCollider2D>();
            if (groundCol == null) groundCol = groundGo.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(420f, 4.0f);
            groundCol.offset = new Vector2(0f, -0.84f);

            // -------------------------------------------------------------
            // 3. Đồng bộ Vị Trí 2 Tượng Đầu - Cuối, Cow Lốp & Boss Nữ
            // -------------------------------------------------------------
            ConfigureStatuesAndCharacters();

            // -------------------------------------------------------------
            // 4. Tạo Hệ Thống 4 Chặng Chốt Chặn & 14 Quái Vật Đa Dạng
            // -------------------------------------------------------------
            Spawn14MonstersAndZoneGates();

            // -------------------------------------------------------------
            // 5. Hiệu Ứng Đom Đóm Đêm Trăng & Ánh Sáng
            // -------------------------------------------------------------
            GameObject fxGo = GameObject.Find("Night_Fireflies_FX");
            if (fxGo == null) fxGo = new GameObject("Night_Fireflies_FX");
            fxGo.transform.position = new Vector3(80f, -1.5f, 0f);
            NightFirefliesFX fireflies = fxGo.GetComponent<NightFirefliesFX>();
            if (fireflies == null) fireflies = fxGo.AddComponent<NightFirefliesFX>();
            fireflies.fireflyCount = 40;
            fireflies.fireflyColor = new Color(0.88f, 0.95f, 0.40f, 0.85f);

            GameObject lightGo = GameObject.Find("Global Light 2D");
            if (lightGo != null)
            {
                Light2D light2d = lightGo.GetComponent<Light2D>();
                if (light2d != null)
                {
                    light2d.color = new Color(0.92f, 0.90f, 0.82f, 1.0f);
                    light2d.intensity = 1.05f;
                }
            }

            // -------------------------------------------------------------
            // 6. Cập nhật Giới Hạn Camera (DragCamera2D) & BattleManager
            // -------------------------------------------------------------
            GameObject mainCamGo = GameObject.Find("Main Camera");
            if (mainCamGo != null)
            {
                mainCamGo.transform.position = new Vector3(-46.0f, 0f, -10f);
                DragCamera2D dragCam = mainCamGo.GetComponent<DragCamera2D>();
                if (dragCam != null)
                {
                    dragCam.groundRenderer = groundSr;
                    dragCam.mapMinX = -60f;
                    dragCam.mapMaxX = 220f;
                    dragCam.UpdateCameraLimits();
                    EditorUtility.SetDirty(dragCam);
                }
                EditorUtility.SetDirty(mainCamGo.transform);
            }

            var battleMgr = Object.FindFirstObjectByType<BattleManager>();
            if (battleMgr != null)
            {
                battleMgr.playerStatueX = -56.0f;
                battleMgr.enemyStatueX = 215.0f;
                battleMgr.roundTimeSeconds = 180f;
                battleMgr.p1MaxHealth = 150f;
                battleMgr.p1CurrentHealth = 150f;
                battleMgr.p2MaxHealth = 180f;
                battleMgr.p2CurrentHealth = 180f;
                EditorUtility.SetDirty(battleMgr);
            }

            var audioMgr = Object.FindFirstObjectByType<ChiecLopCuoi.Audio.AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.LoadEditorClipsIfMissing();
                EditorUtility.SetDirty(audioMgr);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=green>[Level2MapBuilder]</color> Đã dựng thành công Map Level 2 dài 280m, 14 Quái vật 4 Chặng và lưu Scene!");
        }

        private static void ExpandFarMoonBackground()
        {
            GameObject bgFarGo = GameObject.Find("BG_Far");
            if (bgFarGo == null) return;

            SpriteRenderer rootSr = bgFarGo.GetComponent<SpriteRenderer>();
            if (rootSr == null || rootSr.sprite == null) return;

            ParallaxLayer farParallax = bgFarGo.GetComponent<ParallaxLayer>();
            if (farParallax != null)
            {
                farParallax.parallaxSpeed = 0.12f;
                farParallax.isLooping = true;
            }

            // Xóa các bản sao cũ và rải đều các tấm nền bầu trời sao từ -134.4 đến +288.0 (mỗi tấm rộng 19.2m)
            for (int i = bgFarGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bgFarGo.transform.GetChild(i).gameObject);
            }

            float tileWidth = 19.2f;
            for (int idx = -7; idx <= 15; idx++)
            {
                if (idx == 0) continue;
                GameObject tileGo = new GameObject($"BG_Far_Tile_{idx}");
                tileGo.transform.SetParent(bgFarGo.transform, false);
                tileGo.transform.localPosition = new Vector3(idx * tileWidth, 0f, 0f);
                tileGo.transform.localScale = Vector3.one;

                var sr = tileGo.AddComponent<SpriteRenderer>();
                sr.sprite = rootSr.sprite;
                sr.material = rootSr.sharedMaterial;
                sr.sortingLayerID = rootSr.sortingLayerID;
                sr.sortingOrder = rootSr.sortingOrder;
                sr.color = rootSr.color;
            }
        }

        public static void ConfigureDemonSpritesPivot()
        {
            string[] folders = new string[]
            {
                "Assets/Art/Enemies/Sprites",
                "Assets/Resources/Enemies/Sprites"
            };

            foreach (var folder in folders)
            {
                if (!System.IO.Directory.Exists(folder)) continue;
                string[] files = System.IO.Directory.GetFiles(folder, "*.png");
                foreach (var f in files)
                {
                    string assetPath = f.Replace("\\", "/");
                    TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti != null)
                    {
                        bool needsSave = false;
                        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single)
                        {
                            ti.textureType = TextureImporterType.Sprite;
                            ti.spriteImportMode = SpriteImportMode.Single;
                            needsSave = true;
                        }

                        TextureImporterSettings settings = new TextureImporterSettings();
                        ti.ReadTextureSettings(settings);
                        if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter ||
                            Mathf.Abs(settings.spritePivot.y) > 0.01f)
                        {
                            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                            settings.spritePivot = new Vector2(0.5f, 0f);
                            settings.filterMode = FilterMode.Point;
                            settings.alphaIsTransparency = true;
                            ti.SetTextureSettings(settings);
                            needsSave = true;
                        }

                        if (needsSave)
                        {
                            ti.SaveAndReimport();
                        }
                    }
                }
            }
        }

        public static void CopyHUDCanvasFromLevel1(UnityEngine.SceneManagement.Scene targetScene)
        {
            const string LEVEL1_PATH = "Assets/Scenes/SampleScene.unity";
            if (targetScene.path == LEVEL1_PATH) return;

            // Chuẩn công nghiệp: Xuất HUD_Canvas + AudioManager từ Màn 1 ra Prefab trong Resources,
            // sau đó gỡ bản sao trùng lặp khỏi Màn 2-6 để SharedLevelRuntimeBootstrap tự kích hoạt bằng code.
            var level1Scene = EditorSceneManager.OpenScene(LEVEL1_PATH, OpenSceneMode.Additive);
            ChiecLopCuoi.EditorTools.SharedLevelArchitectureTool.ExportLevel1SharedPrefabsFromOpenScene(level1Scene, saveScene: false);
            EditorSceneManager.CloseScene(level1Scene, true);
            EditorSceneManager.SetActiveScene(targetScene);

            ChiecLopCuoi.EditorTools.SharedLevelArchitectureTool.CleanDuplicateSharedObjectsFromScene(targetScene);
        }

        private static void ConfigureStatuesAndCharacters()
        {
            // 1. Tượng Trái (X = -56.0, Y = -1.30) & Tượng Phải (X = +215.0, Y = -0.13) đặt khớp trên mặt đường đá (-3.76)
            GameObject leftStatue = GameObject.Find("prop_statue_girl_0");
            if (leftStatue != null)
            {
                Vector3 wp = leftStatue.transform.position;
                wp.x = -56.0f;
                wp.y = -1.30f;
                leftStatue.transform.position = wp;
                EditorUtility.SetDirty(leftStatue.transform);
            }

            GameObject rightStatue = GameObject.Find("prop_statue_girl_gift_left_0");
            if (rightStatue != null)
            {
                Vector3 wp = rightStatue.transform.position;
                wp.x = 215.0f;
                wp.y = -0.13f;
                rightStatue.transform.position = wp;
                EditorUtility.SetDirty(rightStatue.transform);
            }

            // 2. Cow Lốp (Xuất phát tại X = -50.0, Y = -3.76 trên mặt đường lát đá)
            var cowLop = Object.FindFirstObjectByType<CowLopPlayerController>();
            if (cowLop != null)
            {
                var cowAnim = cowLop.GetComponent<Animator>();
                if (cowAnim == null) cowAnim = cowLop.gameObject.AddComponent<Animator>();
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/CowLop/CowLopAnimatorController.controller");
                if (ctrl != null) cowAnim.runtimeAnimatorController = ctrl;
                cowAnim.enabled = true;

                cowLop.transform.position = new Vector3(-50.0f, -3.76f, 0f);
                cowLop.transform.localScale = new Vector3(2.81f, 2.81f, 1f);
                cowLop.groundY = -3.76f;
                cowLop.minX = -58.0f;
                cowLop.maxX = 218.0f;
                cowLop.maxHealth = 150f;
                cowLop.currentHealth = 150f;
                cowLop.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(cowLop);
                EditorUtility.SetDirty(cowLop.transform);
            }

            // 3. Boss Nữ NPC_Female (Đứng tại Đấu trường cuối màn X = +208.0, Y = -3.76 chạm đất)
            var npcFemale = Object.FindFirstObjectByType<NpcCombatController>();
            if (npcFemale != null)
            {
                var npcAnim = npcFemale.GetComponent<Animator>();
                if (npcAnim == null) npcAnim = npcFemale.gameObject.AddComponent<Animator>();
                var fCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/NPC_Female/FemaleAnimatorController.controller");
                if (fCtrl != null) npcAnim.runtimeAnimatorController = fCtrl;
                npcAnim.enabled = true;

                npcFemale.transform.position = new Vector3(208.0f, -3.76f, 0f);
                npcFemale.transform.localScale = new Vector3(-1.85f, 1.85f, 1f);
                npcFemale.groundY = -3.76f;
                npcFemale.minStageX = -56.0f;
                npcFemale.maxStageX = 216.0f;
                npcFemale.arenaTriggerPlayerX = 175.0f;
                npcFemale.maxHealth = 180f;
                npcFemale.currentHealth = 180f;
                npcFemale.combatUnlocked = false;
                npcFemale.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(npcFemale);
                EditorUtility.SetDirty(npcFemale.transform);
            }

            var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
            if (duelMgr != null)
            {
                duelMgr.nextSceneName = "level3";
                duelMgr.BindResultKOPanel();
                EditorUtility.SetDirty(duelMgr);
            }
        }

        private static void Spawn14MonstersAndZoneGates()
        {
            // Xóa các quái lẻ cũ trong Scene để tạo mới đồng bộ 14 Quái trong container
            var oldMonsters = Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            string sortingLayerName = "Near_Layer";
            int sortingOrder = 8;
            Material sharedMat = null;

            foreach (var old in oldMonsters)
            {
                if (old == null) continue;
                var oldSr = old.GetComponent<SpriteRenderer>();
                if (oldSr != null)
                {
                    sharedMat = oldSr.sharedMaterial;
                }
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject container = GameObject.Find("Monsters_Stage2");
            if (container != null) Object.DestroyImmediate(container);
            container = new GameObject("Monsters_Stage2");
            container.transform.position = Vector3.zero;

            // Danh sách 14 Quỷ phân bổ vào 4 Chặng Chốt Chặn (xoá hết quái character, chỉ dùng 4 loài Quỷ có full animation)
            MonsterSpawnSpec[] specs = new MonsterSpawnSpec[]
            {
                // Chặng 1: Cổng Làng Đêm (X = -60 .. +18 | Chốt tại X = +15) -> 4 Quỷ (~3.15m - 3.35m)
                new MonsterSpawnSpec("M01_Z1_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   -38.5f, 4.35f),
                new MonsterSpawnSpec("M02_Z1_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       -22.0f, 3.92f),
                new MonsterSpawnSpec("M03_Z1_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  -6.0f, 4.10f),
                new MonsterSpawnSpec("M04_Z1_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,        8.0f, 3.78f),

                // Chặng 2: Xóm Khung Cửi (X = +18 .. +75 | Chốt tại X = +72) -> 3 Quỷ
                new MonsterSpawnSpec("M05_Z2_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        28f,   3.94f),
                new MonsterSpawnSpec("M06_Z2_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       46f,   3.81f),
                new MonsterSpawnSpec("M07_Z2_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,    62f,   4.38f),

                // Chặng 3: Rừng Tre Tối (X = +75 .. +135 | Chốt tại X = +132) -> 4 Quỷ
                new MonsterSpawnSpec("M08_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       85f,   3.81f),
                new MonsterSpawnSpec("M09_Z3_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  99f,   4.12f),
                new MonsterSpawnSpec("M10_Z3_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       113f,   3.96f),
                new MonsterSpawnSpec("M11_Z3_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   125f,   4.40f),

                // Chặng 4: Đồn Tiền Tiêu (X = +135 .. +184 | Chốt tại X = +182) -> 3 Quỷ Hộ Vệ
                new MonsterSpawnSpec("M12_Z4_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 145f,   4.15f),
                new MonsterSpawnSpec("M13_Z4_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       160f,   3.98f),
                new MonsterSpawnSpec("M14_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      174f,   3.86f),
            };

            foreach (var spec in specs)
            {
                GameObject mGo = new GameObject(spec.objectName);
                mGo.transform.SetParent(container.transform, false);
                mGo.transform.position = new Vector3(spec.worldX, -3.76f, 0f);
                mGo.transform.localScale = new Vector3(spec.scale, spec.scale, 1f);

                var sr = mGo.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
                if (sharedMat != null) sr.sharedMaterial = sharedMat;

                var ctrl = mGo.AddComponent<MonsterCombatController>();
                ctrl.monsterType = spec.type;
                ctrl.groundY = -3.76f;
                ctrl.ApplyPresetStatsByType();
                ctrl.LoadMonsterSpritesInEditor();
                mGo.AddComponent<MemeDeathSound>();
                EditorUtility.SetDirty(ctrl);
            }

            // Đảm bảo StageZoneGateManager tồn tại trong Scene
            var gateMgr = Object.FindFirstObjectByType<StageZoneGateManager>();
            if (gateMgr == null)
            {
                GameObject gateGo = new GameObject("StageZoneGateManager");
                gateMgr = gateGo.AddComponent<StageZoneGateManager>();
            }
            EditorUtility.SetDirty(gateMgr);
        }
    }
}
