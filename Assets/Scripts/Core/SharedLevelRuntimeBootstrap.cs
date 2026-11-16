using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Core
{
    /// <summary>
    /// Kích hoạt dùng chung hệ thống HUD_Canvas, [AudioManager] và EventSystem chuẩn từ Màn 1
    /// cho các màn chơi còn lại (level2..level6) bằng code tại runtime, tránh trùng lặp cấu trúc trong từng Scene.
    /// </summary>
    public static class SharedLevelRuntimeBootstrap
    {
        private const string HudPrefabResourcePath = "Prefabs/HUD_Canvas";
        private const string AudioPrefabResourcePath = "AudioManager";

        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (_hooked) return;
            _hooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnInitialScene()
        {
            EnsureSharedLevelSystems(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSharedLevelSystems(scene);
        }

        public static void EnsureSharedLevelSystems(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            // Chỉ kích hoạt HUD chiến đấu ở các màn có Gameplay (có CowLopPlayerController hoặc BattleManager)
            var player = Object.FindFirstObjectByType<CowLopPlayerController>();
            var battleMgr = Object.FindFirstObjectByType<BattleManager>();
            if (player == null && battleMgr == null) return;

            EnsureEventSystem(scene);
            EnsureAudioManager();
            GameObject hudCanvas = EnsureHudCanvas(scene);

            if (hudCanvas != null)
            {
                ResetModalPanelsInitialState(hudCanvas);

                var menuCtrl = hudCanvas.GetComponentInChildren<HUDMenuController>(true);
                if (menuCtrl != null)
                {
                    menuCtrl.AutoBindReferences();
                }

                if (battleMgr != null)
                {
                    battleMgr.AutoBindReferences();
                    battleMgr.SetBossHUDVisible(battleMgr.npcFemale != null && battleMgr.npcFemale.combatUnlocked);
                }

                var duelMgr = Object.FindFirstObjectByType<DuelEncounterManager>();
                if (duelMgr != null)
                {
                    duelMgr.BindResultKOPanel();
                }

                VoiceCommandController.EnsureMounted(hudCanvas);
                HUDActionButtonsController.TryAttachToCurrentScene();
                HubRobotController.EnsureMounted(hudCanvas);
            }
        }

        private static GameObject EnsureHudCanvas(Scene scene)
        {
            GameObject existing = GameObject.Find("HUD_Canvas");
            if (existing != null) return existing;

            GameObject prefab = Resources.Load<GameObject>(HudPrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[SharedLevelRuntimeBootstrap] Không tìm thấy Prefab chuẩn tại Resources/{HudPrefabResourcePath}");
                return null;
            }

            GameObject hudInstance = Object.Instantiate(prefab);
            hudInstance.name = "HUD_Canvas";
            SceneManager.MoveGameObjectToScene(hudInstance, scene);

            var canvas = hudInstance.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }

            return hudInstance;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(esGo, scene);
        }

        private static void EnsureAudioManager()
        {
            if (AudioManager.Instance != null) return;

            GameObject prefab = Resources.Load<GameObject>(AudioPrefabResourcePath);
            GameObject audioGo = prefab != null
                ? Object.Instantiate(prefab)
                : new GameObject("[AudioManager]", typeof(AudioManager));

            audioGo.name = "[AudioManager]";
            Object.DontDestroyOnLoad(audioGo);
        }

        private static void ResetModalPanelsInitialState(GameObject hudCanvas)
        {
            string[] hiddenPanels =
            {
                "HUD_Menu",
                "Result_KO_Panel",
                "Tutorial_Guide_Panel",
                "KO_Splash_Overlay",
                "Duel_Encounter_Banner",
                "Victory_Finale_Panel",
                "Audio_Settings_Modal"
            };

            foreach (string panelName in hiddenPanels)
            {
                Transform tf = hudCanvas.transform.Find(panelName);
                if (tf != null) tf.gameObject.SetActive(false);
            }
        }
    }
}
