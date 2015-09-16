using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ColorPicker : MonoBehaviour
{
	
	public Image Image1;
	public Image Image2;
	public Image Image3;
	public Image Image4;

	public Toggle Color1;
	public Toggle Color2;
	public Toggle Color3;
	public Toggle Color4;

	public Texture2D colorPicker;  
	
	private Rect colorPanelRect = new Rect (Screen.width / 1.72f, Screen.height / 1.55f, 120, 120);
	
	void OnGUI ()
	{

		GUI.DrawTexture (colorPanelRect, colorPicker);
		if (GUI.RepeatButton (colorPanelRect, "")) {
			Vector2 pickpos = Event.current.mousePosition;
			float x = pickpos.x - colorPanelRect.x;
			
			float y = pickpos.y - colorPanelRect.y;
			
			int xPixel = (int)(x * (colorPicker.width / (colorPanelRect.width + 0.0f)));
			
			int yPixel = (int)((colorPanelRect.height - y) * (colorPicker.height / (colorPanelRect.height + 0.0f)));
			
			Color col = colorPicker.GetPixel (xPixel, yPixel);


			if (Color1.isOn)
				Image1.color = col;
			else if (Color2.isOn)
				Image2.color = col;
			else if (Color3.isOn)
				Image3.color = col;
			else
				Image4.color = col;

		}


	}
}