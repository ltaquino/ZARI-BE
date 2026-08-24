namespace ZARI.Application.Helpers;

public class FullNameFormatter
    {
        public string WithMiddleNameAndSuffixName(string firstName, string middleName, string lastName, string suffixName)
        {
            string first = string.IsNullOrWhiteSpace(firstName) ? "" : " " + firstName;
            string last = string.IsNullOrWhiteSpace(lastName) ? "" : " " + lastName;
            string middle = string.IsNullOrWhiteSpace(middleName) ? "" : " " + middleName;
            string suffix = string.IsNullOrWhiteSpace(suffixName) ? "" : " " + suffixName;

            string first_and_middle = middle == "" ? first == "" ? "" : first.Trim() : first.Trim() + " " + middle.Trim();
            string last_and_suffix = suffix == "" ? last == "" ? "" : last.Trim() : last.Trim() + " " + suffix.Trim();

            var mediator = first_and_middle == "" ? "" : last_and_suffix == "" ? "" : " ";
            return first_and_middle + mediator + last_and_suffix;
        }
    }
