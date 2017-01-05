using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {

	public string suit;					// suit of the card (C, D, H, or S)
	public int rank;					// rank of the card (1-14)
	public Color color = Color.black;	// color to tint the pips
	public string colS = "Black";		// or "Red"

	// holds all of the decorator gameobjects
	public List<GameObject> decoGOs = new List<GameObject> ();
	// holds all the pip objects
	public List<GameObject> pipGOs = new List<GameObject> ();

	public GameObject back;		// Gameobject of the back of card
	public CardDefinition def;	// parsed from XML

	// list of spriterenderer components for this gameobject and its children
	public SpriteRenderer[] spriteRenderers;

	public bool faceUp {
		get {
			return !back.activeSelf;
		}
		set {
			back.SetActive(!value);
		}
	}

	void Start() {
		SetSortOrder (0);	// ensure that the card starts properly sorted
	}

	// if spriterenderers is not yet defined, define it here
	public void PopulateSpriteRenderers() {
		if (spriteRenderers == null || spriteRenderers.Length == 0)
			spriteRenderers = GetComponentsInChildren<SpriteRenderer> ();
	}

	// set the sortingLayerName on all the spriterenderers Components
	public void SetSortingLayerName(string tSLN) {
		PopulateSpriteRenderers ();

		foreach (SpriteRenderer tSR in spriteRenderers)
			tSR.sortingLayerName = tSLN;
	}

	// set sortingOrder of all sprieRenderer Components
	public void SetSortOrder(int sOrd, bool fuckedup = false) {
		PopulateSpriteRenderers ();

		// white background on bottom
		// then comes pips, decorators, ect
		// back is on top so when visible it covers the rest
		foreach (SpriteRenderer tSR in spriteRenderers) {

			// this is the background
			if (tSR.gameObject == this.gameObject) {
				tSR.sortingOrder = sOrd;
				continue;
			}

			// parse the children by name
			switch (tSR.gameObject.name) {
			case "back":
				tSR.sortingOrder = sOrd + 3;
				break;
			case "Card_Front":
				tSR.sortingOrder = sOrd + 1;
				break;
			case "face":
			default:
				tSR.sortingOrder = sOrd + 2;
				break;
			}
		}
	}

	// Virtual methods can be overriden by subclass methods with the same name
	virtual public void OnMouseUpAsButton() {
		//print(name);  // when clicked, we output the card name
	}
}

// This class stores information about each decorator or 'pip' from DeckXML
[System.Serializable]
public class Decorator {

	public string type;			// for card pips, type = 'pip'
	public Vector3 loc;			// the locaiton of the sprite on the card
	public bool flip = false;	// whether or not to flip the sprite vertically
	public float scale = 1f;	// the scale of the sprite
}

// this class stores information for each 'rank' of card
[System.Serializable]
public class CardDefinition {

	public string face;			// sprite to use for each face card
	public int rank;			// the rank (1-13) of this card

	// because decorators from XML are used the same way on every card
	// pips only stores info about the pips on numbered cards
	public List<Decorator> pips = new List<Decorator> ();

}
