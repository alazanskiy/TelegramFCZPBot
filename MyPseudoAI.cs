using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using System.Data.SqlClient;

namespace AlazanskiyBot //change to use in local debug
{
    public class QueryHistoryBase
    {
        public Task<string> GetData(string s)
        {
            return Task.Run(() =>
            {
                if (s.StartsWith("/start"))
                {
                    return "Зиганчики, для списка слов, которые я пытаюсь понять, используй /keywords";
                }
                else
                {
                    List<AIRule> rulesList = new List<AIRule>();
                    var ruleType = typeof(AIRule);
                    var aiRules = System.Reflection.Assembly.GetAssembly(ruleType).GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.GetInterface("IAIRule") != null);
                    foreach (var air in aiRules)
                    {
                        rulesList.Add((AIRule)System.Activator.CreateInstance(air));
                    }


                    if (s.StartsWith("/keywords"))
                    {
                        string kwords = string.Empty;
                        foreach (AIRule r in rulesList)
                        {
                            kwords += $"{r.Description}, ";
                        }
                        return kwords.Substring(0, kwords.Length - 2);
                    }
                    else
                    {

                        string SqlQuery = "SELECT m.MatchDate, m.MatchTeams, m.MatchScore, t.ShortName FROM dbo.Tournaments t JOIN dbo.Matches m ON t.TournamentID = m.TournamentID  WHERE 1=1 ";


                        int predicatesCount = 0;
                        foreach (AIRule r in rulesList)
                        {
                            string rulePredicate = r.GetPredicate(s);
                            if (!string.IsNullOrEmpty(rulePredicate))
                            {
                                predicatesCount++;
                                SqlQuery += $" AND ({rulePredicate}) ";
                            }
                        }
                        
                        SqlQuery += " ORDER BY m.MDate";

                        if (predicatesCount == 0)
                            return "Извини, старина, я тебя не понял";

                        int rowsGot = 0;
                        string strResult = string.Empty;
                        SqlConnection conn = new SqlConnection(@"Server = tcp:alazanskiy.database.windows.net,1433; Database = FCZPHistory; User ID = readonlyuser@alazanskiy; Password = musorasosat#1589; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 30; ");
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = SqlQuery;
                        conn.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                strResult += $"[{rdr.GetString(3)}]{rdr.GetString(0)} {rdr.GetString(1)} {rdr.GetString(2)}\n\r";
                                rowsGot++;
                            }
                        }
                        conn.Close();
                        if (rowsGot == 0)
                            return "Чёт ничего не нашёл";
                        if (rowsGot > 100)
                            return "Слишком много нашлось инфы, попробуй уточнить";
                        return strResult;
                    }
                }
            });
        }
    }

    public interface IAIRule
    {
        string GetPredicate(string userInput);
    }

    public abstract class AIRule : IAIRule
    {
        public abstract string Description { get; }
        public abstract string GetPredicate(string userInput);
    }

    public class YearAIRule : AIRule
    {
        public override string Description => "года (в числовом виде)";

        public override string GetPredicate(string userInput)
        {
            string resultingPredicate = string.Empty;
            string[] separated = userInput.Split(' ');
            foreach (string s in separated)
            {
                if (s.Length == 2 || s.Length == 4)
                {
                    int yearNumber;
                    if (Int32.TryParse(s, out yearNumber))
                    {
                        if (string.IsNullOrEmpty(resultingPredicate))
                            resultingPredicate = "YEAR(m.MDate) = " + GetYear(s);
                        else
                            resultingPredicate += " OR YEAR(m.MDate) = " + GetYear(s);
                    }
                }

                //System.Text.RegularExpressions.Regex rLong = new System.Text.RegularExpressions.Regex(@"\d{4}-\d{4}");
                //System.Text.RegularExpressions.Regex rShort = new System.Text.RegularExpressions.Regex(@"\d{2}-\d{2}");
                string rLong = @"\d{4}-\d{4}";
                string rShort = @"\d{2}-\d{2}";
                if (System.Text.RegularExpressions.Regex.IsMatch(s, rLong, System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                     System.Text.RegularExpressions.Regex.IsMatch(s, rShort, System.Text.RegularExpressions.RegexOptions.IgnoreCase) )
                {
                    string yearBegin = string.Empty;
                    string yearEnd = string.Empty;
                    if (s.Length == 9)
                    {
                        yearBegin = s.Substring(0, 4);
                        yearEnd = s.Substring(5, 4);
                    }
                    else
                    {
                        yearBegin = s.Substring(0, 2);
                        yearEnd = s.Substring(3, 2);
                    }
                    string newPredicate = $"(YEAR(m.MDate) BETWEEN {GetYear(yearBegin)} AND {GetYear(yearEnd)})";
                    resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? newPredicate : resultingPredicate + " OR " + newPredicate);
                }
            }
            return resultingPredicate;
        }

        private string GetYear(string s)
        {
            string YearForPredicate;
            if (s.Length == 2)
            {
                YearForPredicate = Int32.Parse(s) < 36 ? "20" + s : "19" + s;
            }
            else
                YearForPredicate = s;
            return YearForPredicate;
        }
    }

    public class HomeRule : AIRule
    {
        public override string Description => "дома, гости, выезд";

        public override string GetPredicate(string userInput)
        {
            string resultingPredicate = string.Empty;
            if (userInput.Contains("гост") || userInput.Contains("выезд"))
                resultingPredicate = "m.AwayMatch = 1";
            if (userInput.Contains("дома"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "m.AwayMatch = 0" : resultingPredicate + " OR m.AwayMatch = 0");
            return resultingPredicate;
        }

    }

    public class TournamentRule : AIRule
    {
        public override string Description => "еврокубки, чемпионат, кубок";
        public override string GetPredicate(string userInput)
        {
            string resultingPredicate = string.Empty;
            if (userInput.Contains("еврокубк"))
                resultingPredicate = "t.TournamentID IN (3, 4, 8, 10, 17, 18)";
            if (userInput.Contains("чемпионат"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "t.TournamentID IN (2, 6, 11, 12)" : resultingPredicate + " OR t.TournamentID IN (2, 6, 11, 12)");
            if (userInput.Contains(" кубк") || userInput.Contains("кубок"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "t.TournamentID IN (1, 9)" : resultingPredicate + " OR t.TournamentID IN (1, 9)");
            return resultingPredicate;
        }

    }

    public class ResultRule : AIRule
    {
        public override string Description => "победа, ничья, поражение";
        public override string GetPredicate(string userInput)
        {
            string resultingPredicate = string.Empty;
            if (userInput.Contains("побед") || userInput.Contains("выигр") || userInput.Contains("выеб"))
                resultingPredicate = "m.MatchResult = 1";
            if (userInput.Contains("нич"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ?  "m.MatchResult = 2" : resultingPredicate + " OR m.MatchResult = 2");
            if (userInput.Contains("пораже") || userInput.Contains("проигра") || userInput.Contains("проеб"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "m.MatchResult = 3" : resultingPredicate + " OR m.MatchResult = 3");

            return resultingPredicate;
        }
    }

    public class RivalRule : AIRule
    {
        public override string Description => "против";
        public override string GetPredicate(string userInput)
        {
            string resultingPredicate = string.Empty;
            if (userInput.Contains("против"))
            {                
                string substr = userInput.Substring(userInput.IndexOf("против") + 6, userInput.Length - userInput.IndexOf("против") - 6).Trim();
                int spacePos = substr.IndexOf(" ");
                string teamName = string.Empty;
                if (spacePos > 0)
                {
                    teamName = substr.Substring(0, spacePos - 1).Trim();
                }
                else
                    teamName = substr;
                if (teamName.Length > 4)
                    resultingPredicate = "m.Rival LIKE N'" + teamName.Substring(0, teamName.Length - 2) + "%'";
                else
                    resultingPredicate = "m.Rival LIKE N'" + teamName + "%'";

            }
            if (userInput.Contains("мусор"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "m.Rival = N'Динамо Мск'" : resultingPredicate + " OR m.Rival = N'Динамо Мск'");
            if (userInput.Contains("кони") || userInput.Contains("коня") || userInput.Contains("коней"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "m.Rival = N'ЦСКА'" : resultingPredicate + " OR m.Rival = N'ЦСКА'");
            if (userInput.Contains("свин") || userInput.Contains("мяс"))
                resultingPredicate = (string.IsNullOrEmpty(resultingPredicate) ? "m.Rival = N'Спартак Мск'" : resultingPredicate + " OR m.Rival = N'Спартак Мск'");
            return resultingPredicate;
        }
    }

}