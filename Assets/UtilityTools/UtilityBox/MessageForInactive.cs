using System;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace UtilityTools {
	public static partial class GeneralTool {
		public static Action<object> CreateAction<T>(this MethodInfo methodInfo, object target) {
			if (methodInfo.IsStatic) {
				var delegS = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), methodInfo);
				return param => delegS((T)param);
			}
			var deleg = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, methodInfo);
			return param => deleg((T)param);
		}

		public static Action CreateAction(this MethodInfo methodInfo, object target) {
			if (methodInfo.IsStatic) {
				var delegS = (Action)Delegate.CreateDelegate(typeof(Action), methodInfo);
				return delegS;
			}
			var deleg = (Action)Delegate.CreateDelegate(typeof(Action), target, methodInfo);
			return deleg;
		}

		/// <summary>
		/// Determine if the object has the given method
		/// </summary>
		public static bool InvokeIfExists(this object objectToCheck, string methodName, BindingFlags flags, params object[] parameters) {
			MethodInfo methodInfo = objectToCheck.GetMethodInfo(methodName, flags, parameters);
			if (methodInfo != null) {
				methodInfo.Invoke(objectToCheck, parameters);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets the method info of a object.
		/// </summary>
		/// <returns>The method info.</returns>
		/// <param name="objectToCheck">Object to check.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="parameters">Parameters.</param>
		public static MethodInfo GetMethodInfo(this object objectToCheck, string methodName, BindingFlags flags, params object[] parameters) {
			Type type = objectToCheck.GetType();
			if (parameters.Count() > 0) {
				Type[] parametersTypes = new Type[parameters.Count()];
				for (int i = 0; i < parameters.Count(); i++) {
					parametersTypes[i] = parameters[i].GetType();
				}
				MethodInfo methodInfo = type.GetMethod(methodName, flags, Type.DefaultBinder, parametersTypes, null);
				return methodInfo;
			} else {
				MethodInfo methodInfo = type.GetMethod(methodName, flags);
				return methodInfo;
			}
		}

		public static MethodInfo[] GetMethodsInfo(this object objectToCheck, BindingFlags flags) {
			Type type = objectToCheck.GetType();

			MethodInfo[] methodsInfo = type.GetMethods(flags);
			return methodsInfo;
		}
	}

		public static partial class UnityTool {
		/// <summary>
		/// Invoke the method if it exists in any component of the component's game object, even if they are inactive
		/// </summary>
		public static bool SendMessageForInactive(this Component component, string methodName, params object[] parameters) {
			if (component.InvokeIfExists(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameters)) {
				return true;
			}
			return false;
		}
		/// <summary>
		/// Invoke the method if it exists in any component of the game object, even if they are inactive
		/// </summary>
		public static bool SendMessageForInactive(this GameObject gameobject, string methodName, params object[] parameters) {
			MonoBehaviour[] components = gameobject.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour m in components) {
				if (m.InvokeIfExists(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameters)) {
					return true;
				}
			}
			return false;
		}
	}
}