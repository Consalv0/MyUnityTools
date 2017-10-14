using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UtilityTools;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UtilityToolsEditor;
#endif

[System.Serializable]
public class ActivatedEvents {
	[HideInInspector][SerializeField] 
	public List<ActivatedAction> activatedActions = new List<ActivatedAction>();

	public void Invoke() {
		foreach (var activatedAction in activatedActions) {
			activatedAction.Activate();
		}
	}
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomPropertyDrawer(typeof(ActivatedEvents), true)]
public class ActivatedEventsEditor : PropertyDrawer {
	ReorderableList activatedActions;
	bool isEnabled;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return base.GetPropertyHeight(property, label) + 45 - (property.FindPropertyRelative("activatedActions").arraySize > 0 ? 20 : 0)
			         + (EditorGUIUtility.singleLineHeight * 3 + 12 + 16f) * property.FindPropertyRelative("activatedActions").arraySize;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		if (!isEnabled) { OnEnable(property); isEnabled = true; };
		EditorGUI.BeginProperty(position, label, property);
		activatedActions.DoList(position);
		EditorGUI.EndProperty();
	}

	void OnEnable(SerializedProperty property) {
		activatedActions = new ReorderableList(property.serializedObject, property.FindPropertyRelative("activatedActions"), true, true, true, true) {
			drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, property.displayName);
			}
		};
		activatedActions.elementHeightCallback = (int index) => {
			var element = activatedActions.serializedProperty.GetArrayElementAtIndex(index);
			return EditorGUI.GetPropertyHeight(element);
		};
		activatedActions.drawElementCallback =
		(Rect rect, int index, bool isActive, bool isFocused) => {
			var element = activatedActions.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			rect.height -= 5;
			EditorGUI.PropertyField(rect, element, new GUIContent("Activated Action: " + (index + 1)));
		};
	}
}
#endif

[System.Serializable]
public enum ParameterType {
	Null = 0, Int, Float, Bool, String, Vector, Color, UnityObject
}

public enum InvokeMethod {
	NoInvoke, MethodInfo, ActionWithParameter, Action //, Func
}

[System.Serializable]
public class ActivatedAction {
	public Object target;
	public bool needUpdate = true; // Optimazation for UpdateMethods in editor
	public int parameterInt;
	public float parameterFloat;
	public bool parameterBool;
	public bool isSwitch;
	public string parameterString;
	public Vector3 parameterVector;
	public Color parameterColor;
	public Component parameterComponent;
	public Object parameterObject;
	public bool asComponent;
	[SerializeField]
	bool isGeneric;
	[SerializeField]
	ParameterType parameterType;
	List<MethodInfo> methodsInfo = new List<MethodInfo>(0);
	[SerializeField]
	List<string> methodNames = new List<string>(0);
	[SerializeField]
	int selectedMethodIndex;
	InvokeMethod invokeMethod;
	MethodInfo methodInfoSelected;
	System.Action<object> actionParameter;
	System.Action action;

	public void Activate() {
		if (needUpdate) UpdateMethods();
		if (target == null) return;
		switch (invokeMethod) {
			case InvokeMethod.MethodInfo:
				if (methodInfoSelected.IsGenericMethod) {
					methodInfoSelected.Invoke(target, null);
				} else {
					switch (parameterType) {
						case ParameterType.UnityObject: if (asComponent) { methodInfoSelected.Invoke(target, new object[] { parameterComponent }); break; }
																							  methodInfoSelected.Invoke(target, new object[] { parameterObject }); break;
						case ParameterType.Vector:	if (methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(Vector2))) {
																						methodInfoSelected.Invoke(target, new object[] { (Vector2)parameterVector }); break; }
																						methodInfoSelected.Invoke(target, new object[] { parameterVector }); break;
						case ParameterType.Color: 			methodInfoSelected.Invoke(target, new object[] { parameterColor }); break;
						case ParameterType.Bool: 				methodInfoSelected.Invoke(target, new object[] { parameterBool }); break;
						case ParameterType.String: 			methodInfoSelected.Invoke(target, new object[] { parameterString }); break;
						case ParameterType.Float: 			methodInfoSelected.Invoke(target, new object[] { parameterFloat }); break;
						case ParameterType.Int: 				methodInfoSelected.Invoke(target, new object[] { parameterInt }); break;
						default: Debug.Log("Incorrect type for " + methodInfoSelected); break;
					}
				}
				break;
			case InvokeMethod.ActionWithParameter:
				switch (parameterType) {
					case ParameterType.UnityObject: actionParameter.Invoke(parameterObject); break;
					case ParameterType.Vector:	if (methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(Vector2))) {
																					actionParameter.Invoke((Vector2)parameterVector); break; }
																					actionParameter.Invoke(parameterVector); break;
					case ParameterType.Color: 			actionParameter.Invoke(parameterColor); break;
					case ParameterType.Bool: 				actionParameter.Invoke(parameterBool = isSwitch ? !parameterBool : parameterBool); break;
					case ParameterType.String: 			actionParameter.Invoke(parameterString); break;
					case ParameterType.Float: 			actionParameter.Invoke(parameterFloat); break;
					case ParameterType.Int: 				actionParameter.Invoke(parameterInt); break;
					default: Debug.Log("Incorrect type for " + methodInfoSelected); break;
				}
				break;
			case InvokeMethod.Action:
				action.Invoke();
				break;
			case InvokeMethod.NoInvoke:
				if (UpdateMethods()) {
					Activate();
				} else { Debug.Log("No method to invoke with the current options"); }
				break;
		}
	}

	public bool UpdateMethods() {
		if (target != null) {
			methodNames.Clear();
			var newMethodsInfo = new List<MethodInfo>();
			methodsInfo = target.GetMethodsInfo(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();

			foreach (var method in methodsInfo) {
				var methodParameters = method.GetParameters();
				if (methodParameters.Length <= 1
						&& method.ToString().IndexOf(" get_", System.StringComparison.OrdinalIgnoreCase) <= 0
						&& method.ToString().IndexOf("Update()", System.StringComparison.Ordinal) <= 0
				    && method.ToString().IndexOf(" Is", System.StringComparison.Ordinal) <= 0
				    && method.ToString().IndexOf(" Get", System.StringComparison.Ordinal) <= 0
				    && method.ToString().IndexOf(" internal_", System.StringComparison.OrdinalIgnoreCase) <= 0
				    && method.ToString().IndexOf(" obj_", System.StringComparison.OrdinalIgnoreCase) <= 0
				    && method.ToString().IndexOf(" MemberwiseClone()", System.StringComparison.OrdinalIgnoreCase) <= 0
				    && method.ToString().IndexOf(" Finalize()", System.StringComparison.OrdinalIgnoreCase) <= 0
				    && method.ToString().IndexOf(" ToString()", System.StringComparison.Ordinal) <= 0
				    && method.ToString().IndexOf(" Find", System.StringComparison.Ordinal) <= 0
				    && method.ToString().IndexOf(",", System.StringComparison.Ordinal) <= 0
					 	&& !(method.IsGenericMethod && methodParameters.Any())) {
					if (methodParameters.Any()) {
						if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(int))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(float))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(string))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(bool))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(Vector3))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(Vector2))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(Color))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.Equals(typeof(Object))) newMethodsInfo.Add(method);
						else if (methodParameters.ElementAt(0).ParameterType.IsSubclassOf(typeof(Object))) newMethodsInfo.Add(method);
					} else {
						newMethodsInfo.Add(method);
					}
				}
			}
			methodsInfo = newMethodsInfo;
			foreach (var method in methodsInfo) {
				methodNames.Add(method.ToString().Split(' ').Last());
			}
			selectedMethodIndex = selectedMethodIndex + 1 > methodNames.Count() ? methodNames.Count() - 1 : selectedMethodIndex;
			invokeMethod = UpdateDelegate();
			needUpdate = false;
			return invokeMethod != InvokeMethod.NoInvoke;
		} else {
			selectedMethodIndex = 0;
			methodsInfo = new List<MethodInfo>(0);
			methodNames = new List<string>(0);
		}
		needUpdate = false;
		return false;
	}

	InvokeMethod UpdateDelegate() {
		if (!methodsInfo.Any()) return InvokeMethod.NoInvoke;
		selectedMethodIndex = Mathf.Clamp(selectedMethodIndex, 0, methodsInfo.Count() - 1);
		methodInfoSelected = methodsInfo[selectedMethodIndex];
		isGeneric = methodInfoSelected.IsGenericMethod;
		if (target == null) return InvokeMethod.NoInvoke;
		parameterType = GetParameterType(methodInfoSelected);
		// TODO Maybe add the Func<T, RT> delegate form ;<
		if (methodInfoSelected.ReturnType != typeof(void) && !methodInfoSelected.IsGenericMethod) return InvokeMethod.MethodInfo;
		switch (parameterType) {
			case ParameterType.UnityObject:
				if (methodInfoSelected.IsGenericMethod) {
					if (asComponent) { // Using as component
						if (parameterComponent == null && parameterObject as Component != null) { // There's no component but the parameterObject is, use it
							parameterComponent = parameterObject as Component;
						} else if (parameterComponent == null && parameterObject != null) { // No component try parse it
							if (System.Type.GetType(parameterObject.name + ", Assembly-CSharp") != null) {
								MethodInfo genericFromObject = methodInfoSelected.MakeGenericMethod(System.Type.GetType(parameterObject.name + ", Assembly-CSharp"));
								methodInfoSelected = genericFromObject;
								return InvokeMethod.MethodInfo;
							}
						}
						if (parameterComponent != null) { // There's a component use it
							MethodInfo generic = methodInfoSelected.MakeGenericMethod(parameterComponent.GetType());
							methodInfoSelected = generic;
							return InvokeMethod.MethodInfo;
						}
						return InvokeMethod.NoInvoke; // Can't do anything
					} else if (System.Type.GetType(parameterObject.GetType() + ", UnityEngine") != null) {
						MethodInfo generic = methodInfoSelected.MakeGenericMethod(System.Type.GetType(parameterObject.GetType() + ", UnityEngine"));
						methodInfoSelected = generic;
						return InvokeMethod.MethodInfo;
					} else {
						MethodInfo generic = methodInfoSelected.MakeGenericMethod(parameterObject.GetType());
						methodInfoSelected = generic;
						return InvokeMethod.MethodInfo;
					}
				} else if (parameterObject != null && methodInfoSelected.GetParameters().Any()) {
					//actionParameter = methodInfoSelected.CreateAction<Object>(target);
					//actionParameter = param => actionParameter(parameterObject);
					return InvokeMethod.MethodInfo;
				}
				break;
			case ParameterType.Vector:
				if (methodInfoSelected.GetParameters().Any()) {
					if (methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(Vector3))) {
						actionParameter = methodInfoSelected.CreateAction<Vector3>(target);
						return InvokeMethod.ActionWithParameter;
					} else if (methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(Vector2))) {
						actionParameter = methodInfoSelected.CreateAction<Vector2>(target);
						return InvokeMethod.ActionWithParameter;
					}
				}
				break;
			case ParameterType.Color:
				if (methodInfoSelected.GetParameters().Any() &&
						methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(Color))) {
					actionParameter = methodInfoSelected.CreateAction<Color>(target);
					return InvokeMethod.ActionWithParameter;
				}
				break;
			case ParameterType.String:
				if (methodInfoSelected.GetParameters().Any() &&
						methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(string))) {
					actionParameter = methodInfoSelected.CreateAction<string>(target);
					return InvokeMethod.ActionWithParameter;
				}
				break;
			case ParameterType.Bool:
				if (methodInfoSelected.GetParameters().Any() &&
						methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(bool))) {
					actionParameter = methodInfoSelected.CreateAction<bool>(target);
					return InvokeMethod.ActionWithParameter;
				}
				break;
			case ParameterType.Float:
				if (methodInfoSelected.GetParameters().Any() &&
						methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(float))) {
					actionParameter = methodInfoSelected.CreateAction<float>(target);
					return InvokeMethod.ActionWithParameter;
				}
				break;
			case ParameterType.Int:
				if (methodInfoSelected.GetParameters().Any() &&
						methodInfoSelected.GetParameters()[0].ParameterType.Equals(typeof(int))) {
					actionParameter = methodInfoSelected.CreateAction<int>(target);
					return InvokeMethod.ActionWithParameter;
				}
				break;
			default:
				if (!methodInfoSelected.GetParameters().Any()) {
					action = methodInfoSelected.CreateAction(target);
					return InvokeMethod.Action;
				}
				break;
		}
		return InvokeMethod.NoInvoke;
	}

	ParameterType GetParameterType(MethodInfo method) {
		if (method.IsGenericMethod) return ParameterType.UnityObject;
		if (!method.GetParameters().Any()) return ParameterType.Null;
		var ptype = method.GetParameters()[0].ParameterType;
		if (ptype.IsSubclassOf(typeof(Object)) | ptype.Equals(typeof(Object))) return ParameterType.UnityObject;
		if (ptype.Equals(typeof(Vector3)) || ptype.Equals(typeof(Vector2))) return ParameterType.Vector;
		if (ptype.Equals(typeof(Color))) return ParameterType.Color;
		if (ptype.Equals(typeof(bool))) return ParameterType.Bool;
		if (ptype.Equals(typeof(string))) return ParameterType.String;
		if (ptype.Equals(typeof(float))) return ParameterType.Float;
		if (ptype.Equals(typeof(int))) return ParameterType.Int;
		return parameterType;
	}
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ActivatedAction))]
public class ActivatedActionDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight * 3 + 12;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		bool needUpdate = property.FindPropertyRelative("needUpdate").boolValue;
		if (needUpdate) {
			int indexArray = 0;
			if (property.propertyPath.IndexOf("Array.data[", System.StringComparison.Ordinal) > 0 && 
			    System.Int32.TryParse(property.propertyPath.Split('[', ']')[1], out indexArray)) {
				(property.GetTargetObjectOfProperty(indexArray) as ActivatedAction).UpdateMethods();
			} else {
				(property.GetTargetObjectOfProperty(0) as ActivatedAction).UpdateMethods();
			}
		}

		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);
		int index;
		string ObjectTypeString = "";
		System.Type parameterObjectType;


		index = property.FindPropertyRelative("selectedMethodIndex").intValue;
		if (property.FindPropertyRelative("methodNames").arraySize > 0) {
			ObjectTypeString = property.FindPropertyRelative("methodNames")
			                     .GetArrayElementAtIndex(Mathf.Clamp(index, 0, property.FindPropertyRelative("methodNames").arraySize - 1))
			                     .stringValue.Split(new char[] { '(', ')' })[1];
		}
		parameterObjectType = System.Type.GetType(ObjectTypeString + ", UnityEngine");

		Rect originalRect = position;
		// Draw label
		position.y += EditorGUIUtility.singleLineHeight + 6;
		position.height -= EditorGUIUtility.singleLineHeight + 6;
		GUI.Box(position, "", GUI.skin.GetStyle("HelpBox"));
		position.y -= EditorGUIUtility.singleLineHeight + 6;
		position.height += EditorGUIUtility.singleLineHeight + 6;
		GUI.Box(position, "", GUI.skin.GetStyle("HelpBox"));
		position.x += 6;
		position.width -= 12;
		position.y += 2;
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);
		var targetRect = new Rect(position.x + 6, position.y + 2, position.width - 6, EditorGUIUtility.singleLineHeight);
		var targetMenuRect = new Rect(EditorGUI.IndentedRect(position).position.x - 27, targetRect.y, 30f, EditorGUIUtility.singleLineHeight);
		EditorGUI.PropertyField(targetRect, property.FindPropertyRelative("target"), GUIContent.none);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 1;

		position = originalRect;
		position.y += 6;
		position.height -= 6;
		position.width -= 6;

		var methodNameRect = new Rect(position.x, position.y + 6 + EditorGUIUtility.singleLineHeight,
		                              position.width, EditorGUIUtility.singleLineHeight);
		var parameterRect = new Rect(position.x, position.y + (4 + EditorGUIUtility.singleLineHeight) * 2 + 2,
		                             position.width - position.width / 4f, EditorGUIUtility.singleLineHeight);
		var typeRect = new Rect(position.x + position.width / 1.3f - 16, position.y + (4 + EditorGUIUtility.singleLineHeight) * 2 + 2,
		                        position.width - position.width / 1.3f + 16, EditorGUIUtility.singleLineHeight);

		// Popup list
		string[] guiContents = new string[property.FindPropertyRelative("methodNames").arraySize];
		for (int i = 0; i < property.FindPropertyRelative("methodNames").arraySize; i++) {
			guiContents[i] = property.FindPropertyRelative("methodNames").GetArrayElementAtIndex(i).stringValue;
		}
		index = EditorGUI.Popup(methodNameRect, "Method " + (index + 1).ToString() + ":", index, guiContents);
		needUpdate = GUI.changed;
		property.FindPropertyRelative("selectedMethodIndex").intValue = index;
		// -

		// Change Object dropdown option
		int targetInt = 0;
		var target = property.FindPropertyRelative("target").objectReferenceValue;
		Queue<Object> objects = new Queue<Object>();
		Queue<string> componentNames = new Queue<string>();
		if ((target as GameObject)) {
			var obtainedValues = (target as GameObject).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				componentNames.Enqueue(objects.ElementAt(i).ToString());
			}
			objects.Enqueue(target as GameObject);
			componentNames.Enqueue((target as GameObject).ToString());
		} else if ((target as Component)) {
			var obtainedValues = (target as Component).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				componentNames.Enqueue(objects.ElementAt(i).ToString());
			}
			objects.Enqueue((target as Component).gameObject);
			componentNames.Enqueue((target as Component).gameObject.ToString());
		}
		List<string> listNames = new List<string>();
		targetInt = listNames.FindIndex(x => x.IndexOf((target.ToString()), System.StringComparison.Ordinal) > 0);
		targetInt = EditorGUI.Popup(targetMenuRect, "", targetInt, componentNames.ToArray());
		if (GUI.changed && targetInt >= 0) {
			if (objects.Any()) property.FindPropertyRelative("target").objectReferenceValue = objects.ElementAt(targetInt);
		}

		switch (property.FindPropertyRelative("parameterType").enumValueIndex) {
			case 7:
				if (property.FindPropertyRelative("isGeneric").boolValue) {
					var popRect = parameterRect;
					popRect.x = EditorGUI.IndentedRect(parameterRect).position.x + 73; popRect.width = 30;
					property.FindPropertyRelative("asComponent").boolValue =
						EditorGUI.Popup(popRect, property.FindPropertyRelative("asComponent").boolValue ? 0 : 1, new string[] { "Component", "Object" }) == 0;
					if (property.FindPropertyRelative("asComponent").boolValue) {
						EditorGUI.PrefixLabel(parameterRect, new GUIContent("Parameter Type"));
						property.FindPropertyRelative("parameterComponent").objectReferenceValue =
							EditorGUI.ObjectField(parameterRect, " ", property.FindPropertyRelative("parameterComponent").objectReferenceValue,
																		typeof(Component), true);
						break;
					}
					property.FindPropertyRelative("parameterComponent").objectReferenceValue = null;
				}
				if (parameterObjectType != null && parameterObjectType.IsSubclassOf(typeof(Object))) {
					property.FindPropertyRelative("parameterObject").objectReferenceValue =
						EditorGUI.ObjectField(parameterRect, " ", property.FindPropertyRelative("parameterObject").objectReferenceValue,
						                      parameterObjectType, true);
				} else {
					EditorGUI.ObjectField(parameterRect, property.FindPropertyRelative("parameterObject"), parameterObjectType);
				}
				break;
			case 6: EditorGUI.PropertyField(parameterRect, property.FindPropertyRelative("parameterColor")); break;
			case 5:
				property.FindPropertyRelative("parameterVector").vector3Value = 
					EditorGUI.Vector3Field(parameterRect, Screen.width > 400 ? "Parameter Vector3" : "",
					                       property.FindPropertyRelative("parameterVector").vector3Value);
				break;
			case 4: EditorGUI.PropertyField(parameterRect, property.FindPropertyRelative("parameterString")); break;
			case 3:
				var firstRect = new Rect(parameterRect.x, parameterRect.y,
				                         (parameterRect.xMax - parameterRect.xMin) / 2 , EditorGUIUtility.singleLineHeight);
				var secondRect = new Rect(parameterRect.x + (parameterRect.xMax - parameterRect.xMin) / 2, parameterRect.y,
																	(parameterRect.xMax - parameterRect.xMin) / 2, EditorGUIUtility.singleLineHeight);
				EditorGUIUtility.labelWidth = (parameterRect.xMax - parameterRect.xMin) / 2.2f;
				GUI.enabled &= !property.FindPropertyRelative("isSwitch").boolValue;
				EditorGUI.PropertyField(firstRect, property.FindPropertyRelative("parameterBool"));
				GUI.enabled = true;
				EditorGUIUtility.labelWidth = (parameterRect.xMax - parameterRect.xMin) / 2.8f;
				EditorGUI.PropertyField(secondRect, property.FindPropertyRelative("isSwitch"));
				EditorGUIUtility.labelWidth = 0;
				break;
			case 2: EditorGUI.PropertyField(parameterRect, property.FindPropertyRelative("parameterFloat")); break;
			case 1: EditorGUI.PropertyField(parameterRect, property.FindPropertyRelative("parameterInt")); break;
			default:
				EditorGUI.LabelField(parameterRect, "No Property");
				break;
		}
		EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("parameterType"), GUIContent.none);
		needUpdate = GUI.changed;
		EditorGUI.indentLevel -= 1;
		property.FindPropertyRelative("needUpdate").boolValue |= needUpdate;
		// Set indent back to what it was
		EditorGUI.indentLevel = indent;
		EditorGUI.EndProperty();
	}
}
#endif

