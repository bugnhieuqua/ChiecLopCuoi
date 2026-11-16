#if UNITY_EDITOR
using ChiecLopCuoi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChiecLopCuoi.EditorTools
{
    public static class LoginNpcBossStageTool
    {
        private const string LoginScenePath = "Assets/Scenes/LoginScene.unity";
        private const string HeroineSpritePath = "Assets/Art/UI/Login3D/login_npc_female_red.png";
        private const string ElemGhenPath = "Assets/Art/UI/Login3D/login_elem_ghen.png";
        private const string ElemGianPath = "Assets/Art/UI/Login3D/login_elem_gian.png";
        private const string ElemTuTiPath = "Assets/Art/UI/Login3D/login_elem_tuti.png";
        private const string TireStackPath = "Assets/Art/UI/Login3D/login_tire_stack.png";
        private const string TirePairPath = "Assets/Art/UI/Login3D/login_tire_pair.png";
        private const string TireUprightPath = "Assets/Art/UI/Login3D/login_tire_upright.png";

        [MenuItem("Tools/Login/Add NPC Female Boss to Foreground")]
        public static string AddNpcBossToLoginScene()
        {
            ConfigureSprite(HeroineSpritePath);
            ConfigureSprite(ElemGhenPath);
            ConfigureSprite(ElemGianPath);
            ConfigureSprite(ElemTuTiPath);

            Sprite heroineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroineSpritePath);
            Sprite elemGhenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ElemGhenPath);
            Sprite elemGianSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ElemGianPath);
            Sprite elemTuTiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ElemTuTiPath);
            if (heroineSprite == null || elemGhenSprite == null || elemGianSprite == null || elemTuTiSprite == null)
            {
                return "[LoginNpcBossStageTool] ERROR: Missing one or more Login3D cutouts (npc_red / elementals).";
            }

            var scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
            var stage = GameObject.Find("Login_3D_Stage");
            if (stage == null)
            {
                return "[LoginNpcBossStageTool] ERROR: Missing Login_3D_Stage in LoginScene.";
            }

            var heroTransform = stage.transform.Find("Hero_CowLop");
            var heroSr = heroTransform != null ? heroTransform.GetComponent<SpriteRenderer>() : null;
            Material sharedStageMat = heroSr != null ? heroSr.sharedMaterial : null;

            // Xoá hẳn 3 Quỷ Bóng Tối cũ (cùng mắt phát sáng Demon_Eye_*)
            DestroyChildIfExists(stage.transform, "Demon_Assassin");
            DestroyChildIfExists(stage.transform, "Demon_Horn");
            DestroyChildIfExists(stage.transform, "Demon_Wolf");

            // NPC Nữ áo đỏ đứng tiền cảnh phải, hướng trái (-X) đối đầu Hero_CowLop
            GameObject npcGo = UpsertStageSprite(
                stage.transform, "NPC_Female_Boss", heroineSprite,
                new Vector3(3.05f, -0.95f, 1.25f),
                new Vector3(0.455f, 0.455f, 1f),
                11, sharedStageMat);

            // 3 Quỷ Nguyên Tố đứng hậu thuẫn phía sau lưng NPC Nữ áo đỏ
            GameObject ghenGo = UpsertStageSprite(
                stage.transform, "Elem_QuyGhen", elemGhenSprite,
                new Vector3(4.65f, 0.72f, 2.05f),
                new Vector3(0.43f, 0.43f, 1f),
                9, sharedStageMat);

            GameObject gianGo = UpsertStageSprite(
                stage.transform, "Elem_QuyGian", elemGianSprite,
                new Vector3(6.15f, -0.30f, 2.55f),
                new Vector3(0.60f, 0.60f, 1f),
                8, sharedStageMat);

            GameObject tutiGo = UpsertStageSprite(
                stage.transform, "Elem_QuyTuTi", elemTuTiSprite,
                new Vector3(4.95f, -1.65f, 2.95f),
                new Vector3(0.54f, 0.54f, 1f),
                7, sharedStageMat);

            var controller = stage.GetComponent<Login3DStageController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("npcFemaleBoss").objectReferenceValue = npcGo.transform;
                so.FindProperty("demonAssassin").objectReferenceValue = ghenGo.transform;
                so.FindProperty("demonHorn").objectReferenceValue = gianGo.transform;
                so.FindProperty("demonWolf").objectReferenceValue = tutiGo.transform;
                var glints = so.FindProperty("demonEyeGlints");
                if (glints != null && glints.isArray)
                {
                    glints.ClearArray();
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
            }

            EnsureTiresBehindCowLop(stage.transform, sharedStageMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            return $"[LoginNpcBossStageTool] Replaced shadows with 3 Elementals + placed RED NPC_Female_Boss at {npcGo.transform.localPosition}.";
        }

        [MenuItem("Tools/Login/Add Standing & Lying Tires Behind CowLop")]
        public static string AddTiresBehindCowLop()
        {
            var scene = EditorSceneManager.OpenScene(LoginScenePath, OpenSceneMode.Single);
            var stage = GameObject.Find("Login_3D_Stage");
            if (stage == null)
            {
                return "[LoginNpcBossStageTool] ERROR: Missing Login_3D_Stage in LoginScene.";
            }

            var heroTransform = stage.transform.Find("Hero_CowLop");
            var heroSr = heroTransform != null ? heroTransform.GetComponent<SpriteRenderer>() : null;
            Material sharedStageMat = heroSr != null ? heroSr.sharedMaterial : null;

            string result = EnsureTiresBehindCowLop(stage.transform, sharedStageMat);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return result;
        }

        private static string EnsureTiresBehindCowLop(Transform stageRoot, Material sharedStageMat)
        {
            ConfigureSprite(TireStackPath);
            ConfigureSprite(TirePairPath);
            ConfigureSprite(TireUprightPath);

            Sprite tireStackSp = AssetDatabase.LoadAssetAtPath<Sprite>(TireStackPath);
            Sprite tirePairSp = AssetDatabase.LoadAssetAtPath<Sprite>(TirePairPath);
            Sprite tireUprightSp = AssetDatabase.LoadAssetAtPath<Sprite>(TireUprightPath);
            if (tireStackSp == null || tirePairSp == null || tireUprightSp == null)
            {
                return "[LoginNpcBossStageTool] ERROR: Missing one or more Login3D tire sprites.";
            }

            // Chồng lốp nằm + nghiêng + lốp gai đứng phía sau lưng trái CowLop (x=-6.35, z=2.05)
            GameObject stackGo = UpsertStageSprite(
                stageRoot, "Deco_CowLop_TireStack", tireStackSp,
                new Vector3(-6.35f, -2.55f, 2.05f),
                new Vector3(0.54f, 0.54f, 1f),
                8, sharedStageMat, new Color(0.90f, 0.93f, 0.98f, 1f));

            // Cặp lốp đứng & nằm sát rìa trái tạo chiều sâu tiền cảnh (z=1.70)
            GameObject pairGo = UpsertStageSprite(
                stageRoot, "Deco_CowLop_TirePair", tirePairSp,
                new Vector3(-7.45f, -3.15f, 1.70f),
                new Vector3(0.48f, 0.48f, 1f),
                9, sharedStageMat, new Color(0.95f, 0.97f, 1f, 1f));

            // Lốp đơn dựng đứng hơi nghiêng lùi phía sau gót chân CowLop (z=2.40)
            GameObject uprightGo = UpsertStageSprite(
                stageRoot, "Deco_CowLop_TireUpright", tireUprightSp,
                new Vector3(-5.25f, -3.45f, 2.40f),
                new Vector3(0.36f, 0.36f, 1f),
                7, sharedStageMat, new Color(0.82f, 0.85f, 0.92f, 1f));
            uprightGo.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);

            return $"[LoginNpcBossStageTool] Placed 3 tire props behind CowLop: {stackGo.name}, {pairGo.name}, {uprightGo.name}.";
        }

        private static GameObject UpsertStageSprite(
            Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder, Material sharedMat, Color? tint = null)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.identity;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint ?? Color.white;
            sr.sortingOrder = sortingOrder;
            if (sharedMat != null)
            {
                sr.sharedMaterial = sharedMat;
            }

            EditorUtility.SetDirty(go);
            return go;
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }
            if (dirty) importer.SaveAndReimport();
        }
    }
}
#endif
