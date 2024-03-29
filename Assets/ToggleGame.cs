using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleGame : MonoBehaviour {

	public GameObject stacks;
	public GameObject mainMenu;
	public KlondikeGame game;
	public GameObject zoom;
	public GameObject hamburgerMenu;
	// Use this for initialization
	void Start () {
		
	}

	public void StartGame(int nofSuits) {
		stacks.SetActive (true);
		mainMenu.SetActive (false);
		game.DoStart(nofSuits);
		zoom.SetActive (true);
		hamburgerMenu.SetActive (true);

		ApplicationModel.restartLevel = nofSuits;
	}


	public void StartGame1() {
		StartGame (1);
	}
	public void StartGame2() {
		StartGame (2);
	}
	public void StartGame3() {
		StartGame (3);
	}		
	public void StartGame4() {
		StartGame (4);
	}		

	public void ShowMainMenu() {
		stacks.SetActive (false);
		mainMenu.SetActive (true);
		hamburgerMenu.SetActive (false);
	}

}
