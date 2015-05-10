using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RunResults
{
    class Program
    {
        private static readonly string CAHCE_FILE = "cache.dat";
        private static readonly string CAHCE_FILE_XLS = "cache.csv";
        private static readonly int ITEMS_PER_PAGE = 1000;

        public static readonly int SYNC_INTERVAL = 1;
        static void Main(string[] args)
        {
            string eventId = "timing_r1405_benw10k_elite";

            Dictionary<int, int> bibRanges = new Dictionary<int, int>(1);

            Console.WriteLine("Enter bib range as: NNN NNN. Press enter to end entering ranges.");
            
            string line = "";

            while (true)
            {
                line = Console.ReadLine();
                
                if (line == "") break;
                string[] split = line.Split(' ');
                if (split.Length != 2)
                {
                    Console.WriteLine("Invalid range. Enter NNN NNN. Example: 1000 2000"); continue;
                }
                int start, end;
                if (!Int32.TryParse(split[0], out start) || !Int32.TryParse(split[1], out end))
                {
                    Console.WriteLine("Invalid range. Enter NNN NNN. Example: 1000 2000"); continue;
                }
                if (start > end)
                {
                    Console.WriteLine("Invalid range. Enter NNN NNN. Example: 1000 2000"); continue;
                }
                bibRanges.Add(start, end);
            }

            if (bibRanges.Count == 0)
            {
                Console.WriteLine("No range provided. Exiting");
                Environment.Exit(1);
            }

            SortedDictionary<int, Runner> sortedRunners = new SortedDictionary<int, Runner>();

            //Load runners if have already got them in a previous attempt.
            LoadSortedRunners(sortedRunners);

            Console.WriteLine("Starting to work..");
            int completed = 0;
            int completed_success = 0;
            int completed_failed = 0;
            //int completed_invalid = 0;

            try
            {
                foreach (KeyValuePair<int, int> pair in bibRanges)
                {
                    for (int bibNo = pair.Key; bibNo <= pair.Value; bibNo++)
                    {
                        if (sortedRunners.ContainsKey(bibNo))
                        {
                            //We already have it.
                            continue;
                        }

                        completed++;
                        Runner runner = new Runner(bibNo.ToString(), eventId);
                        try
                        {
                            Console.WriteLine("Getting runner # {0}", bibNo);
                            runner.ParseAndLoad();
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Could not get runner with bib # {0}. Reason = {1}", bibNo, exp.Message);
                            completed_failed++;
                            continue;
                        }

                        //if (runner.IsValid)
                        //{
                            /*
                            for (int i = 0; i < 10002; i++)
                            {
                                completed_success++;
                                sortedRunners.Add(i, runner);
                            }
                             * */
                            
                            completed_success++;

                            sortedRunners.Add(bibNo, runner);
                            
                        //}
                        //else
                        //{
                         //   completed_invalid++;
                        //}
                        Console.WriteLine("Completed {0} entries", completed);

                        if (completed % SYNC_INTERVAL == 0)
                        {
                            Console.WriteLine("Serializing {0} items", sortedRunners.Count);
                            WriteSortedRunners(sortedRunners);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                Console.WriteLine("Serializing {0} items", sortedRunners.Count);
                WriteSortedRunners(sortedRunners);
            }
            //Got all runners.
            CreateHtmlPage(sortedRunners);
        }

        private static void WriteSortedRunners(SortedDictionary<int, Runner> sortedRunners)
        {
            WriteSortedRunnersToCahce(sortedRunners);
            WriteSortedRunnersToXls(sortedRunners);
        }

        private static void LoadSortedRunners(SortedDictionary<int, Runner> sortedRunners)
        {
            LoadSortedRunnersFromCache(sortedRunners);
        }

        private static void CreateHtmlPage(SortedDictionary<int, Runner> zombiedSortedRunners)
        {
            SortedDictionary<int, Runner> sortedRunners = new SortedDictionary<int, Runner>();
            //sorted runners conatains lots of invalid runners. Runners who dont exist. Remove them
            foreach (var pair in zombiedSortedRunners)
            {
                if (pair.Value.IsValid)
                {
                    sortedRunners.Add(pair.Key, pair.Value);
                }
            }

            //How many pages?
            int pages = sortedRunners.Count / ITEMS_PER_PAGE;
            if (sortedRunners.Count % ITEMS_PER_PAGE !=0 )
            {
                pages++;
            }

            Console.WriteLine("Total runners = {0}. Number of pages = {1}", sortedRunners.Count, pages);

            string text = System.IO.File.ReadAllText(@"..\..\sample_table.html");
            string[] split = text.Split('$');

            for (int page = 1; page <= pages; page++)
            {
                int startIndex = (page - 1) * ITEMS_PER_PAGE;
                int endIndex = startIndex + ITEMS_PER_PAGE -1;

                //if last page, then we dont have all elements
                if (page == pages)
                {
                    endIndex = startIndex + (sortedRunners.Count % ITEMS_PER_PAGE) - 1;
                }

                Console.WriteLine("Generating page# {0}. Start index = {1} End index = {2}",
                    page, startIndex, endIndex);

                using (StreamWriter writer = new StreamWriter("run_" + page + ".html", false))
                {
                    //Write the first part of the page
                    writer.Write(split[0]);

                    //Write the pages links.
                    writer.Write(GenerateLinks(page, pages));

                    //Write the second split
                    writer.Write(split[1]);

                    //Write the dynamic part. Rows.
                    //foreach (var rpair in sortedRunners)
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        var rpair = sortedRunners.ElementAt(i);
                        Runner r = rpair.Value;
                        string row =
                            String.Format(@"<tr>
<td>{0}</td>
<td>{1}</td>
<td>{2}</td>
<td>{3}</td>
<td>{4}</td>
<td>{5}</td>
<td>{6}</td>
<td>{7}</td>
<td>{8}</td>
", r.Rank, r.Name, r.BibNo, r.Gender, r.GenderRank, r.Category, r.CategoryRank, r.NetTime, r.GrossTime);
                        writer.Write(row);
                    }

                    //Write the last part of the page
                    writer.Write(split[2]);

                    //Write the pages links.
                    writer.Write(GenerateLinks(page, pages));

                    writer.Write(split[3]);
                }
            }
        }

        private static string GenerateLinks(int page, int pages)
        {
            string line = "";
            for (int i = 1; i <= pages; i++)
            {
                string link = i.ToString();
                if (i == page)
                {
                    line += "<b>" + link + "</b> ";
                }
                else
                {
                    line += "<a href=\"run_" + i + ".html\">" + link + "</a> ";
                }
            }
            return line;
        }

        private static void WriteSortedRunnersToXls(SortedDictionary<int, Runner> sortedRunners)
        {
            if ((sortedRunners == null) || (sortedRunners.Count == 0))
            {
                return;
            }
            try
            {
                using (StreamWriter writer = new StreamWriter(CAHCE_FILE_XLS, false))
                {
                    foreach (var pair in sortedRunners)
                    {
                        Runner r = pair.Value;
                        string row =
                            String.Format("{0},{1},{2},{4},{3},{5},{6},{7},{8}", r.Rank, r.Name, r.BibNo, r.Gender, r.GenderRank,
                            r.Category, r.CategoryRank,Csv.Escape(r.NetTime), Csv.Escape(r.GrossTime));
                        writer.WriteLine(row);
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Could not write runner. Reason = {0}", exp.Message);
            }
            
        }
        private static void LoadSortedRunnersFromXls(SortedDictionary<int, Runner> sortedRunners)
        {
            if (sortedRunners == null)
            {
                sortedRunners = new SortedDictionary<int, Runner>();
            }

            try
            {
                string[] lines = System.IO.File.ReadAllLines(CAHCE_FILE_XLS);
                foreach (string line in lines)
                {
                    Runner r = new Runner();
                    string[] split = line.Split(',');
                    
                    r.Rank = split[0];
                    r.Name = split[1];
                    r.BibNo = split[2];
                    Int32 tmp; Int32.TryParse(split[2], out tmp); r.BibNoInt = tmp;
                    r.Gender = split[3];
                    r.GenderRank = split[4];
                    r.Category = split[5];
                    r.CategoryRank = split[6];
                    r.NetTime = split[7];
                    r.GrossTime = split[8];
                    sortedRunners.Add(r.BibNoInt, r);
                }

            }

            
            catch (FileNotFoundException )
            {
                Console.WriteLine("No cache file found");
            }
            catch (Exception exp)
            {
                Console.WriteLine("Could not load runner. Reason = {0}", exp.Message);
            }
        }       

        private static void WriteSortedRunnersToCahce(SortedDictionary<int, Runner> sortedRunners)
        {
            if ((sortedRunners == null) || (sortedRunners.Count == 0))
            {
                return;
            }
            using (FileStream fs = new FileStream(CAHCE_FILE, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    IFormatter formatter = new BinaryFormatter();
                    writer.Write(sortedRunners.Count);
                    foreach (var pair in sortedRunners)
                    {
                        writer.Write(pair.Key);
                        formatter.Serialize(fs, pair.Value);
                    }
                }
            }
        }
        
        private static void LoadSortedRunnersFromCache(SortedDictionary<int, Runner> sortedRunners)
        {
            if (sortedRunners == null)
            {
                sortedRunners = new SortedDictionary<int, Runner>();
            }
            IFormatter formatter = new BinaryFormatter();

            try
            {
                using (FileStream fs = new FileStream(CAHCE_FILE, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            int bibNoInt = reader.ReadInt32();
                            Runner runner = (Runner)formatter.Deserialize(fs);
                            sortedRunners.Add(bibNoInt, runner);
                        }
                    }
                }
            }
            catch (FileNotFoundException )
            {
                Console.WriteLine("No cache file found");
            }
        }       
    }


}
