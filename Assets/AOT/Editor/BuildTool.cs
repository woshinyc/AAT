using HybridCLR.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class Editer 
{
    [MenuItem("BuildTool/HybridCLRCopyDlls")]
    public static void HybridCLRBuild() {
        // 知道更新哪一些程序集的信息
        var assemblys = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved;
 
        // 获取到程序集 打包后 所在的路径
        string Path = ($"{ Application.dataPath }/../HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}");
       // Debug.Log(Path);
        // 获取到指定路径文件夹 所有文件
        DirectoryInfo directoryInfo = new DirectoryInfo(Path);
        FileInfo[] files = directoryInfo.GetFiles();
        string aaDllpath = ($"{Application.dataPath}/AddressablesResources/HotUpdateDlls/");
        foreach (FileInfo file in files) {
            
            if (file.Extension != ".dll") continue;
            if (!assemblys.Contains(file.Name.Substring(0, file.Name.Length - 4))) continue;
            Debug.Log(aaDllpath + file.Name + ".bytes");
            file.CopyTo(aaDllpath + file.Name + ".bytes", true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //循环所有文件  和 热更新的文件对比


        //如果有想要的指定文件 那就把它复制到指定的aa包资源文件夹中
    }

}
