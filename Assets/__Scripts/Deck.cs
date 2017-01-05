using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deck : MonoBehaviour {

	// Suits
	public Sprite suitClub;
	public Sprite suitDiamond;
	public Sprite suitHeart;
	public Sprite suiteSpade;

	public Sprite[] faceSprites;
	public Sprite[] rankSprites;

	public Sprite cardBack;
	public Sprite cardBackGold;
	public Sprite cardFront;
	public Sprite cardFrontGold;

	// prefabs
	public GameObject prefabSprite;
	public GameObject prefabCard;

	public bool __________;
	
	public PT_XMLReader xmlr;
	
	public List<string> cardNames;
	public List<Card> cards;
	public List<Decorator> decorators;
	public List<CardDefinition> cardDefs;
	public Transform deckAnchor;
	public Dictionary<string, Sprite> dictSuits;

	// init the deck
	public void InitDeck(string deckXMLText) {

		// this creates an anchor for all the card gameobjects in the hierarchy
		if (GameObject.Find ("_Deck") == null) {
			GameObject anchorGO = new GameObject("_Deck");
			deckAnchor = anchorGO.transform;
		}

		// init dictionary of SuitSprites
		dictSuits = new Dictionary<string, Sprite> () {
			{"C", suitClub },
			{"D", suitDiamond },
			{"H", suitHeart },
			{"S", suiteSpade }
		};

		ReadDeck (deckXMLText);
		MakeCards ();
	}


	// parses the XML file into CardDefintitions
	public void ReadDeck (string deckXmlText) {
	
		xmlr = new PT_XMLReader ();		// createa new xml reader
		xmlr.Parse (deckXmlText);		// parse DeckXML

		// read decorators for all cards
		decorators = new List<Decorator> ();		// init decorator list

		// grab xml parser for all decorators in XML file
		PT_XMLHashList xDecos = xmlr.xml ["xml"] [0] ["decorator"];
		Decorator deco;
		for (int i = 0; i < xDecos.Count; ++i) {

			// for each decorator in xml make a new Decorator and copy its attributes
			deco = new Decorator();
			deco.type = xDecos[i].att("type");

			// set the bool flip based on the flip attribute (0 or 1)
			deco.flip = (xDecos[i].att ("flip") == "1");

			// floats need to be parsed from the attribute strings
			deco.scale = float.Parse(xDecos[i].att ("scale"));

			// vector3 loc init to 0,0,0 so it just needs to be modified
			deco.loc.x = float.Parse(xDecos[i].att ("x"));
			deco.loc.y = float.Parse(xDecos[i].att ("y"));
			deco.loc.z = float.Parse(xDecos[i].att ("z"));

			// add the temporary deco to the list decorator
			decorators.Add (deco);
		}

		// read pip locations for each card number
		cardDefs = new List<CardDefinition>();
		PT_XMLHashList xCardDefs = xmlr.xml ["xml"] [0] ["card"];
		CardDefinition cDef;

		// for each of the cards, create a new card definition
		for (int i = 0; i < xCardDefs.Count; ++i) {
			cDef = new CardDefinition();
			cDef.rank = int.Parse(xCardDefs[i].att ("rank"));	// parse the rank values

			// grab all the pips on the card and store them
			PT_XMLHashList xPips = xCardDefs[i]["pip"];
			if (xPips != null) {
				for (int j = 0; j < xPips.Count; ++j) {
					deco = new Decorator();		// pips on card are handled by the Decorator class
					deco.type = "pip";
					deco.flip = (xPips[j].att ("flip") == "1");
					deco.loc.x = float.Parse(xPips[j].att ("x"));
					deco.loc.y = float.Parse(xPips[j].att ("y"));
					deco.loc.z = float.Parse(xPips[j].att ("z"));
					if (xPips[j].HasAtt("scale")) {
						deco.scale = float.Parse(xPips[j].att ("scale"));
					}
					cDef.pips.Add(deco);
				}
			}

			// face cards have a face attributes but other cards do not
			if (xCardDefs[i].HasAtt("face"))
				cDef.face = xCardDefs[i].att ("face");
			cardDefs.Add (cDef);
		}
	}

	// get the proper definition based on Rank (1 - 14 is Ace to King)
	public CardDefinition GetCardDefinitionByRank(int rnk) {

		// search through all of the card defintions
		foreach (CardDefinition cd in cardDefs) {
			if (cd.rank == rnk)
				return cd;
		}
		return null;
	}

	// make the card gameobjects
	public void MakeCards() {
		// each suit goes from 1 to 13
		cardNames = new List<string> ();
		string[] letters = new string[] {"C", "D", "H", "S"};
		foreach (string s in letters) {
			for (int i = 1; i <= 13; ++i)
				cardNames.Add (s + i);
		}

		// make a list to hold all the cards
		cards = new List<Card> ();
		GameObject tGO = null;
		SpriteRenderer tSR = null;

		// iterate through all the cards names that were just made
		for (int i = 0; i < cardNames.Count; ++i) {

			// create new card with parents.transform anchor 
			GameObject cgo = Instantiate (prefabCard) as GameObject;
			cgo.transform.parent = deckAnchor;
			Card card = cgo.GetComponent<Card> ();

			// stack the cards so they look nice
			cgo.transform.localPosition = new Vector3 ((i % 13) * 3, i / 13 * 4, 0);

			// assign basic values to cards
			card.name = cardNames [i];
			card.suit = card.name [0].ToString ();
			card.rank = int.Parse (card.name.Substring (1));

			if (card.suit == "D" || card.suit == "H") {
				card.colS = "Red";
				card.color = Color.red;
			}

			// pull the CardDefinition for this card
			card.def = GetCardDefinitionByRank (card.rank);

			// add decorators
			foreach (Decorator deco in decorators) {

				tGO = Instantiate (prefabSprite) as GameObject;
				tSR = tGO.GetComponent<SpriteRenderer> ();

				// set the suit decorator
				if (deco.type == "suit") {
					tSR.sprite = dictSuits [card.suit];

					// set a rank decorator
				} else {
					tSR.sprite = rankSprites [card.rank];
					tSR.color = card.color;
				}

				// make decorators above card
				tSR.sortingOrder = 1;
				tGO.transform.parent = cgo.transform;
				tGO.transform.localPosition = deco.loc;

				// flip decorator if needed
				if (deco.flip)
					tGO.transform.rotation = Quaternion.Euler (0, 0, 180);

				// set scale so deco not too big
				if (deco.scale != 1)
					tGO.transform.localScale = Vector3.one * deco.scale;

				// name object so we can find it
				tGO.name = deco.type;
				card.decoGOs.Add (tGO);
			}

			// add pips
			foreach (Decorator pip in card.def.pips) {

				tGO = Instantiate (prefabSprite) as GameObject;	// instantiate sprite gameobject
				tGO.transform.parent = cgo.transform;			// set theparent transform
				tGO.transform.localPosition = pip.loc;			// set to position specified in XML
				if (pip.flip)
					tGO.transform.rotation = Quaternion.Euler (0, 0, 180);	// flip if needed
				if (pip.scale != 1)
					tGO.transform.localScale = Vector3.one * pip.scale;		// scale if needed (only for ace)

				// name the gamobject
				tGO.name = "pip";
				tSR = tGO.GetComponent<SpriteRenderer> ();		// get the sprite renderer component
				tSR.sprite = dictSuits [card.suit];				// set sprite to proper suite
				tSR.sortingOrder = 1;							// pip rendered above the card front
				card.pipGOs.Add (tGO);
			}

			// handle face cards here
			// non face cards 'face' member is the empty string
			if (card.def.face != "") {
				tGO = Instantiate (prefabSprite) as GameObject;
				tSR = tGO.GetComponent<SpriteRenderer> ();

				// generate the correct name and pass to face
				tSR.sprite = GetFace (card.def.face + card.suit);
				tSR.sortingOrder = 1;
				tGO.transform.parent = card.transform;
				tGO.transform.localPosition = Vector3.zero;
				tGO.name = "face";
			}

			// add a card back (covers everything else on the car)
			tGO = Instantiate(prefabSprite) as GameObject;
			tSR = tGO.GetComponent<SpriteRenderer>();
			tSR.sprite = cardBack;
			tGO.transform.parent = card.transform;
			tGO.transform.localPosition = Vector3.zero;

			// this is the higher sorting order
			tSR.sortingOrder = 2;
			tGO.name = "back";
			card.back = tGO;

			// default to face down
			card.faceUp = false;

			// add card to deck
			cards.Add (card);
		}
	}

	// find the proper face card and sprite
	public Sprite GetFace(string facestring) {
		foreach (Sprite tS in faceSprites) {
			if (tS.name == facestring)
				return tS;
		}
		return null;
	}

	// shuffle the cards in the deck
	static public void Shuffle(ref List<Card> oCards) {

		// create a temp list to hold the new shuffle order
		List<Card> tCards = new List<Card> ();
		int ndx; 		// hold index of card to move

		// for all cards in the original list
		while (oCards.Count > 0) {
			ndx = Random.Range(0, oCards.Count);	// pick random index
			tCards.Add(oCards[ndx]);				// add to temp list
			oCards.RemoveAt(ndx);					// remove from original list
		}

		// replace original with temporary (because oCards is a reference variable)
		oCards = tCards;

	}

}














