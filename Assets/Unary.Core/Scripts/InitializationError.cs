using System.Collections.Generic;
using UnityEngine;

namespace Unary.Core
{
    public static class InitializationError
    {
        public static void Show(ErrorType type, params object[] parameters)
        {
            string formatted = type.ToString().Replace("_", " ");
            MessageBox.Show(formatted, GetLocalizedError(type, parameters));
        }

        public static string GetLocalizedError(ErrorType type, params object[] parameters)
        {
            string newLines = "\n\n";
            string result = $"Failed to find a localized string for an error type \"{type}\"\n";
            string validate = "Failed to find a localized string for a file validation request";

            SystemLanguage language = Application.systemLanguage;

            if (_validateGameRequest.TryGetValue(language, out var validated))
            {
                validate = validated;
            }
            else if (_validateGameRequest.TryGetValue(SystemLanguage.English, out var validatedEnglish))
            {
                validate = validatedEnglish;
            }

            if (!_localized.TryGetValue(type, out var error))
            {
                return result + newLines + validate;
            }

            if (error.TryGetValue(language, out string localizedString))
            {
                return string.Format(localizedString, parameters) + newLines + validate;
            }

            if (error.TryGetValue(SystemLanguage.English, out string englishString))
            {
                return string.Format(englishString, parameters) + newLines + validate;
            }

            return result + newLines + validate;
        }

        public enum ErrorType
        {
            // Generic
            File_Missing,
            File_Corrupted_Exception,
            // Steam-specific
            Steamworks_Pack_Size,
            Steamworks_Dll_Check,
            Steamworks_Dll_Not_Found,
            Steamworks_Init_Failed,
            Steamworks_Not_Launched,
            Steamworks_EnvironmentVariable_Failed,
            // Systems-specific
            System_Invalid_Namespace
        }

        private static readonly Dictionary<SystemLanguage, string> _validateGameRequest = new()
    {
        {
            SystemLanguage.English, "If this issue persists - try validating game files with Steam."
        }
    };

        private static readonly Dictionary<ErrorType, Dictionary<SystemLanguage, string>> _localized = new()
    {
        {
            ErrorType.File_Missing, new()
            {
                {
                    SystemLanguage.English, "Missing \"{0}\" file at path \"{1}\"."
                }
            }
        },
        {
            ErrorType.File_Corrupted_Exception, new()
            {
                {
                    SystemLanguage.English, "Failed loading \"{0}\" file at path \"{1}\".\n" +
                    "Error message:\n" +
                    "{2}\n" +
                    "Stack trace:\n" +
                    "{3}"
                }
            }
        },
        {
            ErrorType.Steamworks_Pack_Size, new()
            {
                {
                    SystemLanguage.English, "Wrong version of Steamworks.NET is being run in this platform."
                }
            }
        },
        {
            ErrorType.Steamworks_Dll_Check, new()
            {
                {
                    SystemLanguage.English, "One or more of the Steamworks binaries seems to be the wrong version."
                }
            }
        },
        {
            ErrorType.Steamworks_Dll_Not_Found, new()
            {
                {
                    SystemLanguage.English, "Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.Steamworks_Init_Failed, new()
            {
                {
                    SystemLanguage.English, "Steamworks failed to initialize.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.Steamworks_Not_Launched, new()
            {
                {
                    SystemLanguage.English, "Steam is required to play this game.\n" +
                    "Make sure that it is turned on and try launching the game again."
                }
            }
        },
        {
            ErrorType.Steamworks_EnvironmentVariable_Failed, new()
            {
                {
                    SystemLanguage.English, "Steamworks failed to setup environment variables.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.System_Invalid_Namespace, new()
            {
                {
                    SystemLanguage.English, "Class with a fullpath \"{0}\" has bad full class name\n" +
                    "If this issue persists - try validating game files with Steam."
                }
            }
        },
    };

    }
}
