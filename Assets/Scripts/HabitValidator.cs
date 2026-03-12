using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

public class HabitValidator : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_InputField targetInput;

    [Header("Error Display")]
    [SerializeField] private GameObject titleErrorPanel;
    [SerializeField] private TextMeshProUGUI titleErrorText;
    [SerializeField] private GameObject descriptionErrorPanel;
    [SerializeField] private TextMeshProUGUI descriptionErrorText;
    [SerializeField] private GameObject targetErrorPanel;
    [SerializeField] private TextMeshProUGUI targetErrorText;

    [Header("Character Counters")]
    [SerializeField] private TextMeshProUGUI titleCounter;
    [SerializeField] private TextMeshProUGUI descriptionCounter;
    [SerializeField] private TextMeshProUGUI targetCounter;

    [Header("Validation Settings")]
    [SerializeField] private int maxTitleLength = 50;
    [SerializeField] private int maxDescriptionLength = 200;
    [SerializeField] private int minTitleLength = 3;
    [SerializeField] private float minTargetValue = 0.1f;

    [Header("Submit Button")]
    [SerializeField] private Button submitButton;

    [Header("Feedback")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private TextMeshProUGUI successMessage;

    // Input System actions
    private InputAction submitAction;
    private InputAction cancelAction;

    private void Awake()
    {
        // Initialize Input System for Unity 6000
        SetupInputActions();

        // Hide all error panels initially
        ClearAllErrors();

        // Hide success panel
        if (successPanel != null)
            successPanel.SetActive(false);
    }

    private void SetupInputActions()
    {
        // Create input actions for Unity 6000's Input System
        submitAction = new InputAction(binding: "<Keyboard>/enter");
        cancelAction = new InputAction(binding: "<Keyboard>/escape");

        submitAction.performed += ctx => OnSubmitPerformed();
        cancelAction.performed += ctx => OnCancelPerformed();
    }

    private void OnEnable()
    {
        // Enable input actions
        submitAction.Enable();
        cancelAction.Enable();

        // Add real-time validation listeners
        if (titleInput != null)
        {
            titleInput.onValueChanged.AddListener(ValidateTitleRealTime);
            titleInput.onEndEdit.AddListener(ValidateTitleOnEndEdit);
        }

        if (descriptionInput != null)
        {
            descriptionInput.onValueChanged.AddListener(ValidateDescriptionRealTime);
        }

        if (targetInput != null)
        {
            targetInput.onValueChanged.AddListener(ValidateTargetRealTime);
            targetInput.onEndEdit.AddListener(ValidateTargetOnEndEdit);
        }

        // Add submit button listener
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
    }

    private void OnDisable()
    {
        // Disable input actions
        submitAction.Disable();
        cancelAction.Disable();

        // Remove listeners
        if (titleInput != null)
        {
            titleInput.onValueChanged.RemoveListener(ValidateTitleRealTime);
            titleInput.onEndEdit.RemoveListener(ValidateTitleOnEndEdit);
        }

        if (descriptionInput != null)
        {
            descriptionInput.onValueChanged.RemoveListener(ValidateDescriptionRealTime);
        }

        if (targetInput != null)
        {
            targetInput.onValueChanged.RemoveListener(ValidateTargetRealTime);
            targetInput.onEndEdit.RemoveListener(ValidateTargetOnEndEdit);
        }

        if (submitButton != null)
            submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
    }

    private void OnSubmitPerformed()
    {
        // Handle Enter key press
        if (gameObject.activeInHierarchy)
            OnSubmitButtonClicked();
    }

    private void OnCancelPerformed()
    {
        // Handle Escape key press
        if (gameObject.activeInHierarchy)
            CancelForm();
    }

    private void ClearAllErrors()
    {
        SetErrorPanel(titleErrorPanel, false);
        SetErrorPanel(descriptionErrorPanel, false);
        SetErrorPanel(targetErrorPanel, false);
    }

    private void SetErrorPanel(GameObject panel, bool isActive, string message = "")
    {
        if (panel != null)
        {
            panel.SetActive(isActive);

            // Get the error text component in the panel
            TextMeshProUGUI errorText = panel.GetComponentInChildren<TextMeshProUGUI>();
            if (errorText != null)
                errorText.text = message;
        }
    }

    // Real-time validation methods
    private void ValidateTitleRealTime(string value)
    {
        UpdateCounter(titleCounter, value.Length, maxTitleLength);

        if (string.IsNullOrWhiteSpace(value))
        {
            SetErrorPanel(titleErrorPanel, true, "Įpročio pavadinimas negali būti tuščias.");
        }
        else if (value.Length > maxTitleLength)
        {
            SetErrorPanel(titleErrorPanel, true, $"Pavadinimas per ilgas ({value.Length}/{maxTitleLength})");
        }
        else if (value.Length < minTitleLength && value.Length > 0)
        {
            SetErrorPanel(titleErrorPanel, true, $"Reikia dar {minTitleLength - value.Length} simbolių");
        }
        else
        {
            SetErrorPanel(titleErrorPanel, false);
        }
    }

    private void ValidateTitleOnEndEdit(string value)
    {
        // Final validation when user finishes editing
        if (string.IsNullOrWhiteSpace(value))
        {
            SetErrorPanel(titleErrorPanel, true, "Įpročio pavadinimas negali būti tuščias.");
        }
        else if (value.Length < minTitleLength)
        {
            SetErrorPanel(titleErrorPanel, true, $"Pavadinimas turi būti bent {minTitleLength} simbolių ilgio.");
        }
    }

    private void ValidateDescriptionRealTime(string value)
    {
        UpdateCounter(descriptionCounter, value.Length, maxDescriptionLength);

        if (!string.IsNullOrEmpty(value) && value.Length > maxDescriptionLength)
        {
            SetErrorPanel(descriptionErrorPanel, true, $"Aprašymas per ilgas ({value.Length}/{maxDescriptionLength})");
        }
        else
        {
            SetErrorPanel(descriptionErrorPanel, false);
        }
    }

    private void ValidateTargetRealTime(string value)
    {
        UpdateCounter(targetCounter, value.Length, 10); // Max 10 digits for target

        if (string.IsNullOrWhiteSpace(value))
        {
            SetErrorPanel(targetErrorPanel, true, "Tikslo reikšmė negali būti tuščia.");
        }
        else
        {
            float targetValue;
            if (!float.TryParse(value, out targetValue))
            {
                SetErrorPanel(targetErrorPanel, true, "Įveskite teisingą skaičių.");
            }
            else if (targetValue < minTargetValue)
            {
                SetErrorPanel(targetErrorPanel, true, $"Tikslas turi būti bent {minTargetValue}.");
            }
            else
            {
                SetErrorPanel(targetErrorPanel, false);
            }
        }
    }

    private void ValidateTargetOnEndEdit(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            float targetValue;
            if (float.TryParse(value, out targetValue) && targetValue > 0)
            {
                // Format the number nicely
                targetInput.text = targetValue.ToString("0.##");
            }
        }
    }

    private void UpdateCounter(TextMeshProUGUI counter, int currentLength, int maxLength)
    {
        if (counter != null)
        {
            counter.text = $"{currentLength}/{maxLength}";

            // Change color based on length
            if (currentLength >= maxLength - 5)
            {
                counter.color = Color.red;
            }
            else if (currentLength >= maxLength - 10)
            {
                counter.color = Color.yellow;
            }
            else
            {
                counter.color = Color.gray;
            }
        }
    }

    public bool ValidateHabit()
    {
        bool isValid = true;

        // Validate title
        if (string.IsNullOrWhiteSpace(titleInput.text))
        {
            SetErrorPanel(titleErrorPanel, true, "Įpročio pavadinimas negali būti tuščias.");
            isValid = false;
        }
        else if (titleInput.text.Length < minTitleLength)
        {
            SetErrorPanel(titleErrorPanel, true, $"Pavadinimas turi būti bent {minTitleLength} simbolių ilgio.");
            isValid = false;
        }
        else if (titleInput.text.Length > maxTitleLength)
        {
            SetErrorPanel(titleErrorPanel, true, $"Pavadinimas negali viršyti {maxTitleLength} simbolių.");
            isValid = false;
        }
        else
        {
            SetErrorPanel(titleErrorPanel, false);
        }

        // Validate description
        if (!string.IsNullOrEmpty(descriptionInput.text) && descriptionInput.text.Length > maxDescriptionLength)
        {
            SetErrorPanel(descriptionErrorPanel, true, $"Aprašymas negali viršyti {maxDescriptionLength} simbolių.");
            isValid = false;
        }
        else
        {
            SetErrorPanel(descriptionErrorPanel, false);
        }

        // Validate target
        if (string.IsNullOrWhiteSpace(targetInput.text))
        {
            SetErrorPanel(targetErrorPanel, true, "Tikslo reikšmė negali būti tuščia.");
            isValid = false;
        }
        else
        {
            float targetValue;
            if (!float.TryParse(targetInput.text, out targetValue))
            {
                SetErrorPanel(targetErrorPanel, true, "Įveskite teisingą skaičių.");
                isValid = false;
            }
            else if (targetValue < minTargetValue)
            {
                SetErrorPanel(targetErrorPanel, true, $"Tikslas turi būti bent {minTargetValue}.");
                isValid = false;
            }
            else
            {
                SetErrorPanel(targetErrorPanel, false);
            }
        }

        return isValid;
    }

    public void OnSubmitButtonClicked()
    {
        if (ValidateHabit())
        {
            // Create habit object
            HabitData newHabit = new HabitData
            {
                title = titleInput.text,
                description = descriptionInput.text,
                targetValue = float.Parse(targetInput.text),
                createdAt = DateTime.Now
            };

            // Save habit (you would implement your save logic here)
            SaveHabit(newHabit);

            // Show success message
            ShowSuccessMessage("Įprotis sėkmingai sukurtas!");

            // Clear form
            ClearForm();
        }
        else
        {
            // Optionally show a general error message
            Debug.Log("Please fix validation errors before submitting.");

            // You could also play a sound or animate error panels
            AnimateErrorPanels();
        }
    }

    private void ShowSuccessMessage(string message)
    {
        if (successPanel != null && successMessage != null)
        {
            successMessage.text = message;
            successPanel.SetActive(true);

            // Auto-hide after 3 seconds
            Invoke(nameof(HideSuccessMessage), 3f);
        }
    }

    private void HideSuccessMessage()
    {
        if (successPanel != null)
            successPanel.SetActive(false);
    }

    private void AnimateErrorPanels()
    {
        // Simple shake animation for visible error panels
        GameObject[] panels = { titleErrorPanel, descriptionErrorPanel, targetErrorPanel };

        foreach (var panel in panels)
        {
            if (panel != null && panel.activeSelf)
            {
                StartCoroutine(ShakePanel(panel.transform));
            }
        }
    }

    private System.Collections.IEnumerator ShakePanel(Transform panelTransform)
    {
        Vector3 originalPosition = panelTransform.localPosition;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float x = originalPosition.x + UnityEngine.Random.Range(-5f, 5f);
            panelTransform.localPosition = new Vector3(x, originalPosition.y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        panelTransform.localPosition = originalPosition;
    }

    private void SaveHabit(HabitData habit)
    {
        // Implement your save logic here
        // Could use PlayerPrefs, SQLite, or a backend service
        Debug.Log($"Saving habit: {habit.title}, Target: {habit.targetValue}");

        // Example: Save to PlayerPrefs (simplified)
        string habitsJson = PlayerPrefs.GetString("Habits", "[]");
        // Parse, add new habit, save back
    }

    private void ClearForm()
    {
        titleInput.text = "";
        descriptionInput.text = "";
        targetInput.text = "";

        // Focus on first input
        titleInput.Select();
    }

    private void CancelForm()
    {
        // Clear form and hide
        ClearForm();
        gameObject.SetActive(false);
    }
}

// Data class for habit
[System.Serializable]
public class HabitData
{
    public string title;
    public string description;
    public float targetValue;
    public DateTime createdAt;
    public bool isCompleted;
    public float currentProgress;
}