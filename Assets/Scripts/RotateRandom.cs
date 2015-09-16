using UnityEngine;
using System.Collections;

public class RotateRandom : MonoBehaviour {


	// Update is called once per frame
	void Update () {
		int r1 = Random.Range (1, 3);
		int r2 = Random.Range (1, 3);
		int r3 = Random.Range (1, 3);
		transform.Rotate(new Vector3(15*r1, 15*r2, 15*r3) * Time.deltaTime);
	}
}
