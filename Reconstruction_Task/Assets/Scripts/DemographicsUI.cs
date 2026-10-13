using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Asks for age and gender after the consent screen. Continue stays disabled until a valid
// age is typed and one gender option is picked; pressing it saves both to SessionInfo and
// moves on to the main menu (the screen that used to follow the consent screen).
public class DemographicsUI : MonoBehaviour
{
    const int MinAge = 1;
    const int MaxAge = 120;

    public TMP_InputField ageInput;
    public Toggle maleToggle;
    public Toggle femaleToggle;
    public Toggle nonBinaryToggle;
    public Toggle preferNotToSayToggle;
    public Button continueButton;

    // Fresh form every time it's shown (a new participant starts after each finished session).
    void OnEnable()
    {
        ageInput.text = "";
        if (maleToggle.group != null) maleToggle.group.SetAllTogglesOff(false);
        Refresh();
    }

    void Start()
    {
        ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        ageInput.characterLimit = 3;
        ageInput.onValueChanged.AddListener(_ => Refresh());

        foreach (var toggle in new[] { maleToggle, femaleToggle, nonBinaryToggle, preferNotToSayToggle })
            toggle.onValueChanged.AddListener(_ => Refresh());

        continueButton.onClick.AddListener(HandleContinue);
        Refresh();
    }

    bool TryGetAge(out int age) =>
        int.TryParse(ageInput.text, out age) && age >= MinAge && age <= MaxAge;

    bool TryGetGender(out GenderOption gender)
    {
        if (maleToggle.isOn) { gender = GenderOption.Male; return true; }
        if (femaleToggle.isOn) { gender = GenderOption.Female; return true; }
        if (nonBinaryToggle.isOn) { gender = GenderOption.NonBinary; return true; }
        if (preferNotToSayToggle.isOn) { gender = GenderOption.PreferNotToSay; return true; }

        gender = default;
        return false;
    }

    void Refresh()
    {
        continueButton.interactable = TryGetAge(out _) && TryGetGender(out _);
    }

    void HandleContinue()
    {
        if (!TryGetAge(out int age) || !TryGetGender(out GenderOption gender)) return;

        SessionInfo.SetDemographics(age, gender);
        gameObject.SetActive(false);
        MainManager.Instance.MenuUI.SetActive(true);
    }
}
