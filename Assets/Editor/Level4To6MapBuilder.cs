#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Environment;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.NPC;

namespace ChiecLopCuoi.Editor
{
    /// <summary>
    /// Builder dùng chung để dựng 3 map mới (Màn 4, 5, 6 - Cổ trấn & Thành quách)
    /// với kiến trúc nền 3 lớp dài 280m, 4 chặng chốt chặn, ánh sáng & hiệu ứng môi trường đặc trưng.
    /// </summary>
    public static class Level4To6MapBuilder
    {
        private const string BaseTemplateScenePath = "Assets/Scenes/level3.unity";

        private struct MonsterSpawnSpec
        {
            public string objectName;
            public MonsterCombatController.MonsterType type;
            public float worldX;
            public float scale;

            public MonsterSpawnSpec(string name, MonsterCombatController.MonsterType t, float x, float s = 2.30f)
            {
                objectName = name;
                type = t;
                worldX = x;
                scale = s;
            }
        }

        private class MapThemeConfig
        {
            public string mapDisplayName;
            public string scenePath;
            public string nextSceneName;
            public string farSpritePath;
            public string midSpritePath;
            public string groundSpritePath;
            public Color ambientColor;
            public float ambientIntensity;
            public Color fireflyColor;
            public int fireflyCount;
            public float spotlightRadius; // <= 0 sẽ gỡ bỏ HauntedSpotlightFX
            public string[] zoneNames;
            public float heroMaxHp;
            public float bossMaxHp;
            public string monsterContainerName;
            public MonsterSpawnSpec[] monsterSpecs;
            public float[] barrelXs;
        }

        [MenuItem("Tools/Maps/Build Level 4 Map (Pho Den Long Dem)")]
        public static string BuildLevel4() => BuildThemeMap(CreateLevel4Config());

        [MenuItem("Tools/Maps/Build Level 5 Map (Cau Da Hoang Phe)")]
        public static string BuildLevel5() => BuildThemeMap(CreateLevel5Config());

        [MenuItem("Tools/Maps/Build Level 6 Map (Cong Thanh Huyet Nguyet)")]
        public static string BuildLevel6() => BuildThemeMap(CreateLevel6Config());

        [MenuItem("Tools/Maps/Build All New Maps (Level 4, 5, 6)")]
        public static string BuildAllNewMaps()
        {
            string r4 = BuildLevel4();
            string r5 = BuildLevel5();
            string r6 = BuildLevel6();
            return $"{r4}\n{r5}\n{r6}";
        }

        private static MapThemeConfig CreateLevel4Config() => new MapThemeConfig
        {
            mapDisplayName = "Màn 4: Phố Đèn Lồng Đêm",
            scenePath = "Assets/Scenes/level4.unity",
            nextSceneName = "level5",
            farSpritePath = "Assets/Art/Environments/Level4_LanternTown/01_far_sky/bg_far_lantern_night.png",
            midSpritePath = "Assets/Art/Environments/Level4_LanternTown/02_mid_town/bg_mid_lantern_town.png",
            groundSpritePath = "Assets/Art/Environments/Level4_LanternTown/03_near_ground/layer_03_cobble_town.png",
            ambientColor = new Color(1.00f, 0.88f, 0.74f, 1f),
            ambientIntensity = 1.06f,
            fireflyColor = new Color(1.00f, 0.78f, 0.30f, 0.90f),
            fireflyCount = 46,
            spotlightRadius = 0f,
            zoneNames = new[]
            {
                "CHẶNG 1/4: ĐẦU PHỐ ĐÈN LỒNG",
                "CHẶNG 2/4: NGÃ TƯ CỔ TRẤN",
                "CHẶNG 3/4: XÓM LỒNG ĐÈN",
                "CHẶNG 4/4: QUẢNG TRƯỜNG PHỐ CỔ"
            },
            heroMaxHp = 150f,
            bossMaxHp = 400f,
            monsterContainerName = "Monsters_Stage4",
            monsterSpecs = CreateLevel4MixSpecs(),
            barrelXs = new[] { -28f, -4f, 20f, 48f, 76f, 106f, 136f, 164f, 186f }
        };

        private static MapThemeConfig CreateLevel5Config() => new MapThemeConfig
        {
            mapDisplayName = "Màn 5: Cầu Đá Hoang Phế",
            scenePath = "Assets/Scenes/level5.unity",
            nextSceneName = "level6",
            farSpritePath = "Assets/Art/Environments/Level5_RuinedStoneBridge/01_far_sky/bg_far_misty_gorge.png",
            midSpritePath = "Assets/Art/Environments/Level5_RuinedStoneBridge/02_mid_bridge/bg_mid_ruined_bridge.png",
            groundSpritePath = "Assets/Art/Environments/Level5_RuinedStoneBridge/03_near_ground/layer_03_bridge_deck.png",
            ambientColor = new Color(0.82f, 0.88f, 0.98f, 1f),
            ambientIntensity = 0.98f,
            fireflyColor = new Color(0.55f, 0.92f, 1.00f, 0.85f),
            fireflyCount = 50,
            spotlightRadius = 11.5f,
            zoneNames = new[]
            {
                "CHẶNG 1/4: ĐẦU CẦU ĐÁ",
                "CHẶNG 2/4: NHỊP CẦU GÃY",
                "CHẶNG 3/4: CỔNG MIẾU ĐÁ",
                "CHẶNG 4/4: BỜ VỰC SƯƠNG MÙ"
            },
            heroMaxHp = 150f,
            bossMaxHp = 600f,
            monsterContainerName = "Monsters_Stage5",
            monsterSpecs = CreateLevel5MixSpecs(),
            barrelXs = new[] { -30f, -2f, 18f, 46f, 74f, 105f, 134f, 162f, 185f }
        };

        private static MapThemeConfig CreateLevel6Config() => new MapThemeConfig
        {
            mapDisplayName = "Màn 6: Cổng Thành Huyết Nguyệt",
            scenePath = "Assets/Scenes/level6.unity",
            nextSceneName = "LevelSelectScene",
            farSpritePath = "Assets/Art/Environments/Level6_BloodMoonCitadel/01_far_sky/bg_far_blood_moon.png",
            midSpritePath = "Assets/Art/Environments/Level6_BloodMoonCitadel/02_mid_citadel/bg_mid_citadel_walls.png",
            groundSpritePath = "Assets/Art/Environments/Level6_BloodMoonCitadel/03_near_ground/layer_03_citadel_stone.png",
            ambientColor = new Color(0.98f, 0.72f, 0.70f, 1f),
            ambientIntensity = 0.96f,
            fireflyColor = new Color(1.00f, 0.36f, 0.22f, 0.90f),
            fireflyCount = 56,
            spotlightRadius = 0f,
            zoneNames = new[]
            {
                "CHẶNG 1/4: NGOẠI THÀNH ĐỎ",
                "CHẶNG 2/4: HÀO ĐÁ CỔ",
                "CHẶNG 3/4: SÂN HUYẾT NGUYỆT",
                "CHẶNG 4/4: TRƯỚC CỔNG THÀNH"
            },
            heroMaxHp = 150f,
            bossMaxHp = 600f,
            monsterContainerName = "Monsters_Stage6",
            monsterSpecs = CreateLevel6ElementalSpecs(),
            barrelXs = new[] { -30f, -2f, 18f, 48f, 74f, 105f, 134f, 162f, 185f }
        };

        // Màn 4: 4 Quái Nội Tâm thuần chuẩn (Quỷ Ghen, Quỷ Đau, Quỷ Tự Ti, Quỷ Giận)
        private static MonsterSpawnSpec[] CreateLevel4MixSpecs() => new[]
        {
            new MonsterSpawnSpec("M01_Z1_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   -37.0f, 4.35f),
            new MonsterSpawnSpec("M02_Z1_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       -24.0f, 3.92f),
            new MonsterSpawnSpec("M03_Z1_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  -9.0f, 4.10f),
            new MonsterSpawnSpec("M04_Z1_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,        6.0f, 3.78f),

            new MonsterSpawnSpec("M05_Z2_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       25.0f, 3.81f),
            new MonsterSpawnSpec("M06_Z2_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  40.0f, 4.12f),
            new MonsterSpawnSpec("M07_Z2_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        54.0f, 3.94f),
            new MonsterSpawnSpec("M08_Z2_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,    66.0f, 4.38f),

            new MonsterSpawnSpec("M09_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       84.0f, 3.83f),
            new MonsterSpawnSpec("M10_Z3_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        98.0f, 3.96f),
            new MonsterSpawnSpec("M11_Z3_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   112.0f, 4.38f),
            new MonsterSpawnSpec("M12_Z3_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 126.0f, 4.14f),

            new MonsterSpawnSpec("M13_Z4_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       142.0f, 4.00f),
            new MonsterSpawnSpec("M14_Z4_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   155.0f, 4.40f),
            new MonsterSpawnSpec("M15_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      166.0f, 3.86f),
            new MonsterSpawnSpec("M16_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      176.0f, 3.88f),
        };

        // Màn 5: Cầu Đá Hoang Phế — 16 Quái Nội Tâm cường hóa theo 4 chặng
        private static MonsterSpawnSpec[] CreateLevel5MixSpecs() => new[]
        {
            new MonsterSpawnSpec("M01_Z1_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   -37.0f, 4.36f),
            new MonsterSpawnSpec("M02_Z1_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       -24.0f, 3.94f),
            new MonsterSpawnSpec("M03_Z1_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  -9.0f, 4.12f),
            new MonsterSpawnSpec("M04_Z1_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,        6.0f, 3.81f),

            new MonsterSpawnSpec("M05_Z2_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  25.0f, 4.12f),
            new MonsterSpawnSpec("M06_Z2_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        40.0f, 3.96f),
            new MonsterSpawnSpec("M07_Z2_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,    54.0f, 4.38f),
            new MonsterSpawnSpec("M08_Z2_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       66.0f, 3.83f),

            new MonsterSpawnSpec("M09_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       84.0f, 3.84f),
            new MonsterSpawnSpec("M10_Z3_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        98.0f, 3.98f),
            new MonsterSpawnSpec("M11_Z3_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   112.0f, 4.40f),
            new MonsterSpawnSpec("M12_Z3_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 126.0f, 4.15f),

            new MonsterSpawnSpec("M13_Z4_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       142.0f, 4.00f),
            new MonsterSpawnSpec("M14_Z4_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 155.0f, 4.16f),
            new MonsterSpawnSpec("M15_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      166.0f, 3.86f),
            new MonsterSpawnSpec("M16_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      176.0f, 3.88f),
        };

        // Màn 6: Cổng Thành Huyết Nguyệt — Đội quân Quỷ Nội Tâm tinh nhuệ (thay thế hoàn toàn Shadow)
        private static MonsterSpawnSpec[] CreateLevel6ElementalSpecs() => new[]
        {
            new MonsterSpawnSpec("M01_Z1_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   -37.0f, 4.38f),
            new MonsterSpawnSpec("M02_Z1_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       -24.0f, 3.96f),
            new MonsterSpawnSpec("M03_Z1_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  -9.0f, 4.14f),
            new MonsterSpawnSpec("M04_Z1_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,        6.0f, 3.83f),

            new MonsterSpawnSpec("M05_Z2_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        25.0f, 3.98f),
            new MonsterSpawnSpec("M06_Z2_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       40.0f, 3.84f),
            new MonsterSpawnSpec("M07_Z2_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,    54.0f, 4.40f),
            new MonsterSpawnSpec("M08_Z2_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon,  66.0f, 4.16f),

            new MonsterSpawnSpec("M09_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,       84.0f, 3.86f),
            new MonsterSpawnSpec("M10_Z3_PainDemon",       MonsterCombatController.MonsterType.PainDemon,        98.0f, 4.00f),
            new MonsterSpawnSpec("M11_Z3_InsecurityDemon", MonsterCombatController.MonsterType.InsecurityDemon, 112.0f, 4.16f),
            new MonsterSpawnSpec("M12_Z3_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      126.0f, 3.86f),

            new MonsterSpawnSpec("M13_Z4_PainDemon",       MonsterCombatController.MonsterType.PainDemon,       142.0f, 4.02f),
            new MonsterSpawnSpec("M14_Z4_JealousyDemon",   MonsterCombatController.MonsterType.JealousyDemon,   155.0f, 4.42f),
            new MonsterSpawnSpec("M15_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      166.0f, 3.88f),
            new MonsterSpawnSpec("M16_Z4_AngerDemon",      MonsterCombatController.MonsterType.AngerDemon,      176.0f, 3.90f),
        };

        private static string BuildThemeMap(MapThemeConfig config)
        {
            if (!File.Exists(config.farSpritePath) ||
                !File.Exists(config.midSpritePath) ||
                !File.Exists(config.groundSpritePath))
            {
                return $"[Level4To6MapBuilder] SKIPPED {config.mapDisplayName}: thiếu file art nền ({config.farSpritePath} / {config.midSpritePath} / {config.groundSpritePath}).";
            }

            EditorSceneManager.SaveOpenScenes();
            if (File.Exists(config.scenePath))
            {
                AssetDatabase.DeleteAsset(config.scenePath);
            }
            AssetDatabase.CopyAsset(BaseTemplateScenePath, config.scenePath);
            AssetDatabase.Refresh();

            ConfigureRepeatSpriteImporter(config.farSpritePath, 100f);
            ConfigureRepeatSpriteImporter(config.midSpritePath, 100f);
            ConfigureRepeatSpriteImporter(config.groundSpritePath, 100f);
            Level2MapBuilder.ConfigureDemonSpritesPivot();

            Sprite farSprite = AssetDatabase.LoadAssetAtPath<Sprite>(config.farSpritePath);
            Sprite midSprite = AssetDatabase.LoadAssetAtPath<Sprite>(config.midSpritePath);
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(config.groundSpritePath);
            if (farSprite == null || midSprite == null || groundSprite == null)
            {
                return $"[Level4To6MapBuilder] ERROR {config.mapDisplayName}: Không load được sprite sau khi import.";
            }

            var activeScene = EditorSceneManager.OpenScene(config.scenePath, OpenSceneMode.Single);
            Level2MapBuilder.CopyHUDCanvasFromLevel1(activeScene);

            ApplyFarLayer(farSprite);
            ApplyMidLayer(midSprite);
            SpriteRenderer groundSr = ApplyGroundLayer(groundSprite);
            ConfigureActorsAndStatues(config);
            ConfigureZoneGates(config);
            SpawnMonsters(config);
            SpawnSupplyBarrels(config.barrelXs);
            ConfigureAtmosphereAndCamera(config, groundSr);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            EnsureScenesInBuildSettings(config.scenePath);

            return $"[Level4To6MapBuilder] Built {config.mapDisplayName} -> {config.scenePath}";
        }

        private static void ApplyFarLayer(Sprite farSprite)
        {
            GameObject bgFarGo = GameObject.Find("BG_Far");
            if (bgFarGo == null) return;

            for (int i = bgFarGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(bgFarGo.transform.GetChild(i).gameObject);
            }

            Vector3 pos = bgFarGo.transform.position;
            bgFarGo.transform.position = new Vector3(76.8f, pos.y, pos.z);
            bgFarGo.transform.rotation = Quaternion.identity;
            bgFarGo.transform.localScale = Vector3.one;

            var farParallax = bgFarGo.GetComponent<ParallaxLayer>();
            if (farParallax != null)
            {
                farParallax.parallaxSpeed = 0.12f;
                farParallax.isLooping = true;
            }

            SpriteRenderer rootSr = bgFarGo.GetComponent<SpriteRenderer>();
            if (rootSr != null)
            {
                rootSr.sprite = farSprite;
                rootSr.drawMode = SpriteDrawMode.Tiled;
                rootSr.tileMode = SpriteTileMode.Continuous;
                rootSr.size = new Vector2(441.6f, 10.8f);
                rootSr.color = Color.white;
                EditorUtility.SetDirty(rootSr);
            }
        }

        private static void ApplyMidLayer(Sprite midSprite)
        {
            GameObject bgMidGo = GameObject.Find("BG_Mid") ?? new GameObject("BG_Mid");
            bgMidGo.transform.position = new Vector3(80f, -1.08f, 10f);
            bgMidGo.transform.rotation = Quaternion.identity;
            bgMidGo.transform.localScale = Vector3.one;

            var sr = bgMidGo.GetComponent<SpriteRenderer>() ?? bgMidGo.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sprite = midSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(420f, 5.00f);
            sr.sortingLayerName = "Mid_Layer";
            sr.sortingOrder = 0;

            var pl = bgMidGo.GetComponent<ParallaxLayer>() ?? bgMidGo.AddComponent<ParallaxLayer>();
            pl.parallaxSpeed = 0.5f;
            pl.isLooping = true;
        }

        private static SpriteRenderer ApplyGroundLayer(Sprite groundSprite)
        {
            GameObject groundGo = GameObject.Find("Layer3_Ground") ?? new GameObject("Layer3_Ground");
            groundGo.transform.position = new Vector3(80f, -4.92f, 0f);
            groundGo.transform.rotation = Quaternion.identity;
            groundGo.transform.localScale = Vector3.one;

            var sr = groundGo.GetComponent<SpriteRenderer>() ?? groundGo.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sprite = groundSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(420f, 3.00f);
            sr.sortingLayerName = "Near_Layer";
            sr.sortingOrder = 0;

            var pl = groundGo.GetComponent<ParallaxLayer>() ?? groundGo.AddComponent<ParallaxLayer>();
            pl.parallaxSpeed = 1.0f;
            pl.isLooping = true;

            var col = groundGo.GetComponent<BoxCollider2D>() ?? groundGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(420f, 4.0f);
            col.offset = new Vector2(0f, -0.84f);

            return sr;
        }

        private static void ConfigureActorsAndStatues(MapThemeConfig config)
        {
            GameObject leftStatue = GameObject.Find("prop_statue_girl_0");
            if (leftStatue != null) leftStatue.transform.position = new Vector3(-56.0f, -1.30f, 0f);

            GameObject rightStatue = GameObject.Find("prop_statue_girl_gift_left_0");
            if (rightStatue != null) rightStatue.transform.position = new Vector3(215.0f, -0.13f, 0f);

            var cowLop = Object.FindFirstObjectByType<CowLopPlayerController>();
            if (cowLop != null)
            {
                cowLop.transform.position = new Vector3(-50.0f, -3.76f, 0f);
                cowLop.transform.localScale = new Vector3(2.81f, 2.81f, 1f);
                cowLop.groundY = -3.76f;
                cowLop.minX = -58.0f;
                cowLop.maxX = 218.0f;
                cowLop.maxHealth = config.heroMaxHp;
                cowLop.currentHealth = config.heroMaxHp;
                cowLop.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(cowLop);
            }

            var npcFemale = Object.FindFirstObjectByType<NpcCombatController>();
            if (npcFemale != null)
            {
                npcFemale.transform.position = new Vector3(208.0f, -3.76f, 0f);
                npcFemale.transform.localScale = new Vector3(-1.85f, 1.85f, 1f);
                npcFemale.groundY = -3.76f;
                npcFemale.minStageX = -56.0f;
                npcFemale.maxStageX = 216.0f;
                npcFemale.arenaTriggerPlayerX = 175.0f;
                npcFemale.maxHealth = config.bossMaxHp;
                npcFemale.currentHealth = config.bossMaxHp;
                npcFemale.combatUnlocked = false;
                npcFemale.AutoLoadSpritesInEditor();
                EditorUtility.SetDirty(npcFemale);
            }

            var battleMgr = Object.FindFirstObjectByType<BattleManager>();
            if (battleMgr != null)
            {
                battleMgr.playerStatueX = -56.0f;
                battleMgr.enemyStatueX = 215.0f;
                battleMgr.roundTimeSeconds = 150f;
                battleMgr.p1MaxHealth = config.heroMaxHp;
                battleMgr.p1CurrentHealth = config.heroMaxHp;
                battleMgr.p2MaxHealth = config.bossMaxHp;
                battleMgr.p2CurrentHealth = config.bossMaxHp;
                EditorUtility.SetDirty(battleMgr);
            }

            var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
            if (duelMgr != null)
            {
                duelMgr.nextSceneName = config.nextSceneName;
                duelMgr.BindResultKOPanel();
                EditorUtility.SetDirty(duelMgr);
            }
        }

        private static void ConfigureZoneGates(MapThemeConfig config)
        {
            var gateMgr = Object.FindFirstObjectByType<StageZoneGateManager>();
            if (gateMgr == null)
            {
                var gateGo = new GameObject("StageZoneGateManager");
                gateMgr = gateGo.AddComponent<StageZoneGateManager>();
            }

            gateMgr.zones = new List<StageZoneGateManager.StageZone>
            {
                new StageZoneGateManager.StageZone { zoneName = config.zoneNames[0], minX = -60f, maxX = 18f,  gateLimitX = 15f },
                new StageZoneGateManager.StageZone { zoneName = config.zoneNames[1], minX = 18f,  maxX = 75f,  gateLimitX = 72f },
                new StageZoneGateManager.StageZone { zoneName = config.zoneNames[2], minX = 75f,  maxX = 135f, gateLimitX = 132f },
                new StageZoneGateManager.StageZone { zoneName = config.zoneNames[3], minX = 135f, maxX = 184f, gateLimitX = 182f }
            };
            EditorUtility.SetDirty(gateMgr);
        }

        private static void SpawnMonsters(MapThemeConfig config)
        {
            Material sharedMat = null;
            foreach (var old in Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None))
            {
                if (old == null) continue;
                var oldSr = old.GetComponent<SpriteRenderer>();
                if (oldSr != null && sharedMat == null) sharedMat = oldSr.sharedMaterial;
                Object.DestroyImmediate(old.gameObject);
            }

            string[] oldContainers = { "Monsters_Stage2", "Monsters_Stage3", "Monsters_Stage4", "Monsters_Stage5", "Monsters_Stage6" };
            foreach (string cname in oldContainers)
            {
                GameObject existing = GameObject.Find(cname);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            var container = new GameObject(config.monsterContainerName);
            foreach (var spec in config.monsterSpecs)
            {
                var mGo = new GameObject(spec.objectName);
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

        private static void SpawnSupplyBarrels(float[] barrelXs)
        {
            GameObject oldContainer = GameObject.Find("Forest_Supply_Barrels");
            if (oldContainer != null) Object.DestroyImmediate(oldContainer);

            var container = new GameObject("Forest_Supply_Barrels");
            for (int i = 0; i < barrelXs.Length; i++)
            {
                var bGo = new GameObject($"Supply_Barrel_{i + 1:00}");
                bGo.transform.SetParent(container.transform, false);
                bGo.transform.position = new Vector3(barrelXs[i], -3.76f, 0f);
                bGo.AddComponent<SpriteRenderer>();
                var prop = bGo.AddComponent<BreakableForestProp>();
                prop.dropHealthHerb = (i % 2 == 1);
                prop.EnsureSprite();
            }
        }

        private static void ConfigureAtmosphereAndCamera(MapThemeConfig config, SpriteRenderer groundSr)
        {
            GameObject spotGo = GameObject.Find("Haunted_Spotlight_FX");
            if (config.spotlightRadius > 0f)
            {
                if (spotGo == null) spotGo = new GameObject("Haunted_Spotlight_FX");
                var spotFx = spotGo.GetComponent<HauntedSpotlightFX>() ?? spotGo.AddComponent<HauntedSpotlightFX>();
                spotFx.spotlightRadiusScale = config.spotlightRadius;
            }
            else if (spotGo != null)
            {
                Object.DestroyImmediate(spotGo);
            }

            GameObject fxGo = GameObject.Find("Night_Fireflies_FX") ?? new GameObject("Night_Fireflies_FX");
            var fireflies = fxGo.GetComponent<NightFirefliesFX>() ?? fxGo.AddComponent<NightFirefliesFX>();
            fireflies.fireflyCount = config.fireflyCount;
            fireflies.fireflyColor = config.fireflyColor;

            GameObject lightGo = GameObject.Find("Global Light 2D");
            if (lightGo != null)
            {
                var light2d = lightGo.GetComponent<Light2D>();
                if (light2d != null)
                {
                    light2d.color = config.ambientColor;
                    light2d.intensity = config.ambientIntensity;
                }
            }

            GameObject camGo = GameObject.Find("Main Camera");
            if (camGo != null)
            {
                camGo.transform.position = new Vector3(-46.0f, 0f, -10f);
                var dragCam = camGo.GetComponent<DragCamera2D>();
                if (dragCam != null)
                {
                    dragCam.groundRenderer = groundSr;
                    dragCam.mapMinX = -60f;
                    dragCam.mapMaxX = 220f;
                    dragCam.UpdateCameraLimits();
                    EditorUtility.SetDirty(dragCam);
                }
            }

            var audioMgr = Object.FindFirstObjectByType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.LoadEditorClipsIfMissing();
                EditorUtility.SetDirty(audioMgr);
            }
        }

        private static void ConfigureRepeatSpriteImporter(string assetPath, float ppu)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) return;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        public static void EnsureScenesInBuildSettings(params string[] requiredScenePaths)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;
            foreach (string path in requiredScenePaths)
            {
                if (!File.Exists(path)) continue;
                bool exists = list.Exists(s => s.path == path);
                if (!exists)
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                    changed = true;
                }
            }
            if (changed)
            {
                EditorBuildSettings.scenes = list.ToArray();
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/Maps/Convert All Levels BG_Far to Seamless Tiled")]
        public static string UpgradeAllExistingLevelsSeamless()
        {
            ConfigureRepeatSpriteImporter("Assets/Art/Environments/Moon_Panorama/01_far_space/bg_far_space.png", 100f);
            ConfigureRepeatSpriteImporter("Assets/Art/Environments/Moon_Panorama/02_mid_terrain/bg_mid_terrain.png", 100f);
            ConfigureRepeatSpriteImporter("Assets/Art/Environments/Level2_Village/03_near_ground/layer_03_grass_ground.png", 100f);
            ConfigureRepeatSpriteImporter("Assets/Art/Environments/Level3_HauntedForest/01_far_sky/bg_far_forest_night.png", 100f);

            string[] existingScenes =
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/level2.unity",
                "Assets/Scenes/level3.unity"
            };

            var results = new List<string>();
            foreach (string scenePath in existingScenes)
            {
                if (!File.Exists(scenePath)) continue;
                var sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject bgFarGo = GameObject.Find("BG_Far");
                if (bgFarGo != null)
                {
                    for (int i = bgFarGo.transform.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(bgFarGo.transform.GetChild(i).gameObject);
                    }
                    var sr = bgFarGo.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        sr.drawMode = SpriteDrawMode.Tiled;
                        sr.tileMode = SpriteTileMode.Continuous;
                        float h = sr.sprite.bounds.size.y > 0.1f ? sr.sprite.bounds.size.y : 10.8f;
                        sr.size = new Vector2(441.6f, h);
                        EditorUtility.SetDirty(sr);
                    }
                    Vector3 p = bgFarGo.transform.position;
                    bgFarGo.transform.position = new Vector3(76.8f, p.y, p.z);
                    bgFarGo.transform.localScale = Vector3.one;
                    EditorSceneManager.MarkSceneDirty(sc);
                    EditorSceneManager.SaveScene(sc);
                    results.Add($"Upgraded BG_Far Tiled: {scenePath}");
                }
            }
            EnsureScenesInBuildSettings(
                "Assets/Scenes/LoginScene.unity",
                "Assets/Scenes/LevelSelectScene.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/level2.unity",
                "Assets/Scenes/level3.unity",
                "Assets/Scenes/level4.unity",
                "Assets/Scenes/level5.unity",
                "Assets/Scenes/level6.unity"
            );
            return string.Join("\n", results);
        }
    }
}
#endif
