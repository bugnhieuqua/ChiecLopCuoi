#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChiecLopCuoi.Editor
{
    public static class SetupSpriteSheetImporters
    {
        [MenuItem("Tools/Sprites/Setup Characters & Monsters Sprite Importers")]
        public static void OptimizeAllCharacterAndMonsterSprites()
        {
            string[] folders = { "Assets/Art/Characters", "Assets/Art/Enemies" };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool modified = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }

                    bool isSheet = path.ToLower().Contains("sheet") || importer.spriteImportMode == SpriteImportMode.Multiple;
                    SpriteImportMode targetMode = isSheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;

                    if (importer.spriteImportMode != targetMode)
                    {
                        importer.spriteImportMode = targetMode;
                        if (!isSheet)
                        {
                            importer.spritePivot = new Vector2(0.5f, 0.0f);
                        }
                        modified = true;
                    }

                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        modified = true;
                    }

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        modified = true;
                    }

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        modified = true;
                    }

                    if (importer.anisoLevel < 4)
                    {
                        importer.anisoLevel = 4;
                        modified = true;
                    }

                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        modified = true;
                    }

                    if (importer.maxTextureSize < 2048)
                    {
                        importer.maxTextureSize = 2048;
                        modified = true;
                    }

                    if (modified)
                    {
                        importer.SaveAndReimport();
                        count++;
                        Debug.Log($"[SetupSpriteSheetImporters] Configured {path} -> Bilinear, Uncompressed RGBA32, Aniso 4.");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupSpriteSheetImporters] Checked {guids.Length} sprites, updated {count} sprite sheets.");
        }
    }
}
#endif
