
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
        static readonly string CM_DlcListFileName = "CM3D2_dlc_list.json";
        static readonly string CM_DlcListUrl = $"https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/{CM_DlcListFileName}";

        static readonly string COM_DlcListFileName = "COM3D2_dlc_list.json";

        static readonly string COM_DlcListUrl = $"https://raw.githubusercontent.com/Tankerch/COM3D2_DLC_Checker/master/{COM_DlcListFileName}";

        static readonly string RepoUrl = "github.com/Tankerch/COM3D2_DLC_Checker";

        //  =============== END Variables ===============

        static readonly string CM_DlcListPath = Path.Combine(Directory.GetCurrentDirectory(), CM_DlcListFileName);
        static readonly string COM_DlcListPath = Path.Combine(Directory.GetCurrentDirectory(), COM_DlcListFileName);

        static readonly string InstalledKey = "installed";
        static readonly string NotInstalledKey = "notInstalled";

        static readonly HttpClient client = new HttpClient();

        static GameCode SelectedGameCode = GameCode.COM3D2;

        static void Main(string[] args)
        {

            PrintHeader();

            // Get game code selection from user
            SelectedGameCode = GetGameCode();

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
                string usedPath = SelectedGameCode == GameCode.CM3D2 ? CM_DlcListPath : COM_DlcListPath;
                ConsoleColor(System.ConsoleColor.Red, $"\"{usedPath}\" doesn't exist,\nConnect to the internet to download it automatically");
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
            ConsoleColor(System.ConsoleColor.Cyan, $"CM3D2/COM3D2 DLC Checker     |   {RepoUrl}");
            ConsoleColor(System.ConsoleColor.Cyan, "===========================================================================================");
        }

        static GameCode GetGameCode()
        {
            // Default value : COM3D2
            GameCode result = SelectedGameCode;

            Console.WriteLine("Select game you want to check (Use `Up` and `Down` arrow to select):");
            PrintGameSelection(result);
            while (true)
            {
                ConsoleKey input = Console.ReadKey().Key;
                // Enter - Exit console loop
                if (input == ConsoleKey.Enter)
                {
                    break;
                }
                // UpArrow - Select COM3D2
                if (input == ConsoleKey.UpArrow && result == GameCode.CM3D2)
                {
                    result = GameCode.COM3D2;
                    ClearGameSelectionText();
                    PrintGameSelection(result);
                }
                // DownArrow - Select CM3D2
                if (input == ConsoleKey.DownArrow && result == GameCode.COM3D2)
                {
                    result = GameCode.CM3D2;
                    ClearGameSelectionText();
                    PrintGameSelection(result);
                }
            }
            Console.WriteLine("");
            return result;
        }

        static void ClearGameSelectionText()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.SetCursorPosition(0, Console.CursorTop - 1);

        }

        static void PrintGameSelection(GameCode currentSelection)
        {
            const ConsoleColor selectedColor = System.ConsoleColor.Green;
            const ConsoleColor unselectedColor = System.ConsoleColor.White;
            ConsoleColor(currentSelection == GameCode.COM3D2 ? selectedColor : unselectedColor, "- COM3D2 (Custom Order Maid 3D 2)");
            ConsoleColor(currentSelection == GameCode.CM3D2 ? selectedColor : unselectedColor, "- CM3D2 (Custom Maid 3D 2)");
        }

        static async Task GetDLClistFromCloud()
        {
            try
            {
                string url = COM_DlcListUrl;
                string fileName = COM_DlcListFileName;

                // Change to CM3D2
                if (SelectedGameCode == GameCode.CM3D2)
                {
                    url = CM_DlcListUrl;
                    fileName = CM_DlcListFileName;
                }
                string responseBody = await client.GetStringAsync(url);

                // Write to local file
                using StreamWriter writer = new StreamWriter(fileName);
                writer.Write(responseBody);
            }
            catch (HttpRequestException)
            {
                ConsoleColor(System.ConsoleColor.Yellow, "Can't connect to internet, offline file will be used");
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
            string dlcListFilePath = SelectedGameCode == GameCode.CM3D2 ? CM_DlcListPath : COM_DlcListPath;
            string jsonString = File.ReadAllText(dlcListFilePath);
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
                throw new FormatException();
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

            // Compare to major break
            SemanticVersioning.Version currentVersion = new SemanticVersioning.Version(currentVersionString);
            return new SemanticVersioning.Range($"~{minVersionString.Split(".").First()}").IsSatisfied(currentVersion);
        }


        static string GetGameInstallPath()
        {
            // Try to get Game InstallPath from Registry
            string? gameDirectoryRegistry = GetRegistryInstallPath();
            if (gameDirectoryRegistry != null)
            {
                return gameDirectoryRegistry;
            }
            // If missing/not set, apps will use current directory
            else
            {
                ConsoleColor(System.ConsoleColor.Yellow, $"Warning : {SelectedGameCode} installation directory is not set in registry. Will using current directory");
                return Directory.GetCurrentDirectory();
            }
        }

        static string? GetRegistryInstallPath()
        {

            switch (SelectedGameCode)
            {
                case GameCode.CM3D2:
                    string cmKeyName = "HKEY_CURRENT_USER\\SOFTWARE\\KISS\\カスタムメイド3D2";
                    return Registry.GetValue(cmKeyName, "InstallPath", "") as string;
                case GameCode.COM3D2:
                    // WHY KISS?! WHY YOU DO THIS FFS?!!
                    string comKeyName = "HKEY_CURRENT_USER\\SOFTWARE\\KISS\\カスタムオーダーメイド3D2";
                    string? result = Registry.GetValue(comKeyName, "InstallPath", "") as string;

                    // Try to get Registry COM3D2.5 version
                    if (result == null)
                    {
                        comKeyName = "HKEY_CURRENT_USER\\SOFTWARE\\KISS\\カスタムオーダーメイド3D2.5";
                        result = Registry.GetValue(comKeyName, "InstallPath", "") as string;
                    }
                    return result;
                default:
                    return null;
            }
        }

        static List<string> ReadFilesFromGameDirectory()
        {
            string gameRootDir = GetGameInstallPath();
            // string gameRootDir = "D:\\Games\\COM3D2";
            string gamedataDir = gameRootDir + "\\GameData";

            List<string> gamedataList = new List<string>();

            try
            {
                // Common - CM3D2/COM3D2
                gamedataList.AddRange(GetFilesFromDirectory(gamedataDir));
                // Exclusive - COM3D2
                if (SelectedGameCode == GameCode.COM3D2)
                {
                    string gamedata20Dir = gameRootDir + "\\GameData_20";
                    gamedataList.AddRange(GetFilesFromDirectory(gamedata20Dir));
                }

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