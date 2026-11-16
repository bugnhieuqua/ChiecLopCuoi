using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ChiecLopCuoi.UI;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.Editor
{
    public static class LoginSceneBuilder
    {
        private const string POSTER_PATH = "Assets/Art/UI/Backgrounds/bg_login_cowlop_title.png";

        [MenuItem("Tools/Build Login Scene")]
        public static void BuildLoginScene()
        {
            EditorSceneManager.SaveOpenScenes();

            ConfigurePosterImporter(POSTER_PATH);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(16, 18, 28, 255);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // 2. EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            // 3. Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 4. Background Poster (Cow Lốp đeo Lốp Xe + Tên Game "CHIẾC LỐP CUỐI")
            var bgGo = new GameObject("BG_Login");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgRect.pivot = new Vector2(0.5f, 0.5f);

            var bgImage = bgGo.AddComponent<Image>();
            Sprite posterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(POSTER_PATH);
            if (posterSprite == null)
            {
                posterSprite = LoadSprite("Assets/Art/UI/Backgrounds/bg_login_screen_epic.png", "bg_login_screen_epic_0");
            }
            bgImage.sprite = posterSprite;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            // 5. Play Button (Btn_Play)
            var btnGo = new GameObject("Btn_Play");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.18f);
            btnRect.anchorMax = new Vector2(0.5f, 0.18f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(310f, 195f);
            btnRect.anchoredPosition = new Vector2(0f, 15f);

            var btnImage = btnGo.AddComponent<Image>();
            btnImage.sprite = LoadSprite("Assets/Art/UI/Buttons/ui_btn_play.png", "ui_btn_play_0");
            btnImage.preserveAspect = true;
            btnImage.raycastTarget = true;

            var button = btnGo.AddComponent<Button>();
            button.targetGraphic = btnImage;
            button.transition = Selectable.Transition.ColorTint;
            btnGo.AddComponent<UIPulseEffect>();

            // 6. Cụm Chọn Màn Nhanh (MÀN 1 / MÀN 2 / MÀN 3) dùng đúng E:\LTGAME\ChiecLopCuoi\Assets\Art\Btn_map\btn_map.png
            var groupGo = new GameObject("LevelSelect_Group", typeof(RectTransform));
            groupGo.transform.SetParent(canvasGo.transform, false);
            var groupRt = groupGo.GetComponent<RectTransform>();
            groupRt.anchorMin = new Vector2(0.5f, 0.06f);
            groupRt.anchorMax = new Vector2(0.5f, 0.06f);
            groupRt.pivot = new Vector2(0.5f, 0.5f);
            groupRt.sizeDelta = new Vector2(920f, 115f);
            groupRt.anchoredPosition = new Vector2(0f, 8f);

            Sprite btnMapSprite = LoadSprite("Assets/Art/Btn_map/btn_map.png", "btn_map_0");

            Button btnLv1 = CreateLevelSelectButton(groupGo.transform, "Btn_Level1", "MÀN 1: BÌNH MINH", new Vector2(-300f, 0f), btnMapSprite);
            Button btnLv2 = CreateLevelSelectButton(groupGo.transform, "Btn_Level2", "MÀN 2: LÀNG ĐÊM", new Vector2(0f, 0f), btnMapSprite);
            Button btnLv3 = CreateLevelSelectButton(groupGo.transform, "Btn_Level3", "MÀN 3: RỪNG MA", new Vector2(300f, 0f), btnMapSprite);

            // 6b. Khởi tạo LoreCanvas (Sổ Tay Tiểu Sử - cỡ chữ 26pt dễ đọc) trong LoginScene
            StoryBookManager storyMgr = null;
            GameObject lorePanelObj = null;
            var lorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoreCanvas.prefab");
            if (lorePrefab != null)
            {
                var loreInstance = (GameObject)PrefabUtility.InstantiatePrefab(lorePrefab);
                loreInstance.name = "LoreCanvas";
                storyMgr = loreInstance.GetComponent<StoryBookManager>();
                if (storyMgr != null)
                {
                    storyMgr.loadSceneOnComplete = true;
                    storyMgr.targetSceneName = "SampleScene";
                    storyMgr.storyFontSize = 26f;
                    if (storyMgr.storyText != null)
                    {
                        storyMgr.storyText.fontSize = 26f;
                        storyMgr.storyText.fontStyle = FontStyles.Bold;
                    }
                    if (storyMgr.lorePanel != null)
                    {
                        lorePanelObj = storyMgr.lorePanel;
                        lorePanelObj.SetActive(false); // Chỉ mở khi nhấn Play
                    }
                    EditorUtility.SetDirty(storyMgr);
                }
            }

            // 7. LoginScreenManager
            var manager = canvasGo.AddComponent<LoginScreenManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("gameplaySceneName").stringValue = "SampleScene";
            so.FindProperty("playButton").objectReferenceValue = button;
            so.FindProperty("level1Button").objectReferenceValue = btnLv1;
            so.FindProperty("level2Button").objectReferenceValue = btnLv2;
            so.FindProperty("level3Button").objectReferenceValue = btnLv3;
            if (storyMgr != null) so.FindProperty("storyBookManager").objectReferenceValue = storyMgr;
            if (lorePanelObj != null) so.FindProperty("loreCanvasOrPanel").objectReferenceValue = lorePanelObj;
            so.ApplyModifiedProperties();

            // 8. AudioManager (với BGM 48kHz bgm_login_epic.wav)
            var audioGo = new GameObject("[AudioManager]");
            var audioMgr = audioGo.AddComponent<AudioManager>();
            audioMgr.LoadEditorClipsIfMissing();
            EditorUtility.SetDirty(audioMgr);

            // 9. Save Scene
            string scenePath = "Assets/Scenes/LoginScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=green>[LoginSceneBuilder]</color> Đã dựng thành công LoginScene với Poster CHIẾC LỐP CUỐI + Nhạc 48kHz + Chọn Màn 1/2/3!");

            // 10. Đăng ký đủ 4 Scene vào Build Settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LoginScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/level2.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/level3.unity", true)
            };
            EditorBuildSettings.scenes = buildScenes;
        }

        private static Button CreateLevelSelectButton(Transform parent, string name, string label, Vector2 anchoredPos, Sprite btnMapSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            // Tỷ lệ chuẩn của btn_map_0 (703 x 305 ~ 2.3 : 1)
            rt.sizeDelta = new Vector2(268f, 114f);
            rt.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.sprite = btnMapSprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = new Vector2(-24f, -16f);
            trt.anchoredPosition = new Vector2(0f, 2f);

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 19.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.04f, 0.15f, 0.38f, 1f);
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = new Color32(255, 255, 255, 235);
            tmp.raycastTarget = false;

            return btn;
        }

        private static void ConfigurePosterImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite sp && sp.name == spriteName) return sp;
            }
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return null;
        }
    }
}
