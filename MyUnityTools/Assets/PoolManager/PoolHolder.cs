using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityTools;

public class PoolHolder : MonoBehaviour {
	/// <summary>
	/// The pool identifier.
	/// </summary>

#if UNITY_EDITOR
	[ReadOnly]
#endif
	[SerializeField]
	int poolID;
	public int prefabID {
		get {
			return poolID;
		}
	}

	/// <summary>
	/// The pool prefab.
	/// </summary>
	[SerializeField] GameObject poolPrefab;
	public GameObject prefab {
		get {
			return poolPrefab;
		}
	}
	/// <summary>
	/// Makes PoolObjects in this PoolHolder be child of this GameObject only when added.
	/// </summary>
	[Tooltip("Makes PoolObjects in this PoolHolder be child of this GameObject only when added.")]
	public bool parenting = true;
	/// <summary>
	/// Makes PoolObjects in this PoolHolder be disabled/enabled.
	/// </summary>
	[Tooltip("Makes PoolObjects in this PoolHolder be disabled/enabled.")]
	public bool modifyState = true;
	/// <summary>
	/// Invokes methods OnRecycle() and OnReused() on PoolObjects in this PoolHolder.
	/// </summary>
	[Tooltip("Invokes methods OnRecycle() and OnReused() on PoolObjects in this PoolHolder.")]
	public bool sendMessage = true;
	/// <summary>
	/// Amount of PoolObjects added when needed using ForceTakeObject()
	/// </summary>
	[Tooltip("Amount of PoolObjects added when needed using ForceTakeObject()")]
	public int growRate = 10;
	/// <summary>
	/// The size of the PoolObjects in awake.
	/// </summary>
	public int awakeSize = 0;

	/// <summary>
	/// The objects in pool.
	/// </summary>
	[SerializeField] Queue<GameObject> objectsInPool;

	void Start() {
		if (poolPrefab == null) {
			Debug.Log("Pool prefab is missing or null", this);
		} else {
			Debug.Log("Iniciado", this.gameObject);
			poolID = poolPrefab.GetInstanceID();
			objectsInPool = new Queue<GameObject>();
			if (PoolManager.AddExistingPoolHolder(this).GetInstanceID() == GetInstanceID()) {
				Add(awakeSize);
			} else {
				poolPrefab = null;
				poolID = 0;
			}
		}
	}

	/// <summary>
	/// Sets the pool prefab.
	/// </summary>
	/// <param name="prefab">Prefab.</param>
	public void SetPoolPrefab(GameObject prefab) {
		if (poolPrefab == null) {
			poolPrefab = prefab;
			poolID = prefab.GetInstanceID();
			objectsInPool = new Queue<GameObject>();
		} else {
			Debug.LogWarning("Pool prefab is already setted");
		}
	}

	/// <summary>
	/// Adds pool objects by the specified value.
	/// </summary>
	/// <returns>The add.</returns>
	/// <param name="value">Value.</param>
	public void Add(int value) {
		Add(value, transform);
	}
	/// <summary>
	/// Add pool objects by the specified value and parenting.
	/// </summary>
	/// <returns>The add.</returns>
	/// <param name="value">Value.</param>
	/// <param name="parenting">Parenting.</param>
	public void Add(int value, Transform parenting) {
		value = value > 0 ? value : 1;
		for (int i = 0; i < value; i++) {
			GameObject poolObject = Instantiate(poolPrefab);
			poolObject.name = poolObject.GetInstanceID() + "@" + parenting.name;
			if (poolObject.GetComponent<PoolObject>() == null) {
				poolObject.AddComponent<PoolObject>().SetPoolHolder(this);
			} else {
				poolObject.GetComponent<PoolObject>().SetPoolHolder(this);
			}
			if (parenting && this.parenting) { 
				poolObject.transform.parent = parenting;
			}
			if (modifyState) poolObject.SetActive(false);
			objectsInPool.Enqueue(poolObject);
		}
	}

	/// <summary>
	/// Recycle the specified GameObject.
	/// </summary>
	/// <returns>The recycle.</returns>
	/// <param name="gameObject">GameObject.</param>
	public void Recycle(GameObject gameObject) {
		if (sendMessage) gameObject.SendMessageForInactive("OnRecycle");
		if (modifyState) gameObject.SetActive(false);
		objectsInPool.Enqueue(gameObject);
	}

	/// <summary>
	/// Takes a GameObject and recycles it.
	/// </summary>
	/// <returns>The object.</returns>
	public GameObject PickObject() {
		GameObject pickedObject;
		if (objectsInPool.Count == 0) {
			throw new System.Exception("PoolHolder is empty try adding objects first!");
		} else {
			pickedObject = objectsInPool.Dequeue();
			if (sendMessage) pickedObject.SendMessageForInactive("OnRecycle");
			objectsInPool.Enqueue(pickedObject);
			if (sendMessage) pickedObject.SendMessageForInactive("OnReuse");
			if (modifyState) pickedObject.SetActive(true);
			return pickedObject;
		}
	}

	/// <summary>
	/// Forces to take a GameObject even if there aren't more.
	/// </summary>
	/// <returns>The taken object.</returns>
	public GameObject ForceTakeObject() {
		if (objectsInPool.Count == 0) {
			Add(growRate);
			return ForceTakeObject();
		} else {
			GameObject takedObject = objectsInPool.Dequeue();
			if (sendMessage) takedObject.SendMessageForInactive("OnReuse");
			if (modifyState) takedObject.SetActive(true);
			return takedObject;
		}
	}

	/// <summary>
	/// Takes a GameObject from the pool.
	/// </summary>
	/// <returns>The taken object.</returns>
	public GameObject TakeObject() {
		if (objectsInPool.Count == 0) {
			Debug.LogWarning("Pool is empty, try using ForceTakeObject() instead", gameObject);
			return null;
		} else {
			GameObject takedObject = objectsInPool.Dequeue();
			if (sendMessage) takedObject.SendMessageForInactive("OnReuse");
			if (modifyState) takedObject.SetActive(true);
			return takedObject;
		}
	}
	/// <summary>
	/// Takes an array of GameObjects in pool.
	/// </summary>
	/// <returns>The taked objects.</returns>
	/// <param name="value">Value.</param>
	public GameObject[] TakeObjects(int value) {
		List<GameObject> takenObjects = new List<GameObject>();
		if (objectsInPool.Count == 0) {
			return null;
		} else {
			for (int i = 0; i < value; i++) {
				GameObject takedObject = TakeObject();
				if (!takedObject) break;
				takenObjects.Add(takedObject);
			}
			return takenObjects.ToArray();
		}
	}

	void OnDestroy() {
		if (poolID != 0) {
			PoolManager.RemovePoolHolder(poolID);
		}
	}
}
