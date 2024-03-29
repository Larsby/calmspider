using UnityEngine;
using System.Collections;
using Facebook.Unity;

using UnityEngine.SocialPlatforms;
using UnityEngine.SocialPlatforms.GameCenter;
using System.IO;
using System;

public class Utilities : MonoBehaviour
{
	public string game;
	public string HighScore;

	public void Share()
	{


		Facebook.Unity.FB.FeedShare("",
			new Uri("http://www.pastille.se/"),
			"Get " + game,
			null, null);

	}

	public void AppStore()
	{
		Application.OpenURL("https://itunes.apple.com/us/app/calm-cards-spider-solitaire/id1225609364?ls=1&mt=8");
	}
	public void GooglePlay()
	{
		Application.OpenURL("https://play.google.com/store/apps/details?id=se.pastille.calmcards.spider");
	}
	public void More()
	{
		Application.OpenURL("http://www.pastille.se");
	}


	public void Rate()
	{/*

		UniRate r = GameObject.FindObjectOfType<UniRate> ();
		r.ShowPrompt ();
		*/
#if UNITY_IOS
		AppStore();
#endif
#if UNITY_ANDROID
		GoolgePlay();
#endif
	}
	
	
}