#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Viridian.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [Scene] only with strings.");
                return;
            }

            // Find the scene asset on your disk using its saved path
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);

            // Fallback: If path is broken, check Build Settings by name
            if (sceneAsset == null && !string.IsNullOrWhiteSpace(property.stringValue))
            {
                sceneAsset = GetBuildSettingsSceneObject(property.stringValue);
            }

            // Draw the drag-and-drop object field slot
            EditorGUI.BeginChangeCheck();
            SceneAsset newScene = (SceneAsset)EditorGUI.ObjectField(position, label, sceneAsset, typeof(SceneAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                if (newScene != null)
                {
                    // Save the actual file asset path back into the string variable
                    property.stringValue = AssetDatabase.GetAssetPath(newScene);
                }
                else
                {
                    property.stringValue = "";
                }
            }
        }

        private SceneAsset GetBuildSettingsSceneObject(string scenePathOrName)
        {
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                // Check match by full path or just the filename
                if (buildScene.path == scenePathOrName || System.IO.Path.GetFileNameWithoutExtension(buildScene.path) == scenePathOrName)
                {
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path);
                }
            }
            return null;
        }
    }
}
#endif
