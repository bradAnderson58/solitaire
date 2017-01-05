using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// an enum to handle all possible scoring events
public enum ScoreEvent {
	DRAW,
	MINE,
	MINE_GOLD,
	GAME_WIN,
	GAME_LOSS
}

public class Prospector : MonoBehaviour {
	public static Prospector S;
	public static int SCORE_FROM_PREV_ROUND = 0;
	public static int HIGH_SCORE = 0;

	public float reloadDelay = 1f;	// delay between rounds

	public Vector3 fsPosMid = new Vector3 (0.5f, 0.9f, 0);
	public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
	public Vector3 fsPosMid2 = new Vector3(0.5f, 0.5f, 0);
	public Vector3 fsPosEnd = new Vector3(1f, 0.65f, 0);

	public Deck deck;
	public TextAsset deckXML;

	public Layout layout;
	public TextAsset layoutXML;
	public Vector3 layoutCenter;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Transform layoutAnchor;

	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;

	public List<CardProspector> drawPile;

	// field to track score info
	public int chain = 0;		// # of cards in this run
	public int scoreRun = 0;
	public int score = 0;
	public FloatingScore fsRun;

	public GUIText GTGameOver;
	public GUIText GTRoundResult;

	// Use this for initialization
	void Awake () {
		S = this;	// Singleton

		// check for high score in playerPrefs
		if (PlayerPrefs.HasKey("ProspectorHighScore"))
		    HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");

		// add the score from the last round (>0 if win)
		score += SCORE_FROM_PREV_ROUND;
		SCORE_FROM_PREV_ROUND = 0;

		// set up the GUITexts that show at the end of a round
		// Get the GUIText components
		GameObject go = GameObject.Find ("GameOver");
		if (go != null)
			GTGameOver = go.GetComponent<GUIText> ();

		go = GameObject.Find ("RoundResult");
		if (go != null)
			GTRoundResult = go.GetComponent<GUIText> ();

		// make them inivisible
		ShowResultGTs (false);

		go = GameObject.Find ("HighScore");
		string hScore = "High Score: " + Utils.AddCommasToNumber (HIGH_SCORE);
		go.GetComponent<GUIText> ().text = hScore;
	}

	void ShowResultGTs(bool show) {
		GTGameOver.gameObject.SetActive (show);
		GTRoundResult.gameObject.SetActive (show);
	}

	void Start() {
		Scoreboard.S.score = score;

		deck = GetComponent<Deck> ();	// gets the deck
		deck.InitDeck (deckXML.text);	// pass DeckXML to it
		Deck.Shuffle (ref deck.cards);	// shuffle the deck by reference

		layout = GetComponent<Layout> ();		// get the layout
		layout.ReadLayout (layoutXML.text);		// pass LayoutXML to it

		drawPile = ConvertListCardsToListCardProspectors (deck.cards);
		LayoutGame ();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lcd) {
		List<CardProspector> lcp = new List<CardProspector> ();
		CardProspector tCP;
		foreach (Card tCD in lcd) {
			tCP = tCD as CardProspector;
			lcp.Add(tCP);
		}
		return lcp;
	}

	// the Draw function will pull a single card from the drawPile and return it
	CardProspector Draw() {
		CardProspector cd = drawPile [0];		// pull the 0th CardProspector
		drawPile.RemoveAt (0);					// then remove it from the List<> drawPile
		return cd;								// and return it
	}

	// convert from the layout Id int to the CardProspector with that ID
	CardProspector FindCardByLayoutId(int layoutID) {
		foreach (CardProspector tCP in tableau) {
			if (tCP.layoutId == layoutID)
				return tCP;
		}
		return null;
	}

	// position the initial tableau of cards for the start of game
	void LayoutGame() {
		// create an empty GameObject to anchor the tableau
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		// follow the layout outlined by XML
		CardProspector cp;
		foreach (SlotDef tSD in layout.slotDefs) {
			cp = Draw ();				// draw a card
			cp.faceUp = tSD.faceUp;		// set the faceup via the layout
			cp.transform.parent = layoutAnchor;

			// location based on the slot definition
			cp.transform.localPosition = new Vector3(
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerId);
			cp.layoutId = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.TABLEAU;	// all cards in the tableau have this state

			// set sorting layers
			cp.SetSortingLayerName(tSD.layerName);

			tableau.Add(cp);
		}

		// set which cards are hiding others
		foreach (CardProspector tCP in tableau) {
			foreach(int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutId(hid);
				tCP.hiddenBy.Add(cp);
			}
		}

		// set up initial target
		MoveToTarget (Draw ());

		// set up the draw pile
		UpdateDrawPile ();

	}

	// CardClicked is called any time a card in the game is clicked
	public void CardClicked(CardProspector cd) {
		switch (cd.state) {
		case CardState.TARGET:
			// clicking the target does nothing
			break;
		case CardState.DRAWPILE:
			// clicking card from DrawPile will draw the next card
			MoveToDiscard(target);		// moves the target to the discardPile
			MoveToTarget(Draw ());		// Moves the next drawn card
			UpdateDrawPile();			// restack the drawPile
			ScoreManager(ScoreEvent.DRAW);
			break;
		case CardState.TABLEAU:
			// clicking a card in tableau will check if its valid
			bool validMatch = true;

			// if the card is facedown, its not valid
			if (!cd.faceUp)
				validMatch = false;

			// if its not an adjacent rank to target, not valid
			if (!AdjacentRank(cd, target))
				validMatch = false;

			if (!validMatch) return;

			// yay its valid
			tableau.Remove(cd);		// remove from the tablaue list
			MoveToTarget(cd);		// make it the target card
			SetTableauFaces();		// update tableau card faceups
			ScoreManager(ScoreEvent.MINE);
			break;
		}

		// check to see whether the game is over
		CheckForGameOver ();
	}

	// Test for whether the game is over
	void CheckForGameOver() {

		// if the tableau is empty, the game is over (win)
		if (tableau.Count == 0) {
			GameOver(true);
			return;
		}

		// if there are still cards in the draw pile, game is not over
		if (drawPile.Count > 0)
			return;

		// check for remaining valid plays
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank(cd, target)) {
				// if there is a valid play, the game is not over
				return;
			}
		}

		// no valid plays remain, game is over (loss)
		GameOver (false);
	}

	// called when game is over.
	// simple for now but expandable
	void GameOver(bool won) {
		if (won) {
			ScoreManager(ScoreEvent.GAME_WIN);
		} else {
			ScoreManager(ScoreEvent.GAME_LOSS);
		}
		// reload the scene, reset the game
		// wait reloadDelay seconds to let the score travel
		Invoke ("ReloadLevel", reloadDelay);
		//Application.LoadLevel ("__Prospector_Scene_0");
	}

	// reload the level, resetting the game
	void ReloadLevel() {
		Application.LoadLevel ("__Prospector_Scene_0");
	}

	// this turns cards in the Mine faceup or facedown
	void SetTableauFaces() {
		foreach (CardProspector cd in tableau) {
			bool fup = true;

			// if there exists a card in the tableau that hides this card, its facedown
			foreach(CardProspector cover in cd.hiddenBy) {
				if (cover.state == CardState.TABLEAU)
					fup = false;
			}
			cd.faceUp = fup;		// set the value on the card
		}
	}

	// ScoreManager handles all of the scoring
	void ScoreManager(ScoreEvent sEvt) {
		List<Vector3> fsPts;

		switch (sEvt) {
			// same things need to happen whether draw, win, or loss
		case ScoreEvent.DRAW:			// drawing a card
		case ScoreEvent.GAME_WIN:		// won the round
		case ScoreEvent.GAME_LOSS:		// lost the round
			chain = 0;			// resets the score chain
			score += scoreRun;	// add scoreRun to total score
			scoreRun = 0;		// reset scoreRun

			// add fsRun to scoreboard score
			if (fsRun != null) {
				// bezier points
				fsPts = new List<Vector3>();
				fsPts.Add(fsPosRun);
				fsPts.Add(fsPosMid2);
				fsPts.Add(fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init (fsPts, 0, 1);

				// also adjust font size
				fsRun.fontSizes = new List<float>(new float[]{28f, 36f, 4f});
				fsRun = null;	// clear fsRun so we can create it again
			}

			break;
		case ScoreEvent.MINE:			// removed a mine card
			chain++;			// increase score chain
			scoreRun += chain;	// add score for this card to run

			// create FloatingScore for this score and move it from mouse to run
			FloatingScore fs;
			Vector3 p0 = Input.mousePosition;
			p0.x /= Screen.width;
			p0.y /= Screen.height;
			fsPts = new List<Vector3>();
			fsPts.Add(p0);
			fsPts.Add(fsPosMid);
			fsPts.Add(fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore(chain, fsPts);
			fs.fontSizes = new List<float>(new float[] {4f, 50f, 28f});
			if (fsRun == null) {
				fsRun = fs;
				fsRun.reportFinishTo = null;
			}else {
				fs.reportFinishTo = fsRun.gameObject;
			}

			break;
		}

		// a second switch statment to handle wins and losses
		switch (sEvt) {
		case ScoreEvent.GAME_WIN:
			GTGameOver.text = "Round Over";
			// if its a win, add the score to the next round
			// (static fields not reset by Application.LoadLevel)
			Prospector.SCORE_FROM_PREV_ROUND = score;
			print ("you won this round, score: " + score);
			GTRoundResult.text = "You won this round!\nRound Score: " + score;
			ShowResultGTs(true);
			break;
		case ScoreEvent.GAME_LOSS:
			GTGameOver.text = "Game Over";
			//if its a loss, check against high scores
			if (Prospector.HIGH_SCORE <= score) {
				GTRoundResult.text = "You got the high score!\nHigh Score: " + score;
				print ("New High Score! " + score);
				Prospector.HIGH_SCORE = score;
				PlayerPrefs.SetInt ("ProspectorHighScore", score);
			} else {
				print ("Final score: " + score);
				GTRoundResult.text = "Your final score was: " + score;
			}
			ShowResultGTs(true);
			break;
		default:
			print ("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
			break;
		}
	}

	// returns true if the two cards are adjacent in rank (ace king wrap)
	public bool AdjacentRank(CardProspector c0, CardProspector c1) {

		// if either card is facedown, its not adjacent
		if (!c0.faceUp || !c1.faceUp)
			return false;

		// if they are one apart, they are adjacent
		if (Mathf.Abs (c0.rank - c1.rank) == 1)
			return true;

		// if one is A and the other King, also adjacent
		if (c0.rank == 1 && c1.rank == 13)
			return true;
		if (c0.rank == 13 && c1.rank == 1)
			return true;

		return false;
	}

	// moves the current target tot he discardPile
	void MoveToDiscard(CardProspector cd) {
		cd.state = CardState.DISCARD;					// set the state of the card to discard
		discardPile.Add (cd);							// add it to the discard list
		cd.transform.parent = layoutAnchor;				// update its transform parent

		// position it on the discard pile
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerId + 0.5f);

		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);		// place it on the top of the pile for depth sorting
		cd.SetSortOrder (-100 + discardPile.Count);
	}

	// make cd the new target card
	void MoveToTarget(CardProspector cd) {

		// if there is currently a target card, move it to discardPile
		if (target != null)
			MoveToDiscard (target);

		target = cd;									// cd is new target
		cd.state = CardState.TARGET;
		cd.transform.parent = layoutAnchor;

		// move to the target position
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerId);

		cd.faceUp = true;									// make it face up
		cd.SetSortingLayerName (layout.discardPile.layerName);	// set the depth sorting
		cd.SetSortOrder (0);
	}

	// arrange all the cards in the drawPile to show how many are left
	void UpdateDrawPile() {
		CardProspector cd;

		// go through all the cards of the draw pile
		for (int i = 0; i < drawPile.Count; ++i) {
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;
			Vector2 dpStagger = layout.drawPile.stagger;

			// position card correctly with the layout stagger value
			cd.transform.localPosition = new Vector3(
				layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
				-layout.drawPile.layerId + 0.1f * i);

			cd.faceUp = false;					// make them all face-down
			cd.state = CardState.DRAWPILE;

			// set the depth sorting
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder(-10 * i);
		}
	}
}










