using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.DualShock;
using UnityEditor.Rendering;

public class DialogueManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterInfo
    {
        public string name;
        public Sprite icon;
        public AudioClip voice;

        public CharacterInfo(string _name = "", Sprite _icon = null, AudioClip _voice = null)
        {
            name = _name;
            icon = _icon;
            voice = _voice;
        }
    }
    [Serializable]
    public struct DialogueInfo
    {
        [TextAreaAttribute]
        public List<string> lines;
        public List<string> speakingCharacters;
    }
    public static DialogueManager Instance { get; private set; }
    public Animator dialogueBox;
    public TMP_Text dialogueName;
    public TMP_Text dialogueText;
    public Image dialogueIcon;

    public List<CharacterInfo> characterDatabase;
    public List<DialogueInfo> currentDialogueInfo;
    private int currentLine;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    public void RunDialogue(DialogueInfo dialogueInfo)
    {
        currentDialogueInfo.Add(dialogueInfo);
        
        if (currentDialogueInfo.Count <= 1)
        {
            currentLine = 0;
            DisplayNextSentence();
        }
    }
    void DisplayNextSentence() 
    {
        if (currentLine >= currentDialogueInfo[0].lines.Count)
        {
            currentDialogueInfo.RemoveAt(0);
            if (currentDialogueInfo.Count >= 1)
            {
                currentLine = 0;
                DisplayNextSentence();
            }
            else
            {
                dialogueBox.SetBool("Active", false);
                Invoke("EndDialogue", 1);
            }
            return;
        }
        RectTransform textRect = dialogueText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, 32);
        textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, -16);

        CharacterInfo currentCharacter = new();
        for (int i = 0; i < characterDatabase.Count; i++)
        {
            if (characterDatabase[i].name != currentDialogueInfo[0].speakingCharacters[currentLine]) continue;
            currentCharacter = characterDatabase[i];
        }
        dialogueName.text = currentCharacter.name;
        dialogueIcon.sprite = currentCharacter.icon;
        dialogueIcon.gameObject.SetActive(currentCharacter.icon != null);
        dialogueText.text = "";

        StopCoroutine(TypeSentence());
        StartCoroutine(TypeSentence());
        
    }

    string ParseString(string text)
    {
        string moveText = "WASD";
        string jumpText = "Space";
        if (Gamepad.current != null)
            moveText = "<sprite=\"ControllerButtons\" index=0>";

        if (Gamepad.current is DualShockGamepad)
            moveText = "<sprite=\"ControllerButtons\" index=3>";
        else if (Gamepad.current != null)
            moveText = "<sprite=\"ControllerButtons\" index=2>";

        text = text.Replace("[InputActions.Gameplay.Move]", moveText);
        text = text.Replace("[InputActions.Gameplay.Jump]", jumpText);
        return text;
    }

    IEnumerator TypeSentence()
    {
        if (currentLine <= 0)
        {
            dialogueBox.SetBool("Active", true);
            yield return new WaitForSecondsRealtime(0.66f);
        }

        dialogueText.text = "";
        string finalLine = ParseString(currentDialogueInfo[0].lines[currentLine]);
        bool inTag = false;
        foreach (char letter in finalLine.ToCharArray())
        {
            dialogueText.text += letter;

            if (letter == '<')
                inTag = true;
            else if (letter == '>')
                inTag = false;

            //audioManager.Play(dialogueStored.audioClips[currentline-1]);
            if (!inTag)
            {
                while (PauseMenu.Paused)
                {
                    yield return null;
                }
                for (float i = 0; i <= 0.033; i+=Time.unscaledDeltaTime)
                {
                    yield return null;
                }

                if (dialogueText.isTextOverflowing)
                {
                    RectTransform textRect = dialogueText.GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textRect.sizeDelta.y + 16);
                    textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, textRect.anchoredPosition.y + 8);
                }
            }
        }
        //audioManager.Stop("Voice");
        yield return new WaitForSecondsRealtime(2);
        yield return null;

        currentLine++;
        DisplayNextSentence();
    }
    // Update is called once per frame
    void EndDialogue()
    {
        StopAllCoroutines();
        currentLine = 0;
    }   
}