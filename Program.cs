
using Microsoft.Win32;
using System.Text.Json;
using System.Reflection;


// Realse
// dotnet publish -c Release

namespace COM3D2_DLC_Checker
{

    class Program
    {
        //  =============== Variables ===============
        static readonly string DlcListFileName = "COM3D2_dlc_list.json";

        static readonly string DlcListUrl = $"https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/{DlcListFileName}";

        static readonly string RepoUrl = "github.com/Tankerch/COM3D2_DLC_Checker";

        //  =============== END Variables ===============

        static readonly string DlcListPath = Path.Combine(Directory.GetCurrentDirectory(), DlcListFileName);

        static readonly string InstalledKey = "installed";
        static readonly string NotInstalledKey = "notInstalled";

        static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {

            PrintHeader();

            // Get DLC list from Cloud
            // User can connect to cloud?
            // - Yes    : Save cloud data to local file
            // - No     : Use local data at 'DlcListFileName.json' file
            GetDLClistFromCloud().GetAwaiter().GetResult();

            // Read from DLC list
            Dictionary<string, List<string>>? dlcList = null;
            try
            {
                dlcList = ReadDLClist();
            }
            catch (FileNotFoundException)
            {
                ConsoleColor(System.ConsoleColor.Red, $"\"{DlcListPath}\" doesn't exist,\nConnect to the internet to download it automatically");
            }
            catch (FormatException)
            {
                ConsoleColor(System.ConsoleColor.Red, "Failed to read DLC files");
            }
            catch (InvalidDataException)
            {
                ConsoleColor(System.ConsoleColor.Red, "Using outdated apps to read DLC list, try to update this app");
            }
            if (dlcList == null) Exit();

            // Read from game directory
            List<string> gameFiles = ReadFilesFromGameDirectory();

            // Validate DLC
            Dictionary<string, List<string>> result = CompareListToGameFiles(dlcList!, gameFiles);

            // Print result
            PrintList(result[InstalledKey], result[NotInstalledKey]);

            Exit();
        }

        static void PrintHeader()
        {
            ConsoleColor(System.ConsoleColor.Cyan, "===========================================================================================");
            ConsoleColor(System.ConsoleColor.Cyan, $"COM_DLC_Checker     |   {RepoUrl}");
            ConsoleColor(System.ConsoleColor.Cyan, "===========================================================================================");
        }

        static async Task GetDLClistFromCloud()
        {
            try
            {
                string responseBody = await client.GetStringAsync(DlcListUrl);

                // Write to local file
                using StreamWriter writer = new StreamWriter(DlcListFileName);
                writer.Write(responseBody);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Can't connect to internet, offline file will be used");
            }

        }

        static Dictionary<string, List<string>> ReadDLClist()
        {
            // string testString = @"{
            //     ""Version"": 25,
            //     ""Items"": [
            //      {
            //         ""Name"": ""[COM3D2 Compatible Update Patch]"",
            //         ""Files"": [""csv_old.arc""]
            //      },
            //     ]
            // }";
            string jsonString = File.ReadAllText(DlcListPath);
            DLCList? result = JsonSerializer.Deserialize<DLCList>(jsonString);
            if (result == null)
            {
                throw new FormatException();
            }

            // Validate DLC version with Apps
            bool isValid = true;
            try
            {
                isValid = CheckDLCListVersion(result.AppMinVersion);
                if (!isValid)
                {
                    throw new InvalidDataException();
                }
            }
            catch
            {
                throw new InvalidDataException();
            }

            return result.Items.ToDictionary(keySelector: item => item.Name, item => item.Files);
        }

        static bool CheckDLCListVersion(string minVersionString)
        {
            // Min version
            SemanticVersioning.Version minVersion = new SemanticVersioning.Version(minVersionString);

            // Current version
            string? currentVersionString = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

            // Forced to continue when AppVersion is null/missing
            if (currentVersionString == null) return true;

            // Convert AssemblyVersion to SemanticVersion 
            currentVersionString = currentVersionString.Remove(currentVersionString.Length - 2);

            // Compare
            SemanticVersioning.Version currentVersion = new SemanticVersioning.Version(currentVersionString);
            return new SemanticVersioning.Range($"~{minVersionString}").IsSatisfied(currentVersion);
        }


        static string GetCOM3D2installPath()
        {
            // Default: Current Directory of COM3D2_DLC_Checker
            // Will replaced by COM3D2 InstallPath Registry
            const string keyName = "HKEY_CURRENT_USER" + "\\" + "SOFTWARE\\KISS\\カスタムオーダーメイド3D2";

            string? gameDirectoryRegistry = Registry.GetValue(keyName, "InstallPath", "") as string;

            if (gameDirectoryRegistry != null)
            {
                return gameDirectoryRegistry;
            }
            else
            {
                ConsoleColor(System.ConsoleColor.Yellow, "Warning : COM3D2 installation directory is not set in registry. Will using current directory'");
                return Directory.GetCurrentDirectory();
            }
        }

        static List<string> ReadFilesFromGameDirectory()
        {
            string gameRootDir = GetCOM3D2installPath();
            // string gameRootDir = "D:\\Games\\COM3D2";
            string gamedataDir = gameRootDir + "\\GameData";
            string gamedata20Dir = gameRootDir + "\\GameData_20";

            List<string> gamedataList = new List<string>();

            try
            {
                gamedataList.AddRange(GetFilesFromDirectory(gamedataDir));
                gamedataList.AddRange(GetFilesFromDirectory(gamedata20Dir));
            }
            catch
            {
                ConsoleColor(System.ConsoleColor.Red, $"Failed to read Gamedata directory at: {gameRootDir}");
                Exit();
            }

            return gamedataList;
        }

        static IEnumerable<string> GetFilesFromDirectory(string path)
        {
            return Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).TakeWhile(file => file != null).Cast<string>();
        }

        static Dictionary<string, List<string>> CompareListToGameFiles(Dictionary<string, List<string>> dlcList, List<string> gameFiles)
        {
            List<string> installedDlc = new List<string>();
            List<string> notInstalledDlc = new List<string>();

            // Loop for all DLC items
            foreach (KeyValuePair<string, List<string>> item in dlcList)
            {
                bool isSubset = !item.Value.Except(gameFiles).Any();

                // Installed
                if (isSubset)
                {
                    installedDlc.Add(item.Key);
                    continue;
                }

                // Not Installed
                notInstalledDlc.Add(item.Key);
            }

            return new Dictionary<string, List<string>>(){
               {InstalledKey, installedDlc},
               {NotInstalledKey, notInstalledDlc},
            };
        }

        static void PrintList(List<string> installedDlc, List<string> notInstalledDlc)
        {
            ConsoleColor(System.ConsoleColor.Green, "\nFully Installed:");
            foreach (string dlc in installedDlc)
            {
                Console.WriteLine(dlc);
            }

            ConsoleColor(System.ConsoleColor.Yellow, "\nIncompleted/Not Installed :");
            foreach (string dlc in notInstalledDlc)
            {
                Console.WriteLine(dlc);
            }
        }

        static void Exit()
        {
            Console.WriteLine("\nPress 'Enter' to exit the process...");
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    System.Environment.Exit(0);
                }
            }
        }

        // Utils
        static void ConsoleColor(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}