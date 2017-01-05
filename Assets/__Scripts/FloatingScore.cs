using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// an enum to track the possible states of a floating score
public enum FSState {
	IDLE,
	PRE,
	ACTIVE,
	POST
}

// FloatingScore can move itself on screen following a Bezier curve
public class FloatingScore : MonoBehaviour {

	public FSState state = FSState.IDLE;

	[SerializeField]
	private int _score = 0;		// the score field
	public string scoreString;

	//the Score property also sets scoreString when set
	public int score {
		get {
			return _score;
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber (_score);
			GetComponent<GUIText> ().text = scoreString;
		}
	}

	public List<Vector3> bezierPts;		// bezier points for movement
	public List<float> fontSizes;		// bezier points for font scaling
	public float timeStart = -1f;
	public float timeDuration = 1f;
	public string easingCurve = Easing.InOut;	// use easing in Utils.cs

	// the gameobject that will recieve the sendmessage when this is done moving
	public GameObject reportFinishTo = null;

	// set up the floatingScore and movements
	// use parameter defaults for eTimeS and eTimeD
	public void Init(List<Vector3> ePts, float eTimeS = 0, float eTimeD = 1) {
		bezierPts = new List<Vector3> (ePts);

		// if theres only one point -- go there
		if (ePts.Count == 1) {
			transform.position = ePts[0];
			return;
		}

		// if eTimeS is the default, just start at current time
		if (eTimeS == 0)
			eTimeS = Time.time;

		timeStart = eTimeS;
		timeDuration = eTimeD;

		state = FSState.PRE;	// pre state, ready to start moving
	}

	// When this callbackis called by SendMessage
	// add the score from the calling floating score
	public void FSCallback(FloatingScore fs) {
		score += fs.score;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		// if not moving, just return
		if (state == FSState.IDLE)
			return;

		// get u from the current time and duration
		// u ranges from 0 to 1 (usually)
		float u = (Time.time - timeStart) / timeDuration;

		// use easing class form Utils to curve the u value
		float uC = Easing.Ease (u, easingCurve);
		if (u < 0) {
			// dont move yet
			state = FSState.PRE;
			transform.position = bezierPts [0];
		} else {
			// if u >= 1, were done
			if (u >= 1) {
				uC = 1;			// set uC = 1 so we dont overshoot
				state = FSState.POST;

				// if theres a callback object use sendmessage to call the callback method
				if (reportFinishTo != null) {
					reportFinishTo.SendMessage("FSCallback", this);
					Destroy(gameObject);

				// if theres nothing to callback, dont destroy just stay still
				} else {
					state = FSState.IDLE;
				}

			// 0 <= u < 1 means were still active and moving
			} else {
				state = FSState.ACTIVE;
			}

			// move to the right point
			Vector3 pos = Utils.Bezier(uC, bezierPts);
			transform.position = pos;

			// if fontSizes has values in it, adjust the fontsize of the GUIText
			if (fontSizes != null && fontSizes.Count > 0) {
				int size = Mathf.RoundToInt(Utils.Bezier(uC, fontSizes));
				GetComponent<GUIText>().fontSize = size;
			}
		}
	}
}
