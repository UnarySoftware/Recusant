using Core;
using System.Collections.Generic;

namespace Recusant
{
    public class GameplayAutocomplete : System<GameplayAutocomplete>
    {
        private readonly List<string> _entries = new();

        public override void Initialize()
        {
            var keys = GameplayExecutor.Instance.GameplayUnits.Keys;

            foreach (var key in keys)
            {
                _entries.Add(key);
            }
        }

        public List<string> GetAutocompleteMatches(string input, bool strict = false, int maxMatches = int.MaxValue)
        {
            if (input.Contains(" "))
            {
                string[] spaceParts = input.ToUpper().Split(' ');
                input = spaceParts[0];
            }

            string[] inputParts = input.ToUpper().Split('.');

            List<string> results = new();

            int counter = 0;

            foreach (var target in _entries)
            {
                if (counter >= maxMatches)
                {
                    break;
                }

                if (strict)
                {
                    if (input.ToUpper() == target.ToUpper())
                    {
                        results.Add(target);
                        counter++;
                    }
                }
                else
                {
                    string[] targetParts = target.ToUpper().Split('.');

                    if (targetParts.Length < inputParts.Length)
                    {
                        continue;
                    }

                    bool match = true;

                    for (int i = 0; i < inputParts.Length; i++)
                    {
                        if (targetParts[i] != inputParts[i] && !targetParts[i].StartsWith(inputParts[i]))
                        {
                            match = false;
                        }
                    }

                    if (match)
                    {
                        results.Add(target);
                        counter++;
                    }
                }
            }

            return results;
        }

        public static string GetCommonStartingSubString(List<string> strings)
        {
            if (strings.Count == 0)
            {
                return "";
            }

            if (strings.Count == 1)
            {
                return strings[0];
            }

            int charIndex = 0;

            while (IsCommonChar(strings, charIndex))
            {
                ++charIndex;
            }

            return strings[0][..charIndex];
        }

        private static bool IsCommonChar(List<string> strings, int charIndex)
        {
            if (strings[0].Length <= charIndex)
            {
                return false;
            }

            for (int i = 1; i < strings.Count; ++i)
            {
                if (strings[i].Length <= charIndex || strings[i][charIndex] != strings[0][charIndex])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
