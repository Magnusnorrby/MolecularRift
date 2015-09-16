using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class Menu : VRGUI
{

	protected AddAtoms addAtoms;

	private float buttonSize = 50f;

	private int buttonCount = 0;

	private AudioSource source;

	private List<Rect> buttons; 
	private List<Action> buttonFunctions;

	public Texture2D background;

	public AudioClip menuClicked;
	
	public override void OnVRGUI ()
	{
		addAtoms = gameObject.GetComponent<AddAtoms> ();

		source = GetComponent<AudioSource> ();

		GUIStyle myStyle = new GUIStyle ();
		myStyle.fontSize = 25;
		myStyle.fontStyle = FontStyle.Bold;
		myStyle.alignment = TextAnchor.MiddleCenter;
		myStyle.normal.textColor = Color.black; 


		//has to be reset each time we draw the gui
		buttonCount = 0; 
		buttons = new List<Rect> ();
		buttonFunctions = new List<Action> ();

		GUILayout.BeginArea (new Rect (0f, 0f, Screen.width, Screen.height));		

		if (addAtoms.proteinShowing) 
			GUI.Box (createRect (), "Hide Protein", myStyle);
		else 
			GUI.Box (createRect (), "Show Protein", myStyle); 

		buttonFunctions.Add (() => bonds ());

			
		if (addAtoms.ribbonShowing)
			GUI.Box (createRect (), "Hide Ribbons", myStyle);
		else
			GUI.Box (createRect (), "Show Ribbons", myStyle);	
		buttonFunctions.Add (() => ribbons ());

		if (addAtoms.alphaShowing)
			GUI.Box (createRect (), "Hide Alpha Trace", myStyle);
		else
			GUI.Box (createRect (), "Show Alpha Trace", myStyle);
		buttonFunctions.Add (() => alpha ());

		if (addAtoms.waterShowing)
			GUI.Box (createRect (), "Hide Water", myStyle);
		else
			GUI.Box (createRect (), "Show Water", myStyle);
		buttonFunctions.Add (() => water ());

		if (addAtoms.ionsShowing)
			GUI.Box (createRect (), "Hide Ions", myStyle);
		else
			GUI.Box (createRect (), "Show Ions", myStyle);
		buttonFunctions.Add (() => ions ());

		if (addAtoms.HBondShowing)
			GUI.Box (createRect (), "Hide H-Bonds", myStyle);
		else
			GUI.Box (createRect (), "Show H-Bonds", myStyle);
		buttonFunctions.Add (() => hbonds ());

		GUI.Box (createRect (), "Reset Center", myStyle);
		buttonFunctions.Add (() => resetTarget ());

		GUILayout.EndArea ();

		
	}

	private void bonds ()
	{		
		if (addAtoms.proteinShowing) {
			addAtoms.proteinShowing = addAtoms.resetProtein ("bonds");
			if (addAtoms.ballAndStickShowing)
				addAtoms.resetProtein ("balls");
		} else {
			addAtoms.proteinShowing = addAtoms.showMode ("bonds");
			if (addAtoms.ballAndStickShowing)
				addAtoms.showMode ("balls");
		}
	}

	private void ribbons ()
	{
		if (addAtoms.alphaShowing) {
			addAtoms.alphaShowing = addAtoms.resetProtein ("alpha");
		}
		if (addAtoms.ribbonShowing)
			addAtoms.ribbonShowing = addAtoms.resetProtein ("ribbons");
		else
			addAtoms.ribbonShowing = addAtoms.showMode ("ribbons");
	}

	private void alpha ()
	{
		if (addAtoms.ribbonShowing) {
			addAtoms.ribbonShowing = addAtoms.resetProtein ("ribbons");
		}
		if (addAtoms.alphaShowing)
			addAtoms.alphaShowing = addAtoms.resetProtein ("alpha");
		else
			addAtoms.alphaShowing = addAtoms.showMode ("alpha");
	}

	private void water ()
	{		
		if (addAtoms.waterShowing)
			addAtoms.waterShowing = addAtoms.resetProtein ("water");
		else
			addAtoms.waterShowing = addAtoms.showMode ("water");
	}

	private void ions ()
	{		
		if (addAtoms.ionsShowing)
			addAtoms.ionsShowing = addAtoms.resetProtein ("ion");
		else
			addAtoms.ionsShowing = addAtoms.showMode ("ion");
	}

	private void hbonds ()
	{		
		if (addAtoms.HBondShowing)
			addAtoms.HBondShowing = addAtoms.resetProtein ("hbond");
		else
			addAtoms.HBondShowing = addAtoms.showMode ("hbond");
	}

	private void resetTarget(){
		addAtoms.resetTarget ();
	}


	private Rect createRect ()
	{		

		Rect temp = new Rect (Screen.width / 2, buttonSize * 1.5f * buttonCount, buttonSize * 6, buttonSize);
		GUI.DrawTexture (temp, background);
		buttons.Add (temp);
		buttonCount++;
		return temp;
	}


	public override void mouseClick (Vector2 cursorPosition)
	{
		for (int i=0; i<buttonCount; i++) {
			if (buttons [i].Contains (cursorPosition)) {
				source.PlayOneShot (menuClicked, 0.5f);
				buttonFunctions [i] ();
			}
		}
	}
	
}
