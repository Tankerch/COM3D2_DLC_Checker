using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

            List<string> DLC_LIST = READ_DLC_LIST();
            List<string> GAMEDATA_LIST = READ_GAMEDATA();

            COMPARE_DLC(DLC_LIST, GAMEDATA_LIST);


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

        static List<string> READ_DLC_LIST()
        {
            // Skip 1 = Remove version header
            return File.ReadAllLines(DLC_LIST_PATH)
                .Skip(1)
                .ToList();
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

            GAMEDATA_LIST.AddRange(Directory.GetFiles(@GAMEDATA_DIRECTORY, "*", SearchOption.TopDirectoryOnly));
            GAMEDATA_LIST.AddRange(Directory.GetFiles(@GAMEDATA_20_DIRECTORY, "*", SearchOption.TopDirectoryOnly));

            return GAMEDATA_LIST;
        }

        static void COMPARE_DLC(List<string> DLC_LIST, List<string> GAMEDATA_LIST)
        {
            
        }

        static void PRINT_DLC(string[] INSTALLED_DLC, string[] NOT_INSTALLED_DLC)
        {
            
        }

        static void EXIT_PROGRAM()
        {

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
