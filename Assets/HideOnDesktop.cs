﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnDesktop : MonoBehaviour {

	// Use this for initialization
	void Start () {
		#if UNITY_STANDALONE_OSX  || UNITY_STANDALONE
		gameObject.SetActive(false);
		#endif

	}

}