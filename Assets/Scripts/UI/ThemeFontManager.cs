using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Bộ điều phối Font đồng bộ toàn game theo chuẩn thiết kế:
    /// - Nội dung Tiểu sử (StoryBookManager / LorePanel StoryContent): BeVietnamPro SDF.
    /// - Toàn bộ chữ còn lại trong game (HUD, Banner, Menu, Hướng dẫn, Chiến đấu, WIN/LOST, Login): SVN-Blenda Script SDF.
    /// Tuyệt đối không làm thay đổi vị trí/bố cục các vật thể trong Scene.
    /// </summary>
    public class ThemeFontManager : MonoBehaviour
    {
        private static ThemeFontManager _instance;
        private static TMP_FontAsset _storyFont;
        private static TMP_FontAsset _globalUiFont;
        private static TMP_FontAsset _loginFont;
        private static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureInstance();
            ApplyAllFontsInActiveScene();
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var go = new GameObject("[ThemeFontManager]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ThemeFontManager>();

            if (!_hooked)
            {
                _hooked = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAllFontsInActiveScene();
            if (_instance != null)
            {
                _instance.StartCoroutine(_instance.DeferredRefreshRoutine());
            }
        }

        private IEnumerator DeferredRefreshRoutine()
        {
            // Quét lại vài nhịp ngắn sau khi các HUD động / Manager Awake-Start khởi tạo xong
            yield return null;
            ApplyAllFontsInActiveScene();
            yield return new WaitForSecondsRealtime(0.35f);
            ApplyAllFontsInActiveScene();
        }

        private void Start()
        {
            StartCoroutine(PeriodicDynamicFontGuard());
        }

        private IEnumerator PeriodicDynamicFontGuard()
        {
            var wait = new WaitForSecondsRealtime(0.75f);
            while (true)
            {
                ApplyAllFontsInActiveScene();
                yield return wait;
            }
        }

        public static TMP_FontAsset GetStoryFont()
        {
            if (_storyFont == null)
            {
                _storyFont = Resources.Load<TMP_FontAsset>("Fonts/BeVietnamPro SDF");
#if UNITY_EDITOR
                if (_storyFont == null)
                {
                    _storyFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/BeVietnamPro SDF.asset");
                }
#endif
            }
            return _storyFont;
        }

        public static TMP_FontAsset GetGlobalUiFont()
        {
            if (_globalUiFont == null)
            {
                _globalUiFont = Resources.Load<TMP_FontAsset>("Fonts/UTM Zebrawood SDF");
#if UNITY_EDITOR
                if (_globalUiFont == null)
                {
                    _globalUiFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/UTM Zebrawood SDF.asset");
                }
#endif
            }
            return _globalUiFont;
        }

        public static TMP_FontAsset GetLoginFont()
        {
            if (_loginFont == null)
            {
                _loginFont = Resources.Load<TMP_FontAsset>("Fonts/UTM Zebrawood SDF");
#if UNITY_EDITOR
                if (_loginFont == null)
                {
                    _loginFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/UTM Zebrawood SDF.asset");
                }
#endif
            }
            return _loginFont;
        }

        private static void ApplyFontSafely(TMP_Text tmp, TMP_FontAsset font)
        {
            if (tmp == null || font == null) return;

            bool needsFont = tmp.font != font;
            bool needsMat = tmp.fontSharedMaterial == null ||
                            (font.atlasTexture != null && tmp.fontSharedMaterial.GetTexture(ShaderUtilities.ID_MainTex) != font.atlasTexture);

            if (needsFont || needsMat)
            {
                bool canTouchOutline = tmp.gameObject.activeInHierarchy
                    && tmp.fontSharedMaterial != null
                    && tmp.fontSharedMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth);
                float prevOutlineWidth = canTouchOutline ? tmp.outlineWidth : 0f;
                Color32 prevOutlineColor = canTouchOutline ? tmp.outlineColor : default;

                tmp.font = font;
                if (font.material != null)
                {
                    tmp.fontSharedMaterial = font.material;
                }

                if (canTouchOutline && prevOutlineWidth > 0.001f && tmp.fontSharedMaterial != null && tmp.fontSharedMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
                {
                    tmp.outlineWidth = prevOutlineWidth;
                    tmp.outlineColor = prevOutlineColor;
                }
            }
        }

        public static void ApplyStoryFont(TMP_Text tmp)
        {
            ApplyFontSafely(tmp, GetStoryFont());
        }

        public static void ApplyGlobalUiFont(TMP_Text tmp)
        {
            ApplyFontSafely(tmp, GetGlobalUiFont());
        }

        public static void ApplyLoginFont(TMP_Text tmp)
        {
            TMP_FontAsset loginFont = GetLoginFont();
            ApplyFontSafely(tmp, loginFont != null ? loginFont : GetStoryFont());
        }

        public static void ApplyAllFontsInActiveScene()
        {
            TMP_FontAsset storyFont = GetStoryFont();
            TMP_FontAsset uiFont = GetGlobalUiFont();
            TMP_FontAsset loginFont = GetLoginFont();
            if (uiFont == null && storyFont == null && loginFont == null) return;

            bool isLoginScene = SceneManager.GetActiveScene().name == "LoginScene";
            StoryBookManager storyMgr = Object.FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            TMP_Text storyContentTarget = storyMgr != null ? storyMgr.storyText : null;

            TMP_Text[] allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text t = allTexts[i];
                if (t == null) continue;

                if ((t.gameObject.name == "Txt_LoreTitle" || t.gameObject.name == "Txt_LevelTitle" || t.gameObject.name == "Result_Reward_Text") && loginFont != null)
                {
                    ApplyFontSafely(t, loginFont);
                    continue;
                }

                bool isStory = IsStoryContentText(t, storyContentTarget);
                if (isStory)
                {
                    if (storyFont != null) ApplyFontSafely(t, storyFont);
                    continue;
                }

                if (isLoginScene && loginFont != null)
                {
                    ApplyFontSafely(t, loginFont);
                    continue;
                }

                // Tôn trọng font tùy chỉnh khác trong Inspector; tự động chuyển các chữ mặc định/LiberationSans/SVN-Blenda sang UTM Zebrawood SDF.
                if (t.font != null && !t.font.name.Contains("LiberationSans") && !t.font.name.Contains("SVN-Blenda"))
                {
                    continue;
                }

                TMP_FontAsset desired = uiFont != null ? uiFont : storyFont;
                ApplyFontSafely(t, desired);
            }
        }

        private static bool IsStoryContentText(TMP_Text t, TMP_Text explicitStoryTarget)
        {
            if (t != null && t.gameObject.name == "Txt_LoreTitle")
            {
                return false;
            }

            if (explicitStoryTarget != null && t == explicitStoryTarget)
            {
                return true;
            }

            string nameLower = t.gameObject.name.ToLowerInvariant();
            if (nameLower.Contains("storycontent") || nameLower.Contains("text_story"))
            {
                return true;
            }

            Transform p = t.transform.parent;
            while (p != null)
            {
                string pn = p.name.ToLowerInvariant();
                if (pn.Contains("bg_notebook") || pn.Contains("lorepanel") || pn.Contains("lorecanvas") || pn.Contains("story_book") || pn.Contains("storypanel"))
                {
                    return true;
                }
                p = p.parent;
            }

            return false;
        }
    }
}
