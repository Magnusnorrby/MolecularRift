using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;



public class CustomColorAtom : MonoBehaviour
{
	public GameObject periodicTable;
	public Text file;
	private string file_path;
	Dictionary<string, Color> atomColor = new Dictionary<string, Color> ();

	public void Start ()
	{
		file_path = file.text;
		readColors (File.ReadAllLines (file_path));
		updateColor ();



	}

	public void Update ()
	{
		if (file_path != file.text && File.Exists (file.text)) {
			file_path = file.text;
			readColors (File.ReadAllLines (file_path));
			updateColor ();
		}
	}

	public void updateColor ()
	{
		Image[] elements = periodicTable.GetComponentsInChildren<Image> ();
		foreach (Image element in elements) {
			string name = element.GetComponentInChildren<Text> ().text.ToUpper ();
			if (atomColor.ContainsKey (name))
				element.color = atomColor [name];
			else 
				element.color = new Color (0.8f, 0.8f, 0.8f, 1f);
		}
	}


	public void readColors (string[] lines)
	{
		atomColor = new Dictionary<string, Color> ();
		foreach (string line in lines) {
			string[] data = line.Split (',');
			try {
				atomColor.Add (data [0].ToUpper (), new Color (float.Parse (data [1]), float.Parse (data [2]), float.Parse (data [3])));
			} catch (System.Exception e) {
				Debug.Log(e);
			}
		}
	}
}


