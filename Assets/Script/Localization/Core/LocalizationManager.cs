using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    public class LocalizationManager : SingleMonoAutoBehavior<LocalizationManager>
    {

        [SerializeField] private Language defaultLang = Language.Chinese;
        public static Language CurrentLanguage { get; private set; }

        private static List<LocalizedText> _registeredTexts = new List<LocalizedText>();


        public static void RegisterText(LocalizedText text)
        {
            if (!_registeredTexts.Contains(text))
                _registeredTexts.Add(text);
        }

        public static void UnregisterText(LocalizedText text)
        {
            if (_registeredTexts.Contains(text))
                _registeredTexts.Remove(text);
        }

        public static void SwitchLanguage(Language lang)
        {
            CurrentLanguage = lang;
            foreach (var text in _registeredTexts) 
                text.UpdateDisplay();
        }

        protected override void Awake()
        {
            base.Awake();
            CurrentLanguage = defaultLang;
        }
    }
}