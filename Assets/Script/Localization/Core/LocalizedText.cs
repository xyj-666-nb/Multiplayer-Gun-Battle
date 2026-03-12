using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int uniqueId;
        [SerializeField] private List<LocalizedString> localizedStrings = new List<LocalizedString>();

        private TMP_Text tmpText;

        public int UniqueId => uniqueId;
        public List<LocalizedString> LocalizedStrings => localizedStrings;

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
            LocalizationManager.RegisterText(this);
            UpdateDisplay();
        }

        private void OnDestroy() => LocalizationManager.UnregisterText(this);

        public void UpdateDisplay()
        {
            var targetLang = LocalizationManager.CurrentLanguage;
            var match = localizedStrings.Find(s => s.language == targetLang);
            if (tmpText != null && match != null)
                tmpText.text = match.content;
        }

        public void SetId(int id) => uniqueId = id;

        public void InitListByEnum()
        {
            localizedStrings.Clear();
            foreach (Language lang in System.Enum.GetValues(typeof(Language)))
            {
                localizedStrings.Add(new LocalizedString(lang));
            }
        }

        public void LoadData(List<LocalizedString> data)
        {
            localizedStrings = data;
            UpdateDisplay();
        }
    }
}