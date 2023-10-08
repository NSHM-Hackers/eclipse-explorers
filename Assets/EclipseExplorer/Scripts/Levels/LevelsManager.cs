using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Eclipse/Levels/Manager", order: 0)]
public class LevelsManager : MonoBehaviour
{
    public float earthRotateSpeed = 15f / 3600, earthRevolveSpeed = .1f / 3600;
    public float moonRevolveSpeed = 0.003f;

    //These are the references to UI components and containers in the scene
    public GameObject dialogueContainer;
    public GameObject npcContainer;
    public GameObject playerContainer;
    public GameObject itemPopUp;

    public Text npcText;
    public Text npcLabel;
    public Image npcSprite;
    public GameObject playerChoicePrefab;
    public Image playerSprite;
    public Text playerLabel;

    [HideInInspector] public bool dialoguePaused = false; //Custom variable to prevent the manager from calling VD.Next
    [HideInInspector] public bool animatingText = false; //Will help us know when text is currently being animated

    //We'll be using this to store references of the current player choices
    [HideInInspector] public List<PlayerChoice> currentChoices = new List<PlayerChoice>();

    //With this we can start a coroutine and stop it. Used to animate text
    public IEnumerator npcTextAnimator;

    //This uses the returned string[] from nodeData.comments to create the UIs for each comment
    //It first cleans, then it instantiates new choices
    public void SetOptions(string[] choices, Action<int> onChoiceSelect)
    {
        //Create the choices. The prefab comes from a dummy gameobject in the scene
        //This is a generic way of doing it. You could instead have a fixed number of choices referenced.
        for (int i = 0; i < choices.Length; i++)
        {
            GameObject newOp = Instantiate(playerChoicePrefab.gameObject, playerChoicePrefab.transform.position,
                Quaternion.identity) as GameObject;
            newOp.transform.SetParent(playerChoicePrefab.transform.parent, true);
            var choice = newOp.GetComponent<PlayerChoice>();
            newOp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20 - (20 * i));
            newOp.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            choice.TextUI.text = choices[i];
            var i1 = i;
            newOp.GetComponent<Button>().onClick.AddListener(delegate { onChoiceSelect(i1); });
            newOp.SetActive(true);

            currentChoices.Add(newOp.GetComponent<PlayerChoice>());
        }
    }

    public IEnumerator DrawText(string text, float time)
    {
        animatingText = true;

        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (i != words.Length - 1) word += " ";

            string previousText = npcText.text;

            float lastHeight = npcText.preferredHeight;
            npcText.text += word;
            if (npcText.preferredHeight > lastHeight)
            {
                previousText += System.Environment.NewLine;
            }

            for (int j = 0; j < word.Length; j++)
            {
                npcText.text = previousText + word.Substring(0, j + 1);
                yield return new WaitForSeconds(time);
            }
        }

        npcText.text = text;
        animatingText = false;
    }
}