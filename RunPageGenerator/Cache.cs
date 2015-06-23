using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace RunResults
{
    public class Cache
    {
        private static readonly string CAHCE_FILE = "cache.dat";
        private static readonly string CAHCE_FILE_XLS = "cache.csv";

        public static void WriteSortedRunners(SortedDictionary<int, Runner> sortedRunners)
        {
            WriteSortedRunnersToCahce(sortedRunners);
            WriteSortedRunnersToXls(sortedRunners);
        }

        public static void LoadSortedRunners(SortedDictionary<int, Runner> sortedRunners)
        {
            LoadSortedRunnersFromCache(sortedRunners);
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
                            String.Format("{0},{1},{2},{4},{3},{5},{6},{7},{8},{9}", r.Rank, r.Name, r.NameHref, r.BibNo, r.Gender, r.GenderRank,
                            r.Category, r.CategoryRank, Csv.Escape(r.NetTime), Csv.Escape(r.GrossTime));
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
                    int i = 0;

                    r.Rank = split[i++];
                    Int32 tmp; Int32.TryParse(r.Rank, out tmp); r.RankInt = tmp;
                    r.Name = split[i++];
                    r.NameHref = split[i++];
                    r.BibNo = split[i++];
                    Int32.TryParse(r.BibNo, out tmp); r.BibNoInt = tmp;
                    r.Gender = split[i++];
                    r.GenderRank = split[i++];
                    r.Category = split[i++];
                    r.CategoryRank = split[i++];
                    r.NetTime = split[i++];
                    r.GrossTime = split[i++];
                    sortedRunners.Add(r.BibNoInt, r);
                }

            }


            catch (FileNotFoundException)
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
            catch (FileNotFoundException)
            {
                Console.WriteLine("No cache file found");
            }
        }       
    }
}
