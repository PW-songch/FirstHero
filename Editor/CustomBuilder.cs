#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Beebyte.Obfuscator;
using System.Diagnostics;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class CustomBuilder
{
	public const string PREFKEY_WAITING_BUILD_PATH = "WaitingBuildPath";
	public const string RESOURCES_PATH = "Assets/ProjectFolder/Resources";
	public const string BUILT_IN_RESOURCES_PATH = RESOURCES_PATH + "/built_in";
	public const string ASSET_BUNDLE_RESOURCES_PATH = RESOURCES_PATH + "/asset_bundle";

	static private readonly string EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH = 
		Path.GetDirectoryName(RESOURCES_PATH) + "/" + Path.GetFileName(ASSET_BUNDLE_RESOURCES_PATH);
	
	static private ShellHelper.ShellRequest BuildGeneratorShell;

	/// <summary>
	/// Raises the scripts reloaded event.
	/// </summary>
	[UnityEditor.Callbacks.DidReloadScripts]
	static private void OnScriptsReloaded()
	{
		if (UnityEditorInternal.InternalEditorUtility.inBatchMode == true)
		{
			string buildPath = EditorPrefs.GetString(PREFKEY_WAITING_BUILD_PATH);
			if (string.IsNullOrEmpty(buildPath) == false)
			{
				#if UNITY_IOS
				// set code sign
				ScriptBuilder.eCodeSign codeSign = ScriptBuilder.eCodeSign.ALL;
				string codeSignArg = GetArgs(System.Environment.GetCommandLineArgs(), ARG_KEY_CODE_SIGN, string.Empty);
				if (string.IsNullOrEmpty(codeSignArg) == false)
					codeSign = Utility.ParsingStringToEnumType<ScriptBuilder.eCodeSign>(codeSignArg);

				// generate archive and ipa
				iOSGenerateArchiveAndIpa(buildPath, codeSign, true, BuildDoneCallBack);
				#endif

				// restores built in resources asset
				CustomBuilder.RestoreBuiltInResourcesAsset();
			}
		}

		EditorPrefs.DeleteKey(PREFKEY_WAITING_BUILD_PATH);
	}

	/// <summary>
	/// exclude built_in Resources asset.
	/// </summary>
	static public bool ExcludeBuiltInResourcesAsset()
	{
		if (Directory.Exists(Utility.GetProjectPath() + "/" + ASSET_BUNDLE_RESOURCES_PATH) == true)
		{
			UnityEngine.Debug.Log("ExcludeBuiltInResourcesAsset");

            if (Directory.Exists(Utility.GetProjectPath() + "/" + EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH) == true)
            {
                // 기존 파일 제거
                Directory.Delete(Utility.GetProjectPath() + "/" + EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH, true);
            }

            string error = AssetDatabase.MoveAsset(ASSET_BUNDLE_RESOURCES_PATH, EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH);
            if (string.IsNullOrEmpty(error) == false)
                UnityEngine.Debug.LogError(error);
            else
            {
                return true;
                //AssetDatabase.Refresh();
            }
        }

        return false;
	}

	/// <summary>
	/// restores built in resources asset.
	/// </summary>
	static public void RestoreBuiltInResourcesAsset()
	{
		if (Directory.Exists(Utility.GetProjectPath() + "/" + EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH) == true)
		{
			UnityEngine.Debug.Log("RestoreBuiltInResourcesAsset");

            if (Directory.Exists(Utility.GetProjectPath() + "/" + ASSET_BUNDLE_RESOURCES_PATH) == true)
            {
                // 기존 파일 제거
                Directory.Delete(Utility.GetProjectPath() + "/" + ASSET_BUNDLE_RESOURCES_PATH, true);
            }

            string error = AssetDatabase.MoveAsset(EXCLUDE_ASSET_BUNDLE_RESOURCES_PATH, ASSET_BUNDLE_RESOURCES_PATH);
            if (string.IsNullOrEmpty(error) == false)
                UnityEngine.Debug.LogError(error);
			AssetDatabase.Refresh();
		}
	}
	
	/// <summary>
	/// Gets the arguments.
	/// </summary>
	/// <returns>The arguments.</returns>
	/// <param name="_args">Arguments.</param>
	/// <param name="_name">Argument name.</param>
	/// <param name="_def">Default value.</param>
	static private string GetArgs(string[] _args, string _name, string _default = null)
	{
		string arg = _default;
		if (_args != null)
		{
			for (int i = 0; i < _args.Length; ++i)
			{
				if (_args[i] == "-" + _name)
				{
					if (i + 1 < _args.Length)
					{
						arg = _args[i + 1];
						break;
					}
				}
			}
		}

		return arg;
	}

	/// <summary>
	/// Build done callback.
	/// </summary>
	/// <param name="_bSuccess">If set to <c>true</c> build success.</param>
	static private void BuildDoneCallBack(bool _bSuccess)
	{
		if (_bSuccess == true)
			UnityEngine.Debug.Log("========================= Build Done ===========================");
	}

#if UNITY_IOS
	public const string ARG_KEY_XCODE_PATH = "xcodePath";
	public const string ARG_KEY_CODE_SIGN = "codeSign";
	public const string ARG_KEY_OBFUSCATOR = "obfuscator";
#if ARABIC_ONLY
	public const string XCODE_PATH = "/Users/maxonsoft/Documents/Chosen_Heroes_Xcode";
#else
	public const string XCODE_PATH = "/Users/gimhagseong/Documents/First_Hero_Xcode";
#endif
	/// <summary>
	/// Make iOSBuildGenerator arguments.
	/// </summary>
	/// <returns>arguments.</returns>
	/// <param name="_strBuildVersion">String build version.</param>
	/// <param name="_strXcodeFinderPath">String xcode finder path.</param>
	/// <param name="_codeSign">Code sign.</param>
	/// <param name="_bGenerateArchive">If set to <c>true</c> generate archive.</param>
	static private string MakeiOSBuildGeneratorArguments(string _strBuildVersion, 
		string _strXcodeFinderPath, ScriptBuilder.eCodeSign _codeSign, bool _bGenerateArchive = true)
	{
		const string format = " {0}";

		// build version
		StringBuilder sb = new StringBuilder(_strBuildVersion);
		// project path
		sb.AppendFormat(format, Utility.GetProjectPath());
		// xcode path
		sb.AppendFormat(format, _strXcodeFinderPath);
		// build kind (Dev/Alpha/Live)
		string buildKind = _strXcodeFinderPath.Substring(0, _strXcodeFinderPath.LastIndexOf("/" + _strBuildVersion));
		buildKind = buildKind.Substring(buildKind.LastIndexOf('/') + 1);
		switch (buildKind)
		{
			case "Dev":
			case "Alpha":
			case "Live":
				break;
			default:
			buildKind = "";
				break;
		}
		sb.AppendFormat(format, buildKind);
		// code sign type
		switch (_codeSign)
		{
			case ScriptBuilder.eCodeSign.ADHOC:
			case ScriptBuilder.eCodeSign.ALL:
				sb.Append(" adhoc");
				break;
		}
		// generate archive
		sb.AppendFormat(format, _bGenerateArchive);

		return sb.ToString();
	}

	/// <summary>
	/// generate archive and ipa.
	/// </summary>
	/// <param name="_buildPath">Build path.</param>
	static public void iOSGenerateArchiveAndIpa(string _strXcodePath, ScriptBuilder.eCodeSign _codeSign, bool _bGenerateArchive, Action<bool> _endCall = null)
	{
		if (Directory.Exists(_strXcodePath) == true)
		{
			// plist CFBundleIdentifier edit
			string plistPath = _strXcodePath + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
			plist.root.SetString("CFBundleIdentifier", "$(PRODUCT_BUNDLE_IDENTIFIER)");
			File.WriteAllText(plistPath, plist.WriteToString());

			// archive 생성 및 ipa 생성
			iOSBuild(_strXcodePath, _codeSign, _bGenerateArchive, _endCall);
		}
		else if (_endCall != null)
			_endCall(false);
	}

	/// <summary>
	/// iOS build.
	/// </summary>
	/// <param name="_strBuildVersion">String build version.</param>
	/// <param name="_strXcodeFinderPath">String xcode finder path.</param>
	/// <param name="_codeSign">Code sign.</param>
	static public void iOSBuild(string _strXcodePath, ScriptBuilder.eCodeSign _codeSign, bool _bGenerateArchive, Action<bool> _endCall = null)
	{
		if (string.IsNullOrEmpty(_strXcodePath) == false && Directory.Exists(_strXcodePath) == true)
		{
			// run iOSBuildGenerator.sh
			string path = Utility.GetProjectPath();
			string iOSBuildGeneratorPath = path + "/GameData/iOS/iOSBuildGenerator.sh";
			if (File.Exists(iOSBuildGeneratorPath) == true)
			{
				UnityEngine.Debug.Log("#################### iOSBuildGenerator Start ####################");

				bool bBatchMode = UnityEditorInternal.InternalEditorUtility.inBatchMode;

				// lock compile
				EditorApplication.LockReloadAssemblies();

				BuildGeneratorShell = ShellHelper.ProcessCommand(iOSBuildGeneratorPath, 
					MakeiOSBuildGeneratorArguments(Path.GetFileName(_strXcodePath), _strXcodePath, _codeSign, _bGenerateArchive), 
					() => 
					{
						BuildGeneratorShell = null;

						if (_endCall != null)
							_endCall(false);

						if (bBatchMode == true)
							EditorApplication.Exit(0);
					});
				BuildGeneratorShell.onLog += delegate(int arg1, string arg2)
				{
					if (arg1 == 0 && string.IsNullOrEmpty(arg2) == false)
						UnityEngine.Debug.Log(arg2);
				};
				BuildGeneratorShell.onDone += delegate()
				{
					UnityEngine.Debug.Log("#################### iOSBuildGenerator Done ####################");

					// unlock compile
					EditorApplication.UnlockReloadAssemblies();

					BuildGeneratorShell = null;

					if (_endCall != null)
						_endCall(true);

					if (bBatchMode == true)
						EditorApplication.Exit(0);
				};
			}
		}
	}

	/// <summary>
	/// Unitiy build for iOS.
	/// </summary>
	static public void UnityBuildInBatchModeForiOS()
	{
		// get args
		string[] args = System.Environment.GetCommandLineArgs();
		string startScenePath = GetArgs(args, "startScenePath", XCODE_PATH);
		// open start scene
		if (string.IsNullOrEmpty(startScenePath) == false)
		{
			EditorSceneManager.sceneOpened += OnOpenSceneFromUnityBuildInBatchModeForiOS;
			EditorSceneManager.OpenScene(Application.dataPath + startScenePath);
		}
	}

	/// <summary>
	/// Unitiy build for iOS.
	/// </summary>
	static public void OnOpenSceneFromUnityBuildInBatchModeForiOS(UnityEngine.SceneManagement.Scene _scene, OpenSceneMode _mode)
	{
		UnityEngine.Debug.Log("============================ iOS Build Start ============================");

		EditorSceneManager.sceneOpened -= OnOpenSceneFromUnityBuildInBatchModeForiOS;

		// get args
		string[] args = System.Environment.GetCommandLineArgs();
		string xcodePath = GetArgs(args, ARG_KEY_XCODE_PATH, XCODE_PATH);
		if (string.IsNullOrEmpty(xcodePath) == true)
			return;

		// set code sign
		ScriptBuilder.eCodeSign codeSign = ScriptBuilder.eCodeSign.ALL;
		string codeSignArg = GetArgs(args, ARG_KEY_CODE_SIGN, string.Empty);
		if (string.IsNullOrEmpty(codeSignArg) == false)
			codeSign = Utility.ParsingStringToEnumType<ScriptBuilder.eCodeSign>(codeSignArg);

		// set obfuscator
		Options obfuscatorOption = OptionsManager.LoadOptions();
		if (obfuscatorOption != null)
		{
			string obfucate = GetArgs(args, ARG_KEY_OBFUSCATOR, "true");
			obfuscatorOption.enabled = obfucate == "true" ? true : false;
		}

		// load build info
		ScriptBuilder.BuilderInfo builderInfo = new ScriptBuilder.BuilderInfo();
		builderInfo.LoadEditorPrefs();

		// set define symbols
		ScriptBuilder.ApplyDefine(ScriptBuilder.LoadSymbols(), false);

		// start build
		ScriptBuilder.StartBuild(xcodePath, codeSign, builderInfo, BuildOptions.None, true, BuildDoneCallBack);
		//iOSGenerateArchiveAndIpa("/Users/maxonsoft/Documents/hero_world_xcode/Dev/first_hero_v0.0.1_b0_r7", BuildDoneCallBack);
		//iOSGenerateArchiveAndIpa("/Users/gimhagseong/Documents/hero_world_xcode/Dev/first_hero_v0.0.1_b0_r7", BuildDoneCallBack);
	}
#endif

#region [MenuItem]
#if UNITY_IOS
	[MenuItem("CustomMenu/Build/iOS", priority = 111)]
	[MenuItem("CustomMenu/Build/iOS/All/Generate ipa With Archive", priority = 111)]
	static public void iOSGenerateIpaWithArchiveAll()
	{
		iOSGenerateArchiveAndIpa(SelectXcodePath(), ScriptBuilder.eCodeSign.ALL, true);
	}

	[MenuItem("CustomMenu/Build/iOS/All/Generate ipa", priority = 111)]
	static public void iOSGenerateIpaAll()
	{
		iOSGenerateArchiveAndIpa(SelectXcodePath(), ScriptBuilder.eCodeSign.ALL, false);
	}

	[MenuItem("CustomMenu/Build/iOS/Adhoc/Generate ipa With Archive", priority = 111)]
	static public void iOSGenerateIpaWithArchiveAdhoc()
	{
		iOSGenerateArchiveAndIpa(SelectXcodePath(), ScriptBuilder.eCodeSign.ADHOC, true);
	}

	[MenuItem("CustomMenu/Build/iOS/Adhoc/Generate ipa", priority = 111)]
	static public void iOSGenerateIpaAdhoc()
	{
		iOSGenerateArchiveAndIpa(SelectXcodePath(), ScriptBuilder.eCodeSign.ADHOC, false);
	}

	static private string SelectXcodePath()
	{
		string xcodePath = EditorUtility.OpenFolderPanel("Select Xcode project path", XCODE_PATH, string.Empty);
		if (Directory.Exists(xcodePath) == true && Directory.Exists(xcodePath + "/Unity-iPhone.xcodeproj") == true)
			return xcodePath;

		if (string.IsNullOrEmpty(xcodePath) == false)
			EditorUtility.DisplayDialog("ERROR", "Selected path is not Xcode project path", "OK");
		
		return string.Empty;
	}
#endif

	[MenuItem("CustomMenu/Build/Stop Build")]
	static public void StopBuild()
	{
		EditorApplication.UnlockReloadAssemblies();
		if (BuildGeneratorShell != null)
		{
			BuildGeneratorShell.Close();
			UnityEngine.Debug.LogWarning("Canceled build");
		}
	}

//	[MenuItem("CustomMenu/Test")]
//	static public void Test()
//	{
//		//UnityEngine.Debug.Log("");
//	}
#endregion
}
#endif