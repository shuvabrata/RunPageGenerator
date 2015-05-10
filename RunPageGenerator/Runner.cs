using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RunResults
{
    [Serializable]
    public class Runner
    {
        public string Rank { get; set; }

        public string Name { get; set; }

        public string BibNo { get; set; }

        public int BibNoInt { get; set; }

        public object Gender { get; set; }

        public string GenderRank { get; set; }

        public string Category { get; set; }

        public string CategoryRank { get; set; }

        public string NetTime { get; set; }

        public string GrossTime { get; set; }

        public string EventId { get; set; }

        public Runner(string bibNo, string eventId)
        {
            BibNo = bibNo;
            EventId = eventId;
        }

        public Runner()
        {
            // TODO: Complete member initialization
        }

        public void ParseAndLoad()
        {
            //Make the get request
            string url = BuildUrl(BibNo, EventId);

            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(
              url);
            // Get the response.
            using (WebResponse response = request.GetResponse())
            {
                // Display the status.
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                using (Stream dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        // Read the content.
                        string responseFromServer = reader.ReadToEnd();
                        // Display the content.
                        //Console.WriteLine(responseFromServer);
                        // Clean up the streams and the response.
                        ParseResponse(responseFromServer);
                    }
                }
            }
        }

        public void ParseResponse(string responseFromServer)
        {
            //responseFromServer = System.IO.File.ReadAllText(@"..\..\test.html");

            List<string> tableCells = new List<string>();
            string startTag = "<TD ";
            string endTag = "</TD>";
            int startIndex = 0;
            int endIndex = 0;

            while (true)
            {
                startIndex = responseFromServer.IndexOf(startTag, startIndex);
                if (startIndex == -1) break;

                endIndex = responseFromServer.IndexOf(endTag, startIndex);
                if (endIndex == -1) break;

                string cell = responseFromServer.Substring(startIndex, endIndex - startIndex);
                tableCells.Add(cell);
               
                //Move the startIndex for the next lookup. 
                startIndex += startTag.Length;

                if (startIndex > responseFromServer.Length)
                {
                    break;
                }
            }

            //Loop over each cell and look for certain patterns.
            for (int i = 0; i < tableCells.Count; i++)
            {
                if (IsBibNo(tableCells[i]))
                {
                    BibNo = GetBibNo(tableCells, i);
                    int temp;
                    if (Int32.TryParse(BibNo, out temp))
                    {
                        BibNoInt = temp;
                    }
                }

                if (IsName(tableCells[i]))
                {
                    Name = GetName(tableCells, i);
                }

                if (IsGender(tableCells[i]))
                {
                    Gender = GetGender(tableCells, i);
                }

                if (IsCategory(tableCells[i]))
                {
                    Category = GetCategory(tableCells, i);
                }

                if (IsRank(tableCells[i]))
                {
                    Rank = GetRank(tableCells, i);
                }

                if (IsCategoryRank(tableCells[i]))
                {
                    CategoryRank = GetCategoryRank(tableCells, i);
                }

                if (IsGenderRank(tableCells[i]))
                {
                    GenderRank = GetGenderRank(tableCells, i);
                }

                if (IsNetTime(tableCells[i]))
                {
                    NetTime = GetNetTime(tableCells, i);
                }

                if (IsGrossTime(tableCells[i]))
                {
                    GrossTime = GetGrossTime(tableCells, i);
                }
            }

        }

        private bool IsGrossTime(string p)
        {
            return p.Contains("Gross Time");
        }

        private string GetGrossTime(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private bool IsNetTime(string p)
        {
            return p.Contains("Net Time");
        }

        private string GetNetTime(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private bool IsGenderRank(string p)
        {
            return p.Contains("Gender Rank") && !p.Contains(":");
        }

        private string GetGenderRank(List<string> tableCells, int i)
        {
            string rank = GetPopResult1(tableCells, i).Replace("Finishers", "");
            if (rank == "")
            {
                rank = "N/A";
            }
            return rank;
        }

        private bool IsCategoryRank(string p)
        {
            return p.Contains("Category Rank") & !p.Contains(":");
        }

        private string GetCategoryRank(List<string> tableCells, int i)
        {
            string rank = GetPopResult1(tableCells, i).Replace("Finishers", "");
            if (rank == "")
            {
                rank = "N/A";
            }
            return rank;
        }

        private bool IsRank(string p)
        {
            return p.Contains("Rank") && !p.Contains(":") && !IsCategoryRank(p) && !IsGenderRank(p);
        }

        private string GetRank(List<string> tableCells, int i)
        {
            string rank = GetPopResult1(tableCells, i).Replace("Finishers", "");
            if (rank == "")
            {
                rank = "N/A";
            }
            return rank;
        }

        private bool IsCategory(string p)
        {
            return p.Contains(">Category");
        }

        private string GetCategory(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private bool IsGender(string p)
        {
            return p.Contains(">Gender") && !p.Contains(":");
        }

        private object GetGender(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private bool IsName(string p)
        {
            return p.Contains("Name");
        }

        private string GetName(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private string GetBibNo(List<string> tableCells, int i)
        {
            return GetPopResult1(tableCells, i);
        }

        private bool IsBibNo(string line)
        {
            return line.Contains("Bib Number");
        }

        private string GetPopResult1(List<string> tableCells, int i)
        {
            string value = "NA";
            try
            {
                string lineOfInterest = GetLineOfInterest(tableCells, i);

                //<TD class="popresult">1614
                string pattern = "popresult\">";
                int startIndex = lineOfInterest.IndexOf(pattern);
                if (startIndex == -1)
                    return "NA";

                startIndex += pattern.Length;

                int endIndex = lineOfInterest.IndexOf("<span", startIndex);
                if (endIndex == -1)
                {
                    endIndex = lineOfInterest.Length;
                }
                value = lineOfInterest.Substring(startIndex, endIndex - startIndex);
                value.Trim();
                value = value.Replace("\t", "");
                value = value.Replace("\n", "");
                value = value.Replace("\r", "");
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
            return value;
        }

        private string GetLineOfInterest(List<string> tableCells, int i)
        {
            while (!tableCells[i].Contains("popresult"))
            {
                i++;
            }
            return tableCells[i];
        }

        private string BuildUrl(string bibNo, string eventId)
        {
            string format = @"http://www.timingindia.com/beta/includes/details.php?encode=no&bib={0}&tble={1}&format=xml";
            return String.Format(format, bibNo, eventId);
        }

        public bool IsValid
        {
            get
            {
                return (BibNoInt != 0);
            }         
        }

       
    }
}
