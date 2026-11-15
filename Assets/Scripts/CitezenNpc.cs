using System.Collections;
using UnityEngine;
using TMPro;

public class CitezenNpc : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;
    public float typingSpeed = 0.04f;

    [Header("Home Case")]
    public bool offersCase = true;
    [TextArea(2, 4)]
    public string helpOfferText = "Please help me. Something is haunting my home. Will you come investigate?";
    public KeyCode acceptHelpKey = KeyCode.Y;
    public KeyCode declineHelpKey = KeyCode.N;
    public Transform homeEntryPoint;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueTextUI;
    public TMP_Text continuePromptUI;

    [Header("Mga Sasabihin ng NPC")]
    [TextArea(2, 5)]
    public string[] dialogueLines = new string[]
    {
        "Huwag kang lalapit sa lumang bahay sa dulo...",
        "May narinig kaming umiiyak doon kagabi."
    };

    private int currentLineIndex = 0;
    private bool isTalking = false;
    private bool isTyping = false;
    private bool isOfferingHelp = false;
    private Animator animator;
    private Coroutine typingCoroutine;
    private Collider npcCollider;
    private Transform interactionRoot;
    private bool showPromptOverlay;
    private GUIStyle promptOverlayStyle;

    void Awake()
    {
        animator = GetComponentInParent<Animator>();
        interactionRoot = animator != null ? animator.transform : transform;

        if (homeEntryPoint != null && homeEntryPoint.parent != null)
        {
            homeEntryPoint.SetParent(null, true);
        }

        npcCollider = GetComponent<Collider>();
        if (npcCollider == null) npcCollider = GetComponentInChildren<Collider>();
    }

    void Start()
    {
        currentLineIndex = 0;
        isTalking = false;
        isTyping = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continuePromptUI != null) continuePromptUI.gameObject.SetActive(false);

        if (player == null)
        {
            Debug.LogError("CitezenNpc requires a Player reference.", this);
        }

    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(interactionRoot.position, player.position);
        if (npcCollider != null)
        {
            distance = Mathf.Min(distance, Vector3.Distance(
                npcCollider.ClosestPoint(player.position), player.position));
        }
        bool inRange = distance <= interactDistance;
        bool panelVisible = dialoguePanel != null && dialoguePanel.activeInHierarchy;
        bool shouldShowPrompt = inRange && !panelVisible;
        showPromptOverlay = shouldShowPrompt;

        if (inRange)
        {
            Vector3 lookDirection = player.position - interactionRoot.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                interactionRoot.rotation = Quaternion.Slerp(
                    interactionRoot.rotation,
                    Quaternion.LookRotation(lookDirection),
                    Time.deltaTime * 5f);
            }

            if (Input.GetKeyDown(interactKey))
            {
                if (!isTalking)
                {
                    StartDialogue();
                }
                else if (!isOfferingHelp)
                {
                    if (isTyping)
                    {
                        StopCoroutine(typingCoroutine);
                        dialogueTextUI.text = dialogueLines[currentLineIndex];
                        isTyping = false;
                        ShowContinuePrompt();
                    }
                    else
                    {
                        currentLineIndex++;
                        if (currentLineIndex < dialogueLines.Length)
                        {
                            typingCoroutine = StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
                        }
                        else if (offersCase)
                        {
                            ShowHelpOffer();
                        }
                        else
                        {
                            EndDialogue();
                        }
                    }
                }
            }

            if (isOfferingHelp)
            {
                if (Input.GetKeyDown(acceptHelpKey)) EnterHome();
                else if (Input.GetKeyDown(declineHelpKey)) EndDialogue();
            }
        }
        else
        {
            if (isTalking && distance > interactDistance + 1.5f)
            {
                EndDialogue();
            }
        }
    }

    void OnGUI()
    {
        if (!showPromptOverlay) return;

        if (promptOverlayStyle == null)
        {
            promptOverlayStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 40,
                fontStyle = FontStyle.Normal
            };
        }

        string prompt = "Press " + interactKey + " to Interact";
        float promptCenterY = Screen.height * 0.86f;
        Rect promptRect = new Rect(
            0f,
            promptCenterY - 32f,
            Screen.width,
            64f);
        promptOverlayStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(promptRect.x + 2f, promptRect.y + 2f, promptRect.width, promptRect.height), prompt, promptOverlayStyle);
        promptOverlayStyle.normal.textColor = Color.white;
        GUI.Label(promptRect, prompt, promptOverlayStyle);
    }

    void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogError("CitezenNpc requires at least one dialogue line.", this);
            return;
        }

        isTalking = true;
        isOfferingHelp = false;
        showPromptOverlay = false;
        currentLineIndex = 0;

        if (animator != null) animator.SetBool("isTalking", true);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        typingCoroutine = StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
    }

    void ShowHelpOffer()
    {
        isOfferingHelp = true;
        dialogueTextUI.text = helpOfferText;
        if (continuePromptUI != null)
        {
            continuePromptUI.gameObject.SetActive(true);
            continuePromptUI.text = "[" + acceptHelpKey + "] ACCEPT   [" + declineHelpKey + "] DECLINE";
        }
    }

    void EnterHome()
    {
        if (homeEntryPoint == null)
        {
            Debug.LogError("Assign a home entry point to this citizen before accepting their case.", this);
            dialogueTextUI.text = "This citizen's home entry point has not been configured.";
            return;
        }

        EndDialogue();
        TeleportPlayer(homeEntryPoint.position, homeEntryPoint.eulerAngles.y);
    }

    void TeleportPlayer(Vector3 position, float yaw)
    {
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null) characterController = player.GetComponentInParent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        player.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        if (characterController != null) characterController.enabled = true;
    }

    void EndDialogue()
    {
        isTalking = false;
        isTyping = false;
        isOfferingHelp = false;

        if (animator != null) animator.SetBool("isTalking", false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (continuePromptUI != null) continuePromptUI.gameObject.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueTextUI.text = "";

        if (continuePromptUI != null) continuePromptUI.gameObject.SetActive(false);

        foreach (char letter in line)
        {
            dialogueTextUI.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        ShowContinuePrompt();
    }

    void ShowContinuePrompt()
    {
        if (continuePromptUI == null) return;

        continuePromptUI.gameObject.SetActive(true);
        if (currentLineIndex < dialogueLines.Length - 1)
        {
            continuePromptUI.text = "[" + interactKey + "] TO CONTINUE";
        }
        else if (offersCase)
        {
            continuePromptUI.text = "[" + interactKey + "] TO RESPOND";
        }
        else
        {
            continuePromptUI.text = "[" + interactKey + "] TO CLOSE";
        }
    }
}