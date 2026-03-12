using System;

namespace Localization
{
    public enum Language
    {
        Chinese,
        English 
    }
}

namespace Localization
{
    [Serializable]
    public class LocalizedString
    {
        public Language language;
        public string content;

        public LocalizedString(Language lang)
        {
            language = lang;
            content = "";
        }
    }
}

