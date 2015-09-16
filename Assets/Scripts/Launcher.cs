using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Diagnostics;


public class Launcher : MonoBehaviour
{

	public Text dataSource; //from browser
	public Text dataSource2; //from free text
	public Text colorSource;
	public Text error_msg;
	public Toggle proteinText;
	public Toggle ligandText;
	public Toggle hBond;
	public Toggle ribbons;
	public Toggle alpha;
	public Toggle ballAndStick;
	public Toggle lines;
	public Toggle stick;
	public Toggle hetatmBS;
	public Toggle hetatmLines;
	public Toggle hetatmStick;
	public Toggle sphere;
	public Toggle showWater;
	public Toggle showIon;

	public Toggle chain;
	public Toggle spectrum;

	public Toggle hideHydrogens;
	public Toggle polarHydrogens;

	public Toggle MMFF94;
	public Toggle UFF;
	public Toggle Ghemical;

	public Toggle whiteBG;
	public Toggle blackBG;
	public Toggle greyBG;

	public Image color1;
	public Image color2;
	public Image color3;
	public Image color4;


	public void LaunchMolyRift ()
	{
		string molData;
		if (File.Exists (dataSource.text))
			molData = dataSource.text;
		else
			molData = dataSource2.text;

		if (molData.Length == 4) { //internet

			//running external script
			Process process = new Process ();
			process.StartInfo.FileName = "getPDB.exe";
			process.StartInfo.Arguments = molData;           
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = Application.dataPath + "/..";
			process.StartInfo.UseShellExecute = false;
			process.Start ();

			process.WaitForExit ();
			PlayerPrefs.SetString ("File", "pdb.pdb");
			PlayerPrefs.SetString ("ColorFile", colorSource.text);

			Launch ();


		} else if (File.Exists (molData)) { //local file
			string extension = molData.Split ('.')[1];
			if (extension == "pdb" || extension == "sdf" || extension == "mol2") {
				PlayerPrefs.SetString ("File", molData);
				Launch ();
			} else {
				error_msg.text = "Unsupported extension!";
			}


		} else {
			error_msg.text = "Couldn't open file!";
		}
	}



	private void Launch ()
	{
		//check toggle buttons
		PlayerPrefs.SetString ("proteinText", proteinText.isOn.ToString ());
		PlayerPrefs.SetString ("ligandText", ligandText.isOn.ToString ());
		PlayerPrefs.SetString ("hBond", hBond.isOn.ToString ());
		PlayerPrefs.SetString ("ribbons", ribbons.isOn.ToString ());
		PlayerPrefs.SetString ("alpha", alpha.isOn.ToString ());
		PlayerPrefs.SetString ("ballAndStick", ballAndStick.isOn.ToString ());
		PlayerPrefs.SetString ("lines", lines.isOn.ToString ());
		PlayerPrefs.SetString ("stick", stick.isOn.ToString ());
		PlayerPrefs.SetString ("sphere", sphere.isOn.ToString ());
		PlayerPrefs.SetString ("hetatmBS", hetatmBS.isOn.ToString ());
		PlayerPrefs.SetString ("hetatmL", hetatmLines.isOn.ToString ());
		PlayerPrefs.SetString ("hetatmStick", hetatmStick.isOn.ToString ());
		PlayerPrefs.SetString ("showWater", showWater.isOn.ToString ());
		PlayerPrefs.SetString ("showIon", showIon.isOn.ToString ());
		PlayerPrefs.SetString ("chain", chain.isOn.ToString ());
		PlayerPrefs.SetString ("spectrum", spectrum.isOn.ToString ());
		PlayerPrefs.SetString ("hideHydrogens", hideHydrogens.isOn.ToString ());
		PlayerPrefs.SetString ("polarHydrogens", polarHydrogens.isOn.ToString ());

		string ff = "";
		if (MMFF94.isOn)
			ff = "MMFF94";
		else if(UFF.isOn)
			ff = "UFF";
		else if(Ghemical.isOn)
			ff = "Ghemical";
		PlayerPrefs.SetString ("forceField", ff);
		
		string skybox = "";

		if(whiteBG.isOn)
			skybox = "white";
		else if(blackBG.isOn)
			skybox = "black";
		else if(greyBG.isOn)
			skybox = "grey";

		PlayerPrefs.SetString ("skybox", skybox);

		//Colors
		PlayerPrefs.SetString ("color1", colorToString (color1.color));
		PlayerPrefs.SetString ("color2", colorToString (color2.color));
		PlayerPrefs.SetString ("color3", colorToString (color3.color));
		PlayerPrefs.SetString ("color4", colorToString (color4.color));

		//launch MoleRift
		Application.LoadLevel ("MolecularRift");
	}
	
	private string colorToString (Color color)
	{
		return color.r + "," + color.g + "," + color.b + "," + color.a;
	}
}
