#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.Audio;
using System.IO;

namespace ChiecLopCuoi.Editor
{
    [InitializeOnLoad]
    public static class SetupAudioScenes
    {
        static SetupAudioScenes()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            // Kiểm tra xem Prefab đã tồn tại chưa, nếu chưa thì tự động chạy setup
            if (!File.Exists("Assets/Resources/AudioManager.prefab"))
            {
                SetupAll();
            }
        }

        [MenuItem("Tools/Audio/Setup Full Game Audio (All Scenes & Prefab)")]
        public static void SetupAll()
        {
            Debug.Log("[SetupAudioScenes] Starting Audio Setup across the project...");

            // 1. Tạo thư mục nếu chưa có
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 2. Load Clips
            var bgmLogin = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_login_epic.wav");
            var bgmBattle = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_battle_epic.wav");
            var sfxRun = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_run.wav");
            var sfxJump = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump.wav");
            var sfxHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_hit_impact.wav");
            var sfxPickup = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_pickup.wav");
            var uiAttack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_attack.wav");
            var uiDefend = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_defend.wav");
            var uiRetreat = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_retreat.wav");
            var uiClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_click.wav");

            // 3. Tạo AudioManager GameObject & Prefab
            var tempGo = new GameObject("[AudioManager]");
            var audioMgr = tempGo.AddComponent<AudioManager>();
            audioMgr.bgmLoginClip = bgmLogin;
            audioMgr.bgmBattleClip = bgmBattle;
            audioMgr.sfxRunClip = sfxRun;
            audioMgr.sfxJumpClip = sfxJump;
            audioMgr.sfxHitImpactClip = sfxHit;
            audioMgr.sfxPickupClip = sfxPickup;
            audioMgr.uiBtnAttackClip = uiAttack;
            audioMgr.uiBtnDefendClip = uiDefend;
            audioMgr.uiBtnRetreatClip = uiRetreat;
            audioMgr.uiClickClip = uiClick;

            // Lưu thành Prefab trong Resources và Prefabs
            string prefabResPath = "Assets/Resources/AudioManager.prefab";
            string prefabNormalPath = "Assets/Prefabs/AudioManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabResPath);
            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabNormalPath);
            Object.DestroyImmediate(tempGo);

            Debug.Log("[SetupAudioScenes] Saved AudioManager prefab to " + prefabResPath);

            // 4. Setup các Scene
            string originalScenePath = SceneManager.GetActiveScene().path;

            SetupScene("Assets/Scenes/LoginScene.unity", prefabNormalPath);
            SetupScene("Assets/Scenes/SampleScene.unity", prefabNormalPath);

            // 5. Quay lại scene ban đầu
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SetupAudioScenes] SUCCESS: All scenes and AudioManager prefab fully set up with audio!");
        }

        private static void SetupScene(string scenePath, string prefabPath)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // Check AudioManager
            var existing = Object.FindFirstObjectByType<AudioManager>();
            if (existing == null && prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "[AudioManager]";
                Undo.RegisterCreatedObjectUndo(instance, "Add AudioManager");
            }

            // Ensure AudioListener on Camera
            var cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam != null && cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log($"[SetupAudioScenes] Added AudioListener to camera in {scenePath}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SetupAudioScenes] Configured scene: {scenePath}");
        }
    }
}
#endif
