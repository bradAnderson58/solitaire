using UnityEngine;
using System.Collections;
using System.Collections.Generic;


// Scoreboard manages showing the score to the player
public class Scoreboard : MonoBehaviour {
	public static Scoreboard S;

	public GameObject prefabFloatingScore;

	public bool __________________;

	[SerializeField]
	private int _score = 0;
	public string _scoreString;

	// the score property sets scorestring as well
	public int score {
		get {
			return _score;
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber (_score);
			print (_score);
			print (_scoreString);
		}
	}

	// the scoreString property also sets the GUIText
	public string scoreString {
		get {
			return _scoreString;
		}
		set {
			_scoreString = value;
			GetComponent<GUIText> ().text = _scoreString;
		}
	}

	void Awake() {
		S = this;
	}

	// when called by SendMessage, this adds the fs.score to this.score
	public void FSCallback(FloatingScore fs) {
		print ("im called!");
		print (fs.score);
		score += fs.score;
	}

	// this will instantiate a new FloatingScore GameObject and initialize it
	// it also returns a pointer to FloatingScore created so that the calling function
	// can do more to it (like set font sizes)
	public FloatingScore CreateFloatingScore(int amt, List<Vector3> pts) {
		GameObject go = Instantiate (prefabFloatingScore) as GameObject;
		FloatingScore fs = go.GetComponent<FloatingScore> ();
		fs.score = amt;

		// set fs to call back to this
		fs.reportFinishTo = this.gameObject;
		fs.Init (pts);
		return fs;
	}

}
















