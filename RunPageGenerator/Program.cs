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
        
        private static readonly int ITEMS_PER_PAGE = 1000;

        private static readonly int SYNC_INTERVAL = 10;

        private static string tcs10k2014 = "timing_r1405_benw10k_elite";
        private static string tcs10k2015 = "timing_r1505_benw10k_open_10k";

        static void Main(string[] args)
        {
            
            string eventId; 

            Console.WriteLine("Enter Event id: ");
            eventId = Console.ReadLine();

            if (eventId == "")
            {
                eventId = tcs10k2015;
            }

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

            SortedDictionary<int, Runner> bibSortedRunners = new SortedDictionary<int, Runner>();

            //Load runners if have already got them in a previous attempt.
            Cache.LoadSortedRunners(bibSortedRunners);

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
                        if (bibSortedRunners.ContainsKey(bibNo))
                        {
                            //We already have it.
                            continue;
                        }

                        completed++;
                        Runner runner = new Runner(bibNo.ToString(), eventId);
                        try
                        {
                            //Console.WriteLine("Getting runner # {0}", bibNo);
                            runner.ParseAndLoad();
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Could not get runner with bib # {0}. Reason = {1}", bibNo, exp.Message);
                            completed_failed++;
                            continue;
                        }

                        completed_success++;

                        //Add based on bibNo.
                        bibSortedRunners.Add(bibNo, runner);
                       
                        //Console.WriteLine("Completed {0} entries", completed);

                        if (completed % SYNC_INTERVAL == 0)
                        {
                            Console.WriteLine("....Serializing {0} items ....", bibSortedRunners.Count);
                            Cache.WriteSortedRunners(bibSortedRunners);
                        }
                    }
                }
                //Done. Write finally again.
                Console.WriteLine("Serializing {0} items", bibSortedRunners.Count);
                Cache.WriteSortedRunners(bibSortedRunners);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                Console.WriteLine("Serializing {0} items", bibSortedRunners.Count);
                Cache.WriteSortedRunners(bibSortedRunners);
            }
            //Got all runners.
            CreateHtmlPage(bibSortedRunners);
            CreateCsvFile(bibSortedRunners);
        }

    
        private static void CreateCsvFile(SortedDictionary<int, Runner> zombiedSortedRunners)
        {
            SortedDictionary<int, Runner> rankSortedRunners = new SortedDictionary<int, Runner>();
            //sorted runners conatains lots of invalid runners. Runners who dont exist. Remove them
            foreach (var pair in zombiedSortedRunners)
            {
                if (pair.Value.IsValid)
                {
                    //Console.WriteLine("Rank = {0}, RankInt = {1}", pair.Value.Rank, pair.Value.RankInt);
                    rankSortedRunners.Add(pair.Value.RankInt, pair.Value);
                }
                else
                {
                    // Console.WriteLine("Invalid:  Rank = {0}, RankInt = {1} Bib = {2}", pair.Value.Rank, pair.Value.RankInt, pair.Value.BibNo);
                }
            }

            using (StreamWriter writer = new StreamWriter("run" + ".csv", false))
            {
                writer.WriteLine("Rank, Name, BibNo, Gender, GenderRank, Category, CategoryRank, NetTime, GrossTime");
                foreach (var pair in rankSortedRunners)
                {
                    Runner r = pair.Value;
                    string row =
                            String.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},{8}", 
                            r.Rank, r.Name, r.BibNo, r.Gender, r.GenderRank, r.Category, r.CategoryRank,Csv.Escape( r.NetTime), Csv.Escape(r.GrossTime));
                    writer.WriteLine(row);
                }
            }
        }
        

        private static void CreateHtmlPage(SortedDictionary<int, Runner> zombiedSortedRunners)
        {
            SortedDictionary<int, Runner> rankSortedRunners = new SortedDictionary<int, Runner>();
            //sorted runners conatains lots of invalid runners. Runners who dont exist. Remove them
            foreach (var pair in zombiedSortedRunners)
            {
                if (pair.Value.IsValid)
                {
                    //Console.WriteLine("Rank = {0}, RankInt = {1}", pair.Value.Rank, pair.Value.RankInt);
                    rankSortedRunners.Add(pair.Value.RankInt, pair.Value);
                }
                else
                {
                   // Console.WriteLine("Invalid:  Rank = {0}, RankInt = {1} Bib = {2}", pair.Value.Rank, pair.Value.RankInt, pair.Value.BibNo);
                }
            }

            //How many pages?
            int pages = rankSortedRunners.Count / ITEMS_PER_PAGE;
            if (rankSortedRunners.Count % ITEMS_PER_PAGE !=0 )
            {
                pages++;
            }

            Console.WriteLine("Total runners = {0}. Number of pages = {1}", rankSortedRunners.Count, pages);

            string text = System.IO.File.ReadAllText(@"..\..\sample_table.html");
            string[] split = text.Split('$');

            for (int page = 1; page <= pages; page++)
            {
                int startIndex = (page - 1) * ITEMS_PER_PAGE;
                int endIndex = startIndex + ITEMS_PER_PAGE -1;

                //if last page, then we dont have all elements
                if (page == pages)
                {
                    endIndex = startIndex + (rankSortedRunners.Count % ITEMS_PER_PAGE) - 1;
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
                        var rpair = rankSortedRunners.ElementAt(i);
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
", r.Rank, r.NameHref, r.BibNo, r.Gender, r.GenderRank, r.Category, r.CategoryRank, r.NetTime, r.GrossTime);
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

    }


}
