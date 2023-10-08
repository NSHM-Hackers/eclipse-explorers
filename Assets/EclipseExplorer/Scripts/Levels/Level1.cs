using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VIDE_Data;

[RequireComponent(typeof(VIDE_Assign))]
[AddComponentMenu("Eclipse/Levels/1", order: 0)]
public class Level1 : MonoBehaviour
{
    #region Vars

    public CinemachineVirtualCamera fullFocusCam, sunFocusCam, earthFocusCam, moonFocusCam;

    public BodyOrbit earthOrbit, moonOrbit;
    public BodyRotate earthRotate;

    [SerializeField] private LevelsManager levelsManager;
    [SerializeField] private LevelTrivia levelsTrivia;

    private bool sunWatched, earthWatched, moonWatched;

    [FormerlySerializedAs("_videAssign")] [SerializeField]
    private VIDE_Assign videAssign;

    [SerializeField] private VIDE_Assign level1Quiz;

    [SerializeField] private GameObject level2;

    [SerializeField] private Button nextDialogueButton;

    private bool _workingLevel = true;

    #endregion

    #region Start Events

    public void Awake()
    {
        fullFocusCam.Priority = 25;
        sunFocusCam.Priority = 5;
        earthFocusCam.Priority = 5;
        moonFocusCam.Priority = 5;
    }

    public void Start()
    {
        // earthOrbit.orbitingAngularSpeed = levelsManager.earthRevolveSpeed;
        // moonOrbit.orbitingAngularSpeed = levelsManager.moonRevolveSpeed;
        // earthRotate.speed = levelsManager.earthRotateSpeed;
        earthRotate.speed = moonOrbit.orbitingAngularSpeed = earthOrbit.orbitingAngularSpeed = 0;

        if (levelsTrivia == null)
        {
            levelsTrivia = FindObjectOfType<LevelTrivia>();
        }

        videAssign = GetComponent<VIDE_Assign>();
        Interact(videAssign);
        
        nextDialogueButton.onClick.RemoveAllListeners();
        nextDialogueButton.onClick.AddListener(NextDiag);
    }

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
            if (!data.isPlayer)
            {
                if (data.extraVars.ContainsKey("stopOrbiting"))
                {
                    earthRotate.speed = moonOrbit.orbitingAngularSpeed = earthOrbit.orbitingAngularSpeed = 0;
                }

                if (data.extraVars.TryGetValue("outCondition", out var outC))
                {
                    if (data.extraVars.TryGetValue("condInfo", out var cInfo))
                    {
                        var outCondition = outC.ToString();
                        var condInfo = cInfo.ToString();

                        int nodeID = int.Parse(outCondition);
                        if (sunWatched && earthWatched && moonWatched)
                        {
                            VD.SetNode(nodeID);

                            return true;
                        }

                        switch (condInfo.ToLower())
                        {
                            case "sun":
                                sunWatched = true;
                                break;
                            case "moon":
                                moonWatched = true;
                                break;
                            case "earth":
                                earthWatched = true;
                                break;
                        }
                    }
                }
            }
        }

        return false;
    }

    //Conditions we check after VD.Next was called but before we update the UI
    void PostConditions(VD.NodeData data)
    {
        //Don't conduct extra variable actions if we are waiting on a paused action
        if (data.pausedAction) return;

        if (!data.isPlayer) //For player nodes
        {
            //Checks for extraData that concerns font size (CrazyCap node 2)
            if (data.extraData[data.commentIndex].Contains("fs"))
            {
                int fSize = 14;

                string[] fontSize = data.extraData[data.commentIndex].Split(","[0]);
                int.TryParse(fontSize[1], out fSize);
                levelsManager.npcText.fontSize = fSize;
            }
            else
            {
                levelsManager.npcText.fontSize = 14;
            }
        }
    }

    #endregion

    #region Main

    //This begins the dialogue and progresses through it (Called by VIDEDemoPlayer.cs)
    private void Interact(VIDE_Assign dialogue)
    {
        //Sometimes, we might want to check the ExtraVariables and VAs before moving forward
        //We might want to modify the dialogue or perhaps go to another node, or dont start the dialogue at all
        //In such cases, the function will return true
        var doNotInteract = PreConditions(dialogue);
        if (doNotInteract) return;

        if (!VD.isActive)
        {
            Begin(dialogue);
        }
        else
        {
            CallNext();
        }
    }

    //This begins the conversation
    void Begin(VIDE_Assign dialogue)
    {
        //Let's reset the NPC text variables
        levelsManager.npcText.text = "";
        levelsManager.npcLabel.text = "";
        levelsManager.playerLabel.text = "";

        //First step is to call BeginDialogue, passing the required VIDE_Assign component 
        //This will store the first Node data in VD.nodeData
        //But before we do so, let's subscribe to certain events that will allow us to easily
        //Handle the node-changes
        VD.OnActionNode += ActionHandler;
        VD.OnNodeChange += UpdateUI;
        VD.OnEnd += EndDialogue;

        VD.BeginDialogue(dialogue); //Begins dialogue, will call the first OnNodeChange

        levelsManager.dialogueContainer.SetActive(true); //Let's make our dialogue container visible
    }

    //Calls next node in the dialogue
    public void CallNext()
    {
        //Let's not go forward if text is currently being animated, but let's speed it up.
        if (levelsManager.animatingText)
        {
            CutTextAnim();
            return;
        }

        if (!levelsManager.dialoguePaused) //Only if
        {
            VD.Next(); //We call the next node and populate nodeData with new data. Will fire OnNodeChange.
        }
        else
        {
            //Disable item popup and disable pause
            if (levelsManager.itemPopUp.activeSelf)
            {
                levelsManager.dialoguePaused = false;
                levelsManager.itemPopUp.SetActive(false);
            }
        }
    }

    //Input related stuff (scroll through player choices and update highlight)
    void Update()
    {
        if (!_workingLevel) return;
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
                    Interact(videAssign);
                }

                if (data.isPlayer)
                {
                    if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        if (data.commentIndex < levelsManager.currentChoices.Count - 1)
                            data.commentIndex++;
                    }

                    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        if (data.commentIndex > 0)
                            data.commentIndex--;
                    }

                    //Color the Player options. Blue for the selected one
                    for (int i = 0; i < levelsManager.currentChoices.Count; i++)
                    {
                        levelsManager.currentChoices[i].Normal();
                        if (i == data.commentIndex) levelsManager.currentChoices[i].Highlight();
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
        foreach (var op in levelsManager.currentChoices)
        {
            Destroy(op.gameObject);
        }

        levelsManager.currentChoices = new List<PlayerChoice>();
        levelsManager.npcText.text = "";
        levelsManager.npcContainer.SetActive(false);
        levelsManager.playerContainer.SetActive(false);
        levelsManager.playerSprite.sprite = null;
        levelsManager.npcSprite.sprite = null;

        //Look for dynamic text change in extraData
        PostConditions(data);

        //If this new Node is a Player Node, set the player choices offered by the node
        if (data.isPlayer)
        {
            //Set node sprite if there's any, otherwise try to use default sprite
            if (data.sprite != null)
                levelsManager.playerSprite.sprite = data.sprite;
            else if (VD.assigned.defaultPlayerSprite != null)
                levelsManager.playerSprite.sprite = VD.assigned.defaultPlayerSprite;

            levelsManager.SetOptions(data.comments, OnPlayerChoose);

            //If it has a tag, show it, otherwise let's use the alias we set in the VIDE Assign
            if (data.tag.Length > 0)
                levelsManager.playerLabel.text = data.tag;
            else
                levelsManager.playerLabel.text = "You";

            //Sets the player container on
            levelsManager.playerContainer.SetActive(true);
        }
        else //If it's an NPC Node, let's just update NPC's text and sprite
        {
            //Set node sprite if there's any, otherwise try to use default sprite
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

            switch (data.tag.ToLower())
            {
                case "sun":
                    sunFocusCam.Priority = 25;
                    fullFocusCam.Priority = earthFocusCam.Priority = moonFocusCam.Priority = 5;
                    break;
                case "earth":
                    earthFocusCam.Priority = 25;
                    sunFocusCam.Priority = fullFocusCam.Priority = moonFocusCam.Priority = 5;
                    break;
                case "moon":
                    moonFocusCam.Priority = 25;
                    sunFocusCam.Priority = earthFocusCam.Priority = fullFocusCam.Priority = 5;
                    break;
                default:
                    fullFocusCam.Priority = 25;
                    sunFocusCam.Priority = earthFocusCam.Priority = moonFocusCam.Priority = 5;
                    break;
            }

            //Sets the NPC container on
            levelsManager.npcContainer.SetActive(true);
        }
    }

    //Unsuscribe from everything, disable UI, and end dialogue
    //Called automatically because we subscribed to the OnEnd event
    void EndDialogue(VD.NodeData data)
    {
        _workingLevel = false;
        nextDialogueButton.onClick.RemoveAllListeners();
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= UpdateUI;
        VD.OnEnd -= EndDialogue;
        levelsManager.dialogueContainer.SetActive(false);
        VD.EndDialogue();

        earthOrbit.orbitingAngularSpeed = levelsManager.earthRevolveSpeed;
        moonOrbit.orbitingAngularSpeed = levelsManager.moonRevolveSpeed;
        earthRotate.speed = levelsManager.earthRotateSpeed;

        levelsTrivia.BeginQuiz(level1Quiz, Level1Complete);
    }

    private void Level1Complete()
    {
        fullFocusCam.Priority = 15;
        level2.SetActive(true);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        //If the script gets destroyed, let's make sure we force-end the dialogue to prevent errors
        //We do not save changes
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= UpdateUI;
        VD.OnEnd -= EndDialogue;
        if (levelsManager.dialogueContainer != null)
            levelsManager.dialogueContainer.SetActive(false);
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

    void CutTextAnim()
    {
        StopCoroutine(levelsManager.npcTextAnimator);
        levelsManager.npcText.text = VD.nodeData.comments[VD.nodeData.commentIndex]; //Now just copy full text		
        levelsManager.animatingText = false;
    }

    public void NextDiag()
    {
        Interact(videAssign);
    }

    #endregion
}