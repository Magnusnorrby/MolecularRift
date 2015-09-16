using UnityEngine;
using System.Collections;

public class GestureMover : MonoBehaviour {

	public GameObject hand;
	Animator anim; 
	


	void Start () {
		anim = hand.GetComponent<Animator>();
	}

	public void OpenHand(){
		anim.SetBool ("Tracking",true);
		anim.SetInteger ("Gesture", 1);
	}

	public void CloseHand(){
		anim.SetBool ("Tracking",true);
		anim.SetInteger ("Gesture", 2);
		
	}

	public void Lasso(){
		anim.SetBool ("Tracking",true);
		anim.SetInteger ("Gesture", 3);
	}

	public void Tracked(){
		anim.SetBool ("Tracking",true);

	}

	public void NotTracked(){
		anim.SetBool ("Tracking",false);

	}
}
