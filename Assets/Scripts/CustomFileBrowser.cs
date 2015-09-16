using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CustomFileBrowser : MonoBehaviour
{
	//skins and textures
	public GUISkin skin;
	public Texture2D file, folder, back, drive;
	public InputField file_name;
	public Text file_path;
		
	//initialize file browser
	FileBrowser fb = new FileBrowser ();
	string output = "";
	// Use this for initialization
	void Start ()
	{
		//setup file browser style
		fb.guiSkin = skin; //set the starting skin
		//set the various textures
		fb.fileTexture = file; 
		fb.directoryTexture = folder;
		fb.backTexture = back;
		fb.driveTexture = drive;
		//show the search bar
		fb.showSearch = true;
	}
		
	void OnGUI ()
	{


		file_name.text = output;
		file_path.text = output;
		//draw and display output
		if (fb.draw ()) { //true is returned when a file has been selected
			//the output file is a member if the FileInfo class, if cancel was selected the value is null
			output = (fb.outputFile == null) ? "" : fb.outputFile.FullName;
		}
	}

}