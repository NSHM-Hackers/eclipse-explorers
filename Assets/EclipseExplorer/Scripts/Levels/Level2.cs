using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using VIDE_Data;

[AddComponentMenu("Eclipse/Levels/2", order: 0)]
public class Level2 : MonoBehaviour
{
    #region Vars

    public CinemachineVirtualCamera topViewCam;

    public BodyOrbit earthOrbit, moonOrbit;
    public BodyRotate earthRotate;

    [SerializeField] private LevelsManager levelsManager;

    private bool _solarDone = false, _lunarDone = false;

    [SerializeField] private VIDE_Assign solarVideAssign, lunarVideAssign;
    [SerializeField] private float dropPosTolerance = 5;

    [SerializeField] private GameObject level3;

    [SerializeField] private Button nextDialogueButton;

    [SerializeField] private GameObject helpText;

    private bool _workingLevel = true;
    private VIDE_Assign _currentVideAssign;
    private bool _isDraggingMoon;

    private Vector3 originalMoonPos, targetPosition, screenPosition, offset;

    #endregion

    #region Start Events

    public void Start()
    {
        // earthOrbit.orbitingAngularSpeed = levelsManager.earthRevolveSpeed;
        // moonOrbit.orbitingAngularSpeed = levelsManager.moonRevolveSpeed;
        // earthRotate.speed = levelsManager.earthRotateSpeed;
        earthRotate.speed = moonOrbit.orbitingAngularSpeed = earthOrbit.orbitingAngularSpeed = 0;
        topViewCam.Priority = 25;

        nextDialogueButton.onClick.RemoveAllListeners();
        nextDialogueButton.onClick.AddListener(NextDiag);
    }

    #endregion

    #region Main

    //This begins the dialogue and progresses through it (Called by VIDEDemoPlayer.cs)
    private void Interact(VIDE_Assign dialogue)
    {
        //Sometimes, we might want to check the ExtraVariables and VAs before moving forward
        //We might want to modify the dialogue or perhaps go to another node, or dont start the dialogue at all
        //In such cases, the function will return true
        // var doNotInteract = PreConditions(dialogue);
        // if (doNotInteract) return;

        if (!VD.isActive)
        {
            _currentVideAssign = dialogue;
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

        if (VD.isActive) //If there is a dialogue active
        {
            //Lets just store the Node Data variable for the sake of fewer words
            var data = VD.nodeData;

            //Scroll through Player dialogue options if dialogue is not paused and we are on a player node
            //For player nodes, NodeData.commentIndex is the index of the picked choice
            if (!data.pausedAction)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Return))
                {
                    Interact(_currentVideAssign);
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
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                var target = ReturnClickedObject(out _);
                if (target != null && target == moonOrbit.gameObject)
                {
                    _isDraggingMoon = true;
                    targetPosition = target.transform.position;
                    originalMoonPos = targetPosition;
                    Debug.Log("target position :" + targetPosition);
                    //Convert world position to screen position.
                    screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
                    offset = targetPosition - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,
                        Input.mousePosition.y, screenPosition.z));
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_isDraggingMoon)
                {
                    _isDraggingMoon = false;
                    var earthPos = earthOrbit.transform.position;
                    var dir = earthPos.normalized;
                    var solarEclipsePos = moonOrbit.GetNearestOrbitPosition(earthPos - dir * moonOrbit.radius);
                    var lunarEclipsePos = moonOrbit.GetNearestOrbitPosition(earthPos + dir * moonOrbit.radius);
                    // var solarEclipsePos = earthPos - dir * moonOrbit.radius;
                    // var lunarEclipsePos = earthPos + dir * moonOrbit.radius;

                    if (Vector3.Distance(solarEclipsePos, targetPosition) < dropPosTolerance)
                    {
                        moonOrbit.transform.position = solarEclipsePos;
                        Interact(solarVideAssign);
                    }
                    else if (Vector3.Distance(lunarEclipsePos, targetPosition) < dropPosTolerance)
                    {
                        moonOrbit.transform.position = lunarEclipsePos;
                        Interact(lunarVideAssign);
                    }
                    else
                    {
                        moonOrbit.transform.position = originalMoonPos;
                    }
                }
            }

            if (_isDraggingMoon)
            {
                //track mouse position.
                Vector3 currentScreenSpace =
                    new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);

                //convert screen position to world position with offset changes.
                Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenSpace) + offset;

                //It will update target gameobject's current postion.
                targetPosition = currentPosition;
                moonOrbit.transform.position = targetPosition;
            }
        }

        //Note you could also use Unity's Navi system
    }

    GameObject ReturnClickedObject(out RaycastHit hit)
    {
        GameObject target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
        {
            target = hit.collider.gameObject;
        }

        return target;
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

            //Sets the NPC container on
            levelsManager.npcContainer.SetActive(true);
        }
    }

    //Unsuscribe from everything, disable UI, and end dialogue
    //Called automatically because we subscribed to the OnEnd event
    void EndDialogue(VD.NodeData data)
    {
        VD.OnActionNode -= ActionHandler;
        VD.OnNodeChange -= UpdateUI;
        VD.OnEnd -= EndDialogue;
        levelsManager.dialogueContainer.SetActive(false);
        VD.EndDialogue();

        if (_currentVideAssign == solarVideAssign)
        {
            _solarDone = true;
        }
        else if (_currentVideAssign == lunarVideAssign)
        {
            _lunarDone = true;
        }

        _currentVideAssign = null;

        if (_solarDone && _lunarDone)
        {
            _workingLevel = false;

            nextDialogueButton.onClick.RemoveAllListeners();

            earthOrbit.orbitingAngularSpeed = levelsManager.earthRevolveSpeed;
            moonOrbit.orbitingAngularSpeed = levelsManager.moonRevolveSpeed;
            earthRotate.speed = levelsManager.earthRotateSpeed;

            Level2Complete();
        }
    }

    private void Level2Complete()
    {
        topViewCam.Priority = 15;
        level3.SetActive(true);
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
        Interact(_currentVideAssign);
    }

    #endregion
}