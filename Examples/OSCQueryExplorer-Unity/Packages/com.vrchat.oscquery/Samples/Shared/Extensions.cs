using System.Text.RegularExpressions;

namespace VRC.OSCQuery.Samples.Shared
{
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }

    }
}