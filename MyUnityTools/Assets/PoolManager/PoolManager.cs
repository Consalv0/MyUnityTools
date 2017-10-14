using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class PoolManager {
	/// <summary>
	/// The pool holders.
	/// </summary>
	public static Dictionary<int, PoolHolder> poolHolders = new Dictionary<int, PoolHolder>();
	/// <summary>
	/// The default pool holder.
	/// </summary>
	public static GameObject poolManager = GameObject.Find("PoolManager") ?
	                                       GameObject.Find("PoolManager") : new GameObject("PoolManager");

	/// <summary>
	/// Gets the pool holder of the given prefab.
	/// </summary>
	/// <returns>The pool holder.</returns>
	/// <param name="prefab">Prefab.</param>
	public static PoolHolder FindPoolHolder(GameObject prefab) {
		return FindPoolHolder(prefab.GetInstanceID());
	}
	/// <summary>
	/// Gets the pool holder of the given prefab ID.
	/// </summary>
	/// <returns>The pool holder.</returns>
	/// <param name="prefabID">Prefab identifier.</param>
	public static PoolHolder FindPoolHolder(int prefabID) {
		if (poolHolders.Count == 0) {
			UnityEngine.Debug.LogWarning("PoolManager: No 'PoolHolder' instances. Try creating one first.");
			return null;
		} else {
			PoolHolder ph;
			poolHolders.TryGetValue(prefabID, out ph);
			return ph;
		}
	}

	/// <summary>
	/// Adds the existing pool holder.
	/// </summary>
	/// <returns>The existing pool holder.</returns>
	/// <param name="poolHolder">Pool holder.</param>
	public static PoolHolder AddExistingPoolHolder(PoolHolder poolHolder) {
		if (poolHolders.ContainsKey(poolHolder.prefabID)) {
			UnityEngine.Debug.LogWarning("The pool of '" + poolHolder.prefab.name + "' already exist", poolHolder);
			return poolHolders[poolHolder.prefabID];
		} else {
			poolHolders.Add(poolHolder.prefabID, poolHolder);
			return poolHolder;
		}
	}
	/// <summary>
	/// Adds a PoolHolder to a given GameObject.
	/// </summary>
	/// <returns>The added pool holder.</returns>
	/// <param name="prefab">Prefab.</param>
	/// <param name="parent">Parent.</param>
	public static PoolHolder AddPoolHolder(GameObject prefab, GameObject parent) {
		return AddPoolHolder(prefab, parent, 0);
	}
	/// <summary> 
	/// Adds a PoolHolder to a given GameObject with a start size.
	/// </summary>
	/// <returns>The added pool holder.</returns>
	/// <param name="prefab">Prefab.</param>
	/// <param name="parent">Parent.</param>
	/// <param name="poolSize">Pool size.</param>
	public static PoolHolder AddPoolHolder(GameObject prefab, GameObject parent, int poolSize) {
		int prefabID = prefab.GetInstanceID();
		if (poolHolders.ContainsKey(prefabID)) {
			UnityEngine.Debug.LogWarning("The pool of '" + prefab.name + "' already exist", poolHolders[prefabID]);
			return poolHolders[prefabID];
		} else {
			PoolHolder newPoolHolder = parent.AddComponent<PoolHolder>();
			newPoolHolder.SetPoolPrefab(prefab);
			newPoolHolder.Add(poolSize);
			poolHolders.Add(prefabID, newPoolHolder);

			return newPoolHolder;
		}
	}

	/// <summary>
	/// Creates a pool holder.
	/// </summary>
	/// <returns>The pool holder.</returns>
	/// <param name="prefab">Prefab.</param>
	public static PoolHolder CreatePoolHolder(GameObject prefab) {
		return CreatePoolHolder(prefab, -1);
	}
	/// <summary>
	/// Creates a pool holder with a start size.
	/// </summary>
	/// <returns>The pool holder.</returns>
	/// <param name="prefab">Prefab.</param>
	/// <param name="startSize">Pool initial size.</param>
	public static PoolHolder CreatePoolHolder(GameObject prefab, int startSize) {
		int prefabID = prefab.GetInstanceID();

		if (new StackFrame(1, true).GetMethod().Name == "Awake") {
			UnityEngine.Debug.LogWarning("Making a PoolHolder in 'Awake()' may result in errors try using 'Start() instead'", prefab);
		}

		if (poolHolders.ContainsKey(prefabID)) {
			UnityEngine.Debug.Log("Pool already exists", poolHolders[prefabID]);
			return poolHolders[prefabID];
		} else {
			GameObject newPool = new GameObject(prefab.name + "Pool");
			newPool.transform.parent = poolManager.transform;
			PoolHolder newPoolHolder = newPool.AddComponent<PoolHolder>();
			newPoolHolder.SetPoolPrefab(prefab);
			if (startSize > 0) newPoolHolder.awakeSize = startSize;

			return newPoolHolder;
		}
	}

	/// <summary>
	/// Removes the pool holder.
	/// </summary>
	/// <param name="poolID">Pool identifier.</param>
	public static void RemovePoolHolder(int poolID) {
		poolHolders.Remove(poolID);
	}
}
