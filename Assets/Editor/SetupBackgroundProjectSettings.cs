#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupBackgroundProjectSettings
{
    static SetupBackgroundProjectSettings()
    {
        SetupSortingLayers();
    }

    [MenuItem("Tools/Setup Background Sorting Layers")]
    public static void SetupSortingLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");

        string[] layers = new string[] { "Far_Layer", "Mid_Layer", "Near_Layer" };

        bool modified = false;
        foreach (string layer in layers)
        {
            bool exists = false;
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layer)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                int newIndex = sortingLayers.arraySize;
                sortingLayers.InsertArrayElementAtIndex(newIndex);
                SerializedProperty prop = sortingLayers.GetArrayElementAtIndex(newIndex);
                prop.FindPropertyRelative("name").stringValue = layer;
                prop.FindPropertyRelative("uniqueID").intValue = GetUniqueSortingLayerID(sortingLayers);
                prop.FindPropertyRelative("locked").boolValue = false;
                modified = true;
            }
        }

        if (modified)
        {
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupBackground] Sorting layers Far_Layer, Mid_Layer, Near_Layer created successfully!");
        }
    }

    private static int GetUniqueSortingLayerID(SerializedProperty sortingLayers)
    {
        int maxId = 1000;
        for (int i = 0; i < sortingLayers.arraySize - 1; i++)
        {
            int id = sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue;
            if (id > maxId) maxId = id;
        }
        return maxId + 1;
    }
}
#endif
