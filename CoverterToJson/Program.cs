using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace CoverterToJson
{

    

    class Program
    {
        #region классы для сериализации
        public class Contact
        {
            [JsonPropertyName("id")]
            [Required(ErrorMessage = "Идентификатор контакта не установлен")]
            public uint Id { get; set; }

            [Required]
            [JsonPropertyName("lastname")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Недопустимая длина фамилии")]
            public String LastName { get; set; }

            [Required]
            [JsonPropertyName("firstname")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Недопустимая длина имени")]
            public String FirstName { get; set; }

            [JsonPropertyName("middlename")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Недопустимая длина отчества")]
            public String MiddleName { get; set; }

            [Required]
            [JsonPropertyName("phone")]
            [StringLength(11, MinimumLength = 11, ErrorMessage = "Неверное количество знаков в номере телефона")]
            public String Phone { get; set; }
            public static uint Count { get; set; } = 0;
        }

        public class ContactList
        {
            private List<Contact> m_contacts = new List<Contact>();

            [JsonPropertyName("contacts")]
            public List<Contact> Contacts
            { 
              get { return m_contacts; }
              set { m_contacts = value;}
            }

            [JsonPropertyName("count")]
            public int Count => m_contacts.Count; 
        }

        #endregion

        static void Main()
        {
            string regexPat = @"^( +)?[А-ЯЁ][а-яё]+\s+[А-ЯЁ][а-яё]+(?:\s+[А-ЯЁ][а-яё]+)?";     // шаблон для ФИО
            string regexPat1 = @"\+?7? ?3?[ -]?8?[ -]?\(?(\d[ -]?){3}\)?[ -]?(\d[ -]?){7}";    // шаблон для номера телефона
            

            string catPath = System.IO.Directory.GetCurrentDirectory();
            string filePath = "data.txt";
            string inFilePath = Path.Combine(catPath, filePath);

            // метод для делегата MatchEvaluator
            static string MakePhoneUniform(Match m) 
            {
                String str = m.Value;
                StringBuilder sb = new StringBuilder();

                foreach (var ch in str)
                {
                    if (char.IsDigit(ch)) 
                    {
                        sb.Append(ch);
                    }
                }
                if(sb.Length == 10)
                {
                    sb.Insert(0, 7);
                }
                sb.Replace(sb[0], '7');

                return sb.ToString();
            }
            MatchEvaluator mEval = new MatchEvaluator(MakePhoneUniform);

            ContactList contacts = new ContactList();
            
            
            using (StreamReader sr = new StreamReader(new FileStream(inFilePath, FileMode.Open, FileAccess.Read), Encoding.Default))
            using (StreamWriter log = new StreamWriter(File.Open("log.txt", FileMode.Create, FileAccess.Write), Encoding.Default))
            {
                String str;
               
                uint line = 0;
                uint id = 1;

                while ((str = sr.ReadLine()) != null)
                {
                    Match fio = Regex.Match(str, regexPat);
                    Match phone = Regex.Match(str, regexPat1);

                    if (!fio.Success)
                    {
                        log.WriteLine($"Error: error in input file in \"FIO\" field in {line} line");

                        if (!phone.Success)
                        {
                            log.WriteLine($"Error: error in input file in \"phone\" field in {line} line");
                        }
                    }
                    else
                    {
                        string tmp = Regex.Replace(phone.Value, regexPat1, mEval);    // приведение телефона к требуемому формату

                        string[] w = fio.Value.Split(new char[] { ' ', '\t' });

                        Contact contact = new Contact
                        {
                            Id = id,
                            LastName = w[0],
                            FirstName = w[1],
                            MiddleName = (w.Length == 3 ? w[2] : null),
                            Phone = tmp
                        };

                        var context = new ValidationContext(contact);
                        var results = new List<ValidationResult>();

                        if (!Validator.TryValidateObject(contact, context, results, true))
                        {
                            foreach (var error in results)
                            {
                                log.WriteLine($"Validation error in line {line}: {error.ErrorMessage}");
                            }
                        }
                        else
                        {
                            contact.MiddleName ??= "";
                            contacts.Contacts.Add(contact);
                            id++;
                        }
                    }   

                    line++;
                }
            }

            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(JsonSerializer.Serialize(contacts,
                   new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true }));

            using (StreamWriter result = new StreamWriter(File.Open("result.txt", FileMode.Create, FileAccess.Write), Encoding.Default)) 
            {
                result.WriteLine(JsonSerializer.Serialize(contacts,
                   new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true }));
            }

        }
    }
}
