using UnityEngine;
using System.Collections;

public class ToggleActive : MonoBehaviour
{
	public GameObject toggleObject;
	public void Toggle ()
	{
		toggleObject.SetActive (!toggleObject.activeSelf);
	}
}
