using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using OpenBabel;

public class AddAtoms : MonoBehaviour
{
	//loads customized 3d objects such as atoms and bonds
	public GameObject default_atom;
	public GameObject binding;
	public GameObject doubleBinding;
	public GameObject trippleBinding;
	public GameObject aromatic;
	public GameObject sticks;
	public GameObject hBond;
	public GameObject pharmacophore;
	public GameObject EXC;
	public GameObject vector;

	// the available background colors except space which is default
	public Material skyboxW;
	public Material skyboxB;
	public Material skyboxG;

	// the textbox used for displaying atom distances
	public GameObject distBox;
	private TextMesh distText;

	// bools representing menu options, public options are accessed by the in-game menu
	public bool ribbonShowing = false;
	public bool ballAndStickShowing = false;
	public bool proteinShowing = false;
	public bool alphaShowing = false;
	public bool waterShowing = false;
	public bool ionsShowing = false;
	public bool HBondShowing = false;
	public bool sphere = false;
	public bool hetatmStick = false;
	public bool stick = false;

	private bool proteinText;
	private bool ligandText;

	// list containing custom colors
	public List<Color> colorList = new List<Color> ();

	// all atom objects
	private GameObject[] atoms;

	// list for tracking ribbon radius (alpha helix vs regular)
	private float[,] radius;

	// open babel mol object
	private OBMol mol;



	// used for rotation
	private GameObject root;
	private Quaternion rotatedDirection;

	// for centering
	private GameObject center;
	private Vector3 centerPos;
	private int centerCount;

	// for drawing ribbons
	protected TubeRenderer tubeRenderer;
	public GameObject RibbonHolder;

	// for drawing molecular surfaces
	protected SurfaceSphere surfaceSphere;
	public GameObject SurfaceHolder;

	// atom display options
	private float scaleFactor = 0.8f; //scale factor for atom radius ("real size" = scaleFactor 1.7)
	private float defaultScale = 0.7f;
	private Color defaultColor = Color.grey;

	// pharmacophores
	private bool renderPharmacophore = false;
	private bool renderEXC = false;

	// select atom
	private GameObject lockedTarget;
	private Color oldLockedTargetColor;
	private GameObject target;
	private Color oldTargetColor;

	// input file
	private string file;
	private string extension;

	// text color (white for all backgrounds except white)
	private Color textColor = Color.white;
	
	// atom radii
	Dictionary<string, float> atomScales = new Dictionary<string, float> ()
	{
		{"C", 1.0f},
		{"N", 0.729412f},
		{"O", 0.715294f},
		{"FE", 0.912941f},
		{"H", 0.564706f},
		{"CL", 1.029411f},
		{"LI", 1.070588f},
		{"NA", 1.335294f},
		{"K", 1.617647f},
		{"F", 0.864705f},
		{"P", 1.058823f},
		{"S", 1.058823f},
		{"BR", 1.088235f},
		{"SE", 1.117647f},
		{"ZN", 0.817647f},
		{"CU", 0.823529f},
		{"NI", 0.958823f},
		{"HE", 0.823529f},
		{"NE", 0.905882f},
		{"MG", 1.017647f},
		{"SI", 1.235294f},
		{"AR", 1.105882f},
		{"GA", 1.1f},
		{"AS", 1.088235f},
		{"KR", 1.188235f},
		{"PD", 0.958823f},
		{"AG", 1.011764f},
		{"CD", 0.929411f},
		{"IN", 1.135294f},
		{"SN", 1.276470f},
		{"TE", 1.211764f},
		{"I", 1.164705f},
		{"XE", 1.270588f},
		{"PT", 1.029411f},
		{"AU", 0.976470f},
		{"HG", 0.911764f},
		{"TL", 1.152941f},
		{"PB", 1.188235f},
		{"U", 1.0941176f}
	};

	// atom color scheme
	Dictionary<string, Color> atomColor = new Dictionary<string, Color> ();

	
	public void Init ()
	{
		//reads the settings chosen in the launch menu
		file = PlayerPrefs.GetString ("File");
		proteinText = PlayerPrefs.GetString ("proteinText") == "True";
		ligandText = PlayerPrefs.GetString ("ligandText") == "True";
		HBondShowing = PlayerPrefs.GetString ("hBond") == "True";
		ballAndStickShowing = PlayerPrefs.GetString ("ballAndStick") == "True"; 
		stick = PlayerPrefs.GetString ("stick") == "True";
		proteinShowing = PlayerPrefs.GetString ("lines") == "True" || ballAndStickShowing || stick; 
		ribbonShowing = PlayerPrefs.GetString ("ribbons") == "True";
		alphaShowing = PlayerPrefs.GetString ("alpha") == "True";
		sphere = PlayerPrefs.GetString ("sphere") == "True";
		hetatmStick = PlayerPrefs.GetString ("hetatmStick") == "True";


		string skybox = PlayerPrefs.GetString ("skybox");
		if (skybox == "white") {
			RenderSettings.skybox = skyboxW;
			textColor = Color.black; //if the background is white the label text is set to black
		}else if (skybox == "black")
			RenderSettings.skybox = skyboxB;
		else if (skybox == "grey")
			RenderSettings.skybox = skyboxG;

		// reads predefined colors
		string colorFile = PlayerPrefs.GetString ("ColorFile");
		if (File.Exists (colorFile))
			readColors (File.ReadAllLines (colorFile)); //load colors

		rotatedDirection = Quaternion.identity;
		root = (GameObject)Instantiate (new GameObject (), Vector3.zero, Quaternion.identity); //a parent object that lets us move and rotate all objects as one

		resetColors ();

		centerPos = Vector3.zero;
		centerCount = 0;

		readMol (file);

		distText = distBox.GetComponent<TextMesh> ();
	}

	public void readMol(string file){
		bool hideHydrogens = PlayerPrefs.GetString ("hideHydrogens") == "True";
		bool polarHydrogens = PlayerPrefs.GetString ("polarHydrogens") == "True";	
		string ff = PlayerPrefs.GetString ("forceField");

		OBConversion obconv = new OBConversion();

		extension = file.Split ('.')[1];
		if(extension=="pdb")
			obconv.SetInFormat("PDB");
		else if(extension=="sdf")
			obconv.SetInFormat("SDF");
		else if(extension=="mol2")
			obconv.SetInFormat("MOL2");
		
		mol = new OBMol();
		obconv.ReadFile(mol,file);

		if (hideHydrogens)
			mol.DeleteHydrogens ();
		else
			mol.AddHydrogens ();
		
		if (polarHydrogens)
			mol.DeleteNonPolarHydrogens ();
		
		if (ff != "") { //a force field is selected
			
			// Ghemical, MMFF94, UFF
			OBForceField forceField = OBForceField.FindForceField (ff);
			
			forceField.Setup (mol);
			forceField.ConjugateGradients (1000);
			
		}
	}

	public void readColors (string[] colors)
	{
		atomColor = new Dictionary<string, Color> ();
		foreach (string line in colors) {
			string[] data = line.Split (',');
			try {
				atomColor.Add (data [0].ToUpper (), new Color (float.Parse (data [1]), float.Parse (data [2]), float.Parse (data [3])));
			} catch (System.Exception e) {
				Debug.Log (e);
			}
		}
	}
	
	private void resetColors ()
	{
		colorList.Clear ();
		colorList.Add (parseColor (PlayerPrefs.GetString ("color1")));
		colorList.Add (parseColor (PlayerPrefs.GetString ("color2")));
		colorList.Add (parseColor (PlayerPrefs.GetString ("color3")));
		colorList.Add (parseColor (PlayerPrefs.GetString ("color4")));
	}

	public void parsePDB ()
	{ 
		// for determining hydrogen bonds
		List<Vector3> hBondAcceptors = new List<Vector3> ();
		List<Vector3> hBondDonors = new List<Vector3> ();

	
	parseMol: // iteration start if multiple input files are used 

		BezierSpline[] ribbonSplineList = new BezierSpline[30]; //for drawing protein lines (30 is max number of chains)


		int numAtoms = (int)mol.NumAtoms()+1;


		atoms = new GameObject[numAtoms];


		int maxHelix = 500;

		radius = new float[maxHelix, numAtoms];

		int ribbonChainIndex = 0;


		OBElementTable table = new OBElementTable ();

		int currentChain = -1;



		OBAtom[] alphaCarbons = new OBAtom[numAtoms/4];
		int alphaCarbonCount = 0;

		List<Vector3> atomPositions = new List<Vector3> ();
		List<string> atomElements = new List<string> ();

		for(int i=1; i <numAtoms;i++) {
			OBAtom atom = mol.GetAtom(i);
			string tag ;
			OBResidue res = atom.GetResidue();
			Vector3 position = new Vector3 ((float)atom.GetX(), (float) atom.GetY(), (float) atom.GetZ());	
			string elem = table.GetSymbol((int)atom.GetAtomicNum()).ToUpper();


			//checks for pharmacophore labels
			if(res.GetName()=="ACC" || res.GetName()== "FOB" || res.GetName()== "POS" || res.GetName()== "NEG" || res.GetName()== "RNG" || res.GetName()== "DON"){
				if(atom.IsHydrogen()) //added hydrogens gets the same resName and has to be ignored
					continue;
				renderPharmacophore = true;
			}else{
				renderPharmacophore = false;
			}

			// pharmacophores
			if(res.GetName()=="EXC"){
				if(atom.IsHydrogen()) //added hydrogens gets the same resName and has to be ignored
					continue;
				renderEXC = true;
				renderPharmacophore = true;
			}else 
				renderEXC = false;
			
			//creates the atom object
			atoms[i]=createAtomType(elem,position);

			if(res.GetResidueProperty(9)){ //water
				tag = "water";
			}else if(res.GetAtomProperty(atom,4) || (atom.IsHydrogen() && !res.GetResidueProperty(5))){ //ligand
				if(res.GetResidueProperty(5)){ //ion (should be 3 but a bug in openbabel labels ions as protein, thats why the check has to be done inside the ligand check)
					tag = "ion";
				}else{
					TextMesh lText = atoms [i].transform.Find ("Text").GetComponent<TextMesh> ();
					lText.color = textColor;
					if(renderPharmacophore){
						tag = "hetatms"; //make sure pharmacophores always show
						lText.text = res.GetName() + ":" + res.GetIdx();
					}else{
						tag = "hetatmbs";
						if (ligandText) { 
							lText.text = elem + ":" + i.ToString ();
						}
					}
					if (sphere)
						atoms [i].transform.localScale *= 4;

					if(atom.IsHbondAcceptor()){
						foreach(Vector3 candidatePos in hBondDonors){
							checkHBond(candidatePos,position);
						}
					}

					if(atom.IsHbondDonor()){
						foreach(Vector3 candidatePos in hBondAcceptors){
							checkHBond(candidatePos,position);
						}
					}

				}
			}else{ //protein  
				tag = "balls";

				atomPositions.Add(position);

				atomElements.Add(elem);

				if(atom.IsHbondAcceptor())
					hBondAcceptors.Add(position);
				else 
					hBondDonors.Add (position);

				if(res.GetAtomProperty(atom,0)){ //alpha carbon
					alphaCarbons[alphaCarbonCount]=atom;
					if(alphaCarbonCount>6){ //check if the ribbon is alpha helix using torsion angles
						double torsion = mol.GetTorsion(atom,alphaCarbons[alphaCarbonCount-1],alphaCarbons[alphaCarbonCount-2],alphaCarbons[alphaCarbonCount-3]);
						double prevTorsion = mol.GetTorsion(alphaCarbons[alphaCarbonCount-4],alphaCarbons[alphaCarbonCount-5],alphaCarbons[alphaCarbonCount-6],alphaCarbons[alphaCarbonCount-7]);
						double torsionSum = torsion+prevTorsion;
						if(torsionSum>99 && torsionSum<111){ 
							for(int j=ribbonChainIndex-7; j<=ribbonChainIndex; j++){
								radius [currentChain, j] = 1.5f;	//alpha helix
							}
						}else{
							radius [currentChain, ribbonChainIndex] = 0.5f;	
						}

					}
					alphaCarbonCount++;


					if (proteinText) { //only displays text on alpha carbons
						TextMesh pText = atoms [i].transform.Find ("Text").GetComponent<TextMesh> ();
						pText.color = textColor;
						pText.text = res.GetName() + " -" + res.GetNumString();
					}

						int tempChain = (int)res.GetChainNum();
					if(tempChain!=currentChain){
						currentChain=tempChain;
						ribbonChainIndex=0;
					}


					//add points for ribbon rendering
					addBezierPoint (ribbonSplineList, currentChain, ribbonChainIndex, position);

						

					ribbonChainIndex++;
				}
			}

			atoms[i].transform.tag=tag;	
			centerPos+=position;
			centerCount++;

		}

		//createSurface (atomPositions,atomElements); 
		Debug.Log (hBondAcceptors.Count + " " + hBondDonors.Count);
		//evaluate bonds
		for (int i=0; i <mol.NumBonds(); i++) {
			OBBond bond = mol.GetBond (i);
			OBAtom atom = mol.GetAtom((int)bond.GetBeginAtomIdx());
			OBResidue res = atom.GetResidue();
			bool ligand = res.GetAtomProperty(atom,4) || (atom.IsHydrogen() && !res.GetResidueProperty(5));
			if(ligand && sphere) //no ligand bonds if display mode is CPK sphere
				continue;
			try {
				connect(atoms[bond.GetBeginAtomIdx()],atoms[bond.GetEndAtomIdx()],bond);
			} catch (System.Exception e) {
				Debug.Log (e);
			}

		}

		//handle pharmacophore vectors 
		if (mol.HasData ("VECTOR")) {
			string[] vectors = mol.GetData ("VECTOR").GetValue ().Split(new string[] { "\n" }, StringSplitOptions.None);
			foreach (string vector in vectors){
				string[] idx = vector.Split ((char[])null, StringSplitOptions.RemoveEmptyEntries);
				try {
					Vector3 pos = new Vector3(float.Parse(idx[1]),float.Parse(idx[2]),float.Parse(idx[3]));
					int id = int.Parse(idx[0]);
					drawVector(atoms[id],pos,mol.GetAtom(id).GetResidue().GetName());
				} catch (System.Exception e) {
					Debug.Log (e);
				}

			}
		}


		//render protein ribbons
		drawRibbons (ribbonSplineList, "ribbons"); //must be before alpha due to indexing
		drawRibbons (ribbonSplineList, "alpha");

		// check for separate ligand file
		if (file.Contains("protein")) {
			file = file.Replace("protein","ligand"); 
			string file1 = file.Replace("."+extension,".sdf"); // .+extension incase the format would also appear in the file name
			string file2 = file.Replace("."+extension,".mol2");
			if(File.Exists(file1)){
				readMol(file);
				goto parseMol;
			}else if(File.Exists (file2)){
				readMol(file);
				goto parseMol;
			}
		}

		center = createObject (new GameObject (), centerPos / centerCount); //the center of the pdb
		show ();
	}

	//not yet completed molecular surface algorithm. Smooth interpolation between colliding spheres is needed
	void createSurface(List<Vector3> positions, List<string> elements){
		int points = 30;
		float theta = 2.0f * Mathf.PI / points;
		float phi = 1.0f * Mathf.PI / points;

		for(int index=0; index<positions.Count; index++) {
			GameObject sh = createObject (SurfaceHolder, Vector3.zero);
			SurfaceSphere surfaceSphere = sh.GetComponent<SurfaceSphere>();
			surfaceSphere.transform.parent = root.transform;
			surfaceSphere.transform.tag = "surface";
			Vector3 centerPosition = positions[index];
			string element = elements[index];
			float radius = getAtomScale(element).x;
			Color color = Color.cyan;//getAtomColor(element);
			color.a = 0.7f;

			Vector3[] vertices = new Vector3[(points+1)*points];


			Collider[] overLapColliders = Physics.OverlapSphere(centerPosition, radius); //get all potential surface collisions
			for (int i = 0; i < points; i++) {

				for (int j = 0; j <= points; j++) {

					
					Vector3 position = new Vector3 (Mathf.Cos (theta * j)*Mathf.Sin (phi * i), Mathf.Sin (theta * j)*Mathf.Sin (phi * i), Mathf.Cos (phi*i))*radius+centerPosition;

					foreach(Collider col in overLapColliders){
						if(col is SphereCollider && col.transform.position!=centerPosition){ //skips it's own collider
							SphereCollider sphereCol = (SphereCollider) col;
							float dist = Vector3.Distance(sphereCol.transform.position,position);
							if(dist-sphereCol.radius<0){
								Vector3 direction = centerPosition-position;
								direction.Normalize();
								float pi = 3.14159265359f;
								float angle = Vector3.Angle(position-centerPosition,sphereCol.transform.position-position)*pi/180f;
								if(angle>0.785398163f)
									angle-=pi/2f;
								position += ((sphereCol.radius-dist)/Mathf.Cos(angle))*direction*1.5f; //place the position on the collision point
	
								break;
							}
						}
					}

					vertices[j + i * (points + 1)] = position;


				}
			}
			surfaceSphere.Create(vertices,points,points,color);
			surfaceSphere.GetComponent<MeshRenderer> ().enabled = false;

		}

	}
	
	GameObject createAtomType (string elem, Vector3 position)
	{
		GameObject atom;
		Color color = getAtomColor (elem);
		if (renderPharmacophore) { //creates a pharmacophore sphere
			if (renderEXC) {
				atom = createObject (EXC, position);
				atom.transform.localScale = getAtomScale (elem);
				atom.transform.localScale *= scaleFactor / 2f; 
	
			} else {
				atom = createObject (pharmacophore, position);
				atom.transform.localScale = getAtomScale (elem);
				atom.transform.localScale *= scaleFactor; 

			}

			color.a = 0.5f;
			atom.GetComponent<Renderer> ().material.SetColor ("_TintColor", color);
			
			if (!sphere)
				atom.transform.localScale *= 4;// increase the radius unless the it has already been icnreased by display options
		} else { //creates an atom object
			atom = createObject (default_atom, position);
			atom.GetComponent<Renderer> ().material.color = color;
			Vector3 scale = getAtomScale (elem);
			atom.transform.localScale = scale;
			atom.transform.localScale *= scaleFactor;
			atom.transform.GetComponent<SphereCollider> ().radius = scale.x;
		}

		return atom;
	}


	Vector3 getAtomScale (string elem)
	{
		float f = defaultScale;
		if (atomScales.ContainsKey (elem)) {
			f = atomScales [elem];
		}
		return new Vector3 (f, f, f);
	}

	Color getAtomColor (string elem)
	{
		Color c = defaultColor;
		if (atomColor.ContainsKey (elem)) {
			c = atomColor [elem];
		}
		return c;
	}
	
	Color parseColor (string color)
	{
		string[] rgba = new string[4];
		rgba = color.Split (',');
		return new Color (float.Parse (rgba [0]), float.Parse (rgba [1]), float.Parse (rgba [2]), float.Parse (rgba [3]));
	}

	//handles the interpolation points used to create the protein ribbons
	void addBezierPoint (BezierSpline[] splineList, int chains, int chainIndex, Vector3 position)
	{
		if (chainIndex == 0) {
			GameObject go = new GameObject ();
			splineList [chains] = go.AddComponent<BezierSpline> ();
			splineList [chains].SetControlPoint (chainIndex, position);
			splineList [chains].SetControlPoint (chainIndex + 1, position);
			splineList [chains].SetControlPoint (chainIndex + 2, position);
			splineList [chains].SetControlPoint (chainIndex + 3, position);
		} else {
			if (chainIndex % 3 == 0) {
				splineList [chains].AddCurve ();
				splineList [chains].SetControlPoint (chainIndex, position);
				splineList [chains].SetControlPoint (chainIndex + 1, position);
				splineList [chains].SetControlPoint (chainIndex + 2, position);
			} else if (chainIndex % 3 == 1) {
				splineList [chains].SetControlPoint (chainIndex, position);
			} else {
				splineList [chains].SetControlPoint (chainIndex, position);
				splineList [chains].SetControlPoint (chainIndex + 1, position);
			}
		}
	}
	
	void drawRibbons (BezierSpline[] splineList, string tag)
	{
		resetColors ();
		int multiplier = 50; //how many points we extract between each control point
		Color color1 = new Color ();
		Color color2 = new Color ();
		int sum = 0;
		for (int i=0; i < splineList.Length; i++) {
			if (splineList [i] != null) 
				sum += splineList [i].ControlPointCount * multiplier;
		}

		int nbrOfPrevious = 0;
		for (int i=0; i < splineList.Length; i++) {
			if (splineList [i] != null) {
				Vector3[] pos = new Vector3[splineList [i].ControlPointCount * multiplier];

				int frequency = pos.Length;

				//check coloring mode
				if (PlayerPrefs.GetString ("spectrum") == "True") {
					color1 = colorList [0];
					color2 = colorList [1];
				} else if (PlayerPrefs.GetString ("chain") == "True") {
					color1 = getColor ();
					color2 = color1;

				}

				//extracts points from the interpolation function
				int p = 0;
				float stepSize = 1f / (frequency - 1);
				for (int f = 0; f < frequency; f++) {
					pos [p] = splineList [i].GetPoint (f * stepSize);	
					p++;
					
				}

				//split the array in two to avoid the max number of vertices in a mesh
				Vector3[] firstarray, secondarray;
				int splitIndex = 5000;
				bool run = true;
				int localPrev = 0;
				while (run) {
					if (pos.Length > splitIndex) {
						firstarray = new Vector3[splitIndex];
						secondarray = new Vector3[pos.Length - splitIndex];
						Array.Copy (pos, 0, firstarray, 0, splitIndex);
						Array.Copy (pos, splitIndex, secondarray, 0, secondarray.Length);
						nbrOfPrevious += createMesh (firstarray, i, multiplier, color1, color2, sum, nbrOfPrevious, tag, localPrev);
						localPrev+=splitIndex;
						pos = secondarray;
					} else {
						nbrOfPrevious += createMesh (pos, i, multiplier, color1, color2, sum, nbrOfPrevious, tag, localPrev);
						run = false;
					} 

				}
			}
		}

	}

	int createMesh (Vector3[] positions, int i, int multiplier, Color color1, Color color2, int sum, int nbrOfPrevious, string tag, int localPrev)
	{
		GameObject rh = createObject (RibbonHolder, Vector3.zero);
		tubeRenderer = rh.GetComponent<TubeRenderer> ();
		
		if (tag == "ribbons") {
			tubeRenderer.SetPoints (positions, radius, i, multiplier, color1, color2, sum, nbrOfPrevious, localPrev);
		} else { 
			tubeRenderer.SetPoints (positions, 0.5f, color1, color2, sum, nbrOfPrevious);
		}
		
		
		
		tubeRenderer.transform.parent = root.transform;
		rh.tag = tag;
		
		rh.GetComponent<MeshRenderer> ().enabled = false;
		return positions.Length;
	}

	//creates pharmacophore vectors
	void drawVector(GameObject startAtom, Vector3 endAtom, String label){
		GameObject pharmVector = createObject (vector, Vector3.zero);
		//make the vector as long as the distance
		pharmVector.transform.localScale = new Vector3 (pharmVector.transform.localScale.x, pharmVector.transform.localScale.y, Vector3.Distance (startAtom.transform.position, endAtom));
		pharmVector.transform.position = (startAtom.transform.position + endAtom) / 2.0f; //place the vector between the atoms
		pharmVector.transform.LookAt (endAtom); //rotate the vector to face the target
		pharmVector.gameObject.tag = "hetatms";

		TextMesh tm = pharmVector.transform.Find ("Text").GetComponent<TextMesh> ();
		tm.text = label;
		tm.color = textColor;

		Renderer[] renderers = pharmVector.GetComponentsInChildren<Renderer> (); //color the binding according to atom type		
		Material mat = startAtom.GetComponent<Renderer> ().material;
		for (int i = 0; i < renderers.Length; i++) { 
				renderers [i].sharedMaterial.color = mat.color;
		}
	}

	//creates atom bonds
	void connect (GameObject atom1, GameObject atom2, OBBond bondInfo)
	{
		string tag;
		if (atom1.transform.tag == "hetatmbs")
			tag = "hetatms";
		else if (atom1.transform.tag == "water")
			return;
		else
			tag = "bonds";


		Material atom1Mat = atom1.GetComponent<Renderer> ().material;
		Material atom2Mat = atom2.GetComponent<Renderer> ().material;
		if (atom1Mat.HasProperty ("_TintColor") || atom2Mat.HasProperty ("_TintColor"))	//skips bonds to pharmacophores
			return;
		
		GameObject bind;
		if (bondInfo.IsSingle() && ((hetatmStick && tag == "hetatms") || (stick && tag == "bonds")))
			bind = createObject (sticks, Vector3.zero);
		else { //lines
			if (bondInfo.IsDouble()) 
				bind = createObject (doubleBinding, Vector3.zero);
			else if (bondInfo.IsTriple())
				bind = createObject (trippleBinding, Vector3.zero);
			else if (bondInfo.IsAromatic()) 
				bind = createObject (aromatic, Vector3.zero);
			else
				bind = createObject (binding, Vector3.zero);//single bond
		}
		//make the binding as long as the distance
		bind.transform.localScale = new Vector3 (bind.transform.localScale.x, bind.transform.localScale.y, Vector3.Distance (atom1.transform.position, atom2.transform.position));
		bind.transform.position = (atom1.transform.position + atom2.transform.position) / 2.0f; //place the binding between the atoms
		bind.transform.LookAt (atom1.transform.position); //rotate the binding to face the atoms
		bind.gameObject.tag = tag;
		
		
		Renderer[] renderers = bind.GetComponentsInChildren<Renderer> (); //color the binding according to atom type	
		for (int i = 0; i < renderers.Length; i++) { // different amount of  bonds depending on order
			if (i % 2 == 0)
				renderers [i].sharedMaterial = atom1Mat;
			else
				renderers [i].sharedMaterial = atom2Mat;
			
		}
		
		
		
		
	}


	Color getColor ()
	{
		Color c = colorList [0];
		colorList.Remove (c);
		colorList.Add (c);
		return c;
	}


	// toggles in-game objects visibility according to user input
	public void show ()
	{
		//showMode ("surface");

		if (!proteinShowing) 
			resetProtein ("bonds"); //show as standard

	
		if (ribbonShowing) 
			showMode ("ribbons");

		if (alphaShowing) 
			showMode ("alpha");

		if (!HBondShowing) //since they have a particle system and not a renderer they are implicitly turned on
			resetProtein ("hbond");

		if (ballAndStickShowing)
			showMode ("balls");

		if (PlayerPrefs.GetString ("showWater") == "True")
			waterShowing = showMode ("water");

		if (PlayerPrefs.GetString ("showIon") == "True")
			ionsShowing = showMode ("ion");

		showMode ("hetatms");
		if (PlayerPrefs.GetString ("hetatmBS") == "True") {
			showMode ("hetatmbs");
		} else if (sphere) {
			showMode ("hetatmbs");
		} else if (PlayerPrefs.GetString ("hetatmL") != "True" && !hetatmStick) { //ligand is hidden
			resetProtein ("hetatms"); //show as standard
		} 



	}
		
	public bool resetProtein (string tag)
	{
		toggleDisplayMode (tag, false);
		return false;
	}

	public bool showMode (string tag)
	{
		toggleDisplayMode (tag, true);
		return true;
	}

	public void toggleDisplayMode (string tag, bool toggle)
	{
		GameObject[] gameObjects = GameObject.FindGameObjectsWithTag (tag);
		
		if (tag == "hbond") { //does not have a renderer and needs to be handled seperatly
			HBondShowing = toggle;
			for (int i = 0; i < gameObjects.Length; i ++) {
				ParticleSystem[] ps = gameObjects [i].GetComponentsInChildren<ParticleSystem> ();
				foreach (ParticleSystem p in ps) 
					p.enableEmission = toggle;
			}
		} else if (tag == "bonds" || tag == "hetatms") {
			for (var i = 0; i < gameObjects.Length; i ++) {
				Renderer[] renderers = gameObjects [i].GetComponentsInChildren<Renderer> ();			
				foreach (Renderer r in renderers) {
					r.enabled = toggle;
				}
			}
		} else {
		
			for (var i = 0; i < gameObjects.Length; i ++) {
				Renderer r = gameObjects [i].GetComponent<Renderer> ();
				if (r != null) {
					r.enabled = toggle;
				} else { 
					MeshRenderer mr = gameObjects [i].GetComponent<MeshRenderer> ();
					if (mr != null){
						mr.enabled = toggle;
					}
				}
			
			}

		}
	}

	public void destroyObjects ()
	{
		string[] tags = new string[8] {"ribbons", "bonds", "alpha", "beta", "hetatmbs", "hetatms","water","ion"};
		foreach (string tag in tags) {
			GameObject[] gameObjects = GameObject.FindGameObjectsWithTag (tag);
			
			for (int i = 0; i < gameObjects.Length; i ++) {
				Destroy (gameObjects [i]);

 
			}
		}
	}

	//evaluates if a hydrogen bond is present and creates it
	private void checkHBond (Vector3 position1, Vector3 position2)
	{
		if (Vector3.Distance (position1,position2) < 3.5f) { //we have a hBond
			GameObject go = createObject (hBond, position2);	
			go.tag = "hbond";
			go.transform.LookAt (position1);
			go = createObject (hBond, position1);	
			go.tag = "hbond";
			go.transform.LookAt (position2);
		}

	}

	private GameObject createObject (GameObject go, Vector3 position)
	{
		//for centering
		centerPos += position;
		centerCount++;

		GameObject temp = (GameObject)Instantiate (go, position, Quaternion.identity);
		temp.transform.parent = root.transform;
		Renderer r = temp.GetComponent<Renderer> ();
		if (r != null)
			r.enabled = false;
		return temp;
	}

	public void rotateScene (Vector3 direction, Vector3 center)
	{
		rotatedDirection *= Quaternion.AngleAxis (2f, direction); //apply the new rotation

		float angle = 0.009f * Quaternion.Angle (rotatedDirection, new Quaternion (direction.x, direction.y, direction.z, 0));

		root.transform.RotateAround (center, rotatedDirection.eulerAngles, angle);

	}

	public void translateScene (Vector3 direction)
	{
		root.transform.position += direction * 0.3f;

	}



	public void setPosition (Vector3 position) //used for centering the atoms infront of the user in the VR scene
	{
		root.transform.position += position - getCenter ();

	}

	public Vector3 getCenter ()
	{
		return center.transform.position;
	}

	//selects an atom
	public void setTarget (GameObject hit){
		if(target!=null)
			target.GetComponent<Renderer> ().material.color = oldTargetColor;
		target = hit;
		Renderer tr = target.GetComponent<Renderer> ();
		oldTargetColor = tr.material.color;
		tr.material.color = Color.green;
		if (lockedTarget != null) { //if two atoms are selected the distance between them is displayed
			distText.text = "Distance: " + Vector3.Distance (target.transform.position, lockedTarget.transform.position).ToString ("0.00");
			distBox.GetComponent<MeshRenderer> ().enabled = true;
		}
	}

	//resets selected atoms
	public void resetTarget(){
		if (target != null) {
			target.GetComponent<Renderer> ().material.color = oldTargetColor;
			target = null;
		}
		if (lockedTarget != null) {
			lockedTarget.GetComponent<Renderer> ().material.color = oldLockedTargetColor;
			lockedTarget = null;
		}
		distBox.GetComponent<MeshRenderer> ().enabled = false;

	}

	//locks a selected atom to allow selection of a second atom
	public void lockTarget(){
		if (target != null) {
			if(lockedTarget!=null){
				lockedTarget.GetComponent<Renderer> ().material.color = oldLockedTargetColor;
			}
			lockedTarget = target;
			oldLockedTargetColor = oldTargetColor;
			target = null;
		}
	}

	//returns the center for rotation
	public Vector3 getTargetPosition(){
		if (target!= null)
			return target.transform.position;
		else
			return getCenter ();
	}
	
}
