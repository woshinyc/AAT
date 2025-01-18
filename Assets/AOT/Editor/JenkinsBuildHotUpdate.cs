// JenkinsBuild
// Shepherd Zhu
// Jenkins Build Helper
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public partial class JenkinsBuild : MonoBehaviour
{
    // 重要提醒：建议先在工作电脑上配好Groups和Labels，本脚本虽说遇到新文件可以添加到Addressables，但是不太可靠。
    [MenuItem("Shepherd0619/Test")]
    public static void Test()
    {
        string sourcePath = Path.Combine(
           Application.dataPath,
           $"../{SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget)}"
       );
        Debug.Log($"sourcePath:{sourcePath}");

        string destinationPath = Path.Combine(Application.dataPath, "HotUpdateDLLs");

        List<string> hotUpdateAssemblyNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
        for (int i = 0; i < hotUpdateAssemblyNames.Count; i++)
        {
            Debug.Log($"[JenkinsBuild] Copy: {hotUpdateAssemblyNames[i] + ".dll"}");

            Debug.Log(sourcePath + "/" + hotUpdateAssemblyNames[i] + ".dll");
            Debug.Log(Path.Combine(destinationPath, hotUpdateAssemblyNames[i] + ".dll.bytes"));
            Debug.Log(destinationPath+ hotUpdateAssemblyNames[i] + ".dll.bytes");
        }


        sourcePath = Path.Combine(
         Application.dataPath,
         $"../{SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget)}"
     );
        Debug.Log($"sourcePath2:{sourcePath}");

        string aotReferencesFilePath = Path.Combine(
        Application.dataPath,
        SettingsUtil.HybridCLRSettings.outputAOTGenericReferenceFile
    );
        Debug.Log($"aotReferencesFilePath:{aotReferencesFilePath}");


        // 读取AOTGenericReferences.cs文件内容
        string[] aotReferencesFileContent = File.ReadAllLines(aotReferencesFilePath);

        // 查找PatchedAOTAssemblyList列表
        List<string> patchedAOTAssemblyList = new List<string>();

        for (int i = 0; i < aotReferencesFileContent.Length; i++)
        {
            if (aotReferencesFileContent[i].Contains("PatchedAOTAssemblyList"))
            {
                while (!aotReferencesFileContent[i].Contains("};"))
                {
                    var aa = aotReferencesFileContent[i];
                    Debug.Log($"aa:{aa}");
                    if (aotReferencesFileContent[i].Contains("\""))
                    {
                        int startIndex = aotReferencesFileContent[i].IndexOf("\"") + 1;
                        int endIndex = aotReferencesFileContent[i].LastIndexOf("\"");
                        string dllName = aotReferencesFileContent[i].Substring(
                            startIndex,
                            endIndex - startIndex
                        );
                        Debug.Log($"dllName:{dllName}");
                        patchedAOTAssemblyList.Add(dllName);
                    }
                    i++;
                }
                break;
            }
        }

    }

    [MenuItem("Shepherd0619/Test2")]
    public static void Test2()
    {
        var aa = Enum.GetName(typeof(BuildTarget), EditorUserBuildSettings.activeBuildTarget);
        string path = Path.Combine(Application.dataPath, "../ServerData/" + Enum.GetName(typeof(BuildTarget), EditorUserBuildSettings.activeBuildTarget));
        Debug.Log($"aa:{aa}");
  

    

        // AddressableAssetSettings.activeProfileId = activeProfileId;

    }
    // 
    [MenuItem("Shepherd0619/Build Hot Update")]
    /// <summary>
    /// 开始执行HybridCLR热更打包，默认打当前平台
    /// </summary>
    public static void BuildHotUpdate()
    {
        Debug.Log($"构建地址:{EditorUserBuildSettings.activeBuildTarget}");
        BuildHotUpdate(EditorUserBuildSettings.activeBuildTarget);
    }

    /// <summary>
    /// 开始执行HybridCLR热更打包
    /// </summary>
    /// <param name="target">目标平台</param>
    public static void BuildHotUpdate(BuildTarget target)
    {
        Debug.Log(
            $"[JenkinsBuild] Start building hot update for {Enum.GetName(typeof(BuildTarget), target)}"
        );
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        settings.activeProfileId = settings.profileSettings.GetProfileId("腾讯云");


        #region  第一步 用华佗的打包API 调用打包生成文件

        try
        {
            //编译DLL文件。     
            CompileDllCommand.CompileDll(target);                               //调用 HybridCLR 的 CompileDll 方法，为目标平台编译热更 DLL      ===> HybridCLR/CompileDll/ActiveBuildTarget
            //生成Il2Cpp定义、AOT桥接函数、方法桥接文件等。
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();                      //生成 IL2CPP 定义文件：处理热更函数与底层代码的互通需求。       ===> HybridCLR/Generate/Il2CppDef
            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(target);                       //生成 XML 配置：指导编译器如何处理热更 DLL。                    ===> HybridCLR/Generate/LinkXml
            // 生成裁剪后的aot dll
            StripAOTDllCommand.GenerateStripedAOTDlls(target);                  //裁剪 AOT DLL：减少最终 AOT 文件大小，仅保留必要的部分。        ===> HybridCLR/Generate/AOTDlls
            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);      //生成方法桥接和反向调用桥接：处理 C# 和底层代码的调用兼容性问题   ===> HybridCLR/Generate/MethodBridgeAndReversePInvokeWrapper
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);                       //生成 AOT 泛型引用：确保 AOT 模式下，泛型的调用行为一致。         ===> HybridCLR/Generate/AOTGenericReference


        }
        catch (Exception e)
        {
            Debug.Log(
                $"[JenkinsBuild] ERROR while building hot update! Message:\n{e.ToString()}"
            );
            return;
        }
        #endregion
        // 下面开始移动文件
        #region  第二步  把生成的华佗dll 从 HybridCLRData/HotUpdateDlls  复制到工程目录里 Assets/HotUpdateDLLs   并以 .dll.bytes 结尾

        // Application.dataPath:    例: D:/workspace/AAAndHT/Assets
        //#  定义源路径与目标路径
        // 复制打出来的DLL并进行替换  sourcePath：指定 HybridCLR 热更 DLL 的输出目录。 
        // 例:   sourcePath:D:/workspace/AAAndHT/Assets\../HybridCLRData/HotUpdateDlls/StandaloneWindows64
        string sourcePath = Path.Combine(
            Application.dataPath,
            $"../{SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target)}"
        );
        // 定义destinationPath 热更新的路径 ：定义复制的目标目录为 例: Assets/HotUpdateDLLs。
        string destinationPath = Path.Combine(Application.dataPath, "HotUpdateDLLs");
        //如果源路径不存在，则打印错误并中断构建。
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError(
                "[JenkinsBuild] Source directory does not exist! Possibly HybridCLR build failed!"
            );
            return;
        }

        if (!Directory.Exists(destinationPath))
        {
            Debug.Log(
                $"[JenkinsBuild] Destination directory does not exist! Abort the build!  destinationPath:{destinationPath}"
            );
            Directory.CreateDirectory(destinationPath);
            Debug.Log($"[JenkinsBuild] Destination directory created successfully.destinationPath:{destinationPath}");
        }
        #endregion
        // string[] dllFiles = Directory.GetFiles(sourcePath, "*.dll");

        // foreach (string dllFile in dllFiles)
        // {
        //     string fileName = Path.GetFileName(dllFile);
        //     string destinationFile = Path.Combine(destinationPath, fileName + ".bytes");
        //     Debug.Log($"[JenkinsBuild] Copy: {dllFile}");
        //     File.Copy(dllFile, destinationFile, true);
        // }


        //复制文件
        // 1 获取所有需要热更的 DLL 名称（从配置文件 SettingsUtil 中读取）。
        List<string> hotUpdateAssemblyNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
        // 2 将每个 DLL 文件从源路径复制到目标路径，同时添加后缀 .bytes
        for (int i = 0; i < hotUpdateAssemblyNames.Count; i++)
        {
            Debug.Log($"[JenkinsBuild] Copy: {hotUpdateAssemblyNames[i] + ".dll"}");
            File.Copy(sourcePath + "/" + hotUpdateAssemblyNames[i] + ".dll", Path.Combine(destinationPath, hotUpdateAssemblyNames[i] + ".dll.bytes"), true);
        }

        Debug.Log("[JenkinsBuild] Hot Update DLLs copied successfully!");
        #region  第二步 把生成的华佗  AOT 元数据 DLL 从 HybridCLRData/AssembliesPostIl2CppStrip  复制到工程目录里 Assets/HotUpdateDLLs/AOTMetadata
        // 处理 AOT 元数据 DLL
        // 复制打出来的AOT元数据DLL并进行替换
        Debug.Log("[JenkinsBuild] Start copying AOT Metadata DLLs!");

   
        // 将之前 sourcePath = HybridCLRData/HotUpdateDll 改为 sourcePath = HybridCLRData/AssembliesPostIl2CppStrip
        // 复制 AssembliesPostIl2CppStrip里 并进行替换  sourcePath：指定 HybridCLR 热更 DLL 裁剪后 的输出目录。
        // AssembliesPostIl2CppStrip为（经过 IL2CPP 裁剪后的程序集（DLL） 
        // 位置 例: sourcePath为   D:/workspace/AAAndHT/Assets\../HybridCLRData/AssembliesPostIl2CppStrip/StandaloneWindows64
        sourcePath = Path.Combine(
            Application.dataPath,
            $"../{SettingsUtil.GetAssembliesPostIl2CppStripDir(target)}"
        );
        // 定义源路径为裁剪后的 AOT 元数据 DLL 输出目录，目标路径 destinationPath 为 Assets/HotUpdateDLLs/AOTMetadata。
        destinationPath = Path.Combine(Application.dataPath, "HotUpdateDLLs/AOTMetadata");

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError(
                "[JenkinsBuild] Source directory does not exist! Possibly HybridCLR build failed!"
            );
            return;
        }

        if (!Directory.Exists(destinationPath))
        {
            Debug.Log(
                $"[JenkinsBuild] Destination directory does not exist! Abort the build! destinationPath:{destinationPath}"
            );
            Directory.CreateDirectory(destinationPath);
            Debug.Log($"[JenkinsBuild] Destination directory created successfully. destinationPath:{destinationPath}");
        }
        #endregion
        #region  第三步  把 AOTGenericReferences.cs 里面存放 元数据放到热更新目录里
        
        //---A---   找到AOTGenericReferences.cs 文件
        // 获取AOTGenericReferences.cs文件的路径   例：:/Assets\HybridCLRGenerate/AOTGenericReferences.cs
        string aotReferencesFilePath = Path.Combine(
            Application.dataPath,
            SettingsUtil.HybridCLRSettings.outputAOTGenericReferenceFile
        );
        // 检查 AOTGenericReferences.cs 是否存在。
        if (!File.Exists(aotReferencesFilePath))
        {
            //如果文件不存在，则打印错误并中断流程。
            Debug.LogError(
                "[JenkinsBuild] AOTGenericReferences.cs file does not exist! Abort the build!"
            );
            return;
        }

        //---B---   从AOTGenericReferences.cs 文件里提取 需要补充的 元数据的名字 放入到  patchedAOTAssemblyList 里
        string[] aotReferencesFileContent = File.ReadAllLines(aotReferencesFilePath);

        /*  
         解析 AOTGenericReferences.cs 的 PatchedAOTAssemblyList字段 -->  获得 查找PatchedAOTAssemblyList列表
         PatchedAOTAssemblyList:   
                1 裁剪后的 AOT 程序集列表
                2 指定 AOT Metadata 加载范围
                用途
                    HybridCLR 使用这些文件生成 AOTMetadata, 以支持运行时加载和泛型实例化。
                    如果某些 AOT 程序集未被包含在列表中，其泛型支持可能会在运行时失效。
                来源：
                    这些 DLL 文件一般存放在 AssembliesPostIl2CppStrip 文件夹中，是裁剪后的 AOT 元数据文件
        */
        List<string> patchedAOTAssemblyList = new List<string>();

        for (int i = 0; i < aotReferencesFileContent.Length; i++)
        {
            if (aotReferencesFileContent[i].Contains("PatchedAOTAssemblyList"))
            {
                while (!aotReferencesFileContent[i].Contains("};"))
                {
                    if (aotReferencesFileContent[i].Contains("\""))
                    {
                        int startIndex = aotReferencesFileContent[i].IndexOf("\"") + 1;
                        int endIndex = aotReferencesFileContent[i].LastIndexOf("\"");
                        string dllName = aotReferencesFileContent[i].Substring(
                            startIndex,
                            endIndex - startIndex
                        );
                        patchedAOTAssemblyList.Add(dllName);
                    }
                    i++;
                }
                break;
            }
        }
        /*  ---C---
         * 利用 刚整理的 patchedAOTAssemblyList 元数据名单 
         * 从        sourcePath :        /HybridCLRData/AssembliesPostIl2CppStrip/StandaloneWindows64
         * 复制到    destinationPath:    Assets/HotUpdateDLLs/AOTMetadata 
         * 并在结尾处加上 .bytes 后缀
        */

        // 复制DLL文件到目标文件夹，并添加后缀名".bytes"
        foreach (string dllName in patchedAOTAssemblyList)
        {
            string sourceFile = Path.Combine(sourcePath, dllName);
            string destinationFile = Path.Combine(
                destinationPath,
                Path.GetFileName(dllName) + ".bytes"
            );

            if (File.Exists(sourceFile))
            {
                Debug.Log($"[JenkinsBuild] Copy: {sourceFile}");
                File.Copy(sourceFile, destinationFile, true);
                //SetAOTMetadataDllLabel("Assets/HotUpdateDLLs/" + Path.GetFileName(dllName) + ".bytes");
            }
            else
            {
                Debug.Log("[JenkinsBuild] AOTMetadata DLL file not found: " + dllName);
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log("[JenkinsBuild] BuildHotUpdate complete!");

        AssetDatabase.Refresh();
        #endregion

        #region  第四步  将 生成 并移动到 Assets/HotUpdateDLLs 下的 热更新文件和元数据文件  加入到Addressables 的 DLLs 组里 并标记  default + (HotUpdateDLL 或者 AOTMetadataDLL)
        // 刷新后开始给DLL加标签
        //SetHotUpdateDllLabel("Assets/HotUpdateDLLs/Assembly-CSharp.dll.bytes");
        for (int i = 0; i < hotUpdateAssemblyNames.Count; i++)
        {
            SetHotUpdateDllLabel("Assets/HotUpdateDLLs/" + hotUpdateAssemblyNames[i] + ".dll.bytes");
        }

        foreach (string dllName in patchedAOTAssemblyList)
        {
            SetAOTMetadataDllLabel("Assets/HotUpdateDLLs/AOTMetadata/" + Path.GetFileName(dllName) + ".bytes");
        }
        #endregion

        #region  第五步 重新用Addressable 生成一下 文件  并删除旧的文件
        Debug.Log("[JenkinsBuild] Start building Addressables!");
        buildAddressableContent();
        #endregion
        Debug.Log("[JenkinsBuild]  JenkinsBuildHotUpdate finish!");
    }
    public static void BuildHotUpdateForWindows64()
    {
        BuildHotUpdate(BuildTarget.StandaloneWindows64);
    }

    public static void BuildHotUpdateForiOS()
    {
        BuildHotUpdate(BuildTarget.iOS);
    }

    public static void BuildHotUpdateForLinux64()
    {
        BuildHotUpdate(BuildTarget.StandaloneLinux64);
    }

    public static void BuildHotUpdateForAndroid()
    {
        BuildHotUpdate(BuildTarget.Android);
    }



   
    /// <summary>
    /// 将热更DLL加入到Addressables
    /// </summary>
    /// <param name="dllPath">DLL完整路径</param>
    private static void SetHotUpdateDllLabel(string dllPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = settings.FindGroup("DLLs");
        if (group == null)
        {
            // 如果 DLLs 组不存在，则创建新的组
            group = settings.CreateGroup("DLLs", false, false, false, null);
            Debug.Log("[SetHotUpdateDllLabel] Created a new AddressableAssetGroup: DLLs");
        }
        // 检查是否存在 "HotUpdateDLL" 标签
        const string label = "HotUpdateDLL";
        if (!settings.GetLabels().Contains(label))
        {
            settings.AddLabel(label); // 添加新的标签
            Debug.Log($"[SetAOTMetadataDllLabel] Added new label: {label}");
        }
        var guid = AssetDatabase.AssetPathToGUID(dllPath);
        if (settings.FindAssetEntry(guid) != null)
        {
            Debug.Log(
                $"[JenkinsBuild.SetHotUpdateDLLLabel] {dllPath} already exist in Addressables. Abort!"
            );
            return;
        }
        var entry = settings.CreateOrMoveEntry(guid, group);
   
        entry.labels.Add(label);
        //Path.GetFileName(dllPath) 提取 DLL 文件的文件名（不包括路径）。 例如，dllPath = "Assets/HotUpdateDLLs/MyHotUpdate.dll"，则 Path.GetFileName(dllPath) 的结果是 MyHotUpdate.dll。
        entry.address = Path.GetFileName(dllPath); 

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    /// <summary>
    /// 将AOT元数据DLL加入到Addressables
    /// </summary>
    /// <param name="dllPath">DLL完整路径</param>
    private static void SetAOTMetadataDllLabel(string dllPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = settings.FindGroup("DLLs");
        if (group == null)
        {
            // 如果 DLLs 组不存在，则创建新的组
            group = settings.CreateGroup("DLLs", false, false, false, null);
            Debug.Log("[SetAOTMetadataDllLabel] Created a new AddressableAssetGroup: DLLs");
        }
        // 检查是否存在 "HotUpdateDLL" 标签
        const string label = "HotUpdateDLL";
        if (!settings.GetLabels().Contains(label))
        {
            settings.AddLabel(label); // 添加新的标签
            Debug.Log($"[SetAOTMetadataDllLabel] Added new label: {label}");
        }
        var guid = AssetDatabase.AssetPathToGUID(dllPath);
        if (settings.FindAssetEntry(guid) != null)
        {
            Debug.Log(
                $"[JenkinsBuild.SetAOTMetadataDLLLabel] {dllPath} already exist in Addressables. Abort!"
            );
            return;
        }
        var entry = settings.CreateOrMoveEntry(guid, group);
 
        entry.labels.Add(label);
        entry.address = Path.GetFileName(dllPath);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }
    // 重新用Addressable 生成一下 文件  并删除旧的文件
    // 
    private static bool buildAddressableContent()
    {
        //A 生成之前先将对于的ServerData 下的文件夹删除 把本地的ServerData  对于平台文件夹删了
        string path = Path.Combine(Application.dataPath, "../ServerData/" + Enum.GetName(typeof(BuildTarget), EditorUserBuildSettings.activeBuildTarget));
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        // B  重新生成一下本地和远程文件
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            Debug.Log("[JenkinsBuild.buildAddressableContent] Addressables build error encountered: " + result.Error);
        }
        return success;
    }


}