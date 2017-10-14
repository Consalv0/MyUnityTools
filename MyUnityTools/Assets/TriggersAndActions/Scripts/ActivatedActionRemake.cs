using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UtilityTools;
#if UNITY_EDITOR
using UnityEditor;
using UtilityToolsEditor;
#endif

[System.Serializable]
public class ActivatedActionRemake {
	[SerializeField] InvokeMode invokeMode = InvokeMode.RuntimeAndEditor;
	[SerializeField] Object _target;
	[SerializeField] Parameter[] parameters = new Parameter[3];
	[SerializeField] int _methodIndex;

	MethodInfo selectedMethodInfo;
	object[] parameterObjects;
	MethodInfo[] methodsInfo;
	public string[] methodNames;

	System.Action action0;
	System.Action<object> action1;
	System.Func<object> func1;

	public Object target {
		get { return _target; }
		set {
			_target = value;
			UpdateMethodsInfo();
			UpdateMethodNames();
			SelectMethodInfo();
			UpdateParameters();
			UpdateParameterObjects();
			UpdateDelegate();
		}
	}

	public int methodIndex {
		get {
			_methodIndex = Mathf.Clamp(_methodIndex, 0, methodsInfo.Count() - 1);
			return _methodIndex; 
		}
		set {
			_methodIndex = value;
			_methodIndex = Mathf.Clamp(_methodIndex, 0, methodsInfo.Count() - 1);
			SelectMethodInfo();
			UpdateParameters();
			UpdateParameterObjects();
			UpdateDelegate();
		}
	}

	public void Invoke() {
		if (selectedMethodInfo == null) return;
		if (selectedMethodInfo.IsGenericMethod) {
			if (parameterObjects.Any()) {
				selectedMethodInfo.Invoke(target, parameterObjects);
			} else {
				selectedMethodInfo.Invoke(target, null);
			}
		} else if (!parameterObjects.Any()) {
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				action0.Invoke();
			} else {
				selectedMethodInfo.Invoke(target, null);
			}
		} else if (parameterObjects.Count() == 1) {
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				if (parameterObjects[0] == null) {
					selectedMethodInfo.Invoke(target, parameterObjects);
				} else {
					if (parameterObjects[0] != null && (parameterObjects[0].GetType().Equals(typeof(Object)) 
					    || parameterObjects[0].GetType().IsSubclassOf(typeof(Object)))) {
						selectedMethodInfo.Invoke(target, parameterObjects);
					} else {
						action1.Invoke(parameterObjects[0]);
					}
				}
			} else {
				selectedMethodInfo.Invoke(target, parameterObjects);
			}
		} else {
			selectedMethodInfo.Invoke(target, parameterObjects);
		}
	}

	void UpdateMethodsInfo() {
		methodsInfo = new MethodInfo[0];
		if (target == null) {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].Clear(true);
			}
			return;
		}
		var allMethodsInfo = target.GetMethodsInfo(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).ToList();
		List<MethodInfo> newMethodsInfo = new List<MethodInfo>();
		foreach (var method in allMethodsInfo) {
			var methodParameters = method.GetParameters();
			var methodname = method.ToString();
			if (method.IsGenericMethod && methodParameters.Count() >= 3 || method.GetGenericArguments().Count() > 1) continue;
			if (methodParameters.Count() <= 3
				&& methodname.IndexOf(" get_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf("Update()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Is", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Get", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" internal_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" obj_", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" MemberwiseClone()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" Finalize()", System.StringComparison.OrdinalIgnoreCase) <= 0
				&& methodname.IndexOf(" ToString()", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" CompareTag(", System.StringComparison.Ordinal) <= 0
				&& methodname.IndexOf(" Find", System.StringComparison.Ordinal) <= 0) {

				if (methodParameters.Count() <= 3) {
					bool isAcceptable = true;
					foreach (var parameter in methodParameters) {
						var paramType = parameter.ParameterType;
						if (!isAcceptable) continue;
						isAcceptable = (paramType.Equals(typeof(int)) || paramType.Equals(typeof(float)) || paramType.Equals(typeof(double))
														|| paramType.Equals(typeof(bool)) || paramType.Equals(typeof(Vector3)) || paramType.Equals(typeof(Vector2))
														|| paramType.Equals(typeof(string)) || paramType.Equals(typeof(Object)) || paramType.IsSubclassOf(typeof(Object)));
					}
					if (isAcceptable) newMethodsInfo.Add(method);
				}
			}
		}
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.GetParameters().Count()).ToList();
		newMethodsInfo = newMethodsInfo.OrderBy(o => o.ToString().IndexOf(" set_", System.StringComparison.Ordinal)).ToList();
		methodsInfo = newMethodsInfo.ToArray();
	}

	void UpdateMethodNames() {
		methodNames = new string[0];
		if (!methodsInfo.Any()) return;
		methodNames = new string[methodsInfo.Count()];
		for (int i = 0; i < methodsInfo.Count(); i++) {
			var methodParameters = methodsInfo[i].GetParameters();
			var name = methodsInfo[i].ToString();
			List<int> indx = new List<int>();
			name = name.Substring(name.IndexOf(' ') + 1);
			if (methodsInfo[i].GetParameters().Any()) {
				for (int j = name.IndexOf(','); j > -1; j = name.IndexOf(',', j + 1)) {
					indx.Add(j);
				}
				for (int j = methodParameters.Count() - 2; j >= 0; j--) {
					name = name.Insert(indx[j], " " + methodParameters[j].Name);
				}
				indx.Add(name.IndexOf(")", System.StringComparison.Ordinal));
				name = name.Insert(indx.Last(), " " + methodParameters[methodParameters.Count() - 1].Name);
			}
			if (name.Contains("set_")) {
				name = name.Remove(0, 4);
				name = name.First().ToString().ToUpper() + name.Substring(1);
				name = name.Insert(0, "Setter::");
			}
			name = name.Replace("Int32 ", "Int ");
			name = name.Replace("Single ", "Float ");
			name = name.Replace("UnityEngine.", "");
			name = name.Replace("System.", "");
			methodNames[i] = name;
		}
	}

	void SelectMethodInfo() {
		parameterObjects = new object[0];
		if (methodIndex < 0) return;
		if (!methodsInfo.Any()) return;
		selectedMethodInfo = methodsInfo[methodIndex];
	}

	void UpdateParameterObjects() {
		if (selectedMethodInfo == null) return;
		var parametersInfo = selectedMethodInfo.GetParameters();
		parameterObjects = new object[parametersInfo.Count()];
		for (int i = 0; i < parametersInfo.Count(); i++) {
			parameterObjects[i] = GetParameterObject(i);
		}
	}

	object GetParameterObject(int index) {
		if (selectedMethodInfo.IsGenericMethod) index++;
		Parameter param = parameters[index];
		System.Type type = System.Type.GetType(param.type);

		if (type == null) return null;
		if (type.IsSubclassOf(typeof(Object)) | type.Equals(typeof(Object))) { return param.objectParameter; }
		if (type.Equals(typeof(Vector3))) { return param.vectorParameter; }
		if (type.Equals(typeof(Vector2))) { return (Vector2)param.vectorParameter; }
		if (type.Equals(typeof(Color))) { return param.colorParameter; }
		if (type.Equals(typeof(bool))) { return param.boolParameter; }
		if (type.Equals(typeof(string))) { return param.stringParameter; }
		if (type.Equals(typeof(float))) { return param.floatParameter; }
		if (type.Equals(typeof(double))) { return param.doubleParameter; }
		if (type.Equals(typeof(int))) { return param.intParameter; }
		return null;
	}

	void UpdateParameters() {
		if (selectedMethodInfo == null) return;
		if (!selectedMethodInfo.GetParameters().Any() && !selectedMethodInfo.IsGenericMethod) {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].Clear();
			}
			return;
		}
		if (selectedMethodInfo.IsGenericMethod) {
			for (int i = -1; i < parameters.Count() -1; i++) {
				parameters[i + 1].SetValues(selectedMethodInfo, i);
			}
		} else {
			for (int i = 0; i < parameters.Count(); i++) {
				parameters[i].SetValues(selectedMethodInfo, i);
			}
		}
	}

	void UpdateDelegate() {
		if (selectedMethodInfo == null) return;
		if (selectedMethodInfo.IsGenericMethod) {
			if (parameters[0].stringParameter == "") return;
			var type = System.Type.GetType(parameters[0].stringParameter);
			if (type == null) { Debug.LogWarning("'" + parameters[0].stringParameter + "' Type does not exist, Try to use an assebly"); return; }
			MethodInfo genericFromObject = selectedMethodInfo.MakeGenericMethod(System.Type.GetType(parameters[0].stringParameter));
			selectedMethodInfo = genericFromObject;
			return;
		} else if (!selectedMethodInfo.GetParameters().Any()) {
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				action0 = selectedMethodInfo.CreateAction(target);
			}
		} else if (selectedMethodInfo.GetParameters().Count() == 1) {
			var type = parameterObjects[0].GetType();
			if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				// if (type.Equals(typeof(Object))) { action1 = selectedMethodInfo.CreateAction<Object>(target); }
				if (parameters[0].objectParameter != null && type.Equals(typeof(Object))) { action1 = selectedMethodInfo.CreateAction<Object>(target); }
				else if (type.Equals(typeof(Vector3))) { action1 = selectedMethodInfo.CreateAction<Vector3>(target); }
				else if (type.Equals(typeof(Vector2))) { action1 = selectedMethodInfo.CreateAction<Vector2>(target); }
				else if (type.Equals(typeof(Color))) { action1 = selectedMethodInfo.CreateAction<Color>(target); }
				else if (type.Equals(typeof(bool))) { action1 = selectedMethodInfo.CreateAction<bool>(target); }
				else if (type.Equals(typeof(string))) { action1 = selectedMethodInfo.CreateAction<string>(target); }
				else if (type.Equals(typeof(float))) { action1 = selectedMethodInfo.CreateAction<float>(target); }
				else if (type.Equals(typeof(double))) { action1 = selectedMethodInfo.CreateAction<double>(target); }
				else if (type.Equals(typeof(int))) { action1 = selectedMethodInfo.CreateAction<int>(target); }
			}
		} else if (selectedMethodInfo.GetParameters().Count() > 1) {
			//if (selectedMethodInfo.ReturnType.Equals(typeof(void))) {
				
			//}
		}
	}

	System.Action<object> GetAction<T>(MethodInfo methodInfo, object trgt) {
		return methodInfo.CreateAction<T>(trgt);
	}

	[System.Serializable]
	public struct Parameter {
		public string name;
		public string type;
		public bool isGeneric;
		public Object objectParameter;
		public Color colorParameter;
		public Vector3 vectorParameter;
		public string stringParameter;
		public bool boolParameter;
		public bool isSwitch;
		public float floatParameter;
		public float doubleParameter;
		public int intParameter;
		public MethodInfo methodInfo;
		public ParameterInfo parameterInfo;

		public void Clear(bool all = false) {
			name = ""; type = ""; isGeneric = false; methodInfo = null; parameterInfo = null;
			if (all) {
				objectParameter = null;
				colorParameter = Color.black;
				vectorParameter = Vector3.zero;
				stringParameter = System.String.Empty;
				boolParameter = false;
				isSwitch = false;
				floatParameter = 0;
				doubleParameter = 0;
				intParameter = 0;
			}
		}

		public void SetValues(MethodInfo method, int parameterIndex) {
			ParameterInfo[] parameters = method.GetParameters();
			isGeneric = method.IsGenericMethod;
			if (isGeneric && parameterIndex == -1) {
				parameterInfo = null;
				name = "Type" + method.GetGenericArguments()[0];
				type = typeof(System.Type).AssemblyQualifiedName;
			} else {
				if (parameterIndex >= parameters.Count()) { Clear(); return; }
				if (parameterInfo != parameters[parameterIndex]) Clear();
				parameterInfo = parameters[parameterIndex];
				name = parameterInfo.ToString();
				type = parameterInfo.ParameterType.AssemblyQualifiedName;
			}
			name = name.Split(' ').Last();
			name = name.First().ToString().ToUpper() + name.Substring(1);
			name = System.Text.RegularExpressions.Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
		}
	}

	[System.Serializable]
	public enum InvokeMode {
		Off, Once, RuntimeOnly, RuntimeAndEditor
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ActivatedActionRemake), true)]
public class ActivatedActionRemakeDrawer : PropertyDrawer {
	SerializedProperty target, methodNames, methodIndex, invokeMode;
	Rect propertyRect, headerRect, targetRect, tabRect, prefixRect, navigationRect, bodyRect;
	Rect invokeModeRect, methodRect;
	ActivatedActionRemake activatedAction;
	SerializedProperty parameters;
	List<ParameterValue> parameterValues = new List<ParameterValue>();
	List<Rect> parameterRects = new List<Rect>();

	bool isEnabled;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if (!isEnabled) { OnEnable(new Rect(), property, label); }
		var height = base.GetPropertyHeight(property, label);
		int numberOfParameters = 0;
		for (int i = 0; i < parameters.arraySize; i++) {
			if (parameters.GetArrayElementAtIndex(i) != null
					&& !parameters.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Equals("")) {
				numberOfParameters += 1;
			}
		}
		height += (EditorGUIUtility.singleLineHeight + 3.5f) * (2 + numberOfParameters) - 4;
		return height;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		if (!isEnabled) { OnEnable(position, property, label); }
		EditorGUI.BeginProperty(position, label, property);
		property.serializedObject.Update();
		UpdateRects(position);
		DrawBody();

		EditorGUI.PrefixLabel(prefixRect, new GUIContent(target.displayName + " ()"));
		target.objectReferenceValue =
			EditorGUI.ObjectField(targetRect, new GUIContent(), activatedAction.target, typeof(Object), true);

		if (EditorGUI.DropdownButton(navigationRect, new GUIContent(), FocusType.Keyboard)) {
			PopupWindow.Show(navigationRect, new NavigationPopup(target, activatedAction));
		}
		if (GUI.changed) {
			property.serializedObject.ApplyModifiedProperties();
			activatedAction.target = target.objectReferenceValue;
			GUI.changed = false;
		}
		EditorGUI.LabelField(navigationRect, EditorGUIUtility.IconContent("UnityEditor.HierarchyWindow"));

		EditorGUI.PropertyField(invokeModeRect, invokeMode, new GUIContent());
		methodIndex.intValue = EditorGUI.Popup(methodRect, "", methodIndex.intValue, activatedAction.methodNames);
		if (GUI.changed) {
			property.serializedObject.ApplyModifiedProperties();
			activatedAction.methodIndex = methodIndex.intValue;
			GUI.changed = false;
		}

		for (int i = 0; i < parameters.arraySize; i++) {
			parameterValues[i].Draw(parameterRects[i]);
			if (GUI.changed) {
				property.serializedObject.ApplyModifiedProperties();
				activatedAction.methodIndex = activatedAction.methodIndex;
				GUI.changed = false;
			}
		}
		if (GUI.changed) {
			property.serializedObject.ApplyModifiedProperties();
			GUI.changed = false;
		}
		EditorGUI.EndProperty();
	}

	void OnEnable(Rect position, SerializedProperty property, GUIContent label) {
		int indexArray = 0;
		if (property.propertyPath.IndexOf("Array.data[", System.StringComparison.Ordinal) > 0) {
			System.Int32.TryParse(property.propertyPath.Split('[', ']')[1], out indexArray);
		}
		activatedAction = property.GetTargetObjectOfProperty(indexArray) as ActivatedActionRemake;

		target = property.FindPropertyRelative("_target");
		methodNames = property.FindPropertyRelative("methodNames");
		methodIndex = property.FindPropertyRelative("_methodIndex");
		invokeMode = property.FindPropertyRelative("invokeMode");
		parameters = property.FindPropertyRelative("parameters");

		parameterValues.Clear();
		for (int i = 0; i < parameters.arraySize; i++) {
			parameterValues.Add(new ParameterValue(parameters.GetArrayElementAtIndex(i)));
		}
		activatedAction.target = target.objectReferenceValue;

		isEnabled = (int)position.y != 0;
	}

	void UpdateRects(Rect rect) {
		propertyRect = rect;

		headerRect = new Rect(propertyRect);
		headerRect.height = EditorGUIUtility.singleLineHeight + 4;
		headerRect.y += 3;

		tabRect = new Rect(headerRect);
		tabRect.width = EditorGUIUtility.labelWidth;

		prefixRect = new Rect(tabRect);
		prefixRect.x += 6;

		targetRect = new Rect(headerRect);
		targetRect.height = EditorGUIUtility.singleLineHeight;
		targetRect.x += EditorGUIUtility.labelWidth + 35; targetRect.width -= EditorGUIUtility.labelWidth + 35 + 12;

		navigationRect = new Rect(EditorGUIUtility.labelWidth + 20, targetRect.y,
															27, EditorGUIUtility.singleLineHeight + 1);

		bodyRect = new Rect(propertyRect);
		bodyRect.y += headerRect.height; bodyRect.height -= headerRect.height + 2;

		methodRect = bodyRect;
		methodRect.y += 4; methodRect.height = EditorGUIUtility.singleLineHeight;
		methodRect.x += EditorGUIUtility.labelWidth + 6; methodRect.width -= EditorGUIUtility.labelWidth + 12;

		invokeModeRect = bodyRect;
		invokeModeRect.y += 4; invokeModeRect.height = EditorGUIUtility.singleLineHeight;
		invokeModeRect.x += 6; invokeModeRect.width = EditorGUIUtility.labelWidth - 6;

		GenerateParameterRects(parameters.arraySize);
	}

	void GenerateParameterRects(int size) {
		parameterRects.Clear();
		for (int i = 1; i < size + 1; i++) {
			Rect parameterRect = bodyRect;
			parameterRect.y += 4 + 20 * i; parameterRect.height = EditorGUIUtility.singleLineHeight;
			parameterRect.x += 6; parameterRect.width -= 6 * 2;
			parameterRects.Add(parameterRect);
		}
	}

	void DrawBody() {
		GUI.Box(headerRect, "", new GUIStyle("HelpBox"));
		GUI.Box(tabRect, "", new GUIStyle("RL Header"));
		bodyRect.height -= 1;
		GUI.Box(bodyRect, "", new GUIStyle("HelpBox"));
		bodyRect.height += 1;
		GUI.Box(bodyRect, "", new GUIStyle("RL Background"));
	}
}

public class ParameterValue {
	public SerializedProperty name, type, isGeneric;
	public SerializedProperty objectParameter;
	public SerializedProperty colorParameter;
	public SerializedProperty vectorParameter;
	public SerializedProperty stringParameter;
	public SerializedProperty boolParameter, isSwitch;
	public SerializedProperty floatParameter;
	public SerializedProperty doubleParameter;
	public SerializedProperty intParameter;
	public SerializedProperty property;
	public bool isEnabled;

	public ParameterValue(SerializedProperty property) {
		this.property = property;
		if (!isEnabled) { OnEnable(); }
	}

	public void Draw(Rect position) {
		var type = System.Type.GetType(this.type.stringValue);
		if (type == null) return;
		if (type.Equals(typeof(System.Type))) {
			Rect objectRect = position; objectRect.width = 20; objectRect.x = position.width;
			Rect stringRect = position; stringRect.width -= 17;
			objectParameter.objectReferenceValue =
				EditorGUI.ObjectField(objectRect, new GUIContent(), objectParameter.objectReferenceValue,
															typeof(object), true);
			if (GUI.changed) {
				if (objectParameter.objectReferenceValue == null) return;
				string typeName = objectParameter.objectReferenceValue.GetType().FullName;
				if (typeName.Contains("UnityEditor")) {
					typeName = objectParameter.objectReferenceValue.name + ", Assembly-CSharp";
				}
				if (System.Type.GetType(typeName) == null) { typeName += ", UnityEngine"; }
				if (System.Type.GetType(typeName) == null) { typeName = ""; }
				stringParameter.stringValue = typeName;
			}
			stringParameter.stringValue =
				EditorGUI.DelayedTextField(stringRect, new GUIContent(name.stringValue), stringParameter.stringValue); return;
		}
		if (type.IsSubclassOf(typeof(Object)) | type.Equals(typeof(Object))) {
			if (objectParameter.objectReferenceValue != null 
			    && !(objectParameter.objectReferenceValue.GetType().Equals(type) || objectParameter.objectReferenceValue.GetType().IsSubclassOf(type))) {
				objectParameter.objectReferenceValue = null; }
			objectParameter.objectReferenceValue = 
				EditorGUI.ObjectField(position, new GUIContent(name.stringValue), objectParameter.objectReferenceValue, type, true); return; }
		if (type.Equals(typeof(Vector3))) {
			vectorParameter.vector3Value = 
				EditorGUI.Vector3Field(position, new GUIContent(name.stringValue), vectorParameter.vector3Value); return; }
		if (type.Equals(typeof(Vector2))) {
			vectorParameter.vector3Value = 
				EditorGUI.Vector2Field(position, new GUIContent(name.stringValue), vectorParameter.vector3Value); return; }
		if (type.Equals(typeof(Color))) { 
			colorParameter.colorValue =
			 EditorGUI.ColorField(position, new GUIContent(name.stringValue), colorParameter.colorValue); return; }
		if (type.Equals(typeof(bool))) {
			Rect switchRect, boolRect;
			switchRect = position; switchRect.width = (position.xMax - position.xMin) / 2; switchRect.x += (position.xMax - position.xMin) / 2;
			boolRect = position; boolRect.width = (position.xMax - position.xMin) / 2;
			boolParameter.boolValue = 
				EditorGUI.Toggle(boolRect, new GUIContent(name.stringValue), boolParameter.boolValue);
			isSwitch.boolValue = 
				EditorGUI.Toggle(switchRect, new GUIContent("Switch"), isSwitch.boolValue); return; }
		if (type.Equals(typeof(string))) { 
			stringParameter.stringValue = 
				EditorGUI.TextField(position, new GUIContent(name.stringValue), stringParameter.stringValue); return; }
		if (type.Equals(typeof(float))) { 
			floatParameter.floatValue = 
				EditorGUI.FloatField(position, new GUIContent(name.stringValue), floatParameter.floatValue); return; }
		if (type.Equals(typeof(double))) { 
			doubleParameter.doubleValue = 
				EditorGUI.DoubleField(position, new GUIContent(name.stringValue), doubleParameter.doubleValue); return; }
		if (type.Equals(typeof(int))) { 
			intParameter.intValue = 
				EditorGUI.IntField(position, new GUIContent(name.stringValue), intParameter.intValue); return; }
	}

	void OnEnable() {
		name = property.FindPropertyRelative("name");
		type = property.FindPropertyRelative("type");
		isGeneric = property.FindPropertyRelative("isGeneric");
		objectParameter = property.FindPropertyRelative("objectParameter");
		colorParameter = property.FindPropertyRelative("colorParameter");
		vectorParameter = property.FindPropertyRelative("vectorParameter");
		stringParameter = property.FindPropertyRelative("stringParameter");
		boolParameter = property.FindPropertyRelative("boolParameter");
		isSwitch = property.FindPropertyRelative("isSwitch");
		floatParameter = property.FindPropertyRelative("floatParameter");
		doubleParameter = property.FindPropertyRelative("doubleParameter");
		intParameter = property.FindPropertyRelative("intParameter");

		isEnabled = true;
	}
}

public class NavigationPopup : PopupWindowContent {
	public NavigationPopup(SerializedProperty target, ActivatedActionRemake activatedAction) {
		this.target = target;
		this.activatedAction = activatedAction;
		GetNavigationContent();
	}
	ActivatedActionRemake activatedAction;
	public SerializedProperty target;
	Queue<Object> objects = new Queue<Object>();
	Queue<GUIContent[]> componentNames = new Queue<GUIContent[]>();

	public override Vector2 GetWindowSize() {
		if (objects.Any()) {
			return new Vector2(EditorGUIUtility.labelWidth, (EditorGUIUtility.singleLineHeight + 2) * objects.Count() + 12);
		} else {
			return Vector3.zero;
		}
	}

	public override void OnGUI(Rect rect) {
		Rect objRect = new Rect(rect);
		objRect.y += 6;
		objRect.height = EditorGUIUtility.singleLineHeight + 2;
		objRect.x += 12; objRect.width -= 24;

		for (int i = 0; i < objects.Count(); i++) {
			var icon = componentNames.ToList()[i][0];
			var name = componentNames.ToList()[i][1];
			objRect.width = 15;
			GUI.Box(objRect, icon.image, new GUIStyle());
			objRect.x = rect.x + 30;
			objRect.width = rect.width - 30 - 6;
			GUI.Box(objRect, name, new GUIStyle("IN ObjectField"));
			objRect.x = rect.x + 12;
			objRect.width = rect.width - 24;
			if (EditorGUI.DropdownButton(objRect, new GUIContent(), FocusType.Keyboard, new GUIStyle())) {
				target.objectReferenceValue = objects.ToList()[i];
				target.serializedObject.ApplyModifiedProperties();
				activatedAction.target = target.objectReferenceValue;
				editorWindow.Close();
			}
			objRect.y += EditorGUIUtility.singleLineHeight + 2;
		}
	}

	public override void OnOpen() {
		GetNavigationContent();
	}

	public override void OnClose() {
	}

	void GetNavigationContent() {
		objects = new Queue<Object>();
		componentNames = new Queue<GUIContent[]>();
		if ((target.objectReferenceValue as GameObject)) {
			var obtainedValues = (target.objectReferenceValue as GameObject).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				var icon = EditorGUIUtility.ObjectContent(objects.ElementAt(i), objects.ElementAt(i).GetType());
				icon = new GUIContent(icon.image);
				componentNames.Enqueue(new GUIContent[] {
					icon,
					new GUIContent(objects.ElementAt(i).GetType().ToString().Split('.').Last())
				});
			}
			objects.Enqueue(target.objectReferenceValue as GameObject);
			componentNames.Enqueue(new GUIContent[] {
				EditorGUIUtility.IconContent("GameObject Icon"),
				new GUIContent("GameObject")
			});
		} else if ((target.objectReferenceValue as Component)) {
			var obtainedValues = (target.objectReferenceValue as Component).GetComponents<Component>().ToList();
			for (int i = 0; i < obtainedValues.Count(); i++) {
				objects.Enqueue(obtainedValues[i]);
				var icon = EditorGUIUtility.ObjectContent(objects.ElementAt(i), objects.ElementAt(i).GetType());
				icon = new GUIContent(icon.image);
				componentNames.Enqueue(new GUIContent[] {
					icon,
					new GUIContent(objects.ElementAt(i).GetType().ToString().Split('.').Last())
				});
			}
			objects.Enqueue((target.objectReferenceValue as Component).gameObject);
			componentNames.Enqueue(new GUIContent[] {
				EditorGUIUtility.IconContent("GameObject Icon"),
				new GUIContent("GameObject")
			});
		}
	}
}
#endif