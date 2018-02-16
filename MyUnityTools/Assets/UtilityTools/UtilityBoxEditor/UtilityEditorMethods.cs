#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UtilityToolsEditor {
	public partial class EditorTool {
		public static void AddSpecialSpace(float pixels) {
			GUILayout.Space(12 + pixels);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			var lineRect = GUILayoutUtility.GetLastRect();
			lineRect.height = 1f; lineRect.width += 17 - EditorGUI.indentLevel * 12;
			lineRect.x -= -EditorGUI.indentLevel * 12 + 13; lineRect.y -= 6 + pixels;
			EditorGUI.HelpBox(lineRect, "", MessageType.None);
		}

		// - MinMax Slider
		public static void MakeMinMaxSlider(SerializedProperty minValue, SerializedProperty maxValue, float minLimit, float maxLimit, string label) {
			EditorGUI.BeginChangeCheck();
			float _minValue = minValue.floatValue;
			float _maxValue = maxValue.floatValue;
			Rect sliderRect = EditorGUILayout.GetControlRect();
			EditorGUI.PrefixLabel(sliderRect, new GUIContent(label));
			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			if (Screen.width > 332) {
				sliderRect.width -= (50 + 6) * 2 + EditorGUIUtility.labelWidth;
				Rect minDistanceRect = new Rect(sliderRect.x + EditorGUIUtility.labelWidth, sliderRect.y, 50, sliderRect.height);
				sliderRect.x += EditorGUIUtility.labelWidth + (50 + 6) + 6;
				Rect maxDistanceRect = new Rect(sliderRect.xMax, sliderRect.y, 50, sliderRect.height);
				sliderRect.x += -6;
				_minValue = EditorGUI.FloatField(minDistanceRect, _minValue);
				_maxValue = EditorGUI.FloatField(maxDistanceRect, _maxValue);
				EditorGUI.MinMaxSlider(sliderRect, new GUIContent(""), ref _minValue, ref _maxValue, minLimit, maxLimit);
			} else {
				GUILayout.Space(16);
				sliderRect.y += 16;
				sliderRect.x += 12 + 6;
				Rect minDistanceRect = new Rect(sliderRect.x, sliderRect.y, 50, sliderRect.height);
				sliderRect.width -= 50 + 12 + 6;
				Rect maxDistanceRect = new Rect(sliderRect.xMax, sliderRect.y, 50, sliderRect.height);
				sliderRect.x += 50 + 6;
				sliderRect.width -= 50 + 12;
				_minValue = EditorGUI.FloatField(minDistanceRect, _minValue);
				_maxValue = EditorGUI.FloatField(maxDistanceRect, _maxValue);
				EditorGUI.MinMaxSlider(sliderRect, new GUIContent(""), ref _minValue, ref _maxValue, minLimit, maxLimit);
			}
			EditorGUI.indentLevel = indentLevel;
			if (EditorGUI.EndChangeCheck()) {
				minValue.floatValue = _minValue;
				maxValue.floatValue = _maxValue;
			}
		}
		// - MinMax Slider
	}
}
#endif