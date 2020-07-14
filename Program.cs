using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Input;
using Microsoft.Win32;

namespace COM3D2_DLC_Checker
{

    class Program
    {

        // Variabels
        static readonly string DLC_URL = "https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/COM_NewListDLC.lst";
        static readonly string DLC_LIST_PATH = Path.Combine(Directory.GetCurrentDirectory(), "COM_NewListDLC.lst");

        static void Main(string[] args)
        {
            PRINT_HEADER();

            // HTTP_RESOPOND
            //  - Item1 = HTTP Status Code
            //  - Item2 = Internet DLC List content
            Tuple<HttpStatusCode, string> HTTP_RESPOND = CONNECT_TO_INTERNET(DLC_URL);

            if (HTTP_RESPOND.Item1 == HttpStatusCode.OK)
            {
                Console.WriteLine("Connected to {0}", DLC_URL);
                UPDATE_DLC_LIST(HTTP_RESPOND.Item2);
            }
            else
            {
                Console.WriteLine("Can't connect to internet, offline file will be used");
            }

            // DLC LIST = [DLC_FILENAME, DLC_NAME]
            IDictionary<string, string> DLC_LIST = READ_DLC_LIST();
            List<string> GAMEDATA_LIST = READ_GAMEDATA();

            // DLC LIST SORTED
            // Item 1 = INSTALLED_DLC
            // Item 2 = NOT_INSTALLED_DLC
            Tuple<List<string>, List<string>> DLC_LIST_SORTED = COMPARE_DLC(DLC_LIST, GAMEDATA_LIST);

            PRINT_DLC(DLC_LIST_SORTED.Item1, DLC_LIST_SORTED.Item2);

            EXIT_PROGRAM();
        }

        static void PRINT_HEADER()
        {
            CONSOLE_COLOR(ConsoleColor.Cyan, "===========================================================================================");
            CONSOLE_COLOR(ConsoleColor.Cyan, "COM_DLC_Checker     |   Github.com/Tankerch/COM3D2_DLC_Checker");
            CONSOLE_COLOR(ConsoleColor.Cyan, "===========================================================================================");
        }

        static Tuple<HttpStatusCode, string> CONNECT_TO_INTERNET(string DLC_URL)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(DLC_URL);
            HttpWebRequest request = httpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using Stream stream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(stream);

            return new Tuple<HttpStatusCode, string>(response.StatusCode, reader.ReadToEnd());
        }

        static void UPDATE_DLC_LIST(string UPDATED_CONTENT)
        {
            using StreamWriter writer = new StreamWriter(DLC_LIST_PATH);
            writer.Write(UPDATED_CONTENT);
        }

        static IDictionary<string, string> READ_DLC_LIST()
        {
            // Skip 1 = Remove version header
            var DLC_LIST_UNFORMATED = File.ReadAllLines(DLC_LIST_PATH, Encoding.UTF8)
                .Skip(1)
                .ToList();

            // DLC_LIST_FORMAT = [Keys = DLC_Filename, Value = DLC_Name]
            IDictionary<string, string> DLC_LIST_FORMATED = new Dictionary<string, string>();

            foreach (string DLC_LIST in DLC_LIST_UNFORMATED)
            {
                String[] temp_strlist = DLC_LIST.Split(',');
                DLC_LIST_FORMATED.Add(temp_strlist[0], temp_strlist[1]);
            }

            return DLC_LIST_FORMATED;
        }

        static string GET_COM3D2_INSTALLPATH()
        {
            // Default: Current Directory of COM3D2_DLC_Checker
            // Will replaced by COM3D2 InstallPath Registry
            const string keyName = "HKEY_CURRENT_USER" + "\\" + "SOFTWARE\\KISS\\カスタムオーダーメイド3D2";

            string GAME_DIRECTORY_REGISTRY = (string)Registry.GetValue(keyName,"InstallPath","");

            if (GAME_DIRECTORY_REGISTRY != null)
            {
                return GAME_DIRECTORY_REGISTRY;
            }
            else
            {
                CONSOLE_COLOR(ConsoleColor.Yellow, "Warning : COM3D2 installation directory is not set in registry. Will using work directory', 'yellow'");
                return Directory.GetCurrentDirectory();
            }
        }

        static List<string> READ_GAMEDATA()
        {
            string GAME_DIRECTORY = GET_COM3D2_INSTALLPATH();
            string GAMEDATA_DIRECTORY = GAME_DIRECTORY + "\\GameData";
            string GAMEDATA_20_DIRECTORY = GAME_DIRECTORY + "\\GameData_20";

            List<string> GAMEDATA_LIST = new List<string>();

            GAMEDATA_LIST.AddRange(Directory.GetFiles(@GAMEDATA_DIRECTORY, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName));
            GAMEDATA_LIST.AddRange(Directory.GetFiles(@GAMEDATA_20_DIRECTORY, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName));

            return GAMEDATA_LIST;
        }

        static Tuple<List<string>,List<string>> COMPARE_DLC(IDictionary<string, string> DLC_LIST, List<string> GAMEDATA_LIST)
        {
            // DLC LIST = [DLC_FILENAME, DLC_NAME]
            List<string> DLC_FILENAMES = new List<string>(DLC_LIST.Keys);
            List<string> DLC_NAMES= new List<string>(DLC_LIST.Values);

            List<string> INSTALLED_DLC = new List<string>(); 
            foreach(string INSTALLED_DLC_FILENAMES in DLC_FILENAMES.Intersect(GAMEDATA_LIST).ToList())
            {
                // UNIT_DLC_LIST = [DLC_FILENAME, DLC_NAME]
                foreach (KeyValuePair<string,string> UNIT_DLC_LIST in DLC_LIST)
                {
                    if (INSTALLED_DLC_FILENAMES == UNIT_DLC_LIST.Key)
                    {
                        INSTALLED_DLC.Add(UNIT_DLC_LIST.Value);
                        DLC_LIST.Remove(UNIT_DLC_LIST);
                        break;
                    }
                }
            }
            
            List<string> NOT_INSTALLED_DLC = DLC_NAMES.Except(INSTALLED_DLC).ToList();
            INSTALLED_DLC.Sort();
            NOT_INSTALLED_DLC.Sort();
            return Tuple.Create(INSTALLED_DLC, NOT_INSTALLED_DLC);
        }

        static void PRINT_DLC(List<string> INSTALLED_DLC, List<string> NOT_INSTALLED_DLC)
        {
            CONSOLE_COLOR(ConsoleColor.Cyan, "\nAlready Installed:");
            foreach (string DLC in INSTALLED_DLC)
            {
                Console.WriteLine(DLC);
            }

            CONSOLE_COLOR(ConsoleColor.Cyan, "\nNot Installed :");
            foreach (string DLC in NOT_INSTALLED_DLC)
            {
                Console.WriteLine(DLC);
            }
        }

        static void EXIT_PROGRAM()
        {
            Console.WriteLine("\nPress 'Enter' to exit the process...");
            while (true)
            {
                if (Console.ReadKey().Key != ConsoleKey.Enter)
                {
                    break;
                }
            }
        }

        // Extension
        static void CONSOLE_COLOR(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
