using System;
using System.Collections.Generic;
using System.Text;

namespace ResumeParserApp.Helpers.Detector
{
    public static class Language
    {
        public static string Detect(string[] lines)
        {
            LanguageDetector languageDetector = new LanguageDetector();
            return languageDetector.Detect(string.Join(string.Empty, lines));
        }
    }
}
