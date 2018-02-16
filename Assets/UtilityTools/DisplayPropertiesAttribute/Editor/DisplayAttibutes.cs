using UnityEditor;
using UnityEngine;
using UtilityTools;

namespace UtilityToolsEditor {
	[CustomPropertyDrawer(typeof(DisplayPropertiesAttribute))]
	public class DisplayPropertiesEditor : PropertyDrawer {
		bool showProperty = false;
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			var editor = Editor.CreateEditor(property.objectReferenceValue);
			var indentLevel = EditorGUI.indentLevel;
			Rect foldedLabel = new Rect(position.x, position.y, 16, 16);
			showProperty = EditorGUI.Foldout(foldedLabel, showProperty, "");
			position.height = 16;
			EditorGUI.PropertyField(position, property);
			position.y += 20;
			if (!showProperty) return;
			if (editor != null) {
				EditorGUI.indentLevel += 1;
				editor.OnInspectorGUI();
				EditorGUILayout.Space();
				EditorGUI.indentLevel = indentLevel;
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			float height = base.GetPropertyHeight(property, label);
			return height;
		}
	}
}