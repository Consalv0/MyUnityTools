using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ControllerMovement : MonoBehaviour {
	public Camera cam;
	public float speed = 200;
	public float jumpForce = 300;
	public float sprintMultiplier = 2.5f;
	public float footDistance = 1;
	public string horizontalAxis = "Horizontal";
	public string verticalAxis = "Vertical";
	public string jumpButton = "Jump";
	public string sprintButton = "Submit";

	Rigidbody rigidBody;
	Vector2 inputMovement;
	Vector2 movementNormalized;

	void Awake() {
		if (!cam) {
			cam = Camera.main;
		}
		rigidBody = GetComponent<Rigidbody>();
	}

	void FixedUpdate() {
		inputMovement = new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis));
		movementNormalized = inputMovement.normalized;
		var sprint = Input.GetButton(sprintButton) ? sprintMultiplier : 1;
		rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);

		if (movementNormalized.magnitude > 0) {
			Vector3 movement = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z) * inputMovement.y
			                 + new Vector3(cam.transform.right.x, 0, cam.transform.right.z) * inputMovement.x;
			rigidBody.AddForce(movement.normalized * speed * sprint * 10, ForceMode.Force);
		}

		if (Input.GetButtonDown(jumpButton) && IsGrounded()) {
			rigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
		}
		if (Input.GetKeyDown(KeyCode.L)) {
			Cursor.lockState = CursorLockMode.None;
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	bool IsGrounded() {
		Ray ray = new Ray(transform.position, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, footDistance + 0.1f)) {
			if (hit.transform.tag == "Floor") {
				return true;
			}
		}
		return false;
	}

#if UNITY_EDITOR
	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, transform.position + Vector3.down * footDistance);
	}
#endif
}
