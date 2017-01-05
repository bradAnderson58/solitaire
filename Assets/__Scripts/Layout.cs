using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// slotdef is not a subclass of monobehavior
[System.Serializable]  // this makes slotdef visible to the unity inspector
public class SlotDef {
	public float x;
	public float y;
	public bool faceUp = false;
	public string layerName = "Default";
	public int layerId = 0;
	public int id;
	public List<int> hiddenBy = new List<int>();
	public string type = "slot";
	public Vector2 stagger;
}

public class Layout : MonoBehaviour {

	public PT_XMLReader xmlr;	// just like Deck, we need an xml reader
	public PT_XMLHashtable xml;	// for easier xml access
	public Vector2 multiplier;	// for spacing

	// SlotDEf references
	public List<SlotDef> slotDefs;	// all the SlotDefs for row0 to row3
	public SlotDef drawPile;
	public SlotDef discardPile;

	// holds all the possible names for layers
	public string[] sortingLayerNames = new string[]{"Row0", "Row1", "Row2", "Row3", "Draw", "Discard"};  // NOTE: swapped Draw and Discard
	
	// this function is called to read the layout XML file
	public void ReadLayout(string xmlText) {
		xmlr = new PT_XMLReader ();
		xmlr.Parse (xmlText);
		xml = xmlr.xml ["xml"] [0];		// xml is a shortcut to the XML data

		// read the multiplier which sets card spacing
		multiplier.x = float.Parse(xml["multiplier"][0].att ("x"));
		multiplier.y = float.Parse(xml["multiplier"][0].att ("y"));

		// read in the slots
		SlotDef tSD;
		PT_XMLHashList slotX = xml ["slot"];

		for (int i = 0; i < slotX.Count; ++i) {
			tSD = new SlotDef ();	// create a new SlotDef instance

			if (slotX[i].HasAtt("type"))
				tSD.type = slotX[i].att ("type");
			else
				tSD.type = "slot";

			// various attributes are parsed into numerical values
			tSD.x = float.Parse(slotX[i].att ("x"));
			tSD.y = float.Parse(slotX[i].att ("y"));
			tSD.layerId = int.Parse(slotX[i].att ("layer"));

			// this converts the number of the layerId into a text layerName
			// all the assets are at the same Z depth (for Unity 2d) so the
			// layers are used to differentiate which should be shown on top
			tSD.layerName = sortingLayerNames[tSD.layerId];

			// pull additional attributes based on the type of the slot
			switch(tSD.type) {
			case "slot":
				tSD.faceUp = (slotX[i].att ("faceup") == "1");
				tSD.id = int.Parse(slotX[i].att ("id"));
				if (slotX[i].HasAtt("hiddenby")) {
					string[] hiding = slotX[i].att ("hiddenby").Split (',');
					foreach(string s in hiding)
						tSD.hiddenBy.Add (int.Parse(s));
				}
				slotDefs.Add(tSD);
				break;

			case "drawpile":
				tSD.stagger.x = float.Parse(slotX[i].att("xstagger"));
				drawPile = tSD;
				break;

			case "discardpile":
				discardPile = tSD;
				break;
			}


		}
	}

}
