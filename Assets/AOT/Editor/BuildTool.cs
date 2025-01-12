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
        // ֪��������һЩ���򼯵���Ϣ
        var assemblys = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved;
 
        // ��ȡ������ ����� ���ڵ�·��
        string Path = ($"{ Application.dataPath }/../HybridCLRData/HotUpdateDlls/{EditorUserBuildSettings.activeBuildTarget}");
       // Debug.Log(Path);
        // ��ȡ��ָ��·���ļ��� �����ļ�
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
        //ѭ�������ļ�  �� �ȸ��µ��ļ��Ա�


        //�������Ҫ��ָ���ļ� �ǾͰ������Ƶ�ָ����aa����Դ�ļ�����
    }

}
