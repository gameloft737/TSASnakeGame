using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays random "Did You Know" facts from a configurable list.
/// Attach to a GameObject with a TextMeshProUGUI component or assign one in the inspector.
/// </summary>
public class DidYouKnowDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("The TextMeshProUGUI component to display the fact. If not assigned, will try to get from this GameObject.")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Header("Display Settings")]
    [Tooltip("Prefix text before the fact (e.g., 'Did You Know? ')")]
    [SerializeField] private string prefix = "Did You Know? ";
    
    [Tooltip("If true, will pick a new random fact each time the object is enabled")]
    [SerializeField] private bool refreshOnEnable = true;
    
    [Tooltip("If greater than 0, will automatically change to a new fact every X seconds")]
    [SerializeField] private float autoRefreshInterval = 0f;

    [Header("Facts List")]
    [Tooltip("Add your 'Did You Know' facts here. One will be randomly selected to display.")]
    [TextArea(2, 4)]
    [SerializeField] private List<string> facts = new List<string>()
    {
        "Snakes can have over 400 vertebrae!",
        "The fastest snake can move at 12 mph.",
        "Some snakes can go up to two years without eating.",
        "Snakes smell with their tongues!",
        "There are over 3,000 species of snakes in the world.",
        "Snakes don't have eyelids - they have a clear scale protecting their eyes.",
        "The longest snake ever recorded was a reticulated python at 32 feet!",
        "Snakes can feel vibrations through their jaw bones.",
        "Some sea snakes can breathe partially through their skin.",
        "Snakes shed their skin 3-6 times per year.",
        "The king cobra is the only snake that builds a nest for its eggs.",
        "Snakes are found on every continent except Antarctica.",
        "A snake's heart can move around its body to help digest large prey.",
        "Some snakes can glide through the air for over 300 feet!",
        "Snakes have been around for over 100 million years.",
        "The inland taipan has enough venom to kill 100 adult humans.",
        "Snakes can open their jaws up to 150 degrees to swallow prey.",
        "Baby snakes are called snakelets or hatchlings.",
        "Some snakes can survive for months without water.",
        "The thread snake is the smallest snake, about the size of a spaghetti noodle."
    };

    private int lastFactIndex = -1;
    private float refreshTimer;

    private void Awake()
    {
        // Try to get TextMeshProUGUI if not assigned
        if (displayText == null)
        {
            displayText = GetComponent<TextMeshProUGUI>();
        }

        if (displayText == null)
        {
            Debug.LogWarning("DidYouKnowDisplay: No TextMeshProUGUI component found! Please assign one in the inspector.");
        }
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
        {
            DisplayRandomFact();
        }
        refreshTimer = autoRefreshInterval;
    }

    private void Update()
    {
        // Handle auto-refresh if enabled
        if (autoRefreshInterval > 0)
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0)
            {
                DisplayRandomFact();
                refreshTimer = autoRefreshInterval;
            }
        }
    }

    /// <summary>
    /// Displays a random fact from the list. Tries to avoid repeating the same fact twice in a row.
    /// </summary>
    public void DisplayRandomFact()
    {
        if (displayText == null || facts == null || facts.Count == 0)
        {
            return;
        }

        int newIndex;
        
        // If we have more than one fact, try to pick a different one than last time
        if (facts.Count > 1)
        {
            do
            {
                newIndex = Random.Range(0, facts.Count);
            } while (newIndex == lastFactIndex);
        }
        else
        {
            newIndex = 0;
        }

        lastFactIndex = newIndex;
        displayText.text = prefix + facts[newIndex];
    }

    /// <summary>
    /// Displays a specific fact by index.
    /// </summary>
    /// <param name="index">The index of the fact to display.</param>
    public void DisplayFact(int index)
    {
        if (displayText == null || facts == null || facts.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, facts.Count - 1);
        lastFactIndex = index;
        displayText.text = prefix + facts[index];
    }

    /// <summary>
    /// Adds a new fact to the list at runtime.
    /// </summary>
    /// <param name="fact">The fact to add.</param>
    public void AddFact(string fact)
    {
        if (!string.IsNullOrEmpty(fact))
        {
            facts.Add(fact);
        }
    }

    /// <summary>
    /// Removes a fact from the list at runtime.
    /// </summary>
    /// <param name="fact">The fact to remove.</param>
    /// <returns>True if the fact was found and removed.</returns>
    public bool RemoveFact(string fact)
    {
        return facts.Remove(fact);
    }

    /// <summary>
    /// Clears all facts from the list.
    /// </summary>
    public void ClearFacts()
    {
        facts.Clear();
    }

    /// <summary>
    /// Gets the current number of facts in the list.
    /// </summary>
    public int FactCount => facts.Count;
}