using UnityEngine;
using UnityEditor;
using 

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator script = (TerrainGenerator)target;

        // Create a label for N
        EditorGUILayout.LabelField("N");

        // Begin a horizontal layout
        EditorGUILayout.BeginHorizontal();

        // Create a button to decrease N
        if (GUILayout.Button("-"))
        {
            script.N--;
            if (script.N < 2)
            {
                script.N = 2;
            }
        }

        // Display N as a label
        EditorGUILayout.LabelField(script.N.ToString(), GUILayout.Width(30));

        // Create a button to increase N
        if (GUILayout.Button("+"))
        {
            script.N++;
            if (script.N > 8)
            {
                script.N = 8;
            }
        }

        // End the horizontal layout
        EditorGUILayout.EndHorizontal();

        // Update the serialized object
        serializedObject.Update();
        SerializedProperty nProperty = serializedObject.FindProperty("N");
        EditorGUILayout.PropertyField(nProperty);

        // Apply changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}