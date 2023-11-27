using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Codice.CM.Common.Selectors;

public class EditorGeneralFunctions : EditorWindow
{

}
public class RuleTileGenerator : EditorWindow
{
    public RuleTile ruleTile; 
    public Sprite[] tileSprites;

    [MenuItem("The Bounds of Scales/Rule Tile Generator")]
    public static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(RuleTileGenerator), true, "Rule Tile Generator", true);
    }

    int GenerateRuleTile()
    {
        if (ruleTile.m_TilingRules.Count != tileSprites.Length) return 1;
        for (int i = 0; i < ruleTile.m_TilingRules.Count; i++)
        {
            for (int j = 0; j < ruleTile.m_TilingRules[i].m_Sprites.Length; j++)
                ruleTile.m_TilingRules[i].m_Sprites[j] = tileSprites[i];
            ruleTile.m_TilingRules[i].m_ColliderType = Tile.ColliderType.Grid;
        }

        return 0;
    }

    void OnGUI()
    {

        SerializedObject serializedObject = new SerializedObject(this);

        SerializedProperty tileProperty = serializedObject.FindProperty("ruleTile");
        EditorGUILayout.ObjectField(tileProperty);

        SerializedProperty arrayProperty = serializedObject.FindProperty("tileSprites");
        EditorGUILayout.PropertyField(arrayProperty, true);

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Generate Rule Tile"))
        {
            int result = GenerateRuleTile();
            if (result == 1)
                EditorUtility.DisplayDialog("Error", "The amount of tile sprites doesn't match the amount needed!", "OK");
        }
    }
}