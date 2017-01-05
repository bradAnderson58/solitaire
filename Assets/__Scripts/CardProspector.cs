using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// enum for a Cards state
// can be in drawpile, in tableau, in discard, or the currently selected
public enum CardState {
	DRAWPILE,
	TABLEAU,
	TARGET,
	DISCARD
}

// This extends Card, which is reusable for other card games
public class CardProspector : Card {

	public CardState state = CardState.DRAWPILE;
	public List<CardProspector> hiddenBy = new List<CardProspector>();  // which cards keep this one face down
	public int layoutId;		// matches this card to a LayoutXML slot for the tableau
	public SlotDef slotDef;		// stores info pulled in from the LayoutXML slot

	// Dont delete this - it calls its super by default maybe?
	void Start () {
	
	}
	void Update() {

	}

	override public void OnMouseUpAsButton() {
		Prospector.S.CardClicked (this);
		base.OnMouseUpAsButton ();
	}
}
