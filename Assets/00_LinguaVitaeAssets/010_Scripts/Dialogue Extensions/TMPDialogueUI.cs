﻿/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;


/** Note that this is just one way of presenting the
    * dialogue to the user. The only hard requirement
    * is that you provide the RunLine, RunOptions, RunCommand
    * and DialogueComplete coroutines; what they do is up to you.
    */
public class TMPDialogueUI : Yarn.Unity.DialogueUIBehaviour
{
    /// The object that contains the dialogue and the options.
    /** This object will be enabled when conversation starts, and 
        * disabled when it ends.
        */
    public GameObject dialogueContainer;

    /// The UI element that displays lines
    public TMPro.TextMeshPro lineText;

    /// A UI element that appears after lines have finished appearing
    public GameObject continuePrompt;

    /// A delegate (ie a function-stored-in-a-variable) that
    /// we call to tell the dialogue system about what option
    /// the user selected
    private Yarn.OptionChooser SetSelectedOption;

    /// How quickly to show the text, in seconds per character
    [Tooltip("How quickly to show the text, in seconds per character")]
    public float textSpeed = 0.025f;

    /// The buttons that let the user choose an option
    public List<Button> optionButtons;

    /// Make it possible to temporarily disable the controls when
    /// dialogue is active and to restore them when dialogue ends
    public RectTransform gameControlsContainer;

    // This parses and runs custom text tags on Yarn text
    public CustomTagRunner customTagHandler;

    // This is a list of all the color data of each character's 4 vertices
    // in the current line. This is necessary to set alphas correctly.
    private List<Color32> prevColors;

    // This is the component that will allow us to set individual character alphas.
    private TMPro.TMP_Text m_TextComponent;

    private GameManager gameManager;

    void Start()
    {
        customTagHandler = lineText.gameObject.AddComponent<CustomTagRunner>();
        prevColors = new List<Color32>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void Awake ()
    {
        // Start by hiding the container, line and option buttons
        if (dialogueContainer != null)
            dialogueContainer.SetActive(false);

        lineText.gameObject.SetActive (false);

        foreach (var button in optionButtons) {
            button.gameObject.SetActive (false);
        }

        // Hide the continue prompt if it exists
        if (continuePrompt != null)
            continuePrompt.SetActive (false);
    }

    /// Show a line of dialogue, gradually
    public override IEnumerator RunLine (Yarn.Line line)
    {
        lineText.gameObject.SetActive(false);
        lineText.gameObject.SetActive(true);
        // Reset text
        customTagHandler.StopAllCoroutines();
        customTagHandler.clearClones();
        prevColors.Clear();
        lineText.SetText(customTagHandler.ParseForCustomTags(YarnRTFToTMP(line.text)));
        lineText.ForceMeshUpdate();
        customTagHandler.ApplyTagEffects();

        // Set up teletype by setting alpha to 0
        TMPro.TMP_Text m_TextComponent = lineText.GetComponent<TMPro.TMP_Text>();
        TMPro.TMP_TextInfo textInfo = m_TextComponent.textInfo;
        while (textInfo.characterCount == 0)
        {
            yield return new WaitForSeconds(0.25f);
        }
        Color32[] newVertexColors;
        for (int currentCharacter = 0; currentCharacter < textInfo.characterCount; currentCharacter++)
        {
            int materialIndex = textInfo.characterInfo[currentCharacter].materialReferenceIndex;
            newVertexColors = textInfo.meshInfo[materialIndex].colors32;
            int vertexIndex = textInfo.characterInfo[currentCharacter].vertexIndex;
            // Save prev color
            if (textInfo.characterInfo[currentCharacter].isVisible)
            {
                for (int j = 0; j < 4; j++)
                    prevColors.Add(textInfo.meshInfo[materialIndex].colors32[vertexIndex + j]);
            }
            else
            {
                for (int j = 0; j < 4; j++)
                    prevColors.Add(new Color32(0, 0, 0, 0));
            }
            // Set color to transparent
            if (textInfo.characterInfo[currentCharacter].isVisible)
            {
                for (int j = 0; j < 4; j++)
                    newVertexColors[vertexIndex + j] = new Color32(textInfo.meshInfo[materialIndex].colors32[vertexIndex + j].r, textInfo.meshInfo[materialIndex].colors32[vertexIndex + j].g, textInfo.meshInfo[materialIndex].colors32[vertexIndex + j].b, 0);
                m_TextComponent.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
            }
        }

        if (textSpeed > 0.0f) {
            // Display the line one character at a time
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                if (textInfo.characterInfo[i].isVisible)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        newVertexColors[vertexIndex + j] = prevColors[4 * i + j];
                    }
                    m_TextComponent.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
                }
                yield return new WaitForSeconds(textSpeed);
            }
        } else {
            // Display the entire line immediately if textSpeed <= 0
            for (int currentCharacter = 0; currentCharacter < textInfo.characterCount; currentCharacter++)
            {
                int materialIndex = textInfo.characterInfo[currentCharacter].materialReferenceIndex;
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;
                int vertexIndex = textInfo.characterInfo[currentCharacter].vertexIndex;
                // Set color back to the color it is supposed to be
                if (textInfo.characterInfo[currentCharacter].isVisible)
                {
                    for (int j = 0; j < 4; j++)
                        newVertexColors[vertexIndex + j] = prevColors[4 * currentCharacter + j];
                    m_TextComponent.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
                }

            }
        }

        // Reset custom tag runner
        customTagHandler.ClearParsedTags();

        // Show the 'press any key' prompt when done, if we have one
        if (continuePrompt != null)
            continuePrompt.SetActive (true);

        // Wait for trigger press
        while (gameManager.LH_Trigger == false && gameManager.RH_Trigger == false)
        {
            yield return null;
        }

        // Avoid skipping lines if textSpeed == 0
        yield return new WaitForEndOfFrame();

        // Hide the text and prompt
        lineText.gameObject.SetActive (false);

        if (continuePrompt != null)
            continuePrompt.SetActive (false);
    }

    /// Show a list of options, and wait for the player to make a selection.
    public override IEnumerator RunOptions (Yarn.Options optionsCollection, 
                                            Yarn.OptionChooser optionChooser)
    {
        // Do a little bit of safety checking
        if (optionsCollection.options.Count > optionButtons.Count) {
            Debug.LogWarning("There are more options to present than there are" +
                                "buttons to present them in. This will cause problems.");
        }

        // Display each option in a button, and make it visible
        int i = 0;
        foreach (var optionString in optionsCollection.options) {
            optionButtons [i].gameObject.SetActive (true);
            optionButtons [i].GetComponentInChildren<TMPro.TextMeshProUGUI> ().text = optionString;
            i++;
        }

        // Record that we're using it
        SetSelectedOption = optionChooser;

        // Wait until the chooser has been used and then removed (see SetOption below)
        while (SetSelectedOption != null) {
            yield return null;
        }

        // Hide all the buttons
        foreach (var button in optionButtons) {
            button.gameObject.SetActive (false);
        }
    }

    /// Called by buttons to make a selection.
    public void SetOption (int selectedOption)
    {

        // Call the delegate to tell the dialogue system that we've
        // selected an option.
        SetSelectedOption (selectedOption);

        // Now remove the delegate so that the loop in RunOptions will exit
        SetSelectedOption = null; 
    }

    /// Run an internal command.
    public override IEnumerator RunCommand (Yarn.Command command)
    {
        // "Perform" the command
        Debug.Log ("Command: " + command.text);

        yield break;
    }

    /// Called when the dialogue system has started running.
    public override IEnumerator DialogueStarted ()
    {
        Debug.Log ("Dialogue starting!");

        // Enable the dialogue controls.
        if (dialogueContainer != null)
            dialogueContainer.SetActive(true);

        // Hide the game controls.
        if (gameControlsContainer != null) {
            gameControlsContainer.gameObject.SetActive(false);
        }

        yield break;
    }

    /// Called when the dialogue system has finished running.
    public override IEnumerator DialogueComplete ()
    {
        Debug.Log ("Complete!");

        // Hide the dialogue interface.
        if (dialogueContainer != null)
            dialogueContainer.SetActive(false);

        // Show the game controls.
        if (gameControlsContainer != null) {
            gameControlsContainer.gameObject.SetActive(true);
        }

        yield break;
    }

    public string YarnRTFToTMP(string line)
    {
        
        for(int i = 0; i < line.Length-8; i++)
        {
            // Search for any color tags that need a hashtag
            if (line.Substring(i,7) == "<color=" && line.Substring(i,8) != "<color=\"" && line.Substring(i,8) != "<color=#")
            {
                line = line.Substring(0, i) + "<color=#" + line.Substring(i + 7);
            }
            // Search for any alpha tags that need a hashtag
            if (line.Substring(i, 7) == "<alpha=" && line.Substring(i, 8) != "<alpha=#")
            {
                line = line.Substring(0, i) + "<alpha=#" + line.Substring(i + 7);
            }
        }
        
        return line;
    }

}
