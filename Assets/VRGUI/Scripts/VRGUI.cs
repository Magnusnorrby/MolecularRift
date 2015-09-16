using UnityEngine;
using System.Collections;
using System;

public abstract class VRGUI : MonoBehaviour
{
	public Vector3 guiPosition = new Vector3 (0f, 0f, 1f);
	public float   guiSize = 1f;

	public bool    acceptKeyboard = true;
	public int     cursorSize = 32;
	public Texture customCursor = null;
	
	private GameObject    guiRenderPlane = null;
	private RenderTexture guiRenderTexture = null;
	private Vector2       cursorPosition = new Vector2 (Screen.width / 2, Screen.height / 2);
	private Texture       cursor = null;
	
	private bool isInitialized = false;

	private float time;
	
	private void Initialize ()
	{
		// create the render plane
		guiRenderPlane = Instantiate (Resources.Load ("VRGUICurvedSurface")) as GameObject;

		
		// position the render plane
		guiRenderPlane.transform.parent = this.transform;
		guiRenderPlane.transform.localPosition = guiPosition;
		guiRenderPlane.transform.localRotation = Quaternion.Euler (0f, 180f, 0f);
		guiRenderPlane.transform.localScale = new Vector3 (guiSize, guiSize, guiSize);
		
		// create the render texture
		guiRenderTexture = new RenderTexture (Screen.width, Screen.height, 24);
		
		// assign the render texture to the render plane
		guiRenderPlane.GetComponent<Renderer> ().material.mainTexture = guiRenderTexture;
		
		// create the cursor
		if (customCursor != null) {
			cursor = customCursor;
		} else {
			cursor = Resources.Load ("SimpleCursor") as Texture;
		}

		time = 0.0f;

		isInitialized = true;
	}
	
	protected void OnEnable ()
	{
		if (guiRenderPlane != null) {
			guiRenderPlane.SetActive (true);
		}
	}
	
	protected void OnDisable ()
	{
		if (guiRenderPlane != null) {
			guiRenderPlane.SetActive (false);
		}
	}
	
	protected void OnGUI ()
	{
		if (!isInitialized) {
			Initialize ();
		}
		
		// handle key events
		if (Event.current.isKey) {
			// return if not accepting key events
			if (!acceptKeyboard) {
				return;
			}
		}
		
		// save current render texture
		RenderTexture tempRenderTexture = RenderTexture.active; 
		
		// set the render texture to render the GUI onto
		if (Event.current.type == EventType.Repaint) {			
			RenderTexture.active = guiRenderTexture;
			GL.Clear (false, true, new Color (0.0f, 0.0f, 0.0f, 0.0f));
		}
		
		// draw the VRGUI
		OnVRGUI ();
		
		if (Event.current.type == EventType.Repaint) {	
			// draw the cursor
			GUI.DrawTexture (new Rect (cursorPosition.x, cursorPosition.y, cursorSize, cursorSize), 
				cursor, ScaleMode.StretchToFill);

			
			// restore the previous render texture
			RenderTexture.active = tempRenderTexture;

		}
	}
	
	public abstract void OnVRGUI ();

	public void moveCursor (Vector2 newPosition)
	{
		cursorPosition.x = Math.Min (cursorPosition.x - newPosition.x, Screen.width);
		cursorPosition.x = Math.Max (cursorPosition.x - newPosition.x, 0f);
		cursorPosition.y = Math.Min (cursorPosition.y - newPosition.y, Screen.height);
		cursorPosition.y = Math.Max (cursorPosition.y - newPosition.y, 0f);

	}

	public void Click ()
	{
		if (Time.time > time + 1.0f) { //makes sure we dont send a click each frame
			mouseClick (cursorPosition);
			time = Time.time;
		}
	}

	public abstract void mouseClick (Vector2 cursorPosition);

}