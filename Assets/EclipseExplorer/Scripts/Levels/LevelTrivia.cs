using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VIDE_Data;

[AddComponentMenu("Eclipse/Levels/LevelTrivia", order: 0)]
public class LevelTrivia : MonoBehaviour
{
    #region Vars

    //These are the references to UI components and containers in the scene
    public GameObject quizContainer;

    public Text questionText;
    public GameObject playerChoicePrefab;

    //We'll be using this to store references of the current player choices
    [HideInInspector] public List<PlayerChoice> currentChoices = new List<PlayerChoice>();

    private VIDE_Assign _currentQuiz;
    private bool _answeredCorrectly = false;
    private Action _onQuizComplete;

    private bool _runningQuiz = false;

    #endregion

    #region DIALOGUE CONDITIONS

    //DIALOGUE CONDITIONS --------------------------------------------

    //When this returns true, it means that we did something that alters the progression of the dialogue
    //And we don't want to call Next() this time
    bool PreConditions(VIDE_Assign dialogue)
    {
        var data = VD.nodeData;

        if (VD.isActive) //Stuff we check while the dialogue is active
        {
            //Check for extra variables
            if (data.isPlayer)
            {
                var correctChoice = int.Parse(data.extraVars["correctChoice"].ToString());

                if (!_answeredCorrectly)
                {
                    if (data.commentIndex != correctChoice)
                    {
                        currentChoices[data.commentIndex].MarkIncorrect();
                    }
                    else
                    {
                        currentChoices[data.commentIndex].MarkCorrect();
                        _answeredCorrectly = true;
                    }

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Main

    public bool BeginQuiz(VIDE_Assign quizDialogue, Action onQuizComplete)
    {
        playerChoicePrefab.SetActive(false);
        if (VD.isActive)
        {
            return false;
        }

        _currentQuiz = quizDialogue;
        _onQuizComplete = onQuizComplete;
        Interact(quizDialogue);
        _runningQuiz = true;
        return true;
    }

    //This begins the dialogue and progresses through it (Called by VIDEDemoPlayer.cs)
    private void Interact(VIDE_Assign quizDialogue)
    {
        //Sometimes, we might want to check the ExtraVariables and VAs before moving forward
        //We might want to modify the dialogue or perhaps go to another node, or dont start the dialogue at all
        //In such cases, the function will return true
        var doNotInteract = PreConditions(quizDialogue);
        if (doNotInteract) return;

        _answeredCorrectly = false;

        if (!VD.isActive)
        {
            Begin(quizDialogue);
        }
        else
        {
            CallNext();
        }
    }

    //This begins the conversation
    private void Begin(VIDE_Assign dialogue)
    {
        //Let's reset the NPC text variables
        questionText.text = "";

        //First step is to call BeginDialogue, passing the required VIDE_Assign component 
        //This will store the first Node data in VD.nodeData
        //But before we do so, let's subscribe to certain events that will allow us to easily
        //Handle the node-changes
        VD.OnActionNode += ActionHandler;
        VD.OnNodeChange += UpdateUI;
        VD.OnEnd += EndDialogue;

        VD.BeginDialogue(dialogue); //Begins dialogue, will call the first OnNodeChange

        quizContainer.SetActive(true); //Let's make our dialogue container visible
    }

    //Calls next node in the dialogue
    public void CallNext()
    {
        VD.Next();
    }

    //Input related stuff (scroll through player choices and update highlight)
    void Update()
    {
        if (!_runningQuiz) return;
        //Lets just store the Node Data variable for the sake of fewer words
        var data = VD.nodeData;

        if (VD.isActive) //If there is a dialogue active
        {
            //Scroll through Player dialogue options if dialogue is not paused and we are on a player node
            //For player nodes, NodeData.commentIndex is the index of the picked choice
            if (!data.pausedAction)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Return))
                {
                    Interact(_currentQuiz);
                }

                if (data.isPlayer)
                {
                    if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        if (data.commentIndex < currentChoices.Count - 1)
                            data.commentIndex++;
                    }

                    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        if (data.commentIndex > 0)
                            data.commentIndex--;
                    }

                    //Color the Player options. Blue for the selected one
                    for (int i = 0; i < currentChoices.Count; i++)
                    {
                        currentChoices[i].Normal();
                        if (i == data.commentIndex) currentChoices[i].Highlight();
                    }
                }
            }
        }

        //Note you could also use Unity's Navi system
    }

    //When we call VD.Next, nodeData will change. When it changes, OnNodeChange event will fire
    //We subscribed our UpdateUI method to the event in the Begin method
    //Here's where we update our UI
    void UpdateUI(VD.NodeData data)
    {
        //Reset some variables
        //Destroy the current choices
        foreach (var op in currentChoices)
        {
            Destroy(op.gameObject);
        }

        currentChoices = new List<PlayerChoice>();
        questionText.text = "";
        // levelsManager.npcContainer.SetActive(false);
        // levelsManager.playerContainer.SetActive(false);
        // levelsManager.playerSprite.sprite = null;
        // levelsManager.npcSprite.sprite = null;

        //If this new Node is a Player Node, set the player choices offered by the node
        if (data.isPlayer)
        {
            questionText.text = data.tag;
            SetOptions(data.comments);
        }
        else //If it's an NPC Node, let's just update NPC's text and sprite
        {
            /*//Set node sprite if there's any, otherwise try to use default sprite
            if (data.sprite != null)
            {
                //For NPC sprite, we'll first check if there's any "sprite" key
                //Such key is being used to apply the sprite only when at a certain comment index
                //Check CrazyCap dialogue for reference
                if (data.extraVars.ContainsKey("sprite"))
                {
                    if (data.commentIndex == (int)data.extraVars["sprite"])
                        levelsManager.npcSprite.sprite = data.sprite;
                    else
                        levelsManager.npcSprite.sprite =
                            VD.assigned.defaultNPCSprite; //If not there yet, set default dialogue sprite
                }
                else //Otherwise use the node sprites
                {
                    levelsManager.npcSprite.sprite = data.sprite;
                }
            } //or use the default sprite if there isnt a node sprite at all
            else if (VD.assigned.defaultNPCSprite != null)
            {
                levelsManager.npcSprite.sprite = VD.assigned.defaultNPCSprite;
            }

            //This coroutine animates the NPC text instead of displaying it all at once
            levelsManager.npcTextAnimator = levelsManager.DrawText(data.comments[data.commentIndex], 0.02f);
            StartCoroutine(levelsManager.npcTextAnimator);

            //If it has a tag, show it, otherwise let's use the alias we set in the VIDE Assign
            if (data.tag.Length > 0)
                levelsManager.npcLabel.text = data.tag;
            else
                levelsManager.npcLabel.text = VD.assigned.alias;


            //Sets the NPC container on
            levelsManager.npcContainer.SetActive(true);*/
        }
    }

    //Unsuscribe from everything, disable UI, and end dialogue
    //Called automatically because we subscribed to the OnEnd event
    void EndDialogue(VD.NodeData data)
    {
        _runningQuiz = false;
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= UpdateUI;
        VD.OnEnd -= EndDialogue;
        quizContainer.SetActive(false);
        VD.EndDialogue();

        _onQuizComplete();
    }

    void OnDisable()
    {
        //If the script gets destroyed, let's make sure we force-end the dialogue to prevent errors
        //We do not save changes
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= UpdateUI;
        VD.OnEnd -= EndDialogue;
        if (quizContainer != null)
            quizContainer.SetActive(false);
        VD.EndDialogue();
    }

    #endregion

    #region EVENTS AND HANDLERS

    //Just so we know when we finished loading all dialogues, then we unsubscribe
    void OnLoadedAction()
    {
        Debug.Log("Finished loading all dialogues");
        VD.OnLoaded -= OnLoadedAction;
    }

    public void OnPlayerChoose(int choice)
    {
        var data = VD.nodeData;
        data.commentIndex = Mathf.Min(Mathf.Max(choice, 0), data.comments.Length - 1);
    }

    //Another way to handle Action Nodes is to listen to the OnActionNode event, which sends the ID of the action node
    void ActionHandler(int actionNodeID)
    {
        Debug.Log("ACTION TRIGGERED: " + actionNodeID.ToString());
        var nodeData = VD.GetNodeData(actionNodeID);
        Debug.Log(nodeData);
    }

    public void OnPlayerSubmit()
    {
        Interact(_currentQuiz);
    }

    //This uses the returned string[] from nodeData.comments to create the UIs for each comment
    //It first cleans, then it instantiates new choices
    public void SetOptions(string[] choices)
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
            newOp.GetComponent<Button>().onClick.AddListener(delegate { OnPlayerChoose(i1); });
            newOp.SetActive(true);

            currentChoices.Add(newOp.GetComponent<PlayerChoice>());
        }
    }

    #endregion
}