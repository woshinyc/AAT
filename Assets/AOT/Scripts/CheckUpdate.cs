using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CheckUpdate : MonoBehaviour
{
    [Serializable]
    private class DownloadInfo
    {
        public List<string> CatalogsInfo = new List<string>();
    }

    private DownloadInfo m_DownloadInfo = new DownloadInfo();

    private List<object> DownloadCatalogsKey = new List<object>();

    private string DOWNLOADKEY = "DOWNLOADKEY";

    private bool HasCatalogsUpdate => m_DownloadInfo != null && m_DownloadInfo.CatalogsInfo != null && m_DownloadInfo.CatalogsInfo.Count > 0;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Check());
    }
    private IEnumerator Check()
    {
     
        //检查资源更新
        yield return CheckAssetUpdate();
        //下载资源
        yield return DownloadAsset();
        //加载热更新程序集
        yield return LoadHotUpdateAssembly();
        //加载热更新的场景
        // yield return LoadScene("EntryScene");
        yield return LoadCube();

    }
    private IEnumerator LoadCube()
    {

        AsyncOperationHandle<GameObject> ObjHandle = Addressables.LoadAssetAsync<GameObject>("Cube");

        yield return ObjHandle;

        if (ObjHandle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = ObjHandle.Result;
            Instantiate(obj);
            Debug.Log("Cube生成成功");
        }
        else
        {
            Debug.Log("Cube生成成功");
        }
       // Addressables.Release(ObjHandle);
    }
    private IEnumerator CheckAssetUpdate()
    {
        AsyncOperationHandle<List<string>> CatalogsHandle = Addressables.CheckForCatalogUpdates(false);

        yield return CatalogsHandle;

        if (CatalogsHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("检查目录更新成功");
            foreach (var catalog in CatalogsHandle.Result)
            {
                Debug.Log($"Catalog: {catalog}");
            }
            m_DownloadInfo.CatalogsInfo = CatalogsHandle.Result;

            Addressables.Release(CatalogsHandle);

            if (HasCatalogsUpdate)
            {
                //服务器有更新
                //保存 更新的数据
                Debug.Log("服务器有资源更新");
                string JsonStr = JsonUtility.ToJson(m_DownloadInfo);
                PlayerPrefs.SetString(DOWNLOADKEY, JsonStr);

            }
            else
            {
                //服务器没有更新
                //检查上次更新 保存的数据 
                if (PlayerPrefs.HasKey(DOWNLOADKEY))
                {
                    Debug.Log("服务器有未完成更新的资源");

                    string Str = PlayerPrefs.GetString(DOWNLOADKEY);

                    JsonUtility.FromJsonOverwrite(Str, m_DownloadInfo);
                }
            }

            if (HasCatalogsUpdate)
            {
                //更新目录

                AsyncOperationHandle<List<IResourceLocator>> LocatorHandle = Addressables.UpdateCatalogs(m_DownloadInfo.CatalogsInfo, false);

                yield return LocatorHandle;

                if (LocatorHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("更新目录成功");

                    List<IResourceLocator> resourceLocators = LocatorHandle.Result;

                    Addressables.Release(LocatorHandle);

                    DownloadCatalogsKey.Clear();

                    foreach (IResourceLocator locator in resourceLocators)
                    {
                        DownloadCatalogsKey.AddRange(locator.Keys);
                    }
                }
                else
                {
                    Debug.Log("更新目录失败");
                }
            }
        }
        else
        {
            Debug.Log("检查目录更新失败");

        }


    }
    private IEnumerator DownloadAsset()
    {
        AsyncOperationHandle<long> SizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)DownloadCatalogsKey);

        yield return SizeHandle;

        long Size = SizeHandle.Result;

        if (Size == 0) yield break;

        Debug.Log("资源需要下载");

        AsyncOperationHandle Handle = Addressables.DownloadDependenciesAsync((IEnumerable)DownloadCatalogsKey, Addressables.MergeMode.Union, false);

        yield return Handle;

        if (Handle.Status == AsyncOperationStatus.Succeeded)
        {
            PlayerPrefs.DeleteKey(DOWNLOADKEY);
            Debug.Log("下载完成");
        }
        else Debug.Log("下载失败");

        Addressables.Release(Handle);
    }

    private IEnumerator LoadHotUpdateAssembly()
    {

        string Label = "HotUpdateDLL";

        AsyncOperationHandle<IList<TextAsset>> asshandle = Addressables.LoadAssetsAsync<TextAsset>(Label, null);

        IList<TextAsset> dllList = asshandle.WaitForCompletion();

        foreach (TextAsset dll in dllList)
        {
            byte[] ass = dll.bytes;
            Assembly.Load(ass);
        }


        yield return null;
    }

    private IEnumerator LoadScene(string SceneName)
    {
        yield return new WaitForSeconds(1);

        var Handle = Addressables.LoadSceneAsync(SceneName);

        yield return Handle;

        if (Handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.Log("场景加载失败");
            yield break;
        }

        Debug.Log("场景加载成功");
        yield return new WaitForSeconds(1);

    }
}
