/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System;
using Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;


/// <summary>
/// Controls the player's movement in virtual reality.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OVRPlayerController : MonoBehaviour
{
	/// <summary>
	/// The rate acceleration during movement.
	/// </summary>
	public float Acceleration = 0.1f;

	/// <summary>
	/// The rate of damping on movement.
	/// </summary>
	public float Damping = 0.3f;

	/// <summary>
	/// The rate of additional damping when moving sideways or backwards.
	/// </summary>
	public float BackAndSideDampen = 0.5f;

	/// <summary>
	/// The rate of rotation when using the keyboard.
	/// </summary>
	public float RotationRatchet = 45.0f;

	/// <summary>
	/// The player's current rotation about the Y axis.
	/// </summary>
	private float YRotation = 0.0f;

	/// <summary>
	/// If true, tracking data from a child OVRCameraRig will update the direction of movement.
	/// </summary>
	public bool HmdRotatesY = true;

	protected AddAtoms addAtoms;
	protected Menu menu;

	private float MoveScale = 1.0f;
	private Vector3 MoveThrottle = Vector3.zero;

	private OVRPose? InitialPose;
	
	/// <summary>
	/// If true, each OVRPlayerController will use the player's physical height.
	/// </summary>
	public bool useProfileHeight = true;

	protected CharacterController Controller = null;
	protected OVRCameraRig CameraController = null;

	private float MoveScaleMultiplier = 1.0f;
	private float RotationScaleMultiplier = 1.0f;
	private bool  SkipMouseRotation = false;
	private bool  HaltUpdateMovement = false;
	private float SimulationRate = 60f;

	//Kinect resources
	private KinectSensor _Sensor;
	private CoordinateMapper _Mapper;

	private MultiSourceFrameReader _Reader;

	private GameObject rightHand;
	private GameObject leftHand;

	private GestureMover rightHandMover;
	private GestureMover leftHandMover;

	private DepthSpacePoint rightHandPos;

	private int depthFrameWidth;

	private ushort rightHandDepth;
	private ushort oldRightHandDepth;

	private Windows.Kinect.Body[] trackedUsers;
	private ulong driver = 0;

	private UnityEngine.AudioSource source;
	public AudioClip menuToggled;

	private float buttonPushed = 0f;


	VisualGestureBuilderDatabase _gestureDatabase;
	VisualGestureBuilderFrameSource _gestureFrameSource;
	VisualGestureBuilderFrameReader _gestureFrameReader;
	Gesture _menu;
	bool menuGestureOk = false;

	void Awake ()
	{
		Controller = gameObject.GetComponent<CharacterController> ();
		
		if (Controller == null)
			Debug.LogWarning ("OVRPlayerController: No CharacterController attached.");

		// We use OVRCameraRig to set rotations to cameras,
		// and to be influenced by rotation
		OVRCameraRig[] CameraControllers;
		CameraControllers = gameObject.GetComponentsInChildren<OVRCameraRig> ();

		if (CameraControllers.Length == 0)
			Debug.LogWarning ("OVRPlayerController: No OVRCameraRig attached.");
		else if (CameraControllers.Length > 1)
			Debug.LogWarning ("OVRPlayerController: More then 1 OVRCameraRig attached.");
		else
			CameraController = CameraControllers [0];


		YRotation = transform.rotation.eulerAngles.y;

		Physics.IgnoreLayerCollision (0, 8); //ignores collisions between the player and all objects



		menu = gameObject.GetComponent<Menu> ();
		source = GetComponent<UnityEngine.AudioSource> ();


		addAtoms = gameObject.GetComponent<AddAtoms> ();
		addAtoms.Init (); // load the file

		addAtoms.parsePDB (); //populate the scene

		Quaternion ort = (HmdRotatesY) ? CameraController.centerEyeAnchor.rotation : transform.rotation;

		addAtoms.setPosition (ort * Vector3.forward * 30 + this.transform.position); //centers the atoms infront of the user


		//initiate the kinect
		_Sensor = KinectSensor.GetDefault ();
		if (_Sensor != null) {
			_Mapper = _Sensor.CoordinateMapper;
			_Reader = _Sensor.OpenMultiSourceFrameReader (FrameSourceTypes.Body | FrameSourceTypes.Depth);

			rightHand = transform.FindChild ("RightHand").gameObject;	
			leftHand = transform.FindChild ("LeftHand").gameObject;	

			rightHandMover = rightHand.GetComponent<GestureMover> ();
			leftHandMover = leftHand.GetComponent<GestureMover> ();

			var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
			depthFrameWidth = frameDesc.Width;

			if (!_Sensor.IsOpen) {
				_Sensor.Open ();
			}

	
			_gestureDatabase = VisualGestureBuilderDatabase.Create (Application.streamingAssetsPath + "/Menu2.gbd");
			_gestureFrameSource = VisualGestureBuilderFrameSource.Create (_Sensor, 0);
			foreach (var gesture in _gestureDatabase.AvailableGestures) {
				_gestureFrameSource.AddGesture (gesture);
				
				if (gesture.Name == "Menu2") {
					_menu = gesture;
				}

			}

			_gestureFrameReader = _gestureFrameSource.OpenReader ();
			_gestureFrameReader.IsPaused = true;
		}

	}
	
	
	
	protected virtual void Update ()
	{
		if (useProfileHeight) {
			if (InitialPose == null) {
				InitialPose = new OVRPose () {
					position = CameraController.transform.localPosition,
					orientation = CameraController.transform.localRotation
				};

			}

			var p = CameraController.transform.localPosition;
			p.y = OVRManager.profile.eyeHeight - 0.5f * Controller.height;
			p.z = OVRManager.profile.eyeDepth;
			CameraController.transform.localPosition = p;
		} else if (InitialPose != null) {
			CameraController.transform.localPosition = InitialPose.Value.position;
			CameraController.transform.localRotation = InitialPose.Value.orientation;
			InitialPose = null;
		}

		UpdateMultiFrame (); //get kinect data

		UpdateMovement (); //get keyboard input
	

		Vector3 moveDirection = Vector3.zero;

		float motorDamp = (1.0f + (Damping * SimulationRate * Time.deltaTime));

		MoveThrottle.x /= motorDamp;
		MoveThrottle.y /= motorDamp;
		MoveThrottle.z /= motorDamp;

		moveDirection += MoveThrottle * SimulationRate * Time.deltaTime;

		Vector3 predictedXZ = Vector3.Scale ((Controller.transform.localPosition + moveDirection), new Vector3 (1, 0, 1));

		// Move contoller
		Controller.Move (moveDirection);

		Vector3 actualXZ = Vector3.Scale (Controller.transform.localPosition, new Vector3 (1, 0, 1));

		if (predictedXZ != actualXZ)
			MoveThrottle += (actualXZ - predictedXZ) / (SimulationRate * Time.deltaTime);


	}
	
	public virtual void UpdateMovement ()
	{
		if (HaltUpdateMovement)
			return;

		bool moveForward = Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.UpArrow);
		bool moveLeft = Input.GetKey (KeyCode.A) || Input.GetKey (KeyCode.LeftArrow);
		bool moveRight = Input.GetKey (KeyCode.D) || Input.GetKey (KeyCode.RightArrow);
		bool moveBack = Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.DownArrow);
		bool moveUp = Input.GetKey (KeyCode.T);
		bool moveDown = Input.GetKey (KeyCode.G);
		bool zoomIn = Input.GetAxis ("Mouse ScrollWheel") > 0;
		bool zoomOut = Input.GetAxis ("Mouse ScrollWheel") < 0;


		MoveScale = 1.0f;

		if ((moveForward && moveLeft) || (moveForward && moveRight) ||
			(moveBack && moveLeft) || (moveBack && moveRight))
			MoveScale = 0.70710678f;


		MoveScale *= SimulationRate * Time.deltaTime;

		// Compute this for key movement
		float moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;

		Quaternion ort = (HmdRotatesY) ? CameraController.centerEyeAnchor.rotation : transform.rotation;
		Vector3 ortEuler = ort.eulerAngles;
		ortEuler.z = ortEuler.x = 0f;
		ort = Quaternion.Euler (ortEuler);
		
		if (moveForward)
			MoveThrottle += ort * (transform.lossyScale.z * moveInfluence * Vector3.forward);
		if (moveBack)
			MoveThrottle += ort * (transform.lossyScale.z * moveInfluence * BackAndSideDampen * Vector3.back);
		if (moveLeft)
			MoveThrottle += ort * (transform.lossyScale.x * moveInfluence * BackAndSideDampen * Vector3.left);
		if (moveRight)
			MoveThrottle += ort * (transform.lossyScale.x * moveInfluence * BackAndSideDampen * Vector3.right);
		if (moveUp)
			MoveThrottle += ort * (transform.lossyScale.y * moveInfluence * Vector3.up);
		if (moveDown)
			MoveThrottle += ort * (transform.lossyScale.y * moveInfluence * BackAndSideDampen * Vector3.down);

		if (zoomIn) {
			Camera leftCamera = CameraController.leftEyeAnchor.GetComponent<Camera> ();
			Camera rightCamera = CameraController.rightEyeAnchor.GetComponent<Camera> ();
			if (leftCamera.fieldOfView > 2)
				leftCamera.fieldOfView -= 2;
			if (rightCamera.fieldOfView > 2)
				rightCamera.fieldOfView -= 2;
		}

		if (zoomOut) {
			Camera leftCamera = CameraController.leftEyeAnchor.GetComponent<Camera> ();
			Camera rightCamera = CameraController.rightEyeAnchor.GetComponent<Camera> ();
			if (leftCamera.fieldOfView < 99)
				leftCamera.fieldOfView += 2;
			if (rightCamera.fieldOfView < 99)
				rightCamera.fieldOfView += 2;
		}


		//toggle
		if (Input.GetKey (KeyCode.R) & buttonPushed < Time.time - 0.2f) { //show hide ribbons
			if (addAtoms.ribbonShowing)
				addAtoms.ribbonShowing = addAtoms.resetProtein ("ribbons");
			else
				addAtoms.ribbonShowing = addAtoms.showMode ("ribbons");
			buttonPushed = Time.time;
		}

		if (Input.GetKey (KeyCode.N) & buttonPushed < Time.time - 0.2f) { //menu
			source.PlayOneShot (menuToggled, 0.5f);
			menu.enabled = !menu.enabled;  //hide show menu
			buttonPushed = Time.time;
		}


		if (Input.GetKey (KeyCode.V) & buttonPushed < Time.time - 0.2f) { //center
			addAtoms.setPosition (ort * Vector3.forward * 30 + this.transform.position); //centers the atoms infront of the user
			buttonPushed = Time.time;
		}

		if (Input.GetKey (KeyCode.L) & buttonPushed < Time.time - 0.2f) { //screenshot
			Application.CaptureScreenshot (Application.dataPath + "/Screenshot.png",4);
			buttonPushed = Time.time;
		}

		if (Input.GetKey (KeyCode.X) & buttonPushed < Time.time - 0.2f) { //reset center
			addAtoms.resetTarget();
			buttonPushed = Time.time;
		}

		if (Input.GetKey (KeyCode.Z) & buttonPushed < Time.time - 0.2f) { //lock target
			addAtoms.lockTarget();
			buttonPushed = Time.time;
		}
		
		
		if (Input.GetKey (KeyCode.P) & buttonPushed < Time.time - 0.2f) { // change driver
			bool newDriver = false;
			foreach (var user in trackedUsers) {
				if (user == null)
					continue;
				if (user.IsTracked) {
					if (driver == user.TrackingId) {
						newDriver = true;
						continue;
					}
					if (newDriver) {
						setDriver (user.TrackingId);
						newDriver = false;
						break;
					}
				}
			}
			if (newDriver) { //iterate once more if a new driver wasn't selected
				foreach (var user in trackedUsers) {
					if (user == null)
						continue;
					if (user.IsTracked) {
						if (newDriver) {
							setDriver (user.TrackingId);
							break;
						}
					}
				}
			}
		}


		moveInfluence = SimulationRate * Time.deltaTime * Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;

	}

	public void setDriver (ulong newDriver)
	{
		driver = newDriver;
		_gestureFrameReader.IsPaused = false;
		_gestureFrameSource.TrackingId = newDriver;
		_gestureFrameReader.FrameArrived += _gestureFrameReader_FrameArrived;
	}

	public void UpdateBodyFrame (BodyFrame frame)
	{

		if (frame == null)
			return;


		Windows.Kinect.Body[] data = new Body[_Sensor.BodyFrameSource.BodyCount];
	
		frame.GetAndRefreshBodyData (data);
		frame.Dispose ();
		frame = null;

		if (data == null)
			return;

		trackedUsers = data;

		bool driverExist = false;
		foreach (var body in data) {
			if (body == null)
				continue;

			if (body.IsTracked) {
				if (driver == 0)
					setDriver (body.TrackingId);

				if (driver == body.TrackingId) {
					driverExist = true;

					DepthSpacePoint oldRightHandPos = rightHandPos;



					CalculateJointPositions (body, Windows.Kinect.JointType.HandRight);
					CalculateJointPositions (body, Windows.Kinect.JointType.HandLeft);
					DepthSpacePoint headPos = CalculateJointPositions (body, Windows.Kinect.JointType.Head);
					DepthSpacePoint leftHandPos = CalculateJointPositions (body, Windows.Kinect.JointType.HandLeft);

					menuGestureOk = (leftHandPos.Y - 20) < headPos.Y && (body.HandLeftState == HandState.Closed || body.HandLeftState == HandState.Unknown);

					float diffX = oldRightHandPos.X - rightHandPos.X;
					float diffY = oldRightHandPos.Y - rightHandPos.Y;
					float diffZ = oldRightHandDepth - rightHandDepth; 



					if (body.HandRightState == HandState.Closed) { //drive or menu


						float moveInfluence = Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;
						Quaternion ort = (HmdRotatesY) ? CameraController.centerEyeAnchor.rotation : transform.rotation;
						Vector3 ortEuler = ort.eulerAngles;
						ortEuler.z = ortEuler.x = 0f;
						ort = Quaternion.Euler (ortEuler);

						Vector3 direction = Vector3.zero;

						direction.x = CalculateMovement (-diffX, 2) * 2f; //direction is different so we have to invert the sign
						direction.y = CalculateMovement (diffY, 2) * 2f;
						direction.z = CalculateMovement (diffZ, 5);

						if (!menu.enabled) { //move if menu is disabled

							if (body.HandLeftState == HandState.Lasso) { //rotate

								if (Math.Abs (diffX) > Math.Abs (diffY))
									addAtoms.rotateScene (Vector3.back * (diffX / Math.Abs (diffX)), addAtoms.getTargetPosition());
								else
									addAtoms.rotateScene (Vector3.right * (diffY / Math.Abs (diffY)), addAtoms.getTargetPosition());
							} else if (body.HandLeftState == HandState.Closed) {
								addAtoms.translateScene (direction);
							} else {
								MoveThrottle += ort * (moveInfluence * direction);
							}
						} else { //menu is showing, move mouse instead
							menu.moveCursor (new Vector2 (diffX * 2, diffY * 2));
						}



					} else if (body.HandRightState == HandState.Lasso) { 
						if(body.HandLeftState == HandState.Lasso){ //select atom
							RaycastHit hit;
							Quaternion ort = (HmdRotatesY) ? CameraController.centerEyeAnchor.rotation : transform.rotation;
							if (Physics.Raycast (transform.position, ort*Vector3.forward, out hit, 60)) {
								addAtoms.setTarget(hit.transform.gameObject);
								
							}
						}else{ //Zoom
							if (diffX - 1 > 0) {
								Camera leftCamera = CameraController.leftEyeAnchor.GetComponent<Camera> ();
								Camera rightCamera = CameraController.rightEyeAnchor.GetComponent<Camera> ();
								if (leftCamera.fieldOfView > 2)
									leftCamera.fieldOfView -= 2;
								if (rightCamera.fieldOfView > 2)
									rightCamera.fieldOfView -= 2;
							}
						
							if (diffX + 1 < 0) {
								Camera leftCamera = CameraController.leftEyeAnchor.GetComponent<Camera> ();
								Camera rightCamera = CameraController.rightEyeAnchor.GetComponent<Camera> ();
								if (leftCamera.fieldOfView < 99)
									leftCamera.fieldOfView += 2;
								if (rightCamera.fieldOfView < 99)
									rightCamera.fieldOfView += 2;
							}
						}
					} 


					if (body.HandLeftState == HandState.Lasso) {
						if (menu.enabled)
							menu.Click ();
					} 
							
					oldRightHandDepth = rightHandDepth;
				}
			} 
		}
		if (!driverExist)
			driver = 0;
	}

	public void UpdateDepthFrame (DepthFrame frame)
	{
		if (frame == null) 
			return;

		ushort[] depthData = new ushort[_Sensor.DepthFrameSource.FrameDescription.LengthInPixels]; 
		frame.CopyFrameDataToArray (depthData);
		frame.Dispose ();
		frame = null;

		int Y = (int)rightHandPos.Y;
		int X = (int)rightHandPos.X;
		rightHandDepth = depthData [X + Y * depthFrameWidth];

	}
	
	public void UpdateMultiFrame ()
	{
		if (_Sensor == null)
			return;
		
		var frame = _Reader.AcquireLatestFrame ();
		
		if (frame == null)
			return;
		
		UpdateDepthFrame (frame.DepthFrameReference.AcquireFrame ());
		UpdateBodyFrame (frame.BodyFrameReference.AcquireFrame ());
	}

	private float CalculateMovement (float diff, int cutoff)
	{
		float max = 7f; //gives a max cap to movement
		if (diff > cutoff)
			return Math.Min (diff, max);
		else if (diff < -cutoff)				
			return Math.Max (diff, -max);
		else //only move if the difference is large enough 
			return 0f;
	}

	private DepthSpacePoint CalculateJointPositions (Body body, JointType jointType)
	{
		Windows.Kinect.Joint joint = body.Joints [jointType];

		CameraSpacePoint position = joint.Position;

		// sometimes the depth(Z) of an inferred joint may show as negative
		// clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
		if (position.Z < 0) {
			position.Z = 0.1f;
		}

		if (joint.TrackingState == TrackingState.Tracked) {
			switch (jointType) {
			case JointType.HandRight:
				rightHandPos = _Mapper.MapCameraPointToDepthSpace (position);
				PerformeGesture (body.HandRightState, rightHandMover);
				break;

			case JointType.HandLeft:
				PerformeGesture (body.HandLeftState, leftHandMover);
				break;

			}
		}

		return _Mapper.MapCameraPointToDepthSpace (position);

	}

	private void PerformeGesture (HandState hs, GestureMover gm)
	{
		switch (hs) {
		case HandState.NotTracked:
			gm.NotTracked ();
			break;

		case HandState.Closed:
			gm.CloseHand ();
			break;

		case HandState.Open:
			gm.OpenHand ();
			break;

		case HandState.Lasso:
			gm.Lasso ();
			break;
		}
	}

	/// <summary>
	/// Stop this instance.
	/// </summary>
	public void Stop ()
	{
		Controller.Move (Vector3.zero);
		MoveThrottle = Vector3.zero;
		//FallSpeed = 0.0f;
	}


	/// <summary>
	/// Gets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void GetMoveScaleMultiplier (ref float moveScaleMultiplier)
	{
		moveScaleMultiplier = MoveScaleMultiplier;
	}

	/// <summary>
	/// Sets the move scale multiplier.
	/// </summary>
	/// <param name="moveScaleMultiplier">Move scale multiplier.</param>
	public void SetMoveScaleMultiplier (float moveScaleMultiplier)
	{
		MoveScaleMultiplier = moveScaleMultiplier;
	}

	/// <summary>
	/// Gets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void GetRotationScaleMultiplier (ref float rotationScaleMultiplier)
	{
		rotationScaleMultiplier = RotationScaleMultiplier;
	}

	/// <summary>
	/// Sets the rotation scale multiplier.
	/// </summary>
	/// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
	public void SetRotationScaleMultiplier (float rotationScaleMultiplier)
	{
		RotationScaleMultiplier = rotationScaleMultiplier;
	}

	/// <summary>
	/// Gets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">Allow mouse rotation.</param>
	public void GetSkipMouseRotation (ref bool skipMouseRotation)
	{
		skipMouseRotation = SkipMouseRotation;
	}

	/// <summary>
	/// Sets the allow mouse rotation.
	/// </summary>
	/// <param name="skipMouseRotation">If set to <c>true</c> allow mouse rotation.</param>
	public void SetSkipMouseRotation (bool skipMouseRotation)
	{
		SkipMouseRotation = skipMouseRotation;
	}


	/// <summary>
	/// Sets the halt update movement.
	/// </summary>
	/// <param name="haltUpdateMovement">If set to <c>true</c> halt update movement.</param>
	public void SetHaltUpdateMovement (bool haltUpdateMovement)
	{
		HaltUpdateMovement = haltUpdateMovement;
	}

	/// <summary>
	/// Resets the player look rotation when the device orientation is reset.
	/// </summary>
	public void ResetOrientation ()
	{
		Vector3 euler = transform.rotation.eulerAngles;
		euler.y = YRotation;
		transform.rotation = Quaternion.Euler (euler);
	}

	//close kinect connections
	void OnApplicationQuit ()
	{
		if (_Mapper != null) {
			_Mapper = null;
		}
		
		if (_Sensor != null) {
			if (_Sensor.IsOpen) {
				_Sensor.Close ();
			}
			
			_Sensor = null;
		}
	}


	void _gestureFrameReader_FrameArrived (object sender, VisualGestureBuilderFrameArrivedEventArgs e)
	{
		VisualGestureBuilderFrameReference frameReference = e.FrameReference;
		using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame()) {
			if (frame != null && frame.DiscreteGestureResults != null) {

				
				DiscreteGestureResult result = null;
				if (frame.DiscreteGestureResults.Count > 0)
					result = frame.DiscreteGestureResults [_menu];
				if (result == null | !menuGestureOk)
					return;

				if (result.Detected == true & buttonPushed < Time.time - 1f) {
					source.PlayOneShot (menuToggled, 0.5f);
					menu.enabled = !menu.enabled;  //hide show menu
					buttonPushed = Time.time;
				}

			}
		}
	}
}

