using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ResumeParserApp.Helpers;
using ResumeParserApp.Helpers.Detector;
using ResumeParserApp.Model;

namespace ResumeParserApp.Process
{
    public class ResumeProcessor
    {
        private static readonly Regex EmailRegex = new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SocialProfileRegex = new Regex(@"(http(s)?:\/\/)?([\w]+\.)?(linkedin\.com|facebook\.com|github\.com|stackoverflow\.com|bitbucket\.org|sourceforge\.net|(\w+\.)?codeplex\.com|code\.google\.com)\/[A-z 0-9 ? _ - = /]+\/?\s?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly List<Regex> PhoneNumbersRegex = new List<Regex>
        {
            new Regex(@"(\(?\+[0-9]{1,3}\)?[\.\s]?)?[0-9]{7,14}(?:x.+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"((\(?\+\d{2}\)?)?)(\s?\d{2}\s?\d{2}\s?\d{2}\s?\d{2})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"([\+]?\d{4}[\s.-]?\d{4})+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"([\+]?\d{2}[\s.-]?\d{2}[\s.-]?\d{2}[\s.-]?\d{2})+", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        private static readonly Regex ExperienceAndEducationRegex = new Regex(@"\w?\s?\d{4}.+");
        private static readonly Regex AddressRegex = new Regex(@"\w{1,}[. ,'-]?\d{1,}\s?(\d{4}){1}\s?\w{1,}");

        private static readonly List<string> NationalityKeywords = new List<string> { "Nationality", "Nationalitet" };
        private static readonly List<string> KeywordsOfEducation = new List<string> { "Uddannelse", "Education", "ALMEN OG ERHVERVSRETTET UDDANNELSE", "EDUCATION AND TRAINING" };
        private static readonly List<string> KeywordsOfExperience = new List<string> { "Erhvervserfaring", "Erhvervsbeskæftigelse", "Tid og sted", "Experience", "Work Experience" };
        private static readonly List<string> KeywordsOfBreak = new List<string> { "kompetencer", "skills", "Kurser", "Courses", "certificeringer", "certifications", "Udlandsophold", "Abroad", "Sprog", "Languages", "Om mig", "About" };

        private static readonly Regex SplitByWhiteSpaceRegex = new Regex(@"\s+|,", RegexOptions.Compiled);

        private HashSet<string> _firstNameLookUp;
        private List<string> _listOfLanguages;

        public Resume Parse(string[] lines)
        {
            string languageCode = Language.Detect(lines);
            LoadData(languageCode);

            Resume resume = new Resume();

            bool firstNameFound = false;
            bool emailFound = false;
            bool phoneFound = false;
            bool nationalityFound = false;

            foreach (string line in lines)
            {
                firstNameFound = ExtractFirstNameAndLastName(resume, firstNameFound, line);
                emailFound = ExtractEmail(resume, emailFound, line);
                phoneFound = ExtractPhoneNumber(resume, phoneFound, line);
                nationalityFound = ExtractNationality(resume, nationalityFound, line);
                ExtractLanguages(resume, line);
                ExtractSocialProfiles(resume, line);
            }

            ExtractAddress(resume, string.Join(" ", lines.Take(10)));
            ExtractExperienceAndEducation(resume, lines);

            return resume;
        }

        private void LoadData(string lanCode)
        {
            if (lanCode == "da")
            {
                _firstNameLookUp = ResourceLoader.LoadIntoHashSet("FirstName.txt", ',', Encoding.GetEncoding("iso-8859-1"));
                _listOfLanguages = ResourceLoader.LoadIntoList("Nationalities-da-DK.txt", '|', Encoding.GetEncoding("iso-8859-1"));
            }
            else
            {
                _firstNameLookUp = ResourceLoader.LoadIntoHashSet("FirstName.txt", ',', Encoding.Default);
                _listOfLanguages = ResourceLoader.LoadIntoList("Nationalities-en-GB.txt", '|', Encoding.Default);
            }
        }

        private bool ExtractNationality(Resume resume, bool nationalityFound, string line)
        {
            if (nationalityFound)
                return true;

            foreach (string nationalityKeyword in NationalityKeywords)
            {
                int index = line.IndexOf(nationalityKeyword, StringComparison.InvariantCultureIgnoreCase);

                if (index > -1)
                {
                    string nationality = line.Remove(0, line.IndexOf(nationalityKeyword, StringComparison.InvariantCultureIgnoreCase) + nationalityKeyword.Length);
                    resume.Nationality = nationality.Trim(':', ' ');
                    nationalityFound = true;
                }
            }

            return nationalityFound;
        }

        private void ExtractAddress(Resume resume, string line)
        {
            resume.Address = AddressRegex.Match(line).Value;
        }

        private bool ExtractFirstNameAndLastName(Resume resume, bool firstNameFound, string line)
        {
            if (firstNameFound)
                return true;

            var words = SplitByWhiteSpaceRegex.Split(line);

            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i].Trim();

                if (!firstNameFound && _firstNameLookUp.Contains(word))
                {
                    resume.FirstName = word;
                    resume.LastName = string.Join(" ", words.Skip(i + 1).Take(2).Select(c => c.Trim('/', ' ', '.'))).Trim();
                    firstNameFound = true;
                }
            }

            return firstNameFound;
        }

        private bool ExtractPhoneNumber(Resume resume, bool phoneFound, string line)
        {
            if (phoneFound)
                return true;

            foreach (Regex regex in PhoneNumbersRegex)
            {
                Match phoneMatch = regex.Match(line);

                if (phoneMatch.Success)
                {
                    resume.PhoneNumber = Regex.Replace(phoneMatch.Value.Trim(), @"\s+", string.Empty);
                    phoneFound = true;
                    break;
                }
            }

            return phoneFound;
        }

        private void ExtractLanguages(Resume resume, string line)
        {
            foreach (string language in _listOfLanguages)
            {
                if (line.Contains(language, StringComparison.InvariantCultureIgnoreCase))
                {
                    resume.Languages.Add(language);
                }
            }
        }

        private bool ExtractEmail(Resume resume, bool emailFound, string line)
        {
            if (emailFound)
                return true;

            Match emailMatch = EmailRegex.Match(line);

            if (emailMatch.Success)
            {
                resume.EmailAddress = emailMatch.Value;
                emailFound = true;
            }

            return emailFound;
        }

        private void ExtractSocialProfiles(Resume resume, string line)
        {
            MatchCollection socialProfileMatches = SocialProfileRegex.Matches(line);

            foreach (Match socialProfileMatch in socialProfileMatches)
            {
                resume.SocialProfiles.Add(socialProfileMatch.Value.Trim('\n', ' ', '/', '\r'));
            }
        }

        private int GetIndexOfKeyword(string[] lines, IEnumerable<string> keywords, int from = 0)
        {
            int indexOfExperienceKeyword = -1;
            
            foreach (string keyword in keywords)
            {
                if (indexOfExperienceKeyword < 0)
                {
                    indexOfExperienceKeyword = Array.FindIndex(lines.Skip(from).ToArray(), line => line.Equals(keyword, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    break;
                }
            }

            return indexOfExperienceKeyword;
        }

        private int GetIndexOfBreakKeyword(string[] lines, IEnumerable<string> keywords, int from)
        {
            int indexOfBreakKeyword = -1;

            foreach (string keyword in keywords)
            {
                if (indexOfBreakKeyword < 0)
                {
                    indexOfBreakKeyword = Array.FindIndex(lines.Skip(from).ToArray(), line => line.Contains(keyword, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    break;
                }
            }

            return indexOfBreakKeyword;
        }

        private void ExtractExperienceAndEducation(Resume resume, string[] lines)
        {
            int indexOfExperienceKeyword = GetIndexOfKeyword(lines, KeywordsOfExperience);
            int indexOfEducationKeyword = GetIndexOfKeyword(lines, KeywordsOfEducation);
            
            if (indexOfEducationKeyword > -1 && indexOfExperienceKeyword > -1)
            {
                int indexOfBreakKeyword;

                if (indexOfEducationKeyword > indexOfExperienceKeyword)
                {
                    ExractExperience(indexOfExperienceKeyword, indexOfEducationKeyword);
                    indexOfBreakKeyword = GetIndexOfBreakKeyword(lines, KeywordsOfBreak, indexOfEducationKeyword);
                    indexOfBreakKeyword = indexOfBreakKeyword == -1 ? lines.Length : indexOfBreakKeyword + indexOfEducationKeyword;
                    ExtractEducation(indexOfEducationKeyword, indexOfBreakKeyword);
                }
                else
                {
                    ExtractEducation(indexOfEducationKeyword, indexOfExperienceKeyword);
                    indexOfBreakKeyword = GetIndexOfBreakKeyword(lines, KeywordsOfBreak, indexOfExperienceKeyword);
                    indexOfBreakKeyword = indexOfBreakKeyword == -1 ? lines.Length : indexOfBreakKeyword + indexOfExperienceKeyword;
                    ExractExperience(indexOfExperienceKeyword, indexOfBreakKeyword);
                }
            }

            void ExractExperience(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    Match match = ExperienceAndEducationRegex.Match(lines[i]);

                    if (match.Success)
                    {
                        if (int.TryParse(Regex.Match(lines[i], @"\d{4}").Value, out int year))
                        {
                            if (year > 1900 && year <= DateTime.UtcNow.Year)
                            {
                                resume.WorkExperience.Add(lines[i].Trim(',', ' '));
                            }
                        }
                    }
                }
            }

            void ExtractEducation(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    Match match = ExperienceAndEducationRegex.Match(lines[i]);

                    if (match.Success)
                    {
                        if(int.TryParse(Regex.Match(lines[i], @"\d{4}").Value, out int year))
                        {
                            if (year > 1900 && year <= DateTime.UtcNow.Year)
                            {
                                resume.Education.Add(lines[i].Trim(',', ' '));
                            }
                        }
                    }
                }
            }
        }
    }
}
