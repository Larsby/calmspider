using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Facebook.Unity;

public class KlondikeGame : MonoBehaviour
{
	//LISTS
	List<ColumnStack> columnList = new List<ColumnStack> ();
	List<FoundationStack> foundationList = new List<FoundationStack> ();
	List<GameCard> selectedcardsList = new List<GameCard> ();
	List<GameCard> startcardsList = new List<GameCard> ();

	//GAME OBJECTS
	public GameObject Background;

	public GameObject UI;
	public GameObject ScreenCheckObject;

	public GameObject RemotePoint;
	public GameObject ResetButton;
	public GameObject MusicOnButton;
	public GameObject ToggleSetButton;
	public GameObject ToggleStaticBgButton;
	public GameObject ToggleBirdsongOnlyButton;

	public GameObject MusicSFX;
	ParticleSystem CardFX1;
	ParticleSystem CardFX2;
	CardSpotlight MySpotlight;
	public RemainingStack MyRemainingStack;
	public WasteStack MyWasteStack;
	public SelectStack MySelectStack;
	public float StackLight = 14.0f;
	public GameObject foundationFX;
	//TOUCH & MOUSE
	bool TouchTracking = false;
	private Vector3 endPoint;
	private bool isTouch = false;

	//AppleTV
	private bool inMenu = false;
	bool isclicked = false;
	Vector2 poschange;
	private bool isAppleTV = false;
	private int currentMenuItem = -1;
	private int old_currentMenuItem = -1;
	private bool remoteMenuMove = false;
	private bool expensiveEffects = true;
	private bool restOfMenuShowing = false;
	public static KlondikeGame instance = null;
	public float timeInMinuesForCrossFade;
	public float timeInSecondsForCrossFade;
	public float DistanceToTravelBetweenFades = 1.4f;
	public float BlendLerpStep = 0.5f;
	public float DistancePerFrameToMoveOnCurrentScreen = 0.001f;
	public bool MoveWhileCrossFade = true;
	private float crossfadeTime;

	public GameObject[] BackgroundCameras;
	private Vector3 BackgroundCameraPosition;

	private ScreenOverlay overlay;

	private float BlendStart = 0.0f;
	private float BlendEnd = 1.0f;
	private int SelectedStackIndex = -1;
	static float BlendLerpTime = 0.0f;

	public GameObject [] foundationOutlines;
	public GameObject remainingOutline;
	public Image quickRestartButton;
	private bool bRestartButtonShowing = false;
	public Animator bgAnimator;
	public GameObject directionalLight;

	public Material foundationOutlineMaterial;
	public Material remainingOutlineMaterial;
	private bool bFadedFoundationOutline = false;
	const float LIFT_TIME = 0.9f;
	private float liftTimer = 0;
	public GameObject settingsPanel;
	public GameObject buttonSrc;
	private bool SFX = true;
	private bool music = true;
	private bool SettingsPanelShowing;
	private bool antialias = true;
	public GameObject backgroundPicture;
	public GameObject bgCamera;
	//GAME STATES
	enum GameState
	{
		Invalid,
		Started,
		Resolving,
		PlayerWins,
		Animating,
	};

	private List<GameCard> animatingCards = new List<GameCard>();

	public BoxCollider [] ColumnColliders;

	public Texture [] cardSetImages;
	public Texture [] altCardSetImages;
	private List<GameObject> generatedCards = new List<GameObject>();

	private bool bIsRelaxed = true;

	enum SelectedCardState
	{
		Idle,
		touching,
		ended};

	GameState m_state;
	SelectedCardState selectedCardState;

	//CONSTANTS
	const float SELECT_Z = -1.0f;
	//Z startpoint for Selectstack

	//PARAMETERS
	public float ParamBetweenHiddenCardsY = 0.2f;
	//OBS! These are set in editor.
	public float ParamBetweenShownCardsY = 1.7f;
	public float ParamBetweenCardsZ = 0.08f;
	public float ParamStartStackCardsZ = -0.11f;

	private float DeltaTime = 0.0f;

	private int CurrentStack = 0;
	public int SelectedStack;
	private bool ready = false;
	Vector3 currentPosition;
	Vector3 prevPosition;

	public GameObject gameCardPrefab;
	public Material[] cardMaterials;
	private bool bUseAlternativeSet = false;
	private int nofSuits = 1;

	[System.Serializable]
	class PlayerData
	{
		public bool musicOn = true;
		public bool sfxOn = true;
		public bool useAlternativeSet = false;
		public bool antialias = true;
		public bool staticBg = false;
		public bool birdsOnly = false;	}

	private AsyncOperation asyncLoad = null;
	private float volumeFadeTime = 1;

	public AudioClip [] birds;
	public float birdMinVolume = 0.005f;
	public float birdMaxVolume = 0.015f;
	private const int MAX_BIRDS = 6;
	private AudioSource [] birdPlayers = new AudioSource[MAX_BIRDS];
	public float[] birdPlayerMinDelay = { 3, 5, 10 };
	public float[] birdPlayerMaxDelay = { 8, 10,15 };
	public float[] birdPlayeRemainingDelay = { 0.0f,1.0f, 2.0f, 5,10,20,60 };

	private BirdTrack [] birdTracks;

	int currentBirdIndex = -1;
	private bool start = false;
	public Image showErrorImage;

	public AudioSource clickOn_AS;
	public AudioSource clickOff_AS;

	private bool playBirds = true;

	private ResourceRequest musicResourceRequest;
	private int musicLoadIndex = -1;

	public Material backMaterial;

	private bool bStaticBg = false;
	private bool bBirdsOnly = false;

	public GameObject bgAnimated;
	public GameObject bgStatic;

	public ToggleGame toggleGame;


	void Awake ()
	{
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy (gameObject);    

		UpdateFoundationOutlineColor(Color.clear);
		UpdateRemainingOutlineColor(Color.clear);

		//	DontDestroyOnLoad (gameObject);
	}
	public void ToggleSettingsPanel() {
		SettingsPanelShowing = !SettingsPanelShowing;
	}

	void UpdateFoundationOutlineColor(Color newColor)
	{
		foundationOutlineMaterial.color = newColor;
	}
	void UpdateRemainingOutlineColor(Color newColor)
	{
		remainingOutlineMaterial.color = newColor;
	}

	private bool IsSmallDevice() {
		float width = Screen.width / Screen.dpi;
		float height = Screen.height / Screen.dpi;

//		Debug.Log ("DPI: " + Screen.dpi + "  W: " + Screen.width + "  H: " + Screen.height);
//		Debug.Log ("Screen: " + width + " x " + height);
/*
		GameObject DPItext = GameObject.Find ("DPI");
		if (DPItext != null) {
			Text text = DPItext.GetComponent<Text> ();
			text.text = "DPI: " + Screen.dpi + "  W: " + Screen.width + "  H: " + Screen.height + "  Measured: " + width + " x " + height + " memory "+ SystemInfo.systemMemorySize + " g "+ SystemInfo.graphicsMemorySize;
		}
*/
		if (width < 4 || height < 4) // arbitrary, but whatever
			return true;
		else
			return false;
	}
 

	public void testWin2(){
		m_state = GameState.PlayerWins;
		StartCoroutine (GameWon ());
	}
	public void testWin(){
		// MoveAllToFoundation ();
		Invoke ("testWin2", 1f);
	}

	void RemoveAntiAliasing() {
		
		//for whatever reason GetComponent<UnityStandardAssets.ImageEffects.Antialiasing>() does not work so we do this dark magic instead.
		Component[] components = GameObject.Find("Main Camera").GetComponents<Component>();
		Component alias = null;
		foreach (Component c in components) {
			Debug.Log (c.GetType ());
			if ((""+c.GetType ()).Equals ("UnityStandardAssets.ImageEffects.Antialiasing")) {
				alias = c;
				antialias = false;
			}
		}
		if (alias != null) {
			Destroy (alias);
		}
	}

	void SystemRequirementCheck() {
		int MemorySizeInMB = SystemInfo.systemMemorySize;
		if (MemorySizeInMB > 0 && MemorySizeInMB < 1000) {
			Background.SetActive (false);
			backgroundPicture.SetActive (true);
			backgroundPicture.GetComponent<Renderer> ().enabled = true;
			bgAnimator.enabled = false;
			Destroy (Background);

		}
	}
	 

	void UpdateBackColor(Color color) {
		backMaterial.color = color;
	}

	void UpdateOverlayColor(float intensity)
	{
		overlay.intensity = intensity;
	}
	public void DoStart(int nofSuits) {
		this.nofSuits = nofSuits;

		StartKlondike ();	//Start game (no button needed)
		m_state = GameState.Started;
		prevPosition = new Vector3 (0.0f, 0.0f, 0.0f);
		selectedCardState = SelectedCardState.Idle;
		if (bgAnimator != null) {
			bgAnimator.speed = 1f;
		}
			
		//		Application.targetFrameRate = 60;

		// Invoke ("testWin", 0.1f); // test winning animation

		//		iTween.ValueTo (gameObject, iTween.Hash ("from", 0, "to", 0.9f, "time", 0.8f, "easetype", "easeInCubic", "onUpdate", "UpdateOverlayColor"));

		/*
		ScreenOverlay[] overlays = bgCamera.gameObject.GetComponentsInChildren<ScreenOverlay> ();

		overlay = overlays [0];
		if (overlays.Length > 1 && overlays[1].intensity == 0) // just in case someone moves the overlays around
			overlay = overlays [1];
		*/

		start = true;

		backMaterial.color = Color.clear;
		iTween.ValueTo (gameObject, iTween.Hash ("from", Color.clear, "to", Color.white, "time", 0.5f, "easetype", "linear", "onUpdate", "UpdateBackColor"));
	}

	void Start ()
	{
		FB.Init ();
		start = false;
		birdTracks = GetComponents<BirdTrack> ();
		int i = 0;

		foreach (BirdTrack bird in birdTracks) {
			bird.SetClips (birds);
		}
		//	SystemRequirementCheck ();
		LoadPreferences ();
		if (antialias == false) {
			RemoveAntiAliasing ();
		}

		SettingsPanelShowing = false;
		restOfMenuShowing = false;
		m_state = GameState.Invalid;
		//UI.SetActive ();
		//CheckScreenSize ();
		InitGame ();

		for ( i = 0; i < MAX_BIRDS; i++) {
			birdPlayers [i] = gameObject.AddComponent<AudioSource> ();
			birdPlayers [i].volume = 1f;
		}

		if (music)
			PlayNewSong ();
//		iTween.ValueTo (gameObject, iTween.Hash ("from", 0f, "to", 0.9f, "time", 2.8f, "easetype", "linear", "onUpdate", "UpdateOverlayColor"));

		HandleStaticBg ();

		if (ApplicationModel.restartLevel > 0)
			toggleGame.StartGame(ApplicationModel.restartLevel);

		GameObject g = GameObject.Find("PrepCard");
		if (g)
		{
			GameCard gc = g.GetComponent<GameCard>();
			gc.PlayVictoryAnim();
			Destroy(g, 0.5f);
		}
	}

	void InitGame ()
	{
		SwitchOnCardLight ();

		MySpotlight = GameObject.Find ("CardSpotlight").GetComponentInParent<CardSpotlight> ();

		CardFX1 = GameObject.Find ("GlowFX").GetComponent<ParticleSystem> ();
		CardFX2 = GameObject.Find ("SeriesFX").GetComponent<ParticleSystem> ();
	

		bRestartButtonShowing = false;

	//	BackgroundCameraPosition = BackgroundCameras [1].transform.position;
		InitSound ();
		crossfadeTime = Time.time + (timeInMinuesForCrossFade * 60) + timeInSecondsForCrossFade;

		#if !UNITY_EDITOR && UNITY_TVOS
			UnityEngine.Apple.TV.Remote.touchesEnabled = true;
			UnityEngine.Apple.TV.Remote.allowExitToHome = false;

	
			RemotePoint.SetActive(true);
		 	isTouch=false;
			isAppleTV=true;
		#endif
	}

	private void ModifyColumnSpacing() {

		Vector3 leftMost = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, -Camera.main.transform.position.z - columnList[0].gameObject.transform.position.z));
		//Debug.Log ("LM: " + leftMost.x + "  " + columnList[0].gameObject.transform.position);

		float xDiff = Mathf.Abs(leftMost.x) - Mathf.Abs(columnList[0].gameObject.transform.position.x);
		//Debug.Log ("xDiff " + xDiff);

		float aspect = (float)Screen.width / (float)Screen.height;
		if (aspect < 1.45f) { // on Ipad / tablets (4:3 aspect) move the cards up because we don't have the problem with the top navigation bar
			for (int i = 0; i < 10; i++)
				columnList [i].gameObject.transform.localPosition = new Vector3 (columnList [i].gameObject.transform.localPosition.x, 4.0f, columnList [i].gameObject.transform.localPosition.z);
		} else {
			for (int i = 0; i < 10; i++)
				columnList [i].gameObject.transform.localPosition = new Vector3 (columnList [i].gameObject.transform.localPosition.x, aspect <= 1.6f? 2.2f : aspect <= 2f ?1.5f : 0.6f, columnList [i].gameObject.transform.localPosition.z);
			if (aspect <= 1.6f) {
				MyRemainingStack.transform.localPosition = new Vector3 (-10.7f, -8.35f, -1);
			}
			else if (aspect <= 2f) { // 16:9 typ
				MyRemainingStack.transform.localPosition = new Vector3 (-11.9f, -8.5f, 1);
			}
			else { // Iphone X
				MyRemainingStack.transform.localPosition = new Vector3(-11.9f, -8.0f, 1);
			}

		}
	}


	private void SavePreferences ()
	{
		
		BinaryFormatter bf = new BinaryFormatter ();
		string gamePath = Application.persistentDataPath + "/";
		FileStream file = File.Open (gamePath + "gameInfo3.dat", FileMode.OpenOrCreate);
		PlayerData data = new PlayerData ();
		data.musicOn = music;
		data.sfxOn = SFX;
		data.useAlternativeSet = bUseAlternativeSet;
		data.antialias = antialias;
		data.birdsOnly = !playBirds;
		data.staticBg = bStaticBg;
		bf.Serialize (file, data);
		file.Close ();
	}

	private void LoadPreferences ()
	{
		
		string gamePath =  Application.persistentDataPath + "/";
		if (File.Exists (gamePath + "gameInfo3.dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (gamePath + "gameInfo3.dat", FileMode.Open, FileAccess.Read);
			PlayerData data = (PlayerData)bf.Deserialize (file);
			file.Close ();

			music = data.musicOn;

			SFX = cardAudioSource.enabled = data.sfxOn;
			bUseAlternativeSet = data.useAlternativeSet;
			antialias = data.antialias;
			playBirds = !data.birdsOnly;
			bStaticBg = data.staticBg;
		} else {
			music = true;
			SFX = true;
			playBirds = true;
			bStaticBg = false;
			bUseAlternativeSet = false;//IsSmallDevice ();
			SavePreferences ();
		}

//		bUseAlternativeSet = IsSmallDevice (); // for test only
	}



	public	void SetReady (bool ready)
	{
		this.ready = ready;
	}

	public	bool GetReady ()
	{
		return ready;
	}


	void UpdateQRButtonColor(Color newColor)
	{
		quickRestartButton.color = newColor;
	}
	private void OnEndAnimFinished() {
		if (quickRestartButton.gameObject.activeSelf)
			return;
		quickRestartButton.gameObject.SetActive (true);
		quickRestartButton.color = Color.clear;
		iTween.ValueTo (gameObject, iTween.Hash ("from", new Color(255,255,255,0), "to", Color.white, "time", 0.8f, "easetype", "easeInCubic", "onUpdate", "UpdateQRButtonColor"));
		bRestartButtonShowing = true;
	}


	float time;

	KeyCode GetKeyCode ()
	{
		KeyCode result = KeyCode.Underscore;
		#if !UNITY_TVOS || UNITY_EDITOR
		/* if (Input.GetKeyDown (KeyCode.RightArrow)) {
			return KeyCode.RightArrow;
		}
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			return KeyCode.UpArrow;
		}
		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			return KeyCode.DownArrow;
		}
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			return KeyCode.RightArrow;
		}
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			return KeyCode.LeftArrow;
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			return KeyCode.Space;
		} */
		#endif
		#if UNITY_TVOS
		if (Input.GetKeyDown (KeyCode.JoystickButton14)) {
			return KeyCode.Space;
		}
		if (time < Time.time) { // wait between samplings so we don't get to much repeat key.
			float y = Input.GetAxis ("Vertical");
			float x = Input.GetAxis ("Horizontal");

			time = Time.time + 0.1f;

			if (y == 0 && x == 0) {
				
				return  result;
			}

			if (x < 0) {ƒ
				return KeyCode.LeftArrow;
			} else if (x > 0) {
				return KeyCode.RightArrow;
			}
			if (y > 0) {
				return KeyCode.UpArrow;
			} else if (y < 0) {
				return KeyCode.DownArrow;
			}

		}
	
		#endif
		return result;
	}

	float PlayBirdTrack(int trackIndex) {

	
		return birdTracks [trackIndex].Play(birdPlayers[trackIndex],birdMinVolume);
	}

	void PlayBird(int i) {
		int birdIndex = -1;
		do {
			birdIndex =Random.Range (0, 21);
		} while(birdIndex == currentBirdIndex);
		currentBirdIndex = birdIndex;

		birdPlayers[i].clip = birds[birdIndex];
		float amp = 0;
		if (i == 1) { 
			amp = 0.01f;
		} else if (i == 2) {
			amp = 0.05f;
			birdPlayers [i].panStereo = Random.Range (-1.0f, 1.0f);
		}
		birdPlayers [i].volume = Random.Range (birdMinVolume, birdMaxVolume)+amp;

		birdPlayers [i].Play ();

		birdPlayeRemainingDelay [i] = Random.Range (birdPlayerMinDelay[i] + birds[birdIndex].length, birdPlayerMaxDelay[i] + birds[birdIndex].length);
	}


	int clearSet = 0;
	// Update is called once per frame
	void Update ()
	{
		/*
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			nofFinish = 5;
			AnimateFinishedSet(clearSet);
			clearSet++;
		} */


		if (musicLoadIndex > -1) {
			if (musicResourceRequest.isDone) {
				if (music)
					StartLoadedSong ();
				musicLoadIndex = -1;
			}
		}

		if (asyncLoad != null) {
			musicAudioSource.volume -= 0.5f * Time.deltaTime;
			for (int i = 0; i < MAX_BIRDS; i++)
				birdPlayers [i].volume -= 2 * Time.deltaTime;

			if (asyncLoad.progress >= 0.9f && musicAudioSource.volume <= 0) {
				iTween.Stop (); // need to explicitly stop running itweens before loading scene, or Tween.Count() will return above 0 forever !!
				asyncLoad.allowSceneActivation = true;
			}
		} else {
			if (playBirds) {
				for (int i = 0; i < MAX_BIRDS; i++) {
					//Debug.Log ("" + i);
					birdPlayeRemainingDelay [i] -= Time.deltaTime;
					if (birdPlayeRemainingDelay [i] <= 0) {
						birdPlayeRemainingDelay [i] += PlayBirdTrack (i);
					}
				}
			}
		}


		DeltaTime += (Time.deltaTime - DeltaTime) * 0.1f;

		liftTimer -= Time.deltaTime;

		//AnimateBackground ();
		if (ready) {
			//Background.SetActive (false);
		}
		CheckFPS (); 
		if (m_state == GameState.Started) {


			//------- MUSIC PROGRESS------
			MusicManager ();

			//--------CHECK FPS--------

			//APPLETV TOUCH & BUTTON INPUT
			#if !UNITY_EDITOR && UNITY_TVOS
			AppleTVNMenuavigation();
		
			if(inMenu == true) {
			return;
			}
			#endif
			KeyCode pressedKey = GetKeyCode ();
			//------- KEYBOARD INPUT
			/*
			if (pressedKey == KeyCode.LeftArrow) {
				OnMoveLeft ();
			} else if (pressedKey == KeyCode.RightArrow) {
				OnMoveRight ();
			} else if (pressedKey == KeyCode.UpArrow) {
				OnMoveUp ();
			} else if (pressedKey == KeyCode.DownArrow) {
				OnMoveDown ();
			} else if (pressedKey == KeyCode.Space) {
				OnSelectCard ();
			}
			*/
	
			// misol: this is to ensure that no cards are moved while flipping cards
			for (int i = animatingCards.Count - 1; i >= 0; i--)
			{
				if (!animatingCards[i].isAnimating())
					animatingCards.RemoveAt(i);
			}


			//Touch for directtouch (iOS) and for MouseInput
			#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_IPHONE && !UNITY_TVOS
			if(SettingsPanelShowing == false){
			if (Input.GetMouseButtonDown (0)) {// && selectedCardState != SelectedCardState.touching) {
				TouchInputDragStart ();
			}
			if (Input.GetMouseButtonUp (0)) {
				TouchInputDragStop ();
			}
			if (selectedCardState == SelectedCardState.touching) {
				FiddleWithSelectedStack ();
			} else if (TouchTracking == true) {
				SelectStackUpdateTracking ();
			}
		}
			#endif
		}
	}

	private void AppleTVNMenuavigation ()
	{
		 
		if (m_state == GameState.Started || m_state == GameState.PlayerWins) {
			if (inMenu == true) { //in inMenu - only check for buttons on menu
				if (Input.touchCount > 0) {
					if (Input.GetTouch (0).phase == TouchPhase.Began) { //Start drag
						currentMenuItem = GameObject.Find ("Panel").GetComponent<PanelScript> ().activeItem;
						old_currentMenuItem = currentMenuItem;
					}

					if (Input.GetTouch (0).phase == TouchPhase.Moved) {
						Vector2 poschange = Input.GetTouch (0).deltaPosition;
						 
						if (poschange.y > 30)
							currentMenuItem--;
						if (poschange.y < -30)
							currentMenuItem++;

						if (currentMenuItem > 3) {
							currentMenuItem = 3;
						}
						if (currentMenuItem < 0) {
							currentMenuItem = 0;
						}
						  
					}
					if (Input.GetTouch (0).phase == TouchPhase.Ended || Input.GetTouch (0).phase == TouchPhase.Canceled) { //Stop drag
						if (old_currentMenuItem != currentMenuItem) {
							GameObject.Find ("Panel").GetComponent<PanelScript> ().ChangeCurrentMenuItem (currentMenuItem);

						}
					}

				}
			}	 
		}


		//Remote control buttons
		if (Input.GetKeyDown (KeyCode.JoystickButton14)) { //Click on pad
			if (inMenu) {
				ClickedOnMenuItem ();
			} else {
				isclicked = true;
				Debug.Log ("APPLETV: Click-mode active");
				//TouchInputDragStart ();
			}
		}
		if (Input.GetKeyDown (KeyCode.JoystickButton0)) { //click on "Menu"
			if (inMenu) {
				inMenu = false;
	
			} else {
				inMenu = true;	
				currentMenuItem = GameObject.Find ("Panel").GetComponent<PanelScript> ().activeItem;
				GameObject.Find ("Panel").GetComponent<PanelScript> ().ChangeCurrentMenuItem (0);
			}
		}
	}

	public void ToggleMenu ()
	{ 
		PlayClick (!settingsPanel.activeSelf);

		if (settingsPanel.activeSelf) {
			if (bRestartButtonShowing)
				quickRestartButton.gameObject.SetActive (true);
		} else
			quickRestartButton.gameObject.SetActive (false);

		settingsPanel.SetActive (!settingsPanel.activeSelf);

		SetButtonState (MusicSFX,SFX);
		SetButtonState (MusicOnButton, music);
		SetButtonState (ToggleSetButton, !bUseAlternativeSet);

		if (ToggleStaticBgButton != null) 
			SetButtonState (ToggleStaticBgButton, bStaticBg);
		if (ToggleBirdsongOnlyButton != null)
			SetButtonState (ToggleBirdsongOnlyButton, playBirds);
	}
		

	void ClickedOnMenuItem ()
	{
		Debug.Log ("IN ClickedOnMenuItem");
		switch (GameObject.Find ("Panel").GetComponent<PanelScript> ().activeItem) {
		case 0:
			
			ToggleMenu ();
//			Debug.Log ("ClickedOnMenuItem: Menu ON");
		//	UI.SetActive (false);
			inMenu = restOfMenuShowing;
			break;
		case 1:
//			Debug.Log ("ClickedOnMenuItem: RESTART");
			inMenu = false;
			onRestartButton ();
			break;
		case 2:
//			Debug.Log ("ClickedOnMenuItem: Music on");
			inMenu = false;
			ToggleMusic (true);
			break;
		case 3:
//			Debug.Log ("ClickedOnMenuItem: Music off");
			inMenu = false;
			ToggleMusic (false);
			break;
		default:
			break;
		}
	}

	
	//Works as mouse over for AppleTV navigation
	void TouchOver ()
	{

//		Debug.Log ("Touchover");
	  
		//First disable the selectstack so the ray doesn't collide with it
		MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, 1f);

		Ray ray;
		RaycastHit hit;

		ray = Camera.main.ScreenPointToRay (Camera.main.WorldToScreenPoint (RemotePoint.transform.position));

		if (Physics.Raycast (ray, out hit)) {	//Something is hit
			endPoint = hit.point;
			Debug.Log ("APPLETV SOMETHINGS OVER");
			 
			GameObject go = GameObject.Find (hit.collider.name);

			Debug.Log ("TRACKING INIT: " + go.name.ToString ());

			if (hit.collider.name.ToString ().Length > 7 && hit.collider.name.ToString ().Substring (0, 8) == "GameCard") {
				
				Debug.Log ("Something =: " + hit.collider.name.ToString ());
				Debug.Log ("Somethings parent =" + go.transform.parent.name.ToString ());

				GameCard kc = GameObject.Find (go.transform.name).GetComponent<GameCard> ();

				Debug.Log ("Somethings stack =" + kc.Definition.Stack.ToString ());

				CurrentStack = kc.Definition.Stack;

				SetCardLight (kc.Definition.Stack, kc, false);	 

				CurrentStack = -1; //Remain as not set after light is set.
				 
			} else if (hit.collider.name.ToString ().Length >= 10 && hit.collider.name.ToString ().Substring (0, 10) == "WasteStack") { //Bara hit om tom
				Debug.Log ("touchover -wastestack");

				if (MyWasteStack.CountCards () > 0) {
					SetCardLight (8, MyWasteStack.GetTopCard (), false);
				}
				TouchSelectStack (8, 0);
			} else if (hit.collider.name.ToString ().Length >= 14 && hit.collider.name.ToString ().Substring (0, 14) == "RemainingStack") { //Bara hit om tom
				Debug.Log ("touchover - remaingstack");

				if (MyWasteStack.CountCards () > 0) {
					SetCardLight (7, MyRemainingStack.GetTopCard (), false);
				}
//				TouchSelectStack (7, 0);
			} else if (hit.collider.name.ToString ().Length >= 15 && hit.collider.name.ToString ().Substring (0, 15) == "FoundationStack") { //Bara hit om tom
				Debug.Log ("touchover - foundationstack");

				string whichfoundation = hit.collider.name.ToString ().Substring (15, 1);
				Debug.Log ("whichfoundation=" + whichfoundation);
				SetCardLight (int.Parse (whichfoundation), null, false);
				 
			} else { //Nothing hit
				MySpotlight.destinationPoint = new Vector3 (MySpotlight.transform.position.x, MySpotlight.transform.position.y, StackLight); //Just remove it
			}
		} else {
			MySpotlight.destinationPoint = new Vector3 (MySpotlight.transform.position.x, MySpotlight.transform.position.y, 14.0f); //Just remove it
		}

		//Enable the Selectstack again if it's Z is incorrect
		MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, SELECT_Z);

	}


	private int cameraDirection = 1;


	//FPS CHECK
	private int fpsdips = 0;
	private bool doMoveCamera = false;

	void CheckFPS ()
	{
		
		if (1.0f / DeltaTime < 18 ) {	// Less than 20fps
			fpsdips++;
		}
			
		if (fpsdips >= 30) {
			RemoveAntiAliasing ();


			fpsdips = 0;
		}
//		if (fpsdips == -100) {
//			Background.SetActive(true);
//			fpsdips = 0;
//		}



	}


	private void CheckScreenSize ()
	{
		Vector3 screenPoint = Camera.main.WorldToViewportPoint (ScreenCheckObject.transform.position);//this.transform.Find("MenuSprite").gameObject.transform.position );
		 
		if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1) {
			Debug.Log ("FOUND MENUSPRITE! Keep this field of view for camera"); 
		} else {
			Debug.Log ("DID NOT FOUND MENUSPRITE! Change the camera fieldofview");
			Camera.main.fieldOfView = Camera.main.fieldOfView + 1;	//75
			CheckScreenSize (); //Reitarate until it's found
		}
	}

	public void OnButton (string msg)
	{
//		Debug.Log ("OnButton = " + msg);
		switch (msg) {
		case "New":

	//		SetStateOfRestOfMenu (false);
		//	this.transform.Find ("MenuSprite").gameObject.SetActive (false);
				//StartCoroutine(OnReset());
			break;
		case "Quit":
	//		SetStateOfRestOfMenu (false);
		//	this.transform.Find ("MenuSprite").gameObject.SetActive (false);
				//StartCoroutine(OnStop());
			break;
		}
	}

	void SetButtonState (GameObject obj, bool active)
	{
		int activeIndex = active == true ? 0 : 1;
		int inactiveIndex = active == true ? 1 : 0;
		Image img = obj.transform.GetChild (inactiveIndex).gameObject.GetComponent<Image> ();
		obj.transform.GetChild (inactiveIndex).gameObject.SetActive (false);

		img.enabled = false;
		obj.transform.GetChild (activeIndex).gameObject.SetActive (true);
		img = obj.transform.GetChild (activeIndex).gameObject.GetComponent<Image> ();
		img.enabled = true;
	}


	public void onRestartButton ()
	{
//		Debug.Log ("onRestartButton");

		PlayClick (true);

		if (asyncLoad == null) {
			asyncLoad = SceneManager.LoadSceneAsync (SceneManager.GetActiveScene ().name);
			if (asyncLoad != null) {
				asyncLoad.allowSceneActivation = false;
			}
		}
	}

	public void ToMainButton() {
		PlayClick (true);

		ApplicationModel.restartLevel = -1;

		if (asyncLoad == null) {
			asyncLoad = SceneManager.LoadSceneAsync (SceneManager.GetActiveScene ().name);
			if (asyncLoad != null) {
				asyncLoad.allowSceneActivation = false;
			}
		}
	}


	private void HandleStaticBg() {
		if (ToggleStaticBgButton != null && bgAnimated != null && bgStatic != null) {
			bgAnimated.SetActive (!bStaticBg);
			bgStatic.SetActive (bStaticBg);
			Animator a = bgCamera.GetComponentInChildren<Animator> ();
			if (a)
				a.enabled = !bStaticBg;
			if (bStaticBg)
			{
				bgCamera.transform.position = new Vector3(0, -0.9f, -13.21f);
				bgCamera.transform.rotation = Quaternion.identity;
			}
			else
			{
//				bgCamera.transform.position = new Vector3(0, bgCamera.transform.position.y, bgCamera.transform.position.z);
				bgCamera.transform.position = new Vector3(0, 4.97f, -20.21f);
				bgCamera.transform.rotation = Quaternion.Euler(-15.7f, 0,0);

			}
		}
	}

	public void onToggleStaticBg () {
		PlayClick (!bStaticBg);

		bStaticBg = !bStaticBg;
		HandleStaticBg ();
		SetButtonState (ToggleStaticBgButton, bStaticBg);
		SavePreferences ();
	}

	public void onToggleBirdsongOnly ()
	{
		PlayClick (!playBirds);
		playBirds = !playBirds;	

		if (playBirds) {
			for (int i = 0; i < birdTracks.Length; i++) {
				birdTracks [i].Play ();
			}	
		} else {
			for (int i = 0; i < birdTracks.Length; i++) {
				birdTracks [i].StopPlay ();
			}
		}

		SetButtonState (ToggleBirdsongOnlyButton, playBirds);
		SavePreferences ();
	}


	public bool CheckIfIsTouch ()
	{
		return isTouch;
	}

	void ResetSelectedCardsToColumn ()
	{
		int counter = 0;
//		Debug.Log ("ResetSelectedCardsToColumn");
//		Debug.Log ("1MySelectStack.CountCards = " + MySelectStack.CountCards ().ToString ());

		while (MySelectStack.CountCards () > 0) {
			//found problem here if you try and throw a card out of the aces piles, this one breaks.
			columnList [SelectedStack].AddToStack (MySelectStack.GetCard (0));
			MySelectStack.RemoveCardFromStack (0);
			counter++;
		}

		if (SelectedStack != -1) {
			#if UNITY_TVOS
			columnList [SelectedStack].OrganizeStack (false, false, true, true);
			return;
			#endif
			columnList [SelectedStack].OrganizeStack (false, false, true);
		}
//		Debug.Log ("Cards reseted:" + counter.ToString ());
	}

	void ResetSelectedCardsToWasteStack ()
	{
//		Debug.Log ("ResetSelectedCardsToWasteStack");
		if (isAppleTV)
			MyWasteStack.AddToStack (MySelectStack.GetCard (0), true);
		else
			MyWasteStack.AddToStack (MySelectStack.GetCard (0), CheckIfIsTouch ());
		MySelectStack.RemoveCardFromStack (0);
		MyWasteStack.OrganizeStack ();
	}

	public void ResetSelectStack ()
	{
		MySelectStack.RemoveAllCards ();
		while (selectedcardsList.Count > 0) {
			selectedcardsList.RemoveAt (0);
		}
		SelectedStack = -1;

	}

	IEnumerator ResetSelectedCardsToFoundationStack (int in_stack)
	{
//		Debug.Log ("I ResetSelectedCardsToFoundationStack");
		if (MySelectStack.CountCards () > 0 && selectedcardsList.Count > 0) {
			
			Vector3 fromPos = new Vector3 (selectedcardsList [0].transform.position.x, selectedcardsList [0].transform.position.y, selectedcardsList [0].transform.position.z);
		
			foundationList [in_stack - 9].AddToStack (selectedcardsList [0]);
//			Debug.Log ("cards in this foundation:" + foundationList [in_stack - 9].CountCards ().ToString ());

			//Animate it to correct place
			Vector3 toPos = new Vector3 (
				                foundationList [in_stack - 9].transform.position.x,
				                foundationList [in_stack - 9].transform.position.y,
				                foundationList [in_stack - 9].GetTopCard ().transform.position.z - 0.01f
			                );

			selectedcardsList [0].sourceStack = SelectedStack;
			selectedcardsList [0].targetStack = CurrentStack;
			selectedcardsList [0].isTouch = true;
//			AnimateCard (selectedcardsList [0], fromPos, toPos, true, false, 0.6f);

			//Sound 1
//			PlayCardSound (foundationList [CurrentStack - 9].GetCardSound (Random.Range (1, 2)), 1.0f, in_stack);

			selectedcardsList.RemoveAt (0);

			yield return new WaitForSeconds (0.8f);

			foundationList [in_stack - 9].OrganizeStack ();			 

		}

	 
	}


	void CPResetSelectedCardsToFoundationStack (int in_stack)
	{
		if (MySelectStack.CountCards () > 0 && selectedcardsList.Count > 0) {

			foundationList [in_stack - 9].AddToStack (selectedcardsList [0]);
			Debug.Log ("cards in this foundation:" + foundationList [in_stack - 9].CountCards ().ToString ());

			selectedcardsList [0].sourceStack = SelectedStack;
			selectedcardsList [0].targetStack = CurrentStack;
			selectedcardsList [0].isTouch = true;

			selectedcardsList.RemoveAt (0);
			foundationList [in_stack - 9].OrganizeStack ();			 
		}
	}





	IEnumerable WaitSeconds (float t)
	{
		yield return new WaitForSeconds (t);
	}


	/********* NAVIGATION KEYBOARD *******************/

	void OnSelectCard ()	//Keyboard input
	{
		if (m_state == GameState.Started) {
//			Debug.Log ("On select");
			if (SelectedStack == -1) {	
				OnSelectSourceCards (false);
			} else if (selectedcardsList.Count > 0) {
				OnSelectTargetCards (false);
			}

			if (CheckIfGameWon ()) {
				m_state = GameState.PlayerWins;
			}
		}
	}

	void OnSelectSourceCards (bool isTouch)
	{
//		Debug.Log ("--------------NEW SELECT SOURCE--------------");
//		Debug.Log ("Selected Stack=" + SelectedStack.ToString ());

		//Clean selectcardsList first...
		while (selectedcardsList.Count > 0) {
			selectedcardsList.RemoveAt (0);
		}

		if (CurrentStack < 10) { //In columnstack
			columnList [CurrentStack].SelectCard (true);

			SelectSourceCards (columnList [CurrentStack].GetSelectedCard (), isTouch);
//			Debug.Log ("ADD TO SELECTLIST. Cards in the selectlist:" + selectedcardsList.Count.ToString ());
			SelectedStack = CurrentStack;
		}
		if (CurrentStack == 10) {								//Pull card from Remaining stack
			if (m_state != GameState.Animating) {
				MySpotlight.destinationPoint = new Vector3 (MyRemainingStack.transform.position.x, MyRemainingStack.transform.position.y, StackLight);
				StartCoroutine (MoveCardRemainingToWaste (isTouch));
			}						
		}
/*
		if (CurrentStack == 8 && MyWasteStack.CountCards () > 0) {	//Select card from Waste stack
			MyWasteStack.SelectCard ();
			SelectSourceCardFromWaste (isTouch);
			SelectedStack = CurrentStack;
		}
		if (CurrentStack > 8) {								//In Foundationstacks SELECT FROM FOUNDATION IS ALLOWED NOW
			if (foundationList [CurrentStack - 9].GetTopCard () != null && CurrentStack != SelectedStack) {
				
				foundationList [CurrentStack - 9].SelectCard (true);
				SelectSourceCards (foundationList [CurrentStack - 9].GetTopCard (), isTouch);
				SelectedStack = CurrentStack;
			}
		}
		*/
	}

	void OnSelectTargetCards (bool isTouch)
	{
//		Debug.Log ("--------------NEW SELECT TARGET--------------");
//		Debug.Log ("Selected Stack=" + SelectedStack.ToString ());

		if (CurrentStack < 10) {								//In columnstacks
			if (SelectedStack == CurrentStack) {	//If we're in the same place, do unselect
//				Debug.Log ("Unselect");
				ResetToBackAnimation ();
				UnSelectSourceCards (isTouch);

//				Debug.Log ("Currentstack" + CurrentStack.ToString ());
//				Debug.Log ("COUNT::::" + columnList [CurrentStack].CountCards ().ToString ());
				columnList [CurrentStack].SetCurrentCard (columnList [CurrentStack].CountCards () - 1);
			} else {		 
				//WHEN SOURCE AND TARGET IS COLUMN
				if (SelectedStack > -1 && SelectedStack < 10) {	
					StartCoroutine (MoveCardColumnToColumn (isTouch));
				}

				// SOURCE IS WASTESTACK AND TARGET IS COLUMN
	/*			if (SelectedStack == 8) {	
//					Debug.Log ("Before MoveCardWasteToColumn");
					MoveCardWasteToColumn (isTouch);
				}*/
			}
		}
		/*
		if (SelectedStack == 8 && CurrentStack == 8) {	//TARGET WASTE STACK - Reset select
			UnSelectSourceCardFromWaste ();
			SelectedStack = -1;
		}
		if (CurrentStack > 8 && SelectedStack < 9) {		
			//WHEN SOURCE IS COLUMN AND TARGET FOUNDATION STACKS
			if (SelectedStack > -1 && SelectedStack < 7) {	
				StartCoroutine (MoveCardColumnToFoundation (isTouch));
			}

			//WHEN SOURCE IS WASTESTACK AND TARGET IS FOUNDATION STACKS
			if (SelectedStack == 8) { 
				MoveCardWasteToFoundation (isTouch);	
			}
		}
		if (CurrentStack < 7 && SelectedStack > 8) {	//SOURCE FOUNDATION, TARGET COLUMN
			MoveCardFoundationToColumn (isTouch);
		}
		*/
	}


	/********* NAVIGATION TOUCH + MOUSE *******************/
	void TouchInputDragStart ()
	{
		//First Reset if we made a keyboard select before or problem with cards in selectstack
		if (SelectedStack == CurrentStack || forcedFoundationCheck) {
			forcedFoundationCheck = false;
			if (SelectedStack == 208) {
				UnSelectSourceCardFromWaste ();
				ResetSelectedCardsToWasteStack ();
			} else if (SelectedStack > 208) { // && foundationList [SelectedStack - 9].CountCards () > 0) {
				UnSelectSourceCardFromFoundation (SelectedStack);
				//StartCoroutine (ResetSelectedCardsToFoundationStack (SelectedStack));
//				CPResetSelectedCardsToFoundationStack (SelectedStack);
			} else if (SelectedStack < 10) {
				UnSelectSourceCards (true);
				//columnList [CurrentStack].UnSelectCard ();
				ResetSelectedCardsToColumn ();
			}
			ResetSelectStack ();
		}

		//Enable the Selectstack again if it's Z is incorrect
//		MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, SELECT_Z);

		Ray ray;
		RaycastHit hit;

		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		currentPosition = Input.mousePosition;
		ray = Camera.main.ScreenPointToRay (Input.mousePosition); //for unity editor
		#else
		currentPosition = Input.GetTouch(0).position;
		ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position); //for touch device
		#endif

		//In AppleTV we get the position from the active card
		#if UNITY_TVOS && !UNITY_EDITOR
		Debug.Log("APPLETV: currentstack="+CurrentStack);
		//MySelectStack.transform.position = new Vector3 (RemotePoint.transform.position.x,RemotePoint.transform.position.y,MySelectStack.transform.position.z);
		/*
		ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(RemotePoint.transform.position));

		if (CurrentStack == 7) 
		{ //Remaining
			Debug.Log("APPLETV: in touchinputdragstart - REMAINING");
			if (m_state != GameState.Animating) 
			{
				MySpotlight.destinationPoint = new Vector3( MyRemainingStack.transform.position.x,MyRemainingStack.transform.position.y,-6.0f);
				StartCoroutine(MoveCardRemainingToWaste(true));
				isclicked=false;
				return;
			}		
		} */
		#endif

		if (Physics.Raycast (ray, out hit)) {	//Something is hit
			endPoint = hit.point;
//			Debug.Log ("SOMETHINGS HIT");

			if (SelectedStack == -1) {
				GameObject go = GameObject.Find (hit.collider.name);

//				Debug.Log ("TRACKING INIT: " + go.name.ToString ());

				if (hit.collider.name.ToString ().Length > 7 && hit.collider.name.ToString ().Substring (0, 8) == "GameCard") {
					int clickedstack = -1;
					int cardorder = -1;

//					Debug.Log ("Something =: " + hit.collider.name.ToString ());
//					Debug.Log ("Somethings parent =" + go.transform.parent.name.ToString ());
					GameCard kc = GameObject.Find (go.transform.name).GetComponent<GameCard> ();
//					Debug.Log ("Somethings stack =" + kc.Definition.Stack.ToString ());

					if (!kc.isAnimating ()) {

						clickedstack = kc.Definition.Stack;

						cardorder = kc.Definition.CardOrder;

						//Flip card manually?
						float temprot = 0;
						if (clickedstack < 10 && clickedstack > -1 && columnList [clickedstack].AutoCardTurn == false) {
							if (columnList [clickedstack].CountCards () > 0) {
								temprot = Mathf.Abs (Mathf.Round (columnList [clickedstack].GetTopCard ().transform.rotation.y));
//								Debug.Log ("-----------------------------------------temprot=" + temprot.ToString ());
								if (!columnList [clickedstack].GetTopCard ().isFlying ()) {
									if (temprot < 3 && temprot > -3) //manually round
										temprot = 0;
								}
							}
						}

						if (clickedstack < 10 && clickedstack > -1 &&
						    columnList [clickedstack].AutoCardTurn == false &&
						    kc.Definition.FaceUp == false && temprot == 0 && kc.Definition.NextCard == -1) { //only flip unflipped, and at bottom
//							Debug.Log ("Unflipped card to flip now");
							//PlayCardSound (columnList [clickedstack].GetTurnCardSound (Random.Range (1, 4)), 0.85f, clickedstack);
							SelectedStack = -1;
							SelectedStackIndex = clickedstack;
							selectedCardState = SelectedCardState.touching;
							//columnList [clickedstack].FlipBottomCard ();


						} else if (clickedstack < 10 && clickedstack > -1 &&
						           columnList [clickedstack].AutoCardTurn == false &&
						           columnList [clickedstack].GetTopCard ().Definition.FaceUp == true &&
						           temprot != 0) {
							if (temprot != 0) {
								//if(Mathf.Abs(Mathf.Round(columnList [clickedstack].GetTopCard ().transform.rotation.y))!=0) {
								if (columnList [clickedstack].CountCards () > 0) {
									PlayCardSound (columnList [clickedstack].GetTurnCardSound (Random.Range (1, 4)), 0.85f, clickedstack);
								}

								SelectedStack = -1;

//								Debug.Log ("Flip card manually - YES");

								GameCard tmpCard = columnList [clickedstack].FlipBottomCard ();
								if (tmpCard != null)
									animatingCards.Add (tmpCard); 
							}
						} else if (clickedstack != 10 && kc.Definition.Clickable == true) {

							bool isValidBelow = true;
							if (clickedstack < 10)
								isValidBelow = columnList [clickedstack].IsValidBelow (kc);

							if (false) { // (clickedstack > 8 && foundationList [clickedstack - 9].CountCards () == 1) { // misol: enable moving aces for now
								; //Debug.Log ("Can't remove Ace!");
							} else if (animatingCards.Count == 0 && iTween.Count() == 0 && isValidBelow) { // last part: don't allow picking up cards under the top one in waste stack

								// misol: This is where you end up when you start touch/dragging any card with its face up (select/foundation/waste)

								if (kc.Definition.CardValue == 1) {
//									iTween.ValueTo (gameObject, iTween.Hash ("from", Color.clear, "to", Color.white, "time", 0.33f, "easetype", "easeInCubic", "onUpdate", "UpdateFoundationOutlineColor"));
//									bFadedFoundationOutline = true;
								}
									
								float yPressPos = kc.transform.localPosition.y;

								PlayCardSound (GetCardUpSound(), 0.9f, clickedstack);

//								Debug.Log ("Before TouchSelectStack: CardOrder=" + cardorder.ToString ());
								TouchSelectStack (clickedstack, cardorder);

								MySelectStack.Originalx = kc.transform.position.x; 
								MySelectStack.Originaly = kc.transform.position.y;
								MySelectStack.Originalz = kc.transform.position.z;

//								MySelectStack.transform.position = new Vector3 (kc.transform.position.x,kc.transform.position.y,kc.transform.position.z);

								liftTimer = LIFT_TIME;

								MySelectStack.PlayLiftAnim ();

//								Debug.Log ("MySelectStack.Originalx=" + MySelectStack.Originalx.ToString ());

								//Prepare stack position directly to avoid jumping card when clicking in corner of card
								if (clickedstack < 10 && selectedCardState != SelectedCardState.touching) {
									MySelectStack.transform.position = new Vector3 (columnList [clickedstack].transform.position.x, columnList [clickedstack].transform.position.y + yPressPos, 3.8f - 0.01f * columnList [clickedstack].CountCards());
								}
								/*
								if (clickedstack == 8) //wastestack
									MySelectStack.transform.position = new Vector3 (MyWasteStack.transform.position.x, MyWasteStack.transform.position.y, 2.5f);
								if (clickedstack > 8) { //foundation stack
									FoundationStack fs = foundationList [clickedstack - 9];
									MySelectStack.transform.position = new Vector3 (fs.transform.position.x, fs.transform.position.y, 2.5f);
								}
								*/
								 
								TouchTracking = true;
							}
						}  
					} else {
//						Debug.Log ("Can't click on animating object!!!!!!!!!!!!!!!!!!!!");
					}
				} else if (hit.collider.name.ToString ().Length >= 10 && hit.collider.name.ToString ().Substring (0, 10) == "WasteStack") { //Bara hit om tom
//					Debug.Log ("Before TouchSelectStack-wastestack");
					TouchSelectStack (8, 0);

					UI.SetActive (false);

				}  
			}
		}
	}


	public string GetCardUpSound ()
	{
		switch (Random.Range(0,2)) {
		case 0:
			return "kortupp1";
		case 1:
			return "kortupp2";
		}
		return "";
	}


	void TouchInputDragStop ()
	{
//		Debug.Log ("Got a mouseclick UP. TARGET!");
		Ray ray2;
		RaycastHit hit2;
		GameCard kc = null;
		int clickedstack = -1;
	
		//Reset nofly to normal in all stack-cards
		MySelectStack.resetToCardFly ();

		#if UNITY_TVOS && !UNITY_EDITOR
		RemotePoint.transform.position = new Vector3(MySelectStack.transform.position.x,MySelectStack.transform.position.y,RemotePoint.transform.position.z);
		#endif

		//First disable the selectstack so the ray doesn't collide with it
//		MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, 1f);
		MySelectStack.gameObject.SetActive(false);

		#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		ray2 = Camera.main.ScreenPointToRay (Input.mousePosition); //for unity editor
//		Debug.Log ("Target: Mouse x-position=" + Input.mousePosition.x.ToString ());
		#else
		ray2 = Camera.main.ScreenPointToRay(Input.GetTouch(0).position); //for touch device
		#endif

		#if UNITY_TVOS && !UNITY_EDITOR
		ray2 = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(RemotePoint.transform.position));
		Debug.Log ("Target: RemotePoint X =" + RemotePoint.transform.position.x.ToString ());
		#endif
		 
		TouchTracking = false;
		if (selectedCardState == SelectedCardState.touching) {
			// misol: This is where column stack (not remaining stack) flips
			PlayCardSound (columnList [SelectedStackIndex].GetTurnCardSound (Random.Range (1, 4)), 0.85f, SelectedStackIndex);
			GameCard tmpCard = columnList [SelectedStackIndex].FlipBottomCard ();
			if (tmpCard != null)
				animatingCards.Add (tmpCard);
			selectedCardState = SelectedCardState.Idle;
		}


		if (Physics.Raycast (ray2, out hit2)) {
			int cardorder = -1;

			//First check what's "hit" (mouse/touch position is over)

			GameObject go = GameObject.Find (hit2.collider.name);

			//Debug.Log("Something length="+hit2.collider.name.ToString().Length);
			if (hit2.collider.name.ToString ().Length > 0) {
//				Debug.Log ("Something part=" + hit2.collider.name.ToString ());
			}

			// Column hit
			if (hit2.collider.name.ToString ().Length > 8 && hit2.collider.name.ToString ().Substring (0, 8) == "GameCard") {
//				Debug.Log ("Something =: " + hit2.collider.name.ToString ());
				//	Debug.Log ("Somethings parent =" + go.transform.parent.name.ToString ());
				kc = GameObject.Find (go.transform.name).GetComponent<GameCard> ();
//				Debug.Log ("Somethings stack =" + kc.Definition.Stack.ToString ());

				clickedstack = kc.Definition.Stack;
				cardorder = kc.Definition.CardOrder;

			}

			// Empty column hit
			else if (hit2.collider.name.ToString ().Length > 5 && hit2.collider.name.ToString ().Substring (0, 6) == "Column") {
//				Debug.Log ("MOUSE: Empty column hit?");
				ColumnStack cs = GameObject.Find (hit2.collider.name).GetComponent<ColumnStack> ();
			 
				clickedstack = cs.Stack;
			}

			//Foundation hit
			else if (hit2.collider.name.ToString ().Length > 9 && hit2.collider.name.ToString ().Substring (0, 10) == "Foundation") {
				FoundationStack fs = GameObject.Find (hit2.collider.name).GetComponent<FoundationStack> ();
				clickedstack = fs.Stack;
			}

			//Empty Remaining stack hit
			else if (hit2.collider.name.ToString ().Length > 8 && hit2.collider.name.ToString ().Substring (0, 9) == "Remaining") {
				clickedstack = 10;
			} 

			//If nothing is hit we need to reset the selected cards to the original stack
			else {
//				Debug.Log ("NOTHING NEW HIT - Reset Select stack");
				if (SelectedStack != -1) { 
					if (SelectedStack < 10) {
						ResetSelectedCardsToColumn ();
					}
					/*
					if (SelectedStack == 8) {
						ResetSelectedCardsToWasteStack ();
					}
					if (SelectedStack > 8 && SelectedStack < 13) {
//						Debug.Log ("About to....ResetSelectedCardsToFoundationStack");
						ResetSelectedCardsToFoundationStack (SelectedStack);	
					} */
					ResetSelectStack ();
				}
			}
 
			//ACTION
			if (clickedstack != -1 && animatingCards.Count == 0 && iTween.Count() == 0) {
				if (SelectedStack == -1 && clickedstack != 10) {
//					Debug.Log ("RESET SELECT STACK");
					ResetSelectStack ();
				} else {
					if (SelectedStack == clickedstack && SelectedStack < 10) {
//						Debug.Log ("SAMMA STACK");
						ResetToBackAnimation ();
						ResetSelectedCardsToColumn ();
						ResetSelectStack ();
/*					} else if (SelectedStack == clickedstack && SelectedStack > 8) {
//						Debug.Log ("SAMMA Foundation. Selectstackkortantal=" + MySelectStack.CountCards ());
						StartCoroutine (ResetSelectedCardsToFoundationStack (clickedstack));
						ResetSelectStack (); */
					} else {
						if ((clickedstack == 10 && SelectedStack != -1) ) // misol: some special cases that don't seem to be covered (releasing a card on the remaining stack / waste on the waste / foundation on foundation)
						{
							ResetToBackAnimation ();
							//Enable the Selectstack again
							#if !UNITY_EDITOR || UNITY_TVOS
							MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, SELECT_Z);
							RemotePoint.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, RemotePoint.transform.position.z);
							#endif
//							CheckForSelectStackProblem ();

						} else {

							if (kc != null) {
								if (kc.Definition.Clickable == true || clickedstack == 10) {
									TouchChangeStack (clickedstack, cardorder);
								} else if (!kc.Definition.Clickable) {
									ResetToBackAnimation ();
								}
							} else {
								TouchChangeStack (clickedstack, cardorder);
							}
						}
					}
				}
			}
		} else {
			if (animatingCards.Count == 0 && iTween.Count() == 0) {


	//			Debug.Log ("NOTHING");
				ResetToBackAnimation ();
				//Enable the Selectstack again
				#if !UNITY_EDITOR || UNITY_TVOS
				MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, SELECT_Z);
				RemotePoint.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, RemotePoint.transform.position.z);
				#endif
	//			CheckForSelectStackProblem ();
			}
		}

		if (bFadedFoundationOutline) {
			iTween.ValueTo (gameObject, iTween.Hash ("from", Color.white, "to", Color.clear, "time", 0.5f, "easetype", "easeInCubic", "onUpdate","UpdateFoundationOutlineColor"));
			bFadedFoundationOutline = false;
		}

		//Enable the Selectstack again
//		MySelectStack.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, SELECT_Z);
		MySelectStack.gameObject.SetActive(true);

		#if !UNITY_EDITOR || UNITY_TVOS
		RemotePoint.transform.position = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, RemotePoint.transform.position.z);
		#endif


		//FIX PROBLEM WITH FOUNDATION CARDS DROPPED ON SOMETHING INVALID
		// MISOL: does not seem like as good idea
	/*	if (MySelectStack.CountCards () > 0 && SelectedStack > 8) {
			Debug.Log ("FOUNDAITONPROBLEMET----------------------------------------- SelectedStack=" + SelectedStack);
			StartCoroutine (ResetSelectedCardsToFoundationStack (SelectedStack));
			ResetSelectStack ();
		} */

		if (MyRemainingStack.CountCards () == 0 && MyWasteStack.CountCards () == 0 && remainingOutlineMaterial.color == Color.white && selectedcardsList.Count == 0) {
			iTween.ValueTo (gameObject, iTween.Hash ("from", Color.white, "to", Color.clear, "time", 0.7f, "easetype", "easeInCubic", "onUpdate", "UpdateRemainingOutlineColor"));
			foundationFX.GetComponent<StartStopParticleSystem> ().SetEnabled (false);
		}

	}

	//TOUCH INPUT - ADD CARD TO SELECT STACK - (FOR TOUCH START DRAG)
	void TouchSelectStack (int in_stack, int in_order)
	{	 
//		Debug.Log ("TouchSelectStack"); 

		ResetSelectStack ();
	
		if (in_stack < 10) {
			if (columnList.Count < in_stack - 1 || in_stack < 0) {
//				Debug.Log ("Why!?");

			}
				
			CurrentStack = in_stack;
			if (columnList [CurrentStack] != null)
				columnList [CurrentStack].SelectCard (false);

			if (in_order == -1)
				in_order = 0;

			if (columnList [CurrentStack].GetCardUsingOrder (in_order) != null) {
				SelectSourceCards (columnList [CurrentStack].GetCardUsingOrder (in_order), true);
				MySelectStack.UnselectAllCards ();
//				Debug.Log ("ADD TO SELECTLIST. Cards in the selectlist:" + selectedcardsList.Count.ToString ());
				SelectedStack = CurrentStack;
			}
		}
/*
		if (in_stack == 8 && MyWasteStack.CountCards () > 0) {	//WASTESTACK
//			Debug.Log ("DRAG FROM WASTESTACK");
			CurrentStack = in_stack;
			SelectSourceCardFromWaste (true);
			MySelectStack.UnselectAllCards ();
			SelectedStack = CurrentStack;
		}
		if (in_stack > 8 && foundationList [in_stack - 9].CountCards () > 0) {	//SELECT FOUNDATION CARD
			CurrentStack = in_stack;
			foundationList [CurrentStack - 9].SelectCard (false);
			SelectSourceCards (foundationList [CurrentStack - 9].GetTopCard (), true);
			MySelectStack.UnselectAllCards ();
//			Debug.Log ("ADD TO SELECTLIST. Cards in the selectlist:" + selectedcardsList.Count.ToString ());
			SelectedStack = CurrentStack;
		}
*/
	}

	//TOUCH INPUT: MOVE CARD TO OTHER STACK
	void TouchChangeStack (int in_stack, int in_cardorder)
	{

		TouchTracking = false;

		if (m_state == GameState.Started) {

			//FIND CURRENT STACK FROM MOUSECLICK
			if (CurrentStack != -1 && CurrentStack < 10) {
				columnList [CurrentStack].ResetCurrentCard ();
			} else if (CurrentStack != -1 && CurrentStack > 8) {
				//foundationList [CurrentStack].ResetCurrentCard ();
			}

//			Debug.Log ("TouchChangeStack:  SelectedStack=" + SelectedStack.ToString ());

			if (SelectedStack == -1) {	
//				Debug.Log ("MOUSE SELECT SOURCE");
				if (in_stack < 10) {
					CurrentStack = in_stack;
					SetCardLight (CurrentStack, null, true);
				}
				if (in_stack == 10) {
					SetCardLight (CurrentStack, null, true);
				}
/*				if (in_stack == 8) {
					SetCardLight (CurrentStack, null, true);
				}
				if (in_stack > 8) {
					CurrentStack = in_stack;
					SetCardLight (CurrentStack, null, true);
				}*/
				CurrentStack = in_stack;

				//Select many cards
				if (in_cardorder != -1 && CurrentStack < 10) {
					columnList [CurrentStack].MoveTo (in_cardorder);
				}


				OnSelectSourceCards (true);

			} else if (selectedcardsList.Count > 0) {
//				Debug.Log ("TouchChangeStack: SELECT TARGET");
				if (in_stack < 10) {
					CurrentStack = in_stack;
					SetCardLight (CurrentStack, null, true);
				}
				if (in_stack == 10) {
					SetCardLight (CurrentStack, null, true);
				}
/*				if (in_stack == 8) {
					SetCardLight (CurrentStack, null, true);
				}
				if (in_stack > 8) {
					CurrentStack = in_stack;
					SetCardLight (CurrentStack, null, true);
				}*/

//				Debug.Log ("Before WhenSelectTargetCards. CurrentStack=" + CurrentStack);
				OnSelectTargetCards (true);
			}

			if (CheckIfGameWon ()) {
				StartCoroutine (GameWon ());
			}
		}
	}

	void FiddleWithSelectedStack ()
	{
		float y = Input.GetAxis ("Vertical");
		float x = Input.GetAxis ("Horizontal");
//		Debug.Log ("X " + x + " Y " + y);
		if (y > 0) {
			y = -1f;
		} else if (y > 0) {
			y = 1f;
		}
		columnList [SelectedStackIndex].GetTopCard ().FlipCard (new Vector3 (x, y, 0.0f));
//		SelectedStackIndex
		//if (diff.x != 0.0f && diff.y != 0.0f) {
//		MySelectStack.FlipCardStack (new Vector3 (x, y, 0.0f));
	}

	void SelectStackUpdateTracking ()
	{
		Vector3 inPoint;

		#if UNITY_EDITOR || UNITY_STANDALONE_OSX && !UNITY_TVOS
		inPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z - 1.71f); //for unity editor
		#else
		inPoint = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, -Camera.main.transform.position.z - 1.71f); //for touch device
		#endif
		#if UNITY_TVOS && !UNITY_EDITOR
		Vector3 inp = Camera.main.WorldToScreenPoint (RemotePoint.transform.position); // will this work? untested
		inPoint = new Vector3(inp.x, inp.y, -Camera.main.transform.position.z - 1.71f);
		#endif

		endPoint = Camera.main.ScreenToWorldPoint (inPoint);
//		endPoint.x -= MySelectStack.Originalx;
//		endPoint.y -= MySelectStack.Originaly;
		endPoint.z = SELECT_Z;

//		endPoint.x = endPoint.x + CompensatePerspectiveX (SelectedStack);

//		Debug.Break ();
		if (MySelectStack.CountCards () > 0) {
//			if (endPoint.x > -12 && endPoint.x < 12 && endPoint.y > -8 && endPoint.y < 8) { // is this really meaningful?

				if (liftTimer < 0)
					MySelectStack.transform.position = endPoint;
				else
					MySelectStack.transform.position = Vector3.Lerp(MySelectStack.transform.position, endPoint, 1 - (liftTimer / LIFT_TIME));

//			}
		}

		endPoint.z = StackLight;
		MySpotlight.destinationPoint = endPoint;
		/*
		if (liftTimer < 0)
			MySpotlight.destinationPoint = endPoint;
		else
			MySpotlight.destinationPoint = Vector3.Lerp(MySpotlight.destinationPoint, endPoint, 1 - (liftTimer / LIFT_TIME));
		*/
	}
		

	void CheckForSelectStackProblem ()
	{
		//Reset if for some reason we have cards left in the selectstack
//		Debug.Log ("CheckForSelectStackProblem. Cards in selectstack:" + MySelectStack.CountCards ());
		if (MySelectStack.CountCards () > 0) {
//			Debug.Log ("WARNING: Cards was stack in selectstack. Reseting them to origin now.");
			if (MySelectStack.CountCards () == 1 && MySelectStack.GetTopCard ().targetStack > 208 && MySelectStack.GetTopCard ().targetStack < 213) { //From foundation
//				Debug.Log ("WARNING: Foundation card stuck!");
				SelectedStack = MySelectStack.GetTopCard ().targetStack;
				CurrentStack = MySelectStack.GetTopCard ().targetStack;
				StartCoroutine (ResetSelectedCardsToFoundationStack (MySelectStack.GetTopCard ().targetStack));
			} else if (MySelectStack.GetTopCard ().sourceStack > -1 && MySelectStack.GetTopCard ().sourceStack < 10) { //From column
				ResetSelectedCardsToColumn ();
			} /*else if (MySelectStack.GetTopCard ().sourceStack == 8) { //From waste
				ResetSelectedCardsToWasteStack ();
			}*/
		}
	}

	//Compensate for perspective when clicking on leftmost or rightmost cards
	float CompensatePerspectiveX (int clickedstack)
	{
		if (clickedstack > -1 && clickedstack < 10) {
			if (clickedstack == 0)
				return 0.25f;
			else if (clickedstack == 1)
				return 0.1f;
			else if (clickedstack == 5)
				return -0.1f;
			else if (clickedstack == 6)
				return -0.25f;
			else
				return 0; 
		} else {
			return 0;
		}
	}



	public string GetCardDownSound() {
		switch (Random.Range(0,2)) {
		case 0:
			return "kortner1";
		case 1:
			return "kortner2";
		}
		return "";
	}

	//--------------MOVE CARD (ANIMATION AND STACK CHANGING)------------------------

	IEnumerator MoveCardRemainingToWaste (bool isTouch)
	{
		bool validMove = true;

		for (int i = 0; i < 10; i++) {
			GameCard tempCard = columnList [i].GetTopCard ();
			if (tempCard == null && !bIsRelaxed)
				validMove = false;
			else if (tempCard != null && tempCard.Definition.FaceUp == false)
				validMove = false;
		}

		
//		Debug.Log ("------------------------------------>Ready to pull card. Left in Remaining=" + MyRemainingStack.CountCards ().ToString ());
		if (validMove && MyRemainingStack.CountCards () > 0) {
			//Sound
			PlayCardSound (MyRemainingStack.GetDrawCardSound (Random.Range (1, 4)), 1.0f, 7, false);

			if (MyRemainingStack.CountCards() == 1) {
				iTween.ValueTo (gameObject, iTween.Hash ("from", Color.clear, "to", Color.white, "time", 0.7f, "easetype", "easeInCubic", "onUpdate","UpdateRemainingOutlineColor"));
				foundationFX.GetComponent<StartStopParticleSystem> ().SetEnabled (true);
			}
				
			Vector3 fromPos = new Vector3 (
				                  MyRemainingStack.GetTopCard ().transform.position.x,
				                  MyRemainingStack.GetTopCard ().transform.position.y,
				                  MyRemainingStack.GetTopCard ().transform.position.z
			);

			for (int i = 9; i >= 0; i--) {
				MyRemainingStack.GetTopCard ().sourceStack = 10;
				MyRemainingStack.GetTopCard ().targetStack = i;


				int currStack = 0;

				Vector3 toPos = new Vector3 (columnList [i].transform.position.x, columnList [i].transform.position.y, columnList [i].transform.position.z);
				if (columnList [i].CountCards () > 0) {
					toPos = new Vector3 (
						columnList [i].GetTopCard ().transform.position.x,
						columnList [i].GetTopCard ().transform.position.y - ParamBetweenShownCardsY,
						columnList [i].GetTopCard ().transform.position.z - 0.01f
					);
				}
					
				columnList [i].AddToStack (MyRemainingStack.GetTopCard ());

				m_state = GameState.Animating;
				AnimateCard (MyRemainingStack.GetTopCard (), fromPos, toPos, false, true, 0.8f);
				//Wait for a second before continuing, 

				MyRemainingStack.RemoveTopCard ();

				yield return new WaitForSeconds (0.1f);
			}

			yield return new WaitForSeconds (0.7f);
			m_state = GameState.Started;

			for (int i = 9; i >= 0; i--) {
				if (columnList [i].HasFinishedSet ()) {
					yield return new WaitForSeconds (0.3f);
					AnimateFinishedSet (i);
					yield return new WaitForSeconds (0.6f);
					RemoveFinishedSet (i);
				}
			}

			  
		} else {	//NO MORE CARDS IN REMAINING STACK
//			PlayCardSound ("blanda1", 0.8f, 7);
/*
			if (MyWasteStack.CountCards () > 3) {
				PlayCardSound ("blanda1", 0.8f, 7);
			} else {
			//	PlayCardSound ("delavand1", 0.8f, 7);
			} */

//			Debug.Log ("End of remaining card 1");
//			if (MyWasteStack.CountCards() > 0)
//				ResetRemainingCardStack ();
//			Debug.Log ("End of remaining card 2");

			if (!validMove) {
				showErrorImage.gameObject.SetActive (true);
				Invoke ("HideErrorImage", 0.1f);
			}

		}
		SelectedStack = -1;
	}

	private void HideErrorImage() {
		showErrorImage.gameObject.SetActive (false);
	}

	void MoveCardWasteToFoundation (bool isTouch)
	{
		if (foundationList [CurrentStack - 9].IsCardValid (selectedcardsList [0]) && selectedcardsList.Count == 1) {
			
			foundationList [CurrentStack - 9].AddToStack (selectedcardsList [0]);
//			Debug.Log ("cards in this foundation:" + foundationList [CurrentStack - 9].CountCards ().ToString ());

			//Animate it to correct place
			Vector3 toPos = new Vector3 (
				                foundationList [CurrentStack - 9].GetTopCard ().transform.position.x,
				                foundationList [CurrentStack - 9].GetTopCard ().transform.position.y,
				                foundationList [CurrentStack - 9].GetTopCard ().transform.position.z - 0.01f
			                );

			//Vector3 fromPos = new Vector3 (selectedcardsList [0].transform.position.x, selectedcardsList [0].transform.position.y, ParamStartStackCardsZ);
			Vector3 fromPos = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, ParamStartStackCardsZ);
			if (MyWasteStack.CountCards () > 0) {
				fromPos = new Vector3 (MyWasteStack.transform.position.x, MyWasteStack.transform.position.y, -0.5f);
			}
			if (isTouch && fromPos.z > 0) { //When mouse or touch
				fromPos.z = -4;
			}

			if (isTouch) { //misol: AnimateCard will override fromPos anyway. Set it to the same value used there
				fromPos = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, -4);
			}

			selectedcardsList [0].sourceStack = 8;
			selectedcardsList [0].targetStack = CurrentStack;
			foundationList [CurrentStack - 9].GetTopCard ().SetToFoundation ();

			AnimateCard (foundationList [CurrentStack - 9].GetTopCard (), fromPos, toPos, isTouch, false, 0.45f);
			foundationList [CurrentStack - 9].GetTopCard ().transform.position = fromPos;

			//Sound
//			PlayCardSound (GetCardDownSound(), 1.0f, CurrentStack);

			//Do FX
			int cardValue =  foundationList [CurrentStack - 9].GetTopCard ().Definition.CardValue;
			bool pitch = false;
			if (cardValue > 2)
				pitch = true;
			PlayFoundationFX (CurrentStack - 8, cardValue, pitch);
			 
			selectedcardsList.RemoveAt (0);

			SelectedStack = -1;
		} else {
//			Debug.Log ("---------------->MoveCardWasteToFoundation Invalid card");	//OK
			ResetToBackAnimation ();
			Invoke ("MoveCardWasteToFoundationDelayed", 0.4f);
		}
	}
	private void MoveCardWasteToFoundationDelayed() {
		ResetSelectedCardsToWasteStack ();
		ResetSelectStack ();
	}

	void MoveCardWasteToColumn (bool isTouch)
	{
	
		if (columnList [CurrentStack].IsCardValid (selectedcardsList [0].Definition.CardValue, selectedcardsList [0].Definition.CardColor)) {
			//Sound
//			PlayCardSound (columnList [CurrentStack].GetDrawCardSound (Random.Range (1, 4)), 1.0f, CurrentStack);
			PlayCardSound (GetCardDownSound(), 0.75f, CurrentStack);

			selectedcardsList [0].Definition.Stack = CurrentStack;

			//Animate it to correct place
			Vector3 toPos = new Vector3 (columnList [CurrentStack].transform.position.x, columnList [CurrentStack].transform.position.y, columnList [CurrentStack].transform.position.z);//ParamStartStackCardsZ);
			if (columnList [CurrentStack].CountCards () > 0) {
				toPos = new Vector3 (
					columnList [CurrentStack].GetTopCard ().transform.position.x,
					columnList [CurrentStack].GetTopCard ().transform.position.y - ParamBetweenShownCardsY,
					columnList [CurrentStack].GetTopCard ().transform.position.z - 0.01f
				);
			}

			//Vector3 fromPos = new Vector3 (MyWasteStack.transform.position.x, MyWasteStack.transform.position.y, -1.0f);
			Vector3 fromPos = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, -1.0f);
			if (isTouch) {  
				fromPos.z = -2;
			}

			selectedcardsList [0].sourceStack = 8;
			selectedcardsList [0].targetStack = CurrentStack;

			columnList [CurrentStack].AddToStack (selectedcardsList [0]);
		 
			selectedcardsList.RemoveAt (0);
			AnimateCard (columnList [CurrentStack].GetTopCard(), fromPos, toPos, isTouch, false, 0.45f);

//			Debug.Log ("------------------------------------------WASTE CARD USED. CARDS LEFT WASTE STACKr=" + MyWasteStack.CountCards ().ToString ());

			SelectedStack = -1;
		} else {
//			Debug.Log ("---------------->MoveCardWasteToColumn Invalid card");
			ResetToBackAnimation ();
			Invoke ("MoveCardWasteToColumnDelayed", 0.4f);
		}
	}
	private void MoveCardWasteToColumnDelayed() {
		ResetSelectedCardsToWasteStack ();
		ResetSelectStack ();
	}

	int nofFinish = 13;
	private void AnimateFinishedSet(int columnIndex) {
		int index = columnList [columnIndex].CountCards () - 1, i;

		for (i = index; i > index - nofFinish; i--) {
			columnList [columnIndex].SetCurrentCard (i);
			columnList [columnIndex].GetCard (i).DisableShadows ();
		}
			
		for (i = index; i > index - nofFinish; i--) {
			columnList [columnIndex].SetCurrentCard (i);

			Vector3 toPos = Vector3.zero;

			switch (Random.Range (0, 4)) {
			case 0:
				toPos = Camera.main.ViewportToWorldPoint (new Vector3 (-0.2f, Random.Range(-0.1f, 1.1f), -Camera.main.transform.position.z - columnList [0].gameObject.transform.position.z));
				break;
			case 1:
				toPos = Camera.main.ViewportToWorldPoint (new Vector3 (1.2f, Random.Range(-0.1f, 1.1f), -Camera.main.transform.position.z - columnList [0].gameObject.transform.position.z));
				break;
			case 2:
				toPos = Camera.main.ViewportToWorldPoint (new Vector3 (Random.Range(-0.1f, 1.1f), -0.2f, -Camera.main.transform.position.z - columnList [0].gameObject.transform.position.z));
				break;
			case 3:
				toPos = Camera.main.ViewportToWorldPoint (new Vector3 (Random.Range(-0.1f, 1.1f), 1.2f, -Camera.main.transform.position.z - columnList [0].gameObject.transform.position.z));
				break;
			}

			columnList[columnIndex].GetCard(i).doDrop = false;
			AnimateCard (columnList [columnIndex].GetCard(i), columnList [columnIndex].GetCard(i).transform.position, toPos, isTouch, false, 0.55f, false, 0);
			columnList [columnIndex].GetCard (i).PlayVictoryAnim ();
			columnList[columnIndex].GetCard(i).doDrop = true;
		}
	}

	private void RemoveFinishedSet(int columnIndex) {
		int index = columnList [columnIndex].CountCards () - 1, i;

		for (i = index; i > index - nofFinish; i--) {
			GameCard gc = columnList [columnIndex].GetTopCard ();
			columnList [columnIndex].RemoveCardFromStack(columnList [columnIndex].CountCards () - 1);
			gc.transform.parent = null;
			iTween.Stop (gc.gameObject);
			GameObject.Destroy (gc.gameObject);
		}
		columnList [columnIndex].OrganizeStack (false, true, false);

		animatingCards.Clear ();

		if (CheckIfGameWon ()) {
			StartCoroutine (GameWon ());
		}
	}



	IEnumerator MoveCardColumnToColumn (bool isTouch)
	{

//		Debug.Log ("MoveCardColumnToColumn. Targetcol=" + CurrentStack);

		if (selectedcardsList.Count > 0) {

			if (columnList [CurrentStack].IsCardValid (selectedcardsList [0].Definition.CardValue, selectedcardsList [0].Definition.CardColor)) {

				bool emptyStack = false;
				int selstackmemory = SelectedStack;

				//Sound 1
//				PlayCardSound (columnList [CurrentStack].GetDrawCardSound (Random.Range (1, 4)), 0.55f, CurrentStack);
				PlayCardSound (GetCardDownSound(), 0.75f, CurrentStack);

				//Animate it to correct place
				Vector3 toPos = new Vector3 (columnList [CurrentStack].transform.position.x, columnList [CurrentStack].transform.position.y, columnList [CurrentStack].transform.position.z);// -0.11f);
				ColumnStack stack = columnList [CurrentStack];
				stack.OrganizeStack (false, isTouch, true);
				if (columnList [CurrentStack].CountCards () > 0) {
					toPos = new Vector3 (
						columnList [CurrentStack].GetTopCard ().transform.position.x,
						columnList [CurrentStack].GetTopCard ().transform.position.y,
						columnList [CurrentStack].GetTopCard ().transform.position.z - 0.01f							//VARFÖR? Ska toPos bara innehålla förändringen? Fungerar iaf
					);
				}

				Vector3 fromPos = new Vector3 (selectedcardsList [0].transform.position.x, selectedcardsList [0].transform.position.y, MySelectStack.transform.position.z);
				if (columnList [SelectedStack].CountCards () > 0) {
					fromPos = new Vector3 (
						columnList [SelectedStack].GetTopCard ().transform.position.x,
						columnList [SelectedStack].GetTopCard ().transform.position.y,
						columnList [SelectedStack].GetTopCard ().transform.position.z
					);
				}

				if (columnList [CurrentStack].CountCards () == 0)
					emptyStack = true;
				float previousZ = 0.0f;
				int i = 0;
				while (selectedcardsList.Count > 0) {
					columnList [CurrentStack].AddToStack (selectedcardsList [0]);
					if (i != 0) {
						toPos.z = previousZ - ParamBetweenCardsZ*2; // Mathf.Round((toPos.z - 0.01f) * 100f) / 100f; 	
					}
					previousZ = toPos.z;
					i++;
					if (!emptyStack) {
						toPos.y = toPos.y - ParamBetweenShownCardsY;
					}
					emptyStack = false;

					selectedcardsList [0].sourceStack = SelectedStack;
					selectedcardsList [0].targetStack = CurrentStack;
					selectedcardsList [0].isTouch = isTouch;
				 
					AnimateCard (selectedcardsList [0], fromPos, toPos, isTouch, false, 0.45f, false, (i-1)*-0.1f);
			
					columnList [SelectedStack].RemoveChildCard (columnList [SelectedStack].CountCards () - 1);
					selectedcardsList.RemoveAt (0);
				}

				yield return new WaitForSeconds (0.5f);
				if (columnList [CurrentStack].HasFinishedSet ()) {
					yield return new WaitForSeconds (0.3f);
					AnimateFinishedSet (CurrentStack);
					yield return new WaitForSeconds (0.5f);
					RemoveFinishedSet (CurrentStack);
				}

				 
				//Sound 2
				if (columnList [selstackmemory].CountCards () > 0 && !isTouch) {
					yield return new WaitForSeconds (1.0f); //Wait 1.0 for a second before continuing. Do not change time since it's connected to another sound
					PlayCardSound (columnList [CurrentStack].GetTurnCardSound (Random.Range (1, 4)), 0.85f, CurrentStack);
				}
			 
				SelectedStack = -1;
			} else {	//If card is not valid reset stack
//				Debug.Log ("--------------->MoveCardColumnToColumn Invalid card"); //OK
				ResetToBackAnimation ();
				ResetSelectedCardsToColumn ();
				ResetSelectStack ();
			}
		}
	}

	IEnumerator MoveCardColumnToFoundation (bool isTouch)
	{

		if (selectedcardsList.Count > 0) {
			if (foundationList [CurrentStack - 9].IsCardValid (selectedcardsList [0]) && selectedcardsList.Count == 1) {
				int selstackmemory = SelectedStack;

				Vector3 fromPos = new Vector3 (selectedcardsList [0].transform.position.x, selectedcardsList [0].transform.position.y, selectedcardsList [0].transform.position.z);
				if (columnList [SelectedStack].CountCards () > 0) {

//					Debug.Log ("columnList [SelectedStack].CountCards=" + columnList [SelectedStack].CountCards ().ToString ());

					fromPos = new Vector3 (
						columnList [SelectedStack].GetTopCard ().transform.position.x,
						columnList [SelectedStack].GetTopCard ().transform.position.y,
						columnList [SelectedStack].GetTopCard ().transform.position.z
					);
				}
				if (isTouch && fromPos.z > 0) { //When mouse or touch
					fromPos.z = -2.0f;
				}

				foundationList [CurrentStack - 9].AddToStack (selectedcardsList [0]);
//				Debug.Log ("cards in this foundation:" + foundationList [CurrentStack - 9].CountCards ().ToString ());
				FoundationStack fs = foundationList [CurrentStack - 9];
				GameCard card = fs.GetTopCard ();
				if (card != null) {
					
					Vector3 cardPos = fs.transform.position;
					//Animate it to correct place
					Vector3 toPos = new Vector3 (
						                cardPos.x,
						                cardPos.y,
						card.transform.position.z - 0.01f
//						                cardPos.z - 0.01f	//Detta fungerar!

					                );
			 
					selectedcardsList [0].sourceStack = SelectedStack;
					selectedcardsList [0].targetStack = CurrentStack;
					selectedcardsList [0].isTouch = isTouch;

					card.SetToFoundation ();

					card.transform.position = fromPos;
					AnimateCard (card, fromPos, toPos, isTouch, false, 0.35f);

					//Sound 1
//					PlayCardSound (GetCardDownSound(), 1.0f, CurrentStack);

					selectedcardsList.RemoveAt (0);

					yield return new WaitForSeconds (0.2f);

					//Do FX
					bool pitch = false;
					if (card.Definition.CardValue > 2)
						pitch = true;
					PlayFoundationFX (CurrentStack - 8, card.Definition.CardValue, pitch);

					//Sound 2
					if (columnList [selstackmemory].CountCards () > 0 && !isTouch) {
						yield return new WaitForSeconds (0.7f); //Wait 1 sec
						PlayCardSound (columnList [selstackmemory].GetTurnCardSound (Random.Range (1, 4)), 0.85f, CurrentStack);
					}
			 
					SelectedStack = -1;
				} else {	//If card is not valid reset stack
//					Debug.Log ("---------------->MoveCardColumnToFoundation Invalid card");
					ResetToBackAnimation ();
					ResetSelectedCardsToColumn ();
					ResetSelectStack ();
				}
			} else {
				ResetToBackAnimation ();
				ResetSelectedCardsToColumn ();
				ResetSelectStack ();
			}
		}
	}


	void ResetToBackAnimation() {
		if (liftTimer < LIFT_TIME - 0.3f) {
			GameCard card = MySelectStack.GetTopCard ();
			if (card != null) {
				PlayCardSound (GetCardDownSound (), 0.66f, CurrentStack);
			}
		}
		MySelectStack.resetToBackAnimation ();
	}


	private bool forcedFoundationCheck = false;

	void MoveCardFoundationToColumn (bool isTouch)
	{

//		Debug.Log ("MoveCardFoundationToColumn");

		if (columnList [CurrentStack].IsCardValid (selectedcardsList [0].Definition.CardValue, selectedcardsList [0].Definition.CardColor) && foundationList [SelectedStack - 9].CountCards () > 0) {
			//Sound
//			PlayCardSound (columnList [CurrentStack].GetDrawCardSound (Random.Range (1, 4)), 1.0f, CurrentStack);
			PlayCardSound (GetCardDownSound(), 0.75f, CurrentStack);

			selectedcardsList [0].Definition.Stack = CurrentStack;

			//Animate it to correct place
			Vector3 toPos = new Vector3 (columnList [CurrentStack].transform.position.x, columnList [CurrentStack].transform.position.y, ParamStartStackCardsZ);
			if (columnList [CurrentStack].CountCards () > 0) {
				toPos = new Vector3 (
					columnList [CurrentStack].GetTopCard ().transform.position.x,
					columnList [CurrentStack].GetTopCard ().transform.position.y - ParamBetweenShownCardsY,
					columnList [CurrentStack].GetTopCard ().transform.position.z - 0.01f
				);
			}

			Vector3	fromPos = new Vector3 (
				                  foundationList [SelectedStack - 9].GetTopCard ().transform.position.x,
				                  foundationList [SelectedStack - 9].GetTopCard ().transform.position.y,
				                  foundationList [SelectedStack - 9].GetTopCard ().transform.position.z
			                  );
			if (isTouch) {  
				fromPos.z = -2;
			}

			selectedcardsList [0].sourceStack = SelectedStack;
			selectedcardsList [0].targetStack = CurrentStack;
			selectedcardsList [0].isTouch = isTouch;

			columnList [CurrentStack].AddToStack (selectedcardsList [0]);
		 
			selectedcardsList.RemoveAt (0);

			AnimateCard (columnList [CurrentStack].GetTopCard(), fromPos, toPos, isTouch, false, 0.55f);

			SelectedStack = -1;
		} else {
//			Debug.Log ("---------------->MoveCardWasteToColumn Invalid card");
			ResetToBackAnimation ();
//			StartCoroutine (ResetSelectedCardsToFoundationStack (SelectedStack));
//			ResetSelectStack ();
			forcedFoundationCheck = true;
		}
	}




	void AnimateCard (GameCard in_card, Vector3 fromPos, Vector3 toPos, bool isTouch, bool flip, float flytime, bool backflip=false, float zAdd=0)
	{

		if (isTouch) { //If touch always animate from selectstack
 			fromPos = new Vector3 (MySelectStack.transform.position.x, MySelectStack.transform.position.y, -1f); //Fly from this position
			fromPos.z += zAdd;
		}
		animatingCards.Add (in_card);
	
		in_card.SetFlyTarget (fromPos, toPos, flytime, flip, backflip);
	}

	void AnimateSelectedCardToColumn (GameCard in_card)
	{
//		Debug.Log ("AnimateSelectedCardToColumn");

		animatingCards.Add (in_card); 

		in_card.SetFlyTarget (new Vector3 (in_card.transform.position.x, in_card.transform.position.y, -2.0f), in_card.GetFlySource (), 0.4f, false);
	}

	void PlayFoundationFX(int which, int in_cardvalue, bool pitch)
	{
		PlayFoundationSoundFX (which, in_cardvalue, pitch);
		//FX
		GameObject.Find ("GlowFX").transform.parent = GameObject.Find ("FoundationOutline" + which.ToString ()).transform;
		GameObject.Find ("GlowFX").transform.position = new Vector3 (GameObject.Find ("FoundationOutline" + which.ToString ()).transform.position.x,
			GameObject.Find ("GlowFX").transform.position.y, 
			GameObject.Find ("GlowFX").transform.position.z);
		CardFX1.Play ();
			
	

		if (in_cardvalue == 13) {
			GameObject.Find ("SeriesFX").transform.parent = GameObject.Find ("FoundationOutline" + which.ToString ()).transform;
			GameObject.Find ("SeriesFX").transform.position = new Vector3 (GameObject.Find ("FoundationOutline" + which.ToString ()).transform.position.x,
				GameObject.Find ("SeriesFX").transform.position.y, 
				GameObject.Find ("SeriesFX").transform.position.z);
			CardFX2.Play ();
		}

	}


	int [] myFoundationPicks = new int[] {12,12,12,12};
	int myFoundationPickCounter = 12;
	void PlayWinningAnimation ()
	{
		GameCard c;

		if (myFoundationPickCounter < 0)
			return;

		if (myFoundationPickCounter == 12 - 3)
			OnEndAnimFinished ();

//		Debug.Log ("Play Winning Animation");

		int index;
		do {
			index = Random.Range (0,4);
		} while(myFoundationPicks[index] != myFoundationPickCounter);
		c = foundationList [index].GetCard (myFoundationPicks[index]);
		if (c != null) {
			c.setWinning ();
			iTween.MoveTo (c.gameObject, new Vector3 ((3 - index) * -3 + 3, 0, -10), 2);
			myFoundationPicks [index]--;
			bool allPicked = true;
			for (int i = 0; i < 4; i++) {
				if (myFoundationPicks [i] == myFoundationPickCounter)
					allPicked = false;
			}
			if (allPicked)
				myFoundationPickCounter--;
		}
	}

	/********** MOVE INPUT FROM KEYBOARD ***********/

	void OnMoveRight ()
	{
		Debug.Log ("Key - Move Right");

//		Invoke ("testWin", 0.1f); // test winning animation, for debugging


		if (m_state == GameState.Started) {

			CurrentStack++;

			#if !UNITY_TVOS
			if (CurrentStack == 13) {
				CurrentStack = 0;
			}
			#endif
			#if UNITY_TVOS
			if (CurrentStack == 13) {
				CurrentStack = 12;
			}
			if (CurrentStack == 7) { //Can't move there from here
				CurrentStack = 6;	
			}
			#endif

			if (CurrentStack < 7) {	//In columnstacks
				//columnList[CurrentStack].ResetCurrentCard();
				columnList [CurrentStack].SetCurrentCard (columnList [CurrentStack].CountCards () - 1);
				SetCardLight (CurrentStack, columnList [CurrentStack].GetCurrentCard (), false);
			}
			if (CurrentStack > 8) {	//In Foundationstacks
				SetCardLight (CurrentStack, null, false);
			}
			if (CurrentStack == 7 || CurrentStack == 8) {
				SetCardLight (CurrentStack, null, false);
			}
		}

//		Debug.Log ("CurrentStack=" + CurrentStack.ToString ());
//		if (CurrentStack < 7)
//			Debug.Log ("Cards here=" + columnList [CurrentStack].CountCards ());
	}

	void OnMoveLeft ()
	{
		Debug.Log ("Key - Move Left");
		if (m_state == GameState.Started) {

			CurrentStack--;

			#if !UNITY_TVOS
			if (CurrentStack == -1) {
				CurrentStack = 12;
			}
			#endif

			#if UNITY_TVOS
			if (CurrentStack == -1) {
				CurrentStack = 0;
			}
			if (CurrentStack == 6) {	//Can't move there from here
				CurrentStack = 7;
			}
			#endif

			if (CurrentStack < 7) {	//In columnstacks
				columnList [CurrentStack].SetCurrentCard (columnList [CurrentStack].CountCards () - 1);
				SetCardLight (CurrentStack, columnList [CurrentStack].GetCurrentCard (), false);
			}
			if (CurrentStack > 8) {	//In Foundationstacks
				SetCardLight (CurrentStack, null, false);
			}
			if (CurrentStack == 7 || CurrentStack == 8) {
				SetCardLight (CurrentStack, null, false);
			}
		}
//		Debug.Log ("CurrentStack=" + CurrentStack.ToString ());
//		if (CurrentStack < 7)
//			Debug.Log ("Cards here=" + columnList [CurrentStack].CountCards ());
		
	}

	void OnMoveDown ()
	{ 
		Debug.Log ("Key - Move Down");
		if (m_state == GameState.Started) {
			if (CurrentStack < 7) {	//In columnstacks
				columnList [CurrentStack].MoveDown ();
			} else {
				switch (CurrentStack) {
				case 7:
					CurrentStack = 0;
					break;
				case 8:
					CurrentStack = 1;
					break;
				case 9:
					CurrentStack = 3;
					break;
				case 10:
					CurrentStack = 4;
					break;
				case 11:
					CurrentStack = 5;
					break;
				case 12:
					CurrentStack = 6;
					break;
 
				}
				if (columnList [CurrentStack].CountCards () > 0) { //After moving to a column last card is current
					columnList [CurrentStack].SetCurrentCard (columnList [CurrentStack].CountCards () - 1);
				}
			}
			SetCardLight (CurrentStack, columnList [CurrentStack].GetTopCard (), false);
		}
	}

	void OnMoveUp ()
	{ 
		Debug.Log ("Key - Move Up. Currentstack=" + CurrentStack);
	

		if (m_state == GameState.Started) {
			if (CurrentStack < 7) {	//In columnstacks
 
				if (!columnList [CurrentStack].isFaceupCardAbove ()) {
					 
					switch (CurrentStack) {
					case 0:
						CurrentStack = 7;
						break;
					case 1:
						CurrentStack = 8;
						break;
					case 2:
						CurrentStack = 8;
						break;
					case 3:
						CurrentStack = 9;	//do some foundation lightup here?
						break;
					case 4:
						CurrentStack = 10;  //do some foundation lightup here?
						break;
					case 5:
						CurrentStack = 11;  //do some foundation lightup here?
						break;
					case 6:
						CurrentStack = 12;  //do some foundation lightup here?
						break;
					}

					SetCardLight (CurrentStack, null, false);
					Debug.Log ("Now Currentstack changed to " + CurrentStack);
				} else {
					columnList [CurrentStack].MoveUp ();
					SetCardLight (CurrentStack, columnList [CurrentStack].GetCurrentCard (), false);
				}

			}
		}
	}


	void SelectSourceCards (GameCard incard, bool isTouch)
	{
		int nextcard;
		int cardorder;

		//Nedanstående hindrar att kortet blinkar till från position 0 när det läggs in i selectstack
		if (!isTouch) {
			MySelectStack.Originalx = incard.transform.position.x;
			MySelectStack.Originaly = incard.transform.position.y;
			MySelectStack.Originalz = incard.transform.position.z;
			MySelectStack.transform.position = new Vector3 (MySelectStack.Originalx, MySelectStack.Originaly, MySelectStack.Originalz);
		}

		nextcard = incard.Definition.NextCard;

		selectedcardsList.Add (incard);

		//add first card to visual stack
		MySelectStack.AddToStack (incard, isTouch); 

		if (nextcard != -1) {
			cardorder = incard.Definition.CardOrder;
		
			//More cards to select?
			while (nextcard != -1) {
//				Debug.Log ("##CurrentStack=" + CurrentStack.ToString ());
//				Debug.Log ("##Cards in column=" + columnList [CurrentStack].CountCards ().ToString ());
//				Debug.Log ("##nextcard=" + nextcard.ToString ());
//				Debug.Log ("##cardorder=" + cardorder.ToString ());
				cardorder++;

				nextcard = columnList [CurrentStack].GetCardUsingOrder (cardorder).Definition.NextCard;

				MySelectStack.AddToStack (columnList [CurrentStack].GetCardUsingOrder (cardorder), isTouch);
 
				selectedcardsList.Add (columnList [CurrentStack].GetCardUsingOrder (cardorder));

				columnList [CurrentStack].SelectChild (cardorder);
			}
		}

/*		if (CurrentStack > 8) {
			foundationList [CurrentStack - 9].RemoveTopCardFromFoundation ();	
		}*/
		MySelectStack.OrganizeCards (isTouch);
	}

	void UnSelectSourceCards (bool isTouch)
	{ 
//		Debug.Log ("##UnSelectSourceCards");
		if (selectedcardsList.Count > 0) {
			while (selectedcardsList.Count > 0) {
				columnList [CurrentStack].AddToStack (selectedcardsList [0]);
				selectedcardsList.RemoveAt (0);
			}

			columnList [CurrentStack].OrganizeStack (false, isTouch, true);	

			while (MySelectStack.CountCards () > 0)
				MySelectStack.RemoveTopCard ();
		}
		SelectedStack = -1;
	}

	void SelectSourceCardFromWaste (bool isTouch)
	{
		
//		Debug.Log ("WASTE CARD SELECTED/ADDED TO SELECT STACK.");

		if (!isTouch) {
			MySelectStack.Originalx = MyWasteStack.transform.position.x;
			MySelectStack.Originaly = MyWasteStack.transform.position.y;
			MySelectStack.Originalz = MyWasteStack.transform.position.z;
			MySelectStack.transform.position = new Vector3 (MySelectStack.Originalx, MySelectStack.Originaly, MySelectStack.Originalz);
		}

		selectedcardsList.Add (MyWasteStack.GetTopCard ());

		//add to visual stack
		MySelectStack.AddToStack (MyWasteStack.GetTopCard (), isTouch); 
		MyWasteStack.RemoveCardFromStack (MyWasteStack.CountCards () - 1);

		MySelectStack.OrganizeCards (isTouch);

	}

	void UnSelectSourceCardFromWaste ()
	{
//		Debug.Log ("SELECT STACK BACK TO WASTE CARD ");
		if (selectedcardsList.Count > 0) {
			MyWasteStack.AddToStack (selectedcardsList [0], CheckIfIsTouch ());
			selectedcardsList.RemoveAt (0);
		}
		MySelectStack.RemoveAllCards ();
		SelectedStack = -1;
	}

	void UnSelectSourceCardFromFoundation (int in_stack)
	{
//		Debug.Log ("SELECT STACK BACK TO FOUNDATION CARD: in_stack= " + in_stack);
		if (selectedcardsList.Count > 0) {
			foundationList [in_stack - 9].AddToStack (selectedcardsList [0]);
			selectedcardsList.RemoveAt (0);
		}
		SelectedStack = -1;
	}



	private void PutWasteInRemaining () {
		for (int i = MyWasteStack.CountCards (); i > 0; i--) {
			GameCard tmpCard = MyWasteStack.GetCard (i - 1);
//			tmpCard.transform.GetChild(0).Rotate(new Vector3 (180, 0, 0));
			MyRemainingStack.AddToStack (tmpCard);
		}

		while (MyWasteStack.CountCards () > 0) {
			MyWasteStack.RemoveTopCard ();
		}

		MyRemainingStack.OrganizeStack ();
	}
	private void ResetRemainingCardStack ()	//Anropas när remaining är slut
	{
//		Debug.Log ("ResetRemainingCardStack");

		iTween.ValueTo (gameObject, iTween.Hash ("from", Color.white, "to", Color.clear, "time", 0.02f, "easetype", "easeInCubic", "onUpdate","UpdateRemainingOutlineColor"));
		foundationFX.GetComponent<StartStopParticleSystem> ().SetEnabled (false);
		MyRemainingStack.FixTargetStack ();


		for (int i = 0; i < MyWasteStack.CountCards (); i++) {
			GameCard tmpCard = MyWasteStack.GetCard (i);
			GameCard tmpCard2 = MyWasteStack.GetCard (MyWasteStack.CountCards()-1 - i);
			AnimateCard (tmpCard, new Vector3(tmpCard.transform.position.x, tmpCard.transform.position.y, tmpCard.transform.position.z + (i != MyWasteStack.CountCards ()-1? 0.2f:0)), new Vector3(MyRemainingStack.transform.position.x, MyRemainingStack.transform.position.y, tmpCard2.transform.position.z), false, true, 0.92f, true);
//			AnimateCard (tmpCard, tmpCard.transform.position, new Vector3(MyRemainingStack.transform.position.x, MyRemainingStack.transform.position.y, tmpCard.transform.position.z), false, true, 0.8f, true);
		}

		Invoke ("PutWasteInRemaining", 0.97f);
	}
	 


	private void MoveAllToFoundation() {
		int cnt = 0;

		for (int i = 0; i < MyRemainingStack.CountCards (); i++) {
			GameCard tmpCard = MyRemainingStack.GetCard (i);
			tmpCard.transform.position = Vector3.zero;
			foundationList[cnt/ 13].AddToStack (tmpCard, true);
			tmpCard.gameObject.transform.position = foundationList [cnt / 13].transform.position;
			tmpCard.transform.GetChild(0).Rotate(new Vector3 (180, 0, 0));
			cnt++;
		}

		for (int j = 0; j < 10; j++) {
			for (int i = 0; i < columnList [j].CountCards (); i++) {
//				Debug.Log ("AA: " + j + "  " + i);
				columnList [j].SetCurrentCard (i);
				GameCard tmpCard = columnList [j].GetCard (i);
				tmpCard.transform.position = Vector3.zero;
				foundationList [cnt / 13].AddToStack (tmpCard, true);
				tmpCard.gameObject.transform.position = foundationList [cnt / 13].transform.position;
				tmpCard.transform.GetChild(0).Rotate(new Vector3 (180, 0, 0));

				cnt++;
			}
		}


		while (MyRemainingStack.CountCards () > 0) {
			MyRemainingStack.RemoveTopCard ();
		}

		for (int j = 0; j < 10; j++) {
			while (columnList[j].CountCards () > 0) {
				columnList[j].RemoveCardFromStack (0);
			}
		}			
	}

	/********** SETUP STUFF **********/

	void StartKlondike ()
	{
		ShuffleColumnCards ();
		SetupColumnList ();	
		SetupFoundationStacks ();
		SetupOtherStacks ();
		ModifyColumnSpacing();
		ShuffleRemainingCards ();
		SelectedStack = -1;
		SetCardLight (10, MyRemainingStack.GetTopCard (), true);
		CurrentStack = 10;
	}
		

	public void SwitchCardSet() {

		PlayClick (!bUseAlternativeSet);

		bUseAlternativeSet = !bUseAlternativeSet;

		for (int i = 0; i < generatedCards.Count; i++) {
			MeshRenderer mr = generatedCards[i].GetComponentInChildren<MeshRenderer> ();
			if (mr != null) {
				Material[] materials = new Material[2];
				materials [0] = cardMaterials [0];
				if (nofSuits == 1) {
					materials [1] = cardMaterials [i % 13 + 1];
					materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 0) % 13 + 13] : cardSetImages [(i - 0) % 13 + 13];
				} else if (nofSuits == 2) {
					materials [1] = cardMaterials [i % 13 + 1 + ((i-0)/52)*13];
					materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 0) % 13 + ((i-0)/52)*13] : cardSetImages [(i - 0) % 13 + ((i-0)/52)*13];
				} else if (nofSuits == 3) {
					materials [1] = cardMaterials [i % 13 + 1 + ((i-0)/39)*13];
					materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 0) % 13 + ((i-0)/39)*13] : cardSetImages [(i - 0) % 13 + ((i-0)/39)*13];
				} else if (nofSuits == 4) {
					materials [1] = cardMaterials [i % 13 + 1 + ((i-0)/26)*13];
					materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 0) % 13 + ((i-0)/26)*13] : cardSetImages [(i - 0) % 13 + ((i-0)/26)*13];
				}
				mr.materials = materials;
			}
		}

		SetButtonState (ToggleSetButton, !bUseAlternativeSet);
		SavePreferences ();
	}


	void ShuffleColumnCards ()
	{
		const int NOF_CARDS = 52 * 2; // 52
		int slump;
		List<GameCard> tempcardList = new List<GameCard> ();

		GameObject card1 = GameObject.Find ("GameCard1");
		if (card1 == null) {
			for (int i = 1; i <= NOF_CARDS; i++) {
				GameObject go = Instantiate (gameCardPrefab) as GameObject;
				go.name = "GameCard" + i.ToString ();
				go.transform.SetParent(MyRemainingStack.transform, false);
				MeshRenderer mr = go.GetComponentInChildren<MeshRenderer> ();
				if (mr != null) {
					Material [] materials = new Material[2];
					materials [0] = cardMaterials [0];
					if (nofSuits == 1) {
						materials [1] = cardMaterials [i % 13 + 1];
						materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 1) % 13 + 13] : cardSetImages [(i - 1) % 13 + 13];
					} else if (nofSuits == 2) {
						materials [1] = cardMaterials [i % 13 + 1 + ((i-1)/52)*13];
						materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 1) % 13 + ((i-1)/52)*13] : cardSetImages [(i - 1) % 13 + ((i-1)/52)*13];
					} else if (nofSuits == 3) {
						materials [1] = cardMaterials [i % 13 + 1 + ((i-1)/39)*13];
						materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 1) % 13 + ((i-1)/39)*13] : cardSetImages [(i - 1) % 13 + ((i-1)/39)*13];
					} else if (nofSuits == 4) {
						materials [1] = cardMaterials [i % 13 + 1 + ((i-1)/26)*13];
						materials [1].mainTexture = bUseAlternativeSet ? altCardSetImages [(i - 1) % 13 + ((i-1)/26)*13] : cardSetImages [(i - 1) % 13 + ((i-1)/26)*13];
					}
					mr.materials = materials;
				}

				generatedCards.Add(go);

				GameCard gc = go.GetComponent<GameCard> ();
				gc.Definition.CardValue = ((i - 1) % 13) + 1;
				if (nofSuits == 1)
					gc.Definition.CardColor = 1;
				else if (nofSuits == 2)
					gc.Definition.CardColor = (i-1) / 52;
				else if (nofSuits == 3)
					gc.Definition.CardColor = (i-1) / 39;
				else if (nofSuits == 4)
					gc.Definition.CardColor = (i-1) / 26;
			}
		}


		//Clean first
		while (startcardsList.Count > 0) {
			startcardsList.RemoveAt (0);
		}

		for (int i = 1; i <= NOF_CARDS; i++) {
			//tempcardList.Add (GameObject.Find ("PlayCard" + i.ToString ()).GetComponent<Card> ());
			tempcardList.Add (GameObject.Find ("GameCard" + i.ToString ()).GetComponent<GameCard> ());
		}

		for (int rep = 0; rep < 10; rep++) { // (Failed, it seems) attempt to increase quality of randomness by repeating shuffle
			while (tempcardList.Count > 0) {
				slump = Random.Range (0, tempcardList.Count);
				startcardsList.Add (tempcardList [slump]);
				tempcardList.RemoveAt (slump);
			}

			for (int i = 0; i < NOF_CARDS; i++) {
				tempcardList.Add (startcardsList[i]);
			}
		}
	}

	void ShuffleRemainingCards ()
	{
		MyRemainingStack.ShuffleCards ();
	}

	void SetupColumnListReset ()
	{
		for (int i = 0; i < 10; i++) {
			columnList.Add (GameObject.Find ("ColumnStack" + i.ToString ()).GetComponent<ColumnStack> ());
			columnList [i].rotationType = Random.Range (1, 7);
			columnList [i].InitGameObjects ();
			columnList [i].RemoveAllCards ();
			FillColumnStackReset (i, i + 1);
		}
	}

	void FillColumnStackReset (int in_stack, int maxcards)
	{
		int[] serie = { 0, 1, 3, 6, 10, 15, 21 };
		GameCard tempCard;

		columnList [in_stack].RemoveAllCards ();
		tempCard = startcardsList [serie [in_stack]];

		for (int i = 0; i < maxcards; i++) {
			tempCard = startcardsList [serie [in_stack] + i];

			tempCard.Definition.CardOrder = i;
			tempCard.Definition.PrevCard = i - 1;
			tempCard.Definition.NextCard = i + 1;
			tempCard.Definition.Stack = in_stack;
			tempCard.Definition.FaceUp = false;

			tempCard.SetNoFly ();

			columnList [in_stack].AddToStack (tempCard);
		}

		//Last card in Column
		tempCard.Definition.FaceUp = false;
		tempCard.Definition.NextCard = -1;
		tempCard.Definition.Clickable = true;

//		columnList [in_stack].OrganizeStackRestart ();
		columnList [in_stack].OrganizeStack (true, true, true);

	}


	void SetupColumnList ()
	{
		for (int i = 0; i < 10; i++) {
			columnList.Add (GameObject.Find ("ColumnStack" + i.ToString ()).GetComponent<ColumnStack> ());
			columnList [i].rotationType = Random.Range (1, 7);
			columnList [i].InitGameObjects ();
//			FillColumnStack (i, i + 1);
			FillColumnStack (i, i < 4? 6 : 5);
			if (isAppleTV)
				columnList [i].AutoCardTurn = true;
		}
	}


	void FillColumnStack (int in_stack, int maxcards)
	{
		int[] serie = { 0, 6*1, 6*2, 6*3, 6*4, 6*4+5*1, 6*4+5*2, 6*4+5*3, 6*4+5*4, 6*4+5*5 };
		GameCard tempCard;

		tempCard = startcardsList [serie [in_stack]];

		for (int i = 0; i < maxcards; i++) {
			tempCard = startcardsList [serie [in_stack] + i];

			tempCard.Definition.CardOrder = i;
			tempCard.Definition.PrevCard = i - 1;
			tempCard.Definition.NextCard = i + 1;
			tempCard.Definition.Stack = in_stack;
			tempCard.Definition.FaceUp = false;
	  
			columnList [in_stack].AddToStack (tempCard);
		}

		//Last card in Column
		tempCard.Definition.FaceUp = false;
		tempCard.Definition.NextCard = -1;
		tempCard.Definition.Clickable = true;

		columnList [in_stack].OrganizeStack (true, false, true);
 
	}


	void SetupFoundationStacks ()
	{
		FoundationStack fstack = null;

		for (int i = 9; i < 13; i++) {
			fstack = GameObject.Find ("FoundationStack" + (i - 8).ToString ()).GetComponent<FoundationStack> ();
			fstack.SetStack (i);
			fstack.MyParent = fstack.transform;
			fstack.RemoveAllCards ();
			foundationList.Add (fstack);
		}
/*
		GameObject card1 = GameObject.Find ("RameCard1");
		if (card1 == null) {
			for (int i = 1; i <= 52; i++) { // 52
				GameObject go = Instantiate (gameCardPrefab) as GameObject;
				go.name = "GameCard" + i.ToString ();
				go.transform.SetParent(fstack.transform, false);
				MeshRenderer mr = go.GetComponentInChildren<MeshRenderer> ();
				if (mr != null) {
					Material [] materials = new Material[2];
					materials [0] = cardMaterials [0];
					materials [1] = cardMaterials [i];
					mr.materials = materials;
				}

				GameCard gc = go.GetComponent<GameCard> ();
				gc.Definition.CardValue = ((i - 1) % 13) + 1;
				gc.Definition.CardColor = (i - 1) / 13;
			}
		} */

	}

	void SetupOtherStacks ()
	{

		//MyRemainingStack = GameObject.Find ("RemainingStack").GetComponent<RemainingStack> ();
		MyRemainingStack.MyParent = MyRemainingStack.transform;
		MyRemainingStack.OrganizeStack ();

		//MyWasteStack = GameObject.Find ("WasteStack").GetComponent<WasteStack> ();
		MyWasteStack.MyParent = MyWasteStack.transform;

		//MySelectStack = GameObject.Find ("SelectStack").GetComponent<SelectStack> ();
		MySelectStack.MyParent = MySelectStack.transform;
	}

	void PutAllCardsInRemainingStack (bool removefirst)
	{
		GameCard c;
		 
		if (removefirst) {
			MyRemainingStack.RemoveAllCards ();
		}

		for (int i = 0; i < 10; i++) {
			for (int j = 0; j < columnList [i].CountCards (); j++) {
				c = columnList [i].GetWinningCard (j);
				c.setNotWinning ();
				c.SetNoFly ();
				c.transform.position = new Vector3 (0, 0, 0);
				MyRemainingStack.AddToStack (c);
				c.transform.localPosition = new Vector3 (0, 0, 0);
				c.transform.localRotation = Quaternion.Euler (0, 180, 0);
				c.targetStack = -1;
				c.sourceStack = -1;
				c.Definition.FaceUp = false;
			}
		}
		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < foundationList [i].CountCards (); j++) {
				c = foundationList [i].GetCard (j);
				c.setNotWinning ();
				c.SetNoFly ();
				c.transform.position = new Vector3 (0, 0, 0);
				MyRemainingStack.AddToStack (c);
				c.transform.localPosition = new Vector3 (0, 0, 0);
				c.transform.localRotation = Quaternion.Euler (0, 180, 0);
				c.targetStack = -1;
				c.sourceStack = -1;
				c.Definition.FaceUp = false;
			}
		}
		while (MyWasteStack.CountCards () > 0) {
			c = MyWasteStack.GetTopCard ();
			c.setNotWinning ();
			c.SetNoFly ();
			c.transform.position = new Vector3 (0, 0, 0);
			MyRemainingStack.AddToStack (c);
			c.transform.localPosition = new Vector3 (0, 0, 0);
			c.transform.localRotation = Quaternion.Euler (0, 180, 0);
			c.targetStack = -1;
			c.sourceStack = -1;
			c.Definition.FaceUp = false;
			MyWasteStack.RemoveTopCard ();
		}
	}

	/********  SPOTLIGHT STUFF	********/

	private void SwitchOnCardLight ()
	{
		GameObject light = GameObject.Find ("CardSpotlight");
		light.SetActive (true);
	}

	private void SwitchOffCardLight ()
	{
		GameObject light = GameObject.Find ("CardSpotlight");
		light.SetActive (false);
	}

	private void SetCardLight (int in_stack, GameCard in_card, bool isTouch)
	{
		float x = 0;
		float y = 0;
		float z = StackLight;

		if (in_card != null && in_stack < 10) {	//Columncards
			x = in_card.transform.position.x;
			y = in_card.transform.position.y;
		} else if (in_stack != -1) {
			if (in_stack < 10) {	//column stacks
				x = GameObject.Find ("ColumnStack" + (in_stack).ToString ()).GetComponent<ColumnStack> ().transform.position.x;
				y = GameObject.Find ("ColumnStack" + (in_stack).ToString ()).GetComponent<ColumnStack> ().transform.position.y;
			}
			if (in_stack == 10) {
				x = MyRemainingStack.transform.position.x;
				y = MyRemainingStack.transform.position.y;
			}/*
			if (in_stack == 8) {
				x = MyWasteStack.transform.position.x;
				y = MyWasteStack.transform.position.y;
			}
			if (in_stack > 8) {
				x = GameObject.Find ("FoundationStack" + (in_stack - 8).ToString ()).GetComponent<FoundationStack> ().transform.position.x;
				y = GameObject.Find ("FoundationStack" + (in_stack - 8).ToString ()).GetComponent<FoundationStack> ().transform.position.y;
			}*/

		}

		//Animate it
		Vector3 endPos = new Vector3 (x, y, z); 
		//cardspot.GetComponentInParent<CardSpotlight> ().destinationPoint = endPos;

		MySpotlight.destinationPoint = endPos;
		#if UNITY_TVOS
		iTween.MoveTo (MySelectStack.gameObject, new Vector3 (endPos.x, endPos.y, -1f), 0.5f);
			

		#endif
		  
		if (isTouch)
			MySpotlight.GetComponent<Light> ().spotAngle = 93f; //Touch has less pronounced spotlight angle
		else {
			if (isAppleTV) {
				MySpotlight.GetComponent<Light> ().range = 80f;
				MySpotlight.GetComponent<Light> ().spotAngle = 39f; //50
				MySpotlight.GetComponent<Light> ().intensity = 0.23f; //0.15 is original
			} else {
				MySpotlight.GetComponent<Light> ().spotAngle = 93; 
				MySpotlight.GetComponent<Light> ().intensity = 1.93f; //0.15 is original
			}
		}
	}

	bool CheckIfGameWon ()
	{
		bool allGone = true;
		for (int i = 0; i < 10; i++) {
			if (!columnList [i].IsStackEmpty()) {
				allGone = false;
				break;
			}
		}
		return allGone;


//		if (foundationList [0].IsFoundationFull () && foundationList [1].IsFoundationFull () && foundationList [2].IsFoundationFull () && foundationList [3].IsFoundationFull ()) {
//			return true;
//		}
/*
		if (MyWasteStack.CountCards () == 0 && MyRemainingStack.CountCards () == 0) {
			bool goodToGo = true;
			for (int i = 0; i < 7; i++) {
				if (columnList [i].AnyHiddenCardsLeft ()) {
					goodToGo = false;
					break;
				}
			}
			if (goodToGo) {
				return true;
			}
		}
*/
		return false;
	}


	void UpdateBGanimSpeed(float speed)
	{
		bgAnimator.speed = speed;
	}

	IEnumerator GameWon ()
	{
		m_state = GameState.PlayerWins;

		yield return new WaitForSeconds (1.1f);

		directionalLight.transform.rotation = Quaternion.Euler (35, 0, 0); // change rotation a bit so that shadows don't disappear when we disable objects 

		//iTween.ValueTo (gameObject, iTween.Hash ("from", 1, "to", 10, "time", 4, "easetype", "easeOutCubic", "onUpdate", "UpdateBGanimSpeed"));

//		GetComponentInParent<AudioSource> ().Stop ();
		PlayWinSound ();

		yield return new WaitForSeconds (4f);

		OnEndAnimFinished ();

		/*
		int i = 0;
		do {
			PlayWinningAnimation ();
			yield return new WaitForSeconds (0.4f);
		} while(i++ < 52);
*/
		#if !UNITY_TVOS
		//	this.transform.Find ("MenuSprite").gameObject.SetActive (true);
		#else
		UI.SetActive (true);
		#endif
	}


	//----------------------------------- TOUCH INPUT END -----------------------------

	 
	void ShowMessage (string msg)	//No message when winning right now - change it here
	{
		if (msg == "Player") {
//			PlayerWins.SetActive(true);
		} else {
//			PlayerWins.SetActive(false);
		}
	}


	//SOUND & MUSIC ----------------------------------

	float songWait;
	bool songCheck;

	public AudioSource cardAudioSource;
	private AudioClip cardAudioClip;
	private AudioSource musicAudioSource;
	private AudioClip musicAudioClip;
	private AudioSource foundationAudioSource;
	private AudioClip foundationAudioClip;

	void InitSound ()
	{
		musicAudioSource = GetComponent<AudioSource> ();

		foundationAudioSource = GameObject.Find ("FoundationSoundFX").GetComponent<AudioSource> ();
	}

	void MusicManager ()
	{
		if (songCheck) {
			songWait -= Time.deltaTime; //reverse count
		}
			
		if ((songWait < 0f)) { //here you can check if clip is not playing
//			Debug.Log ("sound is end");
			songCheck = false;
			if (music)
				PlayNewSong ();
		}
	}

	void PlayNewSong ()
	{
		//Debug.Log ("-------------------------->START NEW SONG: " + songNames [songCounter]);

		int songIndex = -1;
		do {
			songIndex = Random.Range (0, ApplicationModel.NOF_SONGS); 
		} while(ApplicationModel.usedSongs [songIndex] != 0);

		ApplicationModel.usedSongs [songIndex] = 1;

		musicLoadIndex = songIndex;

		musicResourceRequest = Resources.LoadAsync<AudioClip> ("Music/" + ApplicationModel.songNames [musicLoadIndex]);
	}

	void StartLoadedSong() {

		//musicAudioClip = (AudioClip)Resources.Load ("Music/" + ApplicationModel.songNames [musicLoadIndex], typeof(AudioClip));
		musicAudioClip = (AudioClip)musicResourceRequest.asset;
		if (musicAudioClip == null) {
			Debug.Log ("Failed to load: " + musicLoadIndex + "  " + ApplicationModel.songNames [musicLoadIndex]);
			return;
		}
		musicAudioSource.volume = 0.2f;
		musicAudioSource.clip = musicAudioClip;
		musicAudioSource.Play ();
		//		songWait = musicAudioClip.length;//set wait to be clip's length
		songWait = musicAudioClip.length + Random.Range(3, 12);//set wait to be clip's length plus a random wait
		songCheck = true;
		ApplicationModel.songCounter++;

		if (ApplicationModel.songCounter == ApplicationModel.NOF_SONGS) {	//loop
			ApplicationModel.songCounter = 0;
			for (int i = 0; i < ApplicationModel.NOF_SONGS; i++)
				ApplicationModel.usedSongs [i] = 0;
		}
	}

	void StartAllSounds ()
	{
//		cardAudioSource.enabled = true;
		PlayNewSong ();
		for (int i = 0; i < MAX_BIRDS; i++)
			birdPlayeRemainingDelay [i] = i * 10 + 1;
	}

	void StopMusic ()
	{
		musicAudioSource.Stop ();
	}

	void StopAllSounds ()
	{
		musicAudioSource.Stop ();
//		cardAudioSource.enabled = false;
		for (int i = 0; i < MAX_BIRDS; i++)
			birdPlayers [i].Stop ();
		
	}

	public void TogggleSFX() {
		PlayClick (!SFX, true);

		SFX = !SFX;
		SetButtonState (MusicSFX,SFX);
		cardAudioSource.enabled = SFX;
		SavePreferences ();
	}
	public void ToggleSound() {
		PlayClick (!music, true);

		music = !music;
		ToggleSound (music);
		SetButtonState (MusicOnButton, music);
		SavePreferences ();
	}
	public void ToggleSound (bool toggle)
	{

	
		if (toggle)
			StartAllSounds ();
		else
			StopAllSounds ();
		
	}


	public void PlayClick(bool bOff = false, bool bForced = false) {
		float randMax = 0.3f;

		if (SFX || bForced) {
			if (!bOff) {
				clickOn_AS.volume = 0.5f;
				clickOn_AS.pitch = Random.Range (1.0f - randMax, 1.0f + randMax);
				clickOn_AS.Play ();
			} else {
				clickOff_AS.volume = 0.5f;
				clickOff_AS.pitch = Random.Range (1.0f - randMax, 1.0f + randMax);
				clickOff_AS.Play ();
			}
		}
	}

	public void PlayUIClick(bool bOff = false) {
		PlayClick (bOff, false);
	}


	public void ToggleMusic (bool toggle)
	{
	
		if (toggle)
			PlayNewSong ();
		else
			StopMusic ();
	}
	public IEnumerator PlayDelay(AudioSource cardAudioSource,float volume, float delay) {
		yield return new WaitForSeconds (delay);
		cardAudioSource.PlayOneShot (cardAudioClip, volume); 
	}
	public void PlayCardSound (string soundname, float volume, int stack, bool delay) {
		if (!SFX)
			return;

		float[] columnpans = { -0.9f, -0.75f, -0.6f, -0.3f, 0.0f, 0.3f, 0.6f, 0.75f, 0.9f, 1f };
		float[] foundationpan = { -0.1f, 0.2f, 0.5f, 0.8f };
		float pan = 0f;

		if (stack < 10) {
			pan = columnpans [stack];
		}
		if (stack == 10) {
			pan = -0.4f;
		}
/*
		if (stack > 8) {
			pan = foundationpan [stack - 9];
		}
*/
		cardAudioClip = (AudioClip)Resources.Load (soundname, typeof(AudioClip));
		cardAudioSource.panStereo = pan;	//-1.0 left to +1.0 right
		if (delay) {
			StartCoroutine (PlayDelay (cardAudioSource, volume, CARD_FX_DELAY));
		} else {
			cardAudioSource.PlayOneShot (cardAudioClip, volume); 
		}
	}
	public void PlayCardSound (string soundname, float volume, int stack)
	{
		PlayCardSound (soundname, volume, stack, true);
	}
			public float CARD_FX_DELAY = 0.0f;
	void  PlayFoundationSoundFX (int which, int in_cardvalue) {
			PlayFoundationSoundFX(which,in_cardvalue, false);
	}
	void PlayFoundationSoundFX (int which, int in_cardvalue, bool pitch)
	{
		if (!SFX)
			return;

		//Sound
		if (in_cardvalue == 1) {
			foundationAudioClip = (AudioClip)Resources.Load ("KlondikeSoundFX-Ace", typeof(AudioClip));
		} else {
			foundationAudioClip = (AudioClip)Resources.Load ("KlondikeSoundFX-Foundation", typeof(AudioClip));
		}
		if (pitch) {
			foundationAudioSource.pitch = 1.0f + (0.076f * in_cardvalue); // 0.076 which means that king that has value 13 will have  a pitch of 2.00 
		} else {
			foundationAudioSource.pitch = 1.0f;
		}
		foundationAudioSource.panStereo = -0.5f + (which / 5);
		// foundationAudioSource.PlayOneShot (foundationAudioClip, 0.5f); 
		foundationAudioSource.PlayOneShot (foundationAudioClip, 0.3f); 
		//foundationAudioSource.PlayOneShot (foundationAudioClip, in_cardvalue == 1? 0.28f :  0.5f); // ace sounds is loud and invasive at 0.5

	}

	void PlayWinSound ()
	{
		cardAudioClip = (AudioClip)Resources.Load ("KlondikeSoundFX-Win", typeof(AudioClip));
		cardAudioSource.PlayOneShot (cardAudioClip, 0.8f); 
	}
		
}