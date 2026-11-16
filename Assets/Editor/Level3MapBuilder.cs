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
    public static class Level3MapBuilder
    {
        private const string LEVEL2_PATH = "Assets/Scenes/level2.unity";
        private const string LEVEL3_PATH = "Assets/Scenes/level3.unity";
        private const string FAR_PATH = "Assets/Art/Environments/Level3_HauntedForest/01_far_sky/bg_far_forest_night.png";
        private const string MID_PATH = "Assets/Art/Environments/Level3_HauntedForest/02_mid_trees/bg_mid_haunted_trees.png";
        private const string GROUND_PATH = "Assets/Art/Environments/Level3_HauntedForest/03_near_ground/layer_03_swamp_ground.png";

        private struct MonsterSpawnSpec
        {
            public string objectName;
            public MonsterCombatController.MonsterType type;
            public float worldX;
            public float scale;

            public MonsterSpawnSpec(string name, MonsterCombatController.MonsterType t, float x, float s = 2.25f)
            {
                objectName = name;
                type = t;
                worldX = x;
                scale = s;
            }
        }

        [MenuItem("Tools/Build Level 3 Map")]
        public static void BuildMap()
        {
            Debug.Log("<color=cyan>[Level3MapBuilder]</color> Bắt đầu dựng Map Level 3 (Rừng Đêm Ma Ám — Haunted Night Forest)...");

            EditorSceneManager.SaveOpenScenes();
            if (!System.IO.File.Exists(LEVEL3_PATH))
            {
                AssetDatabase.CopyAsset(LEVEL2_PATH, LEVEL3_PATH);
            }
            else
            {
                AssetDatabase.DeleteAsset(LEVEL3_PATH);
                AssetDatabase.CopyAsset(LEVEL2_PATH, LEVEL3_PATH);
            }
            AssetDatabase.Refresh();

            ConfigureSpriteImporter(FAR_PATH, 100f);
            ConfigureSpriteImporter(MID_PATH, 100f);
            ConfigureSpriteImporter(GROUND_PATH, 100f);
            Level2MapBuilder.ConfigureDemonSpritesPivot();

            Sprite farSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FAR_PATH);
            Sprite midSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MID_PATH);
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GROUND_PATH);

            if (farSprite == null || midSprite == null || groundSprite == null)
            {
                Debug.LogError("<color=red>[Level3MapBuilder]</color> Không tải được bộ Sprite Level 3 Haunted Forest!");
                return;
            }

            var activeScene = EditorSceneManager.OpenScene(LEVEL3_PATH, OpenSceneMode.Single);

            // 0. Bê nguyên HUD_Canvas chuẩn từ Level 1 qua
            Level2MapBuilder.CopyHUDCanvasFromLevel1(activeScene);

            // 1. Nền Xa (BG_Far): Rừng sâu sương mù đêm tối (Parallax 0.12x)
            GameObject bgFarGo = GameObject.Find("BG_Far");
            if (bgFarGo != null)
            {
                SpriteRenderer rootSr = bgFarGo.GetComponent<SpriteRenderer>();
                if (rootSr != null)
                {
                    rootSr.sprite = farSprite;
                    rootSr.color = Color.white;
                }

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
                    sr.sprite = farSprite;
                    if (rootSr != null)
                    {
                        sr.material = rootSr.sharedMaterial;
                        sr.sortingLayerID = rootSr.sortingLayerID;
                        sr.sortingOrder = rootSr.sortingOrder;
                    }
                    sr.color = Color.white;
                }
            }

            // 2. Nền Giữa (BG_Mid): Rừng cây cổ thụ ma quái & tán lá xanh đậm (Parallax 0.5x, Y = -1.08f)
            GameObject bgMidGo = GameObject.Find("BG_Mid");
            if (bgMidGo == null) bgMidGo = new GameObject("BG_Mid");
            bgMidGo.transform.position = new Vector3(80f, -1.08f, 10f);
            bgMidGo.transform.rotation = Quaternion.identity;
            bgMidGo.transform.localScale = Vector3.one;

            SpriteRenderer bgMidSr = bgMidGo.GetComponent<SpriteRenderer>();
            if (bgMidSr == null) bgMidSr = bgMidGo.AddComponent<SpriteRenderer>();
            bgMidSr.enabled = true;
            bgMidSr.sprite = midSprite;
            bgMidSr.drawMode = SpriteDrawMode.Tiled;
            bgMidSr.tileMode = SpriteTileMode.Continuous;
            bgMidSr.size = new Vector2(420f, 5.00f);
            bgMidSr.sortingLayerName = "Mid_Layer";
            bgMidSr.sortingOrder = 0;

            // 3. Nền Đất Gần (Layer3_Ground): Đường đá rêu phong & đầm lầy rễ cây (Parallax 1.0x, Y = -4.92f)
            GameObject groundGo = GameObject.Find("Layer3_Ground");
            if (groundGo == null) groundGo = new GameObject("Layer3_Ground");
            groundGo.transform.position = new Vector3(80f, -4.92f, 0f);
            groundGo.transform.rotation = Quaternion.identity;
            groundGo.transform.localScale = Vector3.one;

            SpriteRenderer groundSr = groundGo.GetComponent<SpriteRenderer>();
            if (groundSr == null) groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.enabled = true;
            groundSr.sprite = groundSprite;
            groundSr.drawMode = SpriteDrawMode.Tiled;
            groundSr.tileMode = SpriteTileMode.Continuous;
            groundSr.size = new Vector2(420f, 3.00f);
            groundSr.sortingLayerName = "Near_Layer";
            groundSr.sortingOrder = 0;

            BoxCollider2D groundCol = groundGo.GetComponent<BoxCollider2D>();
            if (groundCol == null) groundCol = groundGo.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(420f, 4.0f);
            groundCol.offset = new Vector2(0f, -0.84f);

            // 4. Đặt vị trí Tượng, Cow Lốp & Boss Nữ trên mặt đường đá rêu (-3.76f)
            GameObject leftStatue = GameObject.Find("prop_statue_girl_0");
            if (leftStatue != null) leftStatue.transform.position = new Vector3(-56.0f, -1.30f, 0f);

            GameObject rightStatue = GameObject.Find("prop_statue_girl_gift_left_0");
            if (rightStatue != null) rightStatue.transform.position = new Vector3(215.0f, -0.13f, 0f);

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
                cowLop.maxHealth = 165f;
                cowLop.currentHealth = 165f;
                cowLop.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(cowLop);
            }

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
                npcFemale.maxHealth = 220f;
                npcFemale.currentHealth = 220f;
                npcFemale.combatUnlocked = false;
                npcFemale.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(npcFemale);
            }

            // 5. Sinh 16 Quái Vật Rừng Đêm (4 Chặng Chốt Chặn) + 10 Thùng Gỗ Tiếp Tế (BreakableForestProp)
            Spawn16ForestMonsters();
            SpawnBreakableForestBarrels();

            // 6. Hiệu ứng Vòng Sáng Tầm Nhìn (HauntedSpotlightFX) & Đom Đóm Lục Bảo
            GameObject spotGo = GameObject.Find("Haunted_Spotlight_FX");
            if (spotGo == null) spotGo = new GameObject("Haunted_Spotlight_FX");
            var spotFx = spotGo.GetComponent<HauntedSpotlightFX>();
            if (spotFx == null) spotFx = spotGo.AddComponent<HauntedSpotlightFX>();
            spotFx.spotlightRadiusScale = 9.5f;

            GameObject fxGo = GameObject.Find("Night_Fireflies_FX");
            if (fxGo == null) fxGo = new GameObject("Night_Fireflies_FX");
            NightFirefliesFX fireflies = fxGo.GetComponent<NightFirefliesFX>();
            if (fireflies == null) fireflies = fxGo.AddComponent<NightFirefliesFX>();
            fireflies.fireflyCount = 48;
            fireflies.fireflyColor = new Color(0.35f, 1.0f, 0.65f, 0.88f);

            GameObject lightGo = GameObject.Find("Global Light 2D");
            if (lightGo != null)
            {
                Light2D light2d = lightGo.GetComponent<Light2D>();
                if (light2d != null)
                {
                    light2d.color = new Color(0.76f, 0.90f, 0.80f, 1.0f);
                    light2d.intensity = 0.95f;
                }
            }

            // 7. Cập nhật BattleManager & DuelEncounterManager
            var battleMgr = Object.FindFirstObjectByType<BattleManager>();
            if (battleMgr != null)
            {
                battleMgr.playerStatueX = -56.0f;
                battleMgr.enemyStatueX = 215.0f;
                battleMgr.roundTimeSeconds = 180f;
                battleMgr.p1MaxHealth = 150f;
                battleMgr.p1CurrentHealth = 150f;
                battleMgr.p2MaxHealth = 220f;
                battleMgr.p2CurrentHealth = 220f;
                EditorUtility.SetDirty(battleMgr);
            }

            var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
            if (duelMgr != null)
            {
                duelMgr.nextSceneName = "LoginScene";
                duelMgr.BindResultKOPanel();
                EditorUtility.SetDirty(duelMgr);
            }

            var audioMgr = Object.FindFirstObjectByType<ChiecLopCuoi.Audio.AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.LoadEditorClipsIfMissing();
                EditorUtility.SetDirty(audioMgr);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=green>[Level3MapBuilder]</color> Đã dựng thành công Map Level 3 (Haunted Night Forest) và lưu tại Assets/Scenes/level3.unity!");
        }

        private static void ConfigureSpriteImporter(string assetPath, float ppu)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.filterMode = FilterMode.Bilinear;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 2048;

                TextureImporterSettings settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(settings);

                ti.SaveAndReimport();
            }
        }

        private static void Spawn16ForestMonsters()
        {
            var oldMonsters = Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            Material sharedMat = null;
            foreach (var old in oldMonsters)
            {
                if (old == null) continue;
                var oldSr = old.GetComponent<SpriteRenderer>();
                if (oldSr != null && sharedMat == null) sharedMat = oldSr.sharedMaterial;
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject oldContainer = GameObject.Find("Monsters_Stage2");
            if (oldContainer != null) Object.DestroyImmediate(oldContainer);
            GameObject container = GameObject.Find("Monsters_Stage3");
            if (container != null) Object.DestroyImmediate(container);

            container = new GameObject("Monsters_Stage3");
            container.transform.position = Vector3.zero;

            MonsterSpawnSpec[] specs = new MonsterSpawnSpec[]
            {
                // Chặng 1: Bìa Rừng Sương Mù (X = -60 .. +18) -> 4 Quỷ (~3.15m - 3.35m)
                new MonsterSpawnSpec("M01_Z1_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   -37.0f, 4.35f),
                new MonsterSpawnSpec("M02_Z1_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       -24.0f, 3.92f),
                new MonsterSpawnSpec("M03_Z1_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  -9.0f, 4.10f),
                new MonsterSpawnSpec("M04_Z1_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,        6.0f, 3.78f),

                // Chặng 2: Hẻm Cây Cổ Thụ (X = +18 .. +75) -> 4 Quỷ
                new MonsterSpawnSpec("M05_Z2_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       25.0f, 3.81f),
                new MonsterSpawnSpec("M06_Z2_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  40.0f, 4.12f),
                new MonsterSpawnSpec("M07_Z2_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        54.0f, 3.94f),
                new MonsterSpawnSpec("M08_Z2_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,    66.0f, 4.38f),

                // Chặng 3: Đầm Lầy Rễ Ma (X = +75 .. +135) -> 4 Quỷ
                new MonsterSpawnSpec("M09_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       84.0f, 3.83f),
                new MonsterSpawnSpec("M10_Z3_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        98.0f, 3.96f),
                new MonsterSpawnSpec("M11_Z3_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   112.0f, 4.38f),
                new MonsterSpawnSpec("M12_Z3_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 126.0f, 4.14f),

                // Chặng 4: Đền Cổ Rừng Sâu (X = +135 .. +184) -> 4 Quỷ Tinh Nhuệ
                new MonsterSpawnSpec("M13_Z4_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       142.0f, 4.00f),
                new MonsterSpawnSpec("M14_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      155.0f, 3.86f),
                new MonsterSpawnSpec("M15_Z4_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 166.0f, 4.16f),
                new MonsterSpawnSpec("M16_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      176.0f, 3.88f),
            };

            foreach (var spec in specs)
            {
                GameObject mGo = new GameObject(spec.objectName);
                mGo.transform.SetParent(container.transform, false);
                mGo.transform.position = new Vector3(spec.worldX, -3.76f, 0f);
                mGo.transform.localScale = new Vector3(spec.scale, spec.scale, 1f);

                var sr = mGo.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Near_Layer";
                sr.sortingOrder = 8;
                if (sharedMat != null) sr.sharedMaterial = sharedMat;

                var ctrl = mGo.AddComponent<MonsterCombatController>();
                ctrl.monsterType = spec.type;
                ctrl.groundY = -3.76f;
                ctrl.ApplyPresetStatsByType();
                ctrl.LoadMonsterSpritesInEditor();
                mGo.AddComponent<MemeDeathSound>();
                EditorUtility.SetDirty(ctrl);
            }
        }

        private static void SpawnBreakableForestBarrels()
        {
            GameObject oldContainer = GameObject.Find("Forest_Supply_Barrels");
            if (oldContainer != null) Object.DestroyImmediate(oldContainer);

            GameObject container = new GameObject("Forest_Supply_Barrels");
            float[] barrelXs = new float[] { -30f, -2f, 18f, 48f, 74f, 105f, 134f, 162f, 185f };

            for (int i = 0; i < barrelXs.Length; i++)
            {
                GameObject bGo = new GameObject($"Supply_Barrel_{i + 1:00}");
                bGo.transform.SetParent(container.transform, false);
                bGo.transform.position = new Vector3(barrelXs[i], -3.76f, 0f);
                bGo.AddComponent<SpriteRenderer>();
                var prop = bGo.AddComponent<BreakableForestProp>();
                prop.dropHealthHerb = (i % 2 == 1);
                prop.EnsureSprite();
            }
        }
    }
}
