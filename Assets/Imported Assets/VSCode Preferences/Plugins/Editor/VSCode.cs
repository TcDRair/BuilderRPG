/*
 * Unity VSCode Support
 *
 * Seamless support for Microsoft Visual Studio Code in Unity
 *
 * Version:
 *   2.9
 *
 * Authors:
 *   Matthew Davey <matthew.davey@dotbunny.com>
 *
 * Customized by:
 *   TcDRair <whong0012@snu.ac.kr>
 * Target Unity Version :
 *   2020.3.0f1
 * Target OS :
 *   Windows, MacOS, Linux
 */
namespace dotBunny.Unity
{
  using System;
  using System.IO;
  using System.Reflection;
  using System.Diagnostics;
  using System.Collections.Generic;
  using System.Text.RegularExpressions;
  using UnityEditor;
  using UnityEngine;

  [InitializeOnLoad]
  public static class VSCode
  {
    /// <summary>Current Version Number</summary>
    public const float Version = 2.9f;

    /// <summary>Current Version Code</summary>
    public const string VersionCode = "-RELEASE";
    
    /// <summary>Additional File Extensions</summary>
    public const string FileExtensions = ".ts, .bjs, .javascript, .json, .html, .shader, .template";
    
    /// <summary>Download URL for Unity Debbuger</summary>
    public const string UnityDebuggerURL = "https://unity.gallery.vsassets.io/_apis/public/gallery/publisher/unity/extension/unity-debug/latest/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage";

    // Used to keep Unity from crashing when the editor is quit
    static bool alreadyFixedPreferences;
    static readonly string Sep = $"{Path.DirectorySeparatorChar}";
    #region Properties

    /// <summary>Path to VSCode executable</summary>
    public static string CodePath { get {
		  string current = EditorPrefs.GetString("VSCode_CodePath", "");
      if(current == "" || !VSCodeExists(current)) {
        //Value not set, set to "" or current path is invalid, try to autodetect it
        //If autodetect fails, a error will be printed and the default value set
        EditorPrefs.SetString("VSCode_CodePath", AutodetectCodePath());
        //If its not installed or the install folder isn't a "normal" one,
        //AutodetectCodePath will print a error message to the Unity Console
      }
      return EditorPrefs.GetString("VSCode_CodePath", current);
      }
      set => EditorPrefs.SetString("VSCode_CodePath", value);
    }
    
    /// <summary>Get Program Files Path</summary>
    /// <returns>The platforms "Program Files" path.</returns>
    static string ProgramFilesx86() => Environment.GetEnvironmentVariable("ProgramFiles(x86)");

    /// <summary>Get Program Files Path</summary>
    /// <returns>The platforms "Program Files" path.</returns>
    static string ProgramFiles() => Environment.GetEnvironmentVariable("ProgramFiles");
		
    
    /// <summary>Should debug information be displayed in the Unity terminal?</summary>
    public static bool UseDebug {
      get => EditorPrefs.GetBool("VSCode_Debug", false);
      set => EditorPrefs.SetBool("VSCode_Debug", value);
    }

    /// <summary>Is the Visual Studio Code Integration Enabled?</summary>
    /// <remarks>
    /// We do not want to automatically turn it on, for in larger projects not everyone is using VSCode
    /// </remarks>
    public static bool Enabled {
      get => EditorPrefs.GetBool("VSCode_Enabled", false);
      set {
        // When turning the plugin on, we should remove all the previous project files
        if (!Enabled && value) ClearProjectFiles();
        EditorPrefs.SetBool("VSCode_Enabled", value);
      }
    }
    public static bool UseUnityDebugger {
      get => EditorPrefs.GetBool("VSCode_UseUnityDebugger", false);
      set {
        if (value != UseUnityDebugger) {
          // Set value
          EditorPrefs.SetBool("VSCode_UseUnityDebugger", value);
          
          // Do not write the launch JSON file because the debugger uses its own
          if ( value ) WriteLaunchFile = false;
          
          // Update launch file
          UpdateLaunchFile();
        }
      }
    }
    
    /// <summary>When opening a project in Unity, should it automatically open in VS Code.</summary>
    public static bool AutoOpenEnabled {
      get => EditorPrefs.GetBool("VSCode_AutoOpenEnabled", false);
      set => EditorPrefs.SetBool("VSCode_AutoOpenEnabled", value);
    }

    /// <summary>Should the launch.json file be written?</summary>
    /// <remarks>Useful to disable if someone has their own custom one rigged up</remarks>
    public static bool WriteLaunchFile {
      get => EditorPrefs.GetBool("VSCode_WriteLaunchFile", true);
      set => EditorPrefs.SetBool("VSCode_WriteLaunchFile", value);
    }

    /// <summary>Should the plugin automatically update itself.</summary>
    static bool AutomaticUpdates {
      get => EditorPrefs.GetBool("VSCode_AutomaticUpdates", false);
      set => EditorPrefs.SetBool("VSCode_AutomaticUpdates", value);
    }

    static float GitHubVersion {
      get => EditorPrefs.GetFloat("VSCode_GitHubVersion", Version);
      set => EditorPrefs.SetFloat("VSCode_GitHubVersion", value);
    }

    /// <summary>When was the last time that the plugin was updated?</summary>
    static DateTime LastUpdate {
      get {
        var set = EditorPrefs.GetString("VSCode_LastUpdate");
        return (set == "") ? new(2015, 10, 8) : DateTime.Parse(set);
      }
      set => EditorPrefs.SetString("VSCode_LastUpdate", value.ToString());
    }

    /// <summary>Quick reference to the VSCode launch settings file</summary>
    static string LaunchPath => SettingsFolder + Sep + "launch.json";

    /// <summary>Should the parent of the unity project be used as the workspace directory.</summary>
    /// <remarks>Usefull if you have your unity project as a sub-directory.</remarks>
    static bool UseParentWorkspace {
      get => EditorPrefs.GetBool("VSCode_UseParentWorkspace", false);
      set => EditorPrefs.SetBool("VSCode_UseParentWorkspace", value);
    }

    /// <summary>The full path to the Unity project.</summary>
    static string UnityProjectPath => Path.GetDirectoryName(Application.dataPath);
    
    /// <summary>The full path to the workspace.</summary>
    static string WorkspacePath => UseParentWorkspace
      ? Directory.GetParent(UnityProjectPath).FullName
      : UnityProjectPath;

    /// <summary>Should the script editor be reverted when quiting Unity.</summary>
    /// <remarks>Useful for environments where you do not use VSCode for everything.</remarks>
    static bool RevertExternalScriptEditorOnExit {
      get => EditorPrefs.GetBool("VSCode_RevertScriptEditorOnExit", true);
      set => EditorPrefs.SetBool("VSCode_RevertScriptEditorOnExit", value);
    }

    /// <summary>Quick reference to the VSCode settings folder</summary>
    static string SettingsFolder => WorkspacePath + Sep + ".vscode";

    static string SettingsPath => SettingsFolder + Sep + "settings.json";

    static int UpdateTime {
      get => EditorPrefs.GetInt("VSCode_UpdateTime", 7);
      set => EditorPrefs.SetInt("VSCode_UpdateTime", value);
    }
    #endregion

    /// <summary>Integration Constructor</summary>
    static VSCode() {
      if (Enabled) {
        UpdateUnityPreferences(true);
        UpdateLaunchFile();
        
        // Add Update Check
        DateTime targetDate = LastUpdate.AddDays(UpdateTime);
        if (DateTime.Now >= targetDate && AutomaticUpdates) CheckForUpdate();

        // Open VS Code automatically when project is loaded
        if (AutoOpenEnabled) CheckForAutoOpen();
      }
      
      // Event for when script is reloaded 
      AppDomain.CurrentDomain.DomainUnload += DomainUnloadEvent;
    }
    static void DomainUnloadEvent(object sender, EventArgs e) {
      if (Enabled && RevertExternalScriptEditorOnExit) UpdateUnityPreferences(false);
    }


    #region Public Members
    /// <summary>Force Unity To Write Project File</summary>
    /// <remarks>Reflection!</remarks>
    public static void SyncSolution() {
      var T = Type.GetType("UnityEditor.SyncVS,UnityEditor");
      T.GetMethod("SyncSolution", BindingFlags.Public |  BindingFlags.Static).Invoke(null, null);
    }

    /// <summary>Update the solution files so that they work with VS Code</summary>
    public static void UpdateSolution() {
      // No need to process if we are not enabled
      if (!Enabled) return;

      if (UseDebug) UnityEngine.Debug.Log("[VSCode] Updating Solution & Project Files");

      var currentDirectory = Directory.GetCurrentDirectory();
      string[] exts = { "*.sln", "*.csproj" };
      foreach(var ext in exts) {
        foreach (var path in Directory.GetFiles(currentDirectory, ext)) {
          string content = File.ReadAllText(path);
          content = ScrubSolutionContent(content);
          File.WriteAllText(path, content);
          ScrubFile(path);
        }
      }
    }

    #endregion

    #region Private Members
  
    /// <summary>Try to find automatically the installation of VSCode</summary>
    static string AutodetectCodePath()  {
      string[] possiblePaths = {
#if UNITY_EDITOR_OSX
        "/Applications/Visual Studio Code.app",
        "/Applications/Visual Studio Code - Insiders.app"
#elif UNITY_EDITOR_WIN
        $"{ProgramFiles()}{Sep}Microsoft VS Code{Sep}bin{Sep}code.cmd",
        $"{ProgramFiles()}{Sep}Microsoft VS Code Insiders{Sep}bin{Sep}code-insiders.cmd",
        $"{ProgramFilesx86()}{Sep}Microsoft VS Code{Sep}bin{Sep}code.cmd",
        $"{ProgramFilesx86()}{Sep}Microsoft VS Code Insiders{Sep}bin{Sep}code-insiders.cmd"
#else
        "/usr/bin/code",
        "/usr/bin/code-insiders",
        "/bin/code",
        "/usr/local/bin/code",
        "/var/lib/flatpak/exports/bin/com.visualstudio.code",
        "/snap/bin/code",
        "/snap/bin/code-insiders"
#endif
      };

      foreach(var path in possiblePaths) if (VSCodeExists(path)) return path;
      PrintNotFound(possiblePaths[0]);
      return possiblePaths[0]; //returns the default one, printing a warning message 'executable not found'
    }

    /// <summary>Call VSCode with arguments</summary>
    static void CallVSCode(string args) {
      if (!VSCodeExists(CodePath)) { PrintNotFound(CodePath); return; }

      Process proc = new() {
        StartInfo = {
#if UNITY_EDITOR_OSX
          FileName = "open",
          Arguments = (CodePath.Contains("Insiders")
            ? " -n -b \"com.microsoft.VSCodeInsiders\" --args " + args.Replace(@"\", @"\\")
            : " -n -b \"com.microsoft.VSCode\" --args " + args.Replace(@"\", @"\\"),
          UseShellExecute = false,
#elif UNITY_EDITOR_WIN
          FileName = CodePath,
	        Arguments = args,
          UseShellExecute = false,
#else
          FileName = CodePath,
	        Arguments = args.Replace(@"\", @"\\"),
          UseShellExecute = false,
#endif
          WindowStyle = ProcessWindowStyle.Hidden,
          CreateNoWindow = true,
          RedirectStandardOutput = true
        }
      };
      proc.Start();
    }

    /// <summary>Check for Updates with GitHub</summary>
    static void CheckForUpdate() {
      EditorUtility.DisplayProgressBar("VSCode", "Checking for updates ...", 0.5f);

      // Because were not a runtime framework, lets just use the simplest way of doing this
      var fileContent = "";
      try {
        using var webClient = new System.Net.WebClient();
        fileContent = webClient.DownloadString("https://raw.githubusercontent.com/dotBunny/VSCode/master/Plugins/Editor/VSCode.cs");
      } catch (Exception e) {
        if (UseDebug) UnityEngine.Debug.Log("[VSCode] " + e.Message);
        // Don't go any further if there is an error
        return;
      } finally { EditorUtility.ClearProgressBar(); }

      // Set the last update time
      LastUpdate = DateTime.Now;

      // Fix for oddity in downlo
      if (fileContent[..2] != "/*") {
        int startPosition = fileContent.IndexOf("/*", StringComparison.CurrentCultureIgnoreCase);

        // Jump over junk characters
        fileContent = fileContent[startPosition..];
      }

      string[] fileExploded = fileContent.Split('\n');
      if (fileExploded.Length > 7) {
        if (float.TryParse(fileExploded[6].Replace("*", "").Trim(), out var github))
          GitHubVersion = github;

        if (github > Version) {
          var GUIDs = AssetDatabase.FindAssets("t:Script VSCode");
          var path = Application.dataPath.Replace("/Assets", "") + Sep +
                 AssetDatabase.GUIDToAssetPath(GUIDs[0]).Replace("/", Sep);

          if (EditorUtility.DisplayDialog(
            "VSCode Update",
            "A newer version of the VSCode plugin is available, would you like to update your version?",
            "Yes",
            "No"
          )) {
            // Write update file
            File.WriteAllText(path, fileContent);

            // Force update on text file
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(GUIDs[0]), ImportAssetOptions.ForceUpdate);
          }
        }
      }
    }

    /// <summary>Checks whether it should auto-open VSCode</summary>
    /// <remarks>
    /// VSCode() gets called on Launch and Run, through IntializeOnLoad
    /// https://docs.unity3d.com/ScriptReference/InitializeOnLoadAttribute.html
    /// To make sure it only opens VSCode when Unity (re)launches (i.e. opens a project),
    /// we compare the launch time, which we calculate using EditorApplication.timeSinceStartup.  
    /// </remarks>
    static void CheckForAutoOpen() {
      double timeInSeconds = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
      int unityLaunchTimeInSeconds = (int)(timeInSeconds - EditorApplication.timeSinceStartup);
      int prevUnityLaunchTime = EditorPrefs.GetInt("VSCode_UnityLaunchTime", 0);
      // If launch time has changed, then Unity was re-opened 
      if (unityLaunchTimeInSeconds > prevUnityLaunchTime) {
        // Launch VSCode
        MenuOpenProject();
        // Save new launch time
        EditorPrefs.SetInt("VSCode_UnityLaunchTime", unityLaunchTimeInSeconds);
      }
    }

    /// <summary>Clear out any existing project files and lingering stuff that might cause problems</summary>
    static void ClearProjectFiles() {
      var currentDirectory = Directory.GetCurrentDirectory();

      var exts = new string[] { "*.sln", "*.csproj", "*.unityproj" };
      foreach(var ext in exts) {
        var files = Directory.GetFiles(currentDirectory, ext);
        foreach(var file in files) File.Delete(file);
      }

      // Replace with our clean files (only in Unity 5)
      SyncSolution();
    }

    /// <summary>Force Unity Preferences Window To Read From Settings</summary>
    static void FixUnityPreferences() {
      // I want that window, please and thank you
      var T = Type.GetType("UnityEditor.PreferencesWindow,UnityEditor");

      // Only run this when the editor window is visible (cause its what screwed us up)
      if (EditorWindow.focusedWindow?.GetType() == T)
      {
        var window = EditorWindow.GetWindow(T, true, "Unity Preferences");


        if (window == null)
        {
          if (UseDebug) UnityEngine.Debug.Log("[VSCode] No Preferences Window Found (really?)");
          
          return;
        }

        var invokerType = window.GetType();
        var invokerMethod = invokerType.GetMethod("ReadPreferences", BindingFlags.NonPublic | BindingFlags.Instance);

        if (invokerMethod?.Invoke(window, null) is null && UseDebug)
          UnityEngine.Debug.Log("[VSCode] No Reflection Method Found For Preferences");
      }
    }

    /// <summary>Determine what port Unity is listening for on Windows</summary>
    static int GetDebugPort() {
#if UNITY_EDITOR_WIN
      Process process = new() {
        StartInfo = {
          FileName = "netstat",
          Arguments = "-a -n -o -p TCP",
          UseShellExecute = false,
          RedirectStandardOutput = true
        }
      };
      process.Start();

      string output = process.StandardOutput.ReadToEnd();
      string[] lines = output.Split('\n');

      process.WaitForExit();

      foreach (string line in lines) {
        string[] tokens = Regex.Split(line, @"\s+");
        if (tokens.Length > 4 && int.TryParse(tokens[5], out var test)) {
          if (test > 1023) {
            try { if (Process.GetProcessById(test).ProcessName == "Unity") return test; }
            catch {  } // if not found, ignore exception and continue
          }
        }
      }
#else
      Process process = new() {
        StartInfo = {
          FileName = "lsof",
          Arguments = "-c Unity -i 4tcp -a",
          UseShellExecute = false,
          RedirectStandardOutput = true
        }
      };
      process.Start();

      // Not thread safe (yet!)
      string output = process.StandardOutput.ReadToEnd();
      string[] lines = output.Split('\n');

      process.WaitForExit();

      foreach (string line in lines) {
        if (line.StartsWith("Unity")) {
          string[] portions = line.Split("TCP *:");
          if (portions.Length >= 2) {
            string cleanPort = new Regex(@"[^\d]").Replace(portions[1], "");
            if (int.TryParse(cleanPort, out var port) && port > -1) return port;
          }
        }
      }
#endif
      return -1;
    }

    /// <summary>Manually install the original Unity Debuger</summary> 
    /// <remarks>This should auto update to the latest.</remarks>
    static void InstallUnityDebugger() {
      EditorUtility.DisplayProgressBar("VSCode", "Downloading Unity Debugger ...", 0.1f);
      byte[] fileContent;

      try {
        using var webClient = new System.Net.WebClient();
        fileContent = webClient.DownloadData(UnityDebuggerURL);
      } catch (Exception e) {
        if (UseDebug) UnityEngine.Debug.Log("[VSCode] " + e.Message);
        // Don't go any further if there is an error
        return;
      }
      finally { EditorUtility.ClearProgressBar(); }
      
      // Do we have a file to install?
      if (fileContent != null) {
        string fileName = $"{Path.GetTempPath()}{Guid.NewGuid()}.vsix";
        File.WriteAllBytes(fileName, fileContent);
        
        CallVSCode(fileName);
      }
    }
   
    // HACK: This is in until Unity can figure out why MD keeps opening even though a different program is selected.
    [MenuItem("Assets/Open C# Project In Code", false, 1000)]
    static void MenuOpenProject() {
      // Force the project files to be sync
      SyncSolution();
      // Load Project
      CallVSCode($"\"{WorkspacePath}\"");
    }
    [MenuItem("Assets/Open C# Project In Code", true, 1000)]
    static bool ValidateMenuOpenProject() => Enabled;

    /// <summary>Print a error message to the Unity Console about not finding the code executable</summary>
    static void PrintNotFound(string path)
      => UnityEngine.Debug.LogError("[VSCode] Code executable in '" + path + "' not found. Check your" +
      "Visual Studio Code installation and insert the correct path in the Preferences menu.");


    /// <summary>VS Code Integration Preferences Item</summary>
    /// <remarks>Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off</remarks>
    [SettingsProvider]
    static SettingsProvider VSCodePreferencesItem() {
      var provider = new SettingsProvider("Preferences/VSCode", SettingsScope.User)
      {
        label = "Visual Studio Code",
        guiHandler = (searchContext) => {
          //? Original part of the code, when using [PreferencesItem] attribute
          if (EditorApplication.isCompiling) {
            EditorGUILayout.HelpBox("Please wait for Unity to finish compiling. \nIf the window doesn't refresh, simply click on the window or move it around to cause a repaint to happen.", MessageType.Warning);
            return;
          }
          EditorGUILayout.BeginVertical();
            var versionInfo = $"{string.Format("{0:0.00}", Version)}{VersionCode}, GitHub version @ {string.Format("{0:0.00}", GitHubVersion)}";
            EditorGUI.BeginChangeCheck();
              // Need the VS Code executable
              EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("VS Code Path", GUILayout.Width(75));
                CodePath = EditorGUILayout.DelayedTextField(CodePath,  GUILayout.ExpandWidth(true));
                GUI.SetNextControlName("PathSetButton");  
                if(GUILayout.Button("...", GUILayout.Height(14), GUILayout.Width(20))) {
                  GUI.FocusControl("PathSetButton");
                  string path = EditorUtility.OpenFilePanel( "Visual Studio Code Executable", "", "" );
                  if( path.Length != 0 && File.Exists(path) || Directory.Exists(path)) CodePath = path;
                }
              EditorGUILayout.EndHorizontal();
              EditorGUILayout.Space();

              Enabled = EditorGUILayout.Toggle(new GUIContent("Enable Integration", "Should the integration work its magic for you?"), Enabled);

              UseUnityDebugger = EditorGUILayout.Toggle(new GUIContent("Use Unity Debugger", "Should the integration integrate with Unity's VSCode Extension (must be installed)."), UseUnityDebugger);

              AutoOpenEnabled = EditorGUILayout.Toggle(new GUIContent("Enable Auto Open", "When opening a project in Unity, should it automatically open in VS Code?"), AutoOpenEnabled);

              EditorGUILayout.Space();
              RevertExternalScriptEditorOnExit = EditorGUILayout.Toggle(new GUIContent("Revert Script Editor On Unload", "Should the external script editor setting be reverted to its previous setting on project unload? This is useful if you do not use Code with all your projects."),RevertExternalScriptEditorOnExit);
      
              EditorGUILayout.Space();
              UseParentWorkspace = EditorGUILayout.Toggle(new GUIContent("Parent as workspace", "Should the parent of the project be used as the workspace directory? Usefull if you have the Unity project in a subdirectory."),UseParentWorkspace);

              UseDebug = EditorGUILayout.Toggle(new GUIContent("Output Messages To Console", "Should informational messages be sent to Unity's Console?"), UseDebug);

              WriteLaunchFile = EditorGUILayout.Toggle(new GUIContent("Always Write Launch File", "Always write the launch.json settings when entering play mode?"), WriteLaunchFile);

              EditorGUILayout.Space();

              AutomaticUpdates = EditorGUILayout.Toggle(new GUIContent("Automatic Updates", "Should the plugin automatically update itself?"), AutomaticUpdates);

              UpdateTime = EditorGUILayout.IntSlider(new GUIContent("Update Timer (Days)", "After how many days should updates be checked for?"), UpdateTime, 1, 31);

              EditorGUILayout.Space();
              EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck()) {
              UpdateUnityPreferences(Enabled);

              // TODO: Force Unity To Reload Preferences
              // This seems to be a hick up / issue
              if (UseDebug) UnityEngine.Debug.Log("[VSCode] Integration" + (Enabled ? "Enabled" : "Disabled"));        
            }

            if (GUILayout.Button(new GUIContent("Force Update", "Check for updates to the plugin, right NOW!"))) {
              CheckForUpdate();
          EditorGUILayout.EndVertical();
              return;
            }
            if (GUILayout.Button(new GUIContent("Write Workspace Settings", "Output a default set of workspace settings for VSCode to use, ignoring many different types of files."))) {
              WriteWorkspaceSettings();
          EditorGUILayout.EndVertical();
              return;
            }
            EditorGUILayout.Space();

            if (UseUnityDebugger) {
              EditorGUILayout.HelpBox("In order for the \"Use Unity Debuggger\" option to function above, you need to have installed the Unity Debugger Extension for Visual Studio Code.", MessageType.Warning);
              if (GUILayout.Button(new GUIContent("Install Unity Debugger", "Install the Unity Debugger Extension into Code"))) {
                InstallUnityDebugger();
          EditorGUILayout.EndVertical();
                return;
              }
            }
          EditorGUILayout.EndVertical();
        },
      };
      return provider;
    }

    /// <summary>Asset Open Callback (from Unity)</summary>
    /// <remarks>Called when Unity is about to open an asset.</remarks>
    [UnityEditor.Callbacks.OnOpenAsset]
    static bool OnOpenedAsset(int instanceID, int line)
    {
      // bail out if we are not using VSCode
      if (!Enabled) return false;

      // determine asset that has been double clicked in the project view
      var selected = EditorUtility.InstanceIDToObject(instanceID);

      // additional file extensions
      string selectedFileExt = (Path.GetExtension(AssetDatabase.GetAssetPath(selected)) ?? "").ToLower();
      // open supported object types
      if (
        (selected is MonoScript or Shader) ||
        FileExtensions.Contains(selectedFileExt, StringComparison.OrdinalIgnoreCase)
      ) {
        string completeFilepath = UnityProjectPath + Sep + AssetDatabase.GetAssetPath(selected);

        string args = (line == -1)
        ? $"\"{WorkspacePath}\" \"{completeFilepath}\" -r"
        : $"\"{WorkspacePath}\" -g \"{completeFilepath}:{line}\" -r";
        // call 'open'
        CallVSCode(args);
        return true;
      }

      // Didnt find a code file? let Unity figure it out
      return false;
    }

    //! if not UNITY_2017_2_OR_NEWER : modify following two methods
    /// <summary>
    /// Executed when the Editor's playmode changes allowing for capture of required data
    /// </summary>
    static void OnPlaymodeStateChanged(PlayModeStateChange state) {
      if (Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        UpdateLaunchFile();
    }

    /// <summary>Detect when scripts are reloaded and relink playmode detection</summary>
    [UnityEditor.Callbacks.DidReloadScripts()]
    static void OnScriptReload() {
      EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
      EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
    }

    /// <summary>Remove extra/erroneous lines from a file.</summary>
    static void ScrubFile(string path) {
      string[] lines = File.ReadAllLines(path);
      List<string> newLines = new();
      for (int i = 0; i < lines.Length; i++) {
        // Check Empty
        if (lines[i].Replace("\t", "").Trim().Length > 0)
          newLines.Add(lines[i]);
      }
      File.WriteAllLines(path, newLines.ToArray());
    }

    /// <summary>Remove extra/erroneous data from project file (content).</summary>
    static string ScrubProjectContent(string content) {
      if (content.Length == 0) return "";
#if !UNITY_EDITOR_WIN
      // Moved to 3.5, 2.0 is legacy.
      if (content.Contains("<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>"))
        content = Regex.Replace(content, "<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>", "<TargetFrameworkVersion>v2.0</TargetFrameworkVersion>");
#endif

      string targetPath = "";// "<TargetPath>Temp" + Sep + "bin" + Sep + "Debug" + Sep + "</TargetPath>"; //OutputPath
      string langVersion = "<LangVersion>default</LangVersion>";

      int endLocation = 0;
      while (true) {
        int startLocation = content.IndexOf("<PropertyGroup", endLocation);
        if (startLocation == -1) break;
        endLocation = content.IndexOf("</PropertyGroup>", startLocation);
        if (endLocation == -1) break;

        string addedOptions = "";
        if (!content[startLocation..endLocation].Contains("<TargetPath>"))
          addedOptions += $"\n\r\t{targetPath}\n\r";
        if (!content[startLocation..endLocation].Contains("<LangVersion>"))
          addedOptions += $"\n\r\t{langVersion}\n\r";
        content = content.Insert(endLocation, addedOptions);
      }

      return content;
    }

    /// <summary>Remove extra/erroneous data from solution file (content).</summary>
    static string ScrubSolutionContent(string content)
    {
      // Replace Solution Version
      content = content.Replace(
        "Microsoft Visual Studio Solution File, Format Version 11.00\r\n# Visual Studio 2008\r\n",
        "\r\nMicrosoft Visual Studio Solution File, Format Version 12.00\r\n# Visual Studio 2012");

      // Remove Solution Properties (Unity Junk)
      int startIndex = content.IndexOf("GlobalSection(SolutionProperties) = preSolution");
      if (startIndex != -1) {
        int endIndex = content.IndexOf("EndGlobalSection", startIndex);
        content = content[..startIndex] + content[(endIndex + 16)..];
      }

      return content;
    }
     
    /// <summary>
    /// Update Visual Studio Code Launch file
    /// </summary>
    static void UpdateLaunchFile() {
      if (!Enabled) return;
      else if (UseUnityDebugger) {
        if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);

        // Write out proper formatted JSON (hence no more SimpleJSON here)
        string fileContent = "{\n\t\"version\": \"0.2.0\",\n\t\"configurations\": [\n\t\t{\n\t\t\t\"name\": \"Unity Editor\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\t\t},\n\t\t{\n\t\t\t\"name\": \"Windows Player\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\t\t},\n\t\t{\n\t\t\t\"name\": \"OSX Player\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\t\t},\n\t\t{\n\t\t\t\"name\": \"Linux Player\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\t\t},\n\t\t{\n\t\t\t\"name\": \"iOS Player\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\t\t},\n\t\t{\n\t\t\t\"name\": \"Android Player\",\n\t\t\t\"type\": \"unity\",\n\t\t\t\"request\": \"launch\"\n\n\t\t}\n\t]\n}";
        File.WriteAllText(LaunchPath, fileContent);
      }
      else if (WriteLaunchFile) {
        int port = GetDebugPort();
        if (port > -1) {
          if (!Directory.Exists(SettingsFolder))
            Directory.CreateDirectory(SettingsFolder);

          // Write out proper formatted JSON (hence no more SimpleJSON here)
          string fileContent = "{\n\t\"version\":\"0.2.0\",\n\t\"configurations\":[ \n\t\t{\n\t\t\t\"name\":\"Unity\",\n\t\t\t\"type\":\"mono\",\n\t\t\t\"request\":\"attach\",\n\t\t\t\"address\":\"localhost\",\n\t\t\t\"port\":" + port + "\n\t\t}\n\t]\n}";
          File.WriteAllText(LaunchPath, fileContent);

          if (UseDebug) UnityEngine.Debug.Log($"[VSCode] Debug Port Found ({port})");
        }
        else if (UseDebug) UnityEngine.Debug.LogWarning("[VSCode] Unable to determine debug port.");
      }
    }

    /// <summary>Update Unity Editor Preferences</summary>
    /// <param name="enabled">Should we turn on this party!</param>
    static void UpdateUnityPreferences(bool enabled) {
      if (enabled) {
        // App
        if (EditorPrefs.GetString("kScriptsDefaultApp") != CodePath)
          EditorPrefs.SetString("VSCode_PreviousApp", EditorPrefs.GetString("kScriptsDefaultApp"));
        EditorPrefs.SetString("kScriptsDefaultApp", CodePath);

        // Arguments
        if (EditorPrefs.GetString("kScriptEditorArgs") != "-r -g `$(File):$(Line)`")
          EditorPrefs.SetString("VSCode_PreviousArgs", EditorPrefs.GetString("kScriptEditorArgs"));
        EditorPrefs.SetString("kScriptEditorArgs", "-r -g `$(File):$(Line)`");
        EditorPrefs.SetString("kScriptEditorArgs" + CodePath, "-r -g `$(File):$(Line)`");


        // MonoDevelop Solution
        if (EditorPrefs.GetBool("kMonoDevelopSolutionProperties", false))
          EditorPrefs.SetBool("VSCode_PreviousMD", true);
        EditorPrefs.SetBool("kMonoDevelopSolutionProperties", false);

        // Support Unity Proj (JS)
        if (EditorPrefs.GetBool("kExternalEditorSupportsUnityProj", false))
          EditorPrefs.SetBool("VSCode_PreviousUnityProj", true);
        EditorPrefs.SetBool("kExternalEditorSupportsUnityProj", false);

        if (!EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", false))
          EditorPrefs.SetBool("VSCode_PreviousAttach", false);
        EditorPrefs.SetBool("AllowAttachedDebuggingOfEditor", true);
        
      }
      else
      {
        // Restore previous app
        if (!string.IsNullOrEmpty(EditorPrefs.GetString("VSCode_PreviousApp")))
          EditorPrefs.SetString("kScriptsDefaultApp", EditorPrefs.GetString("VSCode_PreviousApp"));

        // Restore previous args
        if (!string.IsNullOrEmpty(EditorPrefs.GetString("VSCode_PreviousArgs")))
          EditorPrefs.SetString("kScriptEditorArgs", EditorPrefs.GetString("VSCode_PreviousArgs"));

        // Restore MD setting
        if (EditorPrefs.GetBool("VSCode_PreviousMD", false))
          EditorPrefs.SetBool("kMonoDevelopSolutionProperties", true);

        // Restore MD setting
        if (EditorPrefs.GetBool("VSCode_PreviousUnityProj", false))
          EditorPrefs.SetBool("kExternalEditorSupportsUnityProj", true);

        // Always leave editor attaching on, I know, it solves the problem of needing to restart for this
        // to actually work
        EditorPrefs.SetBool("AllowAttachedDebuggingOfEditor", true);
      }

      if (!alreadyFixedPreferences) {
        alreadyFixedPreferences = true;
        FixUnityPreferences();
      }
    }

    /// <summary>
    /// Determines if the current path to the code executable is valid or not (exists)
    /// </summary>
    static bool VSCodeExists(string curPath)
#if UNITY_EDITOR_OSX
      => System.IO.Directory.Exists(curPath);
#else
      => new FileInfo(curPath).Exists;
#endif

    /// <summary>Write Default Workspace Settings</summary>
    static void WriteWorkspaceSettings()
    {
      if (UseDebug) UnityEngine.Debug.Log("[VSCode] Workspace Settings Written");

      if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);

      string exclusions =
        // Associations
        "{\n" +
        "\t\"files.associations\":\n" +
        "\t{\n" +
        "\t\t\"*.bjs\":\"javascript\",\n" +
        "\t\t\"*.javascript\":\"javascript\"\n" +
        "\t},\n" +
        "\t\"files.exclude\":\n" +
        "\t{\n" +
        // Hidden Files
        "\t\t\"**/.DS_Store\":true,\n" +
        "\t\t\"**/.git\":true,\n" +
        "\t\t\"**/.gitignore\":true,\n" +
        "\t\t\"**/.gitattributes\":true,\n" +
        "\t\t\"**/.gitmodules\":true,\n" +
        "\t\t\"**/.svn\":true,\n" +

        // Compressed Files
        "\t\t\"**/*.zip\":true,\n" +
        "\t\t\"**/*.gz\":true,\n" +
        "\t\t\"**/*.7z\":true,\n" +

        // Project Files
        "\t\t\"**/*.booproj\":true,\n" +
        "\t\t\"**/*.pidb\":true,\n" +
        "\t\t\"**/*.suo\":true,\n" +
        "\t\t\"**/*.user\":true,\n" +
        "\t\t\"**/*.userprefs\":true,\n" +
        "\t\t\"**/*.unityproj\":true,\n" +
        "\t\t\"**/*.dll\":true,\n" +
        "\t\t\"**/*.exe\":true,\n" +

        // Media Files
        "\t\t\"**/*.pdf\":true,\n" +

        // Video
        "\t\t\"**/*.mp4\":true,\n" +

        // Audio
        "\t\t\"**/*.mid\":true,\n" +
        "\t\t\"**/*.midi\":true,\n" +
        "\t\t\"**/*.wav\":true,\n" +
        "\t\t\"**/*.mp3\":true,\n" +
        "\t\t\"**/*.ogg\":true,\n" +

        // Textures
        "\t\t\"**/*.gif\":true,\n" +
        "\t\t\"**/*.ico\":true,\n" +
        "\t\t\"**/*.jpg\":true,\n" +
        "\t\t\"**/*.jpeg\":true,\n" +
        "\t\t\"**/*.png\":true,\n" +
        "\t\t\"**/*.psd\":true,\n" +
        "\t\t\"**/*.tga\":true,\n" +
        "\t\t\"**/*.tif\":true,\n" +
        "\t\t\"**/*.tiff\":true,\n" +
        "\t\t\"**/*.hdr\":true,\n" +
        "\t\t\"**/*.exr\":true,\n" +

        // Models
        "\t\t\"**/*.3ds\":true,\n" +
        "\t\t\"**/*.3DS\":true,\n" +
        "\t\t\"**/*.fbx\":true,\n" +
        "\t\t\"**/*.FBX\":true,\n" +
        "\t\t\"**/*.lxo\":true,\n" +
        "\t\t\"**/*.LXO\":true,\n" +
        "\t\t\"**/*.ma\":true,\n" +
        "\t\t\"**/*.MA\":true,\n" +
        "\t\t\"**/*.obj\":true,\n" +
        "\t\t\"**/*.OBJ\":true,\n" +

        // Unity File Types
        "\t\t\"**/*.asset\":true,\n" +
        "\t\t\"**/*.cubemap\":true,\n" +
        "\t\t\"**/*.flare\":true,\n" +
        "\t\t\"**/*.mat\":true,\n" +
        "\t\t\"**/*.meta\":true,\n" +
        "\t\t\"**/*.prefab\":true,\n" +
        "\t\t\"**/*.unity\":true,\n" +
        "\t\t\"**/*.anim\":true,\n" +
        "\t\t\"**/*.controller\":true,\n" +

        // Folders
        "\t\t\"build/\":true,\n" +
        "\t\t\"Build/\":true,\n" +
        "\t\t\"Library/\":true,\n" +
        "\t\t\"library/\":true,\n" +
        "\t\t\"obj/\":true,\n" +
        "\t\t\"Obj/\":true,\n" +
        "\t\t\"ProjectSettings/\":true,\r" +
        "\t\t\"temp/\":true,\n" +
        "\t\t\"Temp/\":true\n" +
        "\t}\n" +
        "}";

      // Dont like the replace but it fixes the issue with the JSON
      File.WriteAllText(SettingsPath, exclusions);
    }
    #endregion
  }

  /// <summary>
  /// VSCode Asset AssetPostprocessor
  /// <para>This will ensure any time that the project files are generated the VSCode versions will be made</para>
  /// </summary>
  /// <remarks>Undocumented Event</remarks>
  public class VSCodeAssetPostprocessor : AssetPostprocessor {
    /// <summary>On documented, project generation event callback</summary>
    protected static void OnGeneratedCSProjectFiles() => VSCode.UpdateSolution();
    // Force execution of VSCode update
  }
}
