#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Text;
using UnityEditor.iOS.Xcode;

public class BuildPostProcessor
{
	private const string PROJECT_TARGET_NAME = "Unity-iPhone";
	private const string PROJECT_FRAMEWORK_PATH = "Frameworks";
	private const string FACEBOOK_APP_ID = "2110998512494820";
	private const string RESERVED_CLIENT_ID = "com.googleusercontent.apps.702988316132-vv0uovnleitsbtgf5o76fl6f690cvr6r";

	[PostProcessBuildAttribute(1)]
	public static void OnPostProcessBuild(BuildTarget target, string path)
	{
		if(target == BuildTarget.iOS)
		{
			string projPath = PBXProject.GetPBXProjectPath(path);
			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));
			string appTarget = proj.TargetGuidByName(PROJECT_TARGET_NAME);
			string srcFilePath = Application.dataPath + "/../GameData/iOS/framework";

			// framework 추가
			DirectoryInfo frameworkDir = new DirectoryInfo(srcFilePath);
			List<string> frameworksNameList = new List<string>();
			// directories
			foreach(DirectoryInfo file in frameworkDir.GetDirectories())
				frameworksNameList.Add(file.Name);
			foreach (string framework in frameworksNameList) 
			{
				string frameworkNamePath = PROJECT_FRAMEWORK_PATH + "/" + framework;
				CopyAndReplaceDirectory(srcFilePath + "/" + framework, Path.Combine(path, frameworkNamePath));
                AddFileToBuild(proj, appTarget, frameworkNamePath, frameworkNamePath);
            }

            // files
            foreach (FileInfo file in frameworkDir.GetFiles())
            {
                if (file.Name != ".DS_Store")
                {
                    string frameworkNamePath = PROJECT_FRAMEWORK_PATH + "/" + file.Name;
                    CopyAndReplaceFile(srcFilePath + "/" + file.Name, Path.Combine(path, frameworkNamePath));
                    AddFileToBuild(proj, appTarget, frameworkNamePath, frameworkNamePath);
                }
            }

            // 기본 프레임워크 추가
            proj.AddFrameworkToProject(appTarget, "AddressBook.framework", false);
			proj.AddFrameworkToProject(appTarget, "SafariServices.framework", false);
			proj.AddFrameworkToProject(appTarget, "Security.framework", false);
			proj.AddFrameworkToProject(appTarget, "SystemConfiguration.framework", false);
			proj.AddFrameworkToProject(appTarget, "AssetsLibrary.framework", false);
			proj.AddFrameworkToProject(appTarget, "Foundation.framework", false);
			proj.AddFrameworkToProject(appTarget, "CoreLocation.framework", false);
			proj.AddFrameworkToProject(appTarget, "CoreMotion.framework", false);
			proj.AddFrameworkToProject(appTarget, "CoreGraphics.framework", false);
			proj.AddFrameworkToProject(appTarget, "CoreText.framework", false);
			proj.AddFrameworkToProject(appTarget, "MediaPlayer.framework", false);
			proj.AddFrameworkToProject(appTarget, "UiKit.framework", false);
			proj.AddFrameworkToProject(appTarget, "WebKit.framework", false);
			proj.AddFrameworkToProject(appTarget, "UserNotifications.framework", false);
			proj.AddFrameworkToProject(appTarget, "MobileCoreServices.framework", false);
			proj.AddFrameworkToProject(appTarget, "AVFoundation.framework", false);
			proj.AddFrameworkToProject(appTarget, "CoreMedia.framework", false);
			proj.AddFrameworkToProject(appTarget, "ImageIO.framework", false);
			proj.AddFrameworkToProject(appTarget, "QuartzCore.framework", false);
			proj.AddFrameworkToProject(appTarget, "Photos.framework", false);
			proj.AddFrameworkToProject(appTarget, "StoreKit.framework", false);
			proj.AddFrameworkToProject(appTarget, "ReplayKit.framework", true);
			proj.AddFrameworkToProject(appTarget, "UserNotifications.framework", true);

			// set property
			proj.SetBuildProperty(appTarget, "ENABLE_BITCODE", "NO");
			// Preprocessor Macros 추가
			proj.AddBuildPropertyForConfig(proj.BuildConfigByName(appTarget, "ReleaseForProfiling"), "GCC_PREPROCESSOR_DEFINITIONS", "RUNNING_ON_XCODE");
			proj.AddBuildPropertyForConfig(proj.BuildConfigByName(appTarget, "ReleaseForRunning"), "GCC_PREPROCESSOR_DEFINITIONS", "RUNNING_ON_XCODE");   
			proj.AddBuildProperty(appTarget, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/" + PROJECT_FRAMEWORK_PATH);
			proj.AddBuildProperty(appTarget, "OTHER_LDFLAGS", "-ObjC");

			AddUsrLib(proj, appTarget, "libz.tbd");
            // AddUsrLib(proj, appTarget, "libsqlite3.tbd");
            // AddUsrLib(proj, appTarget, "libiconv.2.dylib");

            const string entitlementName = "chosenheroes.entitlements";
   //         string entitlementFile = string.Format("/{0}/{1}", UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTargetName(), entitlementName);
			//string entitlementFilePath = path + entitlementFile;

			//if(!File.Exists(entitlementFilePath))
			//{
			//	//없으니 새로 생성함
			//	using(StreamWriter sw = new StreamWriter(entitlementFilePath))
			//	{
			//		sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			//		sw.WriteLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
			//		sw.WriteLine("<plist version=\"1.0\">");
			//		sw.WriteLine("<dict>");
			//		sw.WriteLine("\t<key>aps-environment</key>");
			//		sw.WriteLine("\t<string>development</string>");
			//		sw.WriteLine("</dict>");
			//		sw.WriteLine("</plist>");
			//	}
			//}

			//string guid = proj.AddFile(UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTargetName() + "/chosenheroes.entitlements", "chosenheroes.entitlements");
			//proj.AddFileToBuild(appTarget, guid);
			//proj.SetBuildProperty(appTarget, "CODE_SIGN_ENTITLEMENTS", "$(PROJECT_DIR)" + entitlementFile);

			File.WriteAllText(projPath, proj.WriteToString());

			// plist edit
			ChangeXcodePlist(path);

            SetCapabilities(projPath, entitlementName);

			// mocca 수정 내용이 반영된 UnityAppController.mm 복사
			// 후에 버전이 변경되면 내용도 업데이트를 해준다.
			CopyAndReplaceFile(Application.dataPath + "/../GameData/iOS/UnityAppController.mm", path + "/Classes/UnityAppController.mm");

			// Keyboard.mm 오류 수정된 파일로 복사
			CopyAndReplaceFile(Application.dataPath + "/../GameData/iOS/Keyboard.mm", path + "/Classes/UI/Keyboard.mm");

			// 1024x1024 아이콘 셋팅
			Set1024AppIcon(path);

			// BundleDisplayName localizing 셋팅
			string infoPlistPath = string.Format("{0}/{1}", path, UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTestTargetName() + "/en.lproj/InfoPlist.strings");
			if(File.Exists(infoPlistPath))
			{
				// 기존 InfoPlist.strings 삭제
				File.Delete(infoPlistPath);
				// InfoPlist.strings 새로 생성
				using(StreamWriter sw = new StreamWriter(infoPlistPath))
				{
					sw.WriteLine("\"CFBundleDisplayName\" = \"Chosen Heroes\";");
					sw.WriteLine("\"CFBundleName\" = \"Chosen Heroes\";");
					sw.WriteLine("\"NSPhotoLibraryUsageDescription\" = \"Access your photos to create a post.\";");
				}
			}

			// BundleDisplayName localizing 셋팅
			AddLocalizedStringsIOS(path, Application.dataPath + "/../GameData/iOS/Localizing");

			EditorUtility.RevealInFinder(path);

			Debug.Log("Build iOS. path: " + projPath);
		}
	}

    // Unity 5.6.1에서는 PBXProject가 Capability를 지원하지 않으므로 Custom pbxProject를 쓴다.
    public static void SetCapabilities(string pbxProjectPath, string entitlementFilePath)
    {
        UnityEditor.iOS.Xcode.Custom.ProjectCapabilityManager capabilityMng =
            new UnityEditor.iOS.Xcode.Custom.ProjectCapabilityManager(pbxProjectPath, entitlementFilePath, PROJECT_TARGET_NAME);
        capabilityMng.AddGameCenter();
        capabilityMng.AddPushNotifications(true);
        capabilityMng.AddInAppPurchase();
        capabilityMng.WriteToFile();
    }

	public static void ChangeXcodePlist(string pathToBuiltProject)
	{
		string plistPath = pathToBuiltProject + "/Info.plist";

		PlistDocument plist = new PlistDocument();
		plist.ReadFromString(File.ReadAllText(plistPath));

		// Get root
		PlistElementDict rootDict = plist.root;

		// BundleDisplayName localizing 셋팅
		rootDict.SetBoolean("LSHasLocalizedDisplayName", true);

		// 수출 규정 관련 문서 누락 경고 무시
		rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

		// url type
		PlistElementArray urlType = rootDict.CreateArray("CFBundleURLTypes");
		PlistElementDict urlGoogle = urlType.AddDict();
		urlGoogle.SetString("CFBundleTypeRole", "Editor");
		urlGoogle.SetString("CFBundleURLName", "com.yallagamez.chosenheroes");       

		// PlistElementDict urlItemDic = urlType.AddDict();
		// urlItemDic.SetString("Document Role", "Editor");
		PlistElementArray schemesArray = urlGoogle.CreateArray("CFBundleURLSchemes");
		schemesArray.AddString("com.yallagamez.chosenheroes");
		schemesArray.AddString("fb" + FACEBOOK_APP_ID);
		schemesArray.AddString(RESERVED_CLIENT_ID);

		// faceboo app id
		rootDict.SetString("FacebookAppID", FACEBOOK_APP_ID);
		rootDict.SetString ("FacebookDisplayName", "${PRODUCT_NAME}");
		PlistElementArray applicationQueriesSchemes = rootDict.CreateArray("LSApplicationQueriesSchemes");
		applicationQueriesSchemes.AddString("fb-messenger-api");
		applicationQueriesSchemes.AddString("fbauth2");
		applicationQueriesSchemes.AddString("fbshareextension"); 
		applicationQueriesSchemes.AddString("naversearchapp");  
		applicationQueriesSchemes.AddString("naversearchthirdlogin");  

		PlistElementArray capability = rootDict["UIRequiredDeviceCapabilities"] as PlistElementArray;
		capability.AddString("gamekit");

		// 사진첩 저장 목적 명시
		rootDict.SetString("NSPhotoLibraryUsageDescription", "save screen shot");    

		// Write to file
		File.WriteAllText(plistPath, plist.WriteToString());
	}

    private static void AddFileToBuild(PBXProject proj, string appTarget, string path, string projectPath, PBXSourceTree sourceTree = PBXSourceTree.Source)
    {
        proj.AddFileToBuild(appTarget, proj.AddFile(path, projectPath, sourceTree));
    }

    private static void AddUsrLib(PBXProject proj, string targetGuid, string framework)
    {
        string fileGuid = proj.AddFile("usr/lib/" + framework, "Frameworks/" + framework, PBXSourceTree.Sdk);
        proj.AddFileToBuild(targetGuid, fileGuid);
    }

    /// <summary>
    /// 1024x1024 아이콘 셋팅
    /// </summary>
    private static void Set1024AppIcon(string projectPath)
	{
		string iconInfoForderPath = projectPath + 
			string.Format("/{0}/Images.xcassets/AppIcon.appiconset/", UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTargetName());
		string iconInfoFilePath = iconInfoForderPath + "Contents.json";
		if (File.Exists(iconInfoFilePath) == true)
		{
			JSONObject iconInfo = JSONObject.Parse(File.ReadAllText(iconInfoFilePath));
			JSONArray arrayImages = iconInfo.GetArray("images");
			if (arrayImages != null)
			{
				// 1024 아이콘 정보 추가
				JSONObject icon1024 = new JSONObject();
				icon1024.Add("size", "1024x1024");
				icon1024.Add("idiom", "ios-marketing");
				icon1024.Add("filename", "Icon-1024.png");
				icon1024.Add("scale", "1x");
				arrayImages.Add(icon1024);
				iconInfo.Add("images", arrayImages);
				File.WriteAllText(iconInfoFilePath, iconInfo.ToString());

				// 아이콘 복사 추가
				string copyPath = iconInfoForderPath + "Icon-1024.png";
				if (File.Exists(copyPath) == false)
					File.Copy(Application.dataPath + "/../Assets/2DResource/ICON/Icon-1024.png", copyPath, true);
			}
		}
	}

	public static void AddLocalizedStringsIOS(string projectPath, string localizedDirectoryPath)
	{        
		DirectoryInfo dir = new DirectoryInfo(localizedDirectoryPath);
		if(!dir.Exists)
			return;

		List<string> locales = new List<string>();
		var localeDirs = dir.GetDirectories("*.lproj", SearchOption.TopDirectoryOnly);

		foreach(var sub in localeDirs)
			locales.Add(Path.GetFileNameWithoutExtension(sub.Name));

		AddLocalizedStringsIOS(projectPath, localizedDirectoryPath, locales);
	}

	public static void AddLocalizedStringsIOS(string projectPath, string localizedDirectoryPath, List<string> validLocales)
	{
		string projPath = PBXProject.GetPBXProjectPath(projectPath);
		UnityEditor.iOS.Xcode.Custom.PBXProject proj = new UnityEditor.iOS.Xcode.Custom.PBXProject();
		proj.ReadFromFile(projPath);

		foreach(var locale in validLocales)
		{
			// copy contents in the localization directory to project directory
			string src = Path.Combine(localizedDirectoryPath, locale + ".lproj");
			DirectoryCopy(src, Path.Combine(projectPath, UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTargetName() + "/" + locale + ".lproj"));

			string fileRelatvePath = string.Format("{0}/{1}.lproj/InfoPlist.strings", UnityEditor.iOS.Xcode.Custom.PBXProject.GetUnityTargetName(), locale);
			proj.AddLocalization("InfoPlist.strings", locale, fileRelatvePath);
		}

		proj.WriteToFile(projPath);
	}


	private static void DirectoryCopy(string sourceDirName, string destDirName)
	{
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);

		if (!dir.Exists)
			return;

		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}

		FileInfo[] files = dir.GetFiles();

		foreach (FileInfo file in files)
		{
			// skip unity meta files
			if(file.FullName.EndsWith(".meta"))
				continue;
			string temppath = Path.Combine(destDirName, file.Name);
			file.CopyTo(temppath, true);
		}

		DirectoryInfo[] dirs = dir.GetDirectories();
		foreach (DirectoryInfo subdir in dirs)
		{
			string temppath = Path.Combine(destDirName, subdir.Name);
			DirectoryCopy(subdir.FullName, temppath);
		}
	}

	private static void CopyAndReplaceFile(string srcPath, string dstPath)
	{
		if (File.Exists(srcPath) == true)
		{
			File.Copy(srcPath, dstPath, true);
		}
	}

	private static void CopyAndReplaceDirectory(string srcPath, string dstPath)
	{
		if (Directory.Exists(dstPath))
		{
			Directory.Delete(dstPath, true);
		}
		if (File.Exists(dstPath))
		{
			File.Delete(dstPath);
		}

		Directory.CreateDirectory(dstPath);

		foreach (var file in Directory.GetFiles(srcPath))
			File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));

		foreach (var dir in Directory.GetDirectories(srcPath))
			CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)));
	}
}
#endif