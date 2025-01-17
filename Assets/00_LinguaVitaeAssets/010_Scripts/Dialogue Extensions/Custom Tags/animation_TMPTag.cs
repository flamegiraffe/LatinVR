﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationTag", menuName = "CustomTags/AnimationTag")]
public class animation_TMPTag : CustomTMPTag
{

    public override string tag_name
    {
        get
        {
            return "animation";
        }
    }

    public override bool needs_closing_tag
    {
        get
        {
            return false;
        }
    }

    public override IEnumerator applyToText(TMPro.TextMeshPro text, int startIndex, int length, string param)
    {
        int i;
        for(i = 0; i < text.text.Length; i++)
        {
            if(text.text[i] == ':')
            {
                break;
            }
        }
        string char_name = text.text.Substring(0, i);
        Debug.Log(char_name);
        GameObject.Find("GameManager").GetComponent<GameManager>().DebugPanel.transform.GetChild(1).GetComponent<UnityEngine.UI.Text>().text = "Character: " + char_name + "; Animation: " + param;
        GameObject.Find(char_name).GetComponent<NPC>().PlayAnimation(param);
        yield return new WaitForSeconds(0.0f);
    }
}
