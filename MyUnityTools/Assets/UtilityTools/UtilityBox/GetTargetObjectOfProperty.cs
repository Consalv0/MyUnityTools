#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UtilityToolsEditor {
	public static partial class EditorTool {
		public static T GetActualObjectForSerializedProperty<T>(FieldInfo fieldInfo, SerializedProperty property) where T : class {
			var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
			if (obj == null) { return null; }

			T actualObject = null;
			if (obj.GetType().IsArray) {
				var index = System.Convert.ToInt32(new string(property.propertyPath.Where(c => char.IsDigit(c)).ToArray()));
				actualObject = ((T[])obj)[index];
			} else {
				actualObject = obj as T;
			}
			return actualObject;
		}

		public static object GetTargetObjectOfProperty(this SerializedProperty prop, int index) {
			string path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			string[] elements = path.Split('.');
			foreach (var element in elements) {
				if (element.Contains("[")) {
					var elementName = element.Substring(0, element.IndexOf("[", System.StringComparison.Ordinal));
					obj = GetValue(obj, elementName, index);
				} else {
					obj = GetValue(obj, element);
				}
			}
			return obj;
		}

		static object GetValue(object source, string name, int index) {
			var enumerable = GetValue(source, name) as IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();

			for (int i = 0; i <= index; i++) {
				if (!enm.MoveNext()) return null;
			}
			return enm.Current;
		}

		static object GetValue(object source, string name) {
			if (source == null)
				return null;
			var type = source.GetType();
			while (type != null) {
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}
			return null;
		}
	}

	// --------------------------------------------------------------------------------------------------------------------
	// <author>
	//   HiddenMonk
	//   http://answers.unity3d.com/users/496850/hiddenmonk.html
	//   
	//   Johannes Deml
	//   send@johannesdeml.com
	// </author>
	// --------------------------------------------------------------------------------------------------------------------

	namespace Supyrb {
		using System;
		using UnityEngine;
		using UnityEditor;
		using System.Reflection;
		using System.Collections.Generic;

		/// <summary>
		/// Extension class for SerializedProperties
		/// See also: http://answers.unity3d.com/questions/627090/convert-serializedproperty-to-custom-class.html
		/// </summary>
		public static class SerializedPropertyExtensions {
			/// <summary>
			/// Get the object the serialized property holds by using reflection
			/// </summary>
			/// <typeparam name="T">The object type that the property contains</typeparam>
			/// <param name="property"></param>
			/// <returns>Returns the object type T if it is the type the property actually contains</returns>
			public static T GetValue<T>(this SerializedProperty property) {
				return GetNestedObject<T>(property.propertyPath, GetSerializedPropertyRootComponent(property));
			}

			/// <summary>
			/// Set the value of a field of the property with the type T
			/// </summary>
			/// <typeparam name="T">The type of the field that is set</typeparam>
			/// <param name="property">The serialized property that should be set</param>
			/// <param name="value">The new value for the specified property</param>
			/// <returns>Returns if the operation was successful or failed</returns>
			public static bool SetValue<T>(this SerializedProperty property, T value) {

				object obj = GetSerializedPropertyRootComponent(property);
				//Iterate to parent object of the value, necessary if it is a nested object
				string[] fieldStructure = property.propertyPath.Split('.');
				for (int i = 0; i < fieldStructure.Length - 1; i++) {
					obj = GetFieldOrPropertyValue<object>(fieldStructure[i], obj);
				}
				string fieldName = fieldStructure.Last();

				return SetFieldOrPropertyValue(fieldName, obj, value);

			}

			/// <summary>
			/// Get the component of a serialized property
			/// </summary>
			/// <param name="property">The property that is part of the component</param>
			/// <returns>The root component of the property</returns>
			public static Component GetSerializedPropertyRootComponent(SerializedProperty property) {
				return (Component)property.serializedObject.targetObject;
			}

			/// <summary>
			/// Iterates through objects to handle objects that are nested in the root object
			/// </summary>
			/// <typeparam name="T">The type of the nested object</typeparam>
			/// <param name="path">Path to the object through other properties e.g. PlayerInformation.Health</param>
			/// <param name="obj">The root object from which this path leads to the property</param>
			/// <param name="includeAllBases">Include base classes and interfaces as well</param>
			/// <returns>Returns the nested object casted to the type T</returns>
			public static T GetNestedObject<T>(string path, object obj, bool includeAllBases = false) {
				foreach (string part in path.Split('.')) {
					obj = GetFieldOrPropertyValue<object>(part, obj, includeAllBases);
				}
				return (T)obj;
			}

			public static T GetFieldOrPropertyValue<T>(string fieldName, object obj, bool includeAllBases = false,
																								 BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static |
																								 BindingFlags.Public | BindingFlags.NonPublic) {
				FieldInfo field = obj.GetType().GetField(fieldName, bindings);
				if (field != null) return (T)field.GetValue(obj);

				PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
				if (property != null) return (T)property.GetValue(obj, null);

				if (includeAllBases) {

					foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType())) {
						field = type.GetField(fieldName, bindings);
						if (field != null) return (T)field.GetValue(obj);

						property = type.GetProperty(fieldName, bindings);
						if (property != null) return (T)property.GetValue(obj, null);
					}
				}

				return default(T);
			}

			public static bool SetFieldOrPropertyValue(string fieldName, object obj, object value, bool includeAllBases = false,
																								 BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static
																								 | BindingFlags.Public | BindingFlags.NonPublic) {
				FieldInfo field = obj.GetType().GetField(fieldName, bindings);
				if (field != null) {
					field.SetValue(obj, value);
					return true;
				}

				PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
				if (property != null) {
					property.SetValue(obj, value, null);
					return true;
				}

				if (includeAllBases) {
					foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType())) {
						field = type.GetField(fieldName, bindings);
						if (field != null) {
							field.SetValue(obj, value);
							return true;
						}

						property = type.GetProperty(fieldName, bindings);
						if (property != null) {
							property.SetValue(obj, value, null);
							return true;
						}
					}
				}
				return false;
			}

			public static IEnumerable<Type> GetBaseClassesAndInterfaces(this Type type, bool includeSelf = false) {
				List<Type> allTypes = new List<Type>();

				if (includeSelf) allTypes.Add(type);

				if (type.BaseType == typeof(object)) {
					allTypes.AddRange(type.GetInterfaces());
				} else {
					allTypes.AddRange(
									Enumerable
									.Repeat(type.BaseType, 1)
									.Concat(type.GetInterfaces())
									.Concat(type.BaseType.GetBaseClassesAndInterfaces())
									.Distinct());
				}

				return allTypes;
			}
		}
	}
}
#endif