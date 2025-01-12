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
     
        //�����Դ����
        yield return CheckAssetUpdate();
        //������Դ
        yield return DownloadAsset();
        //�����ȸ��³���
        yield return LoadHotUpdateAssembly();
        //�����ȸ��µĳ���
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
            Debug.Log("Cube���ɳɹ�");
        }
        else
        {
            Debug.Log("Cube���ɳɹ�");
        }
       // Addressables.Release(ObjHandle);
    }
    private IEnumerator CheckAssetUpdate()
    {
        AsyncOperationHandle<List<string>> CatalogsHandle = Addressables.CheckForCatalogUpdates(false);

        yield return CatalogsHandle;

        if (CatalogsHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("���Ŀ¼���³ɹ�");
            foreach (var catalog in CatalogsHandle.Result)
            {
                Debug.Log($"Catalog: {catalog}");
            }
            m_DownloadInfo.CatalogsInfo = CatalogsHandle.Result;

            Addressables.Release(CatalogsHandle);

            if (HasCatalogsUpdate)
            {
                //�������и���
                //���� ���µ�����
                Debug.Log("����������Դ����");
                string JsonStr = JsonUtility.ToJson(m_DownloadInfo);
                PlayerPrefs.SetString(DOWNLOADKEY, JsonStr);

            }
            else
            {
                //������û�и���
                //����ϴθ��� ��������� 
                if (PlayerPrefs.HasKey(DOWNLOADKEY))
                {
                    Debug.Log("��������δ��ɸ��µ���Դ");

                    string Str = PlayerPrefs.GetString(DOWNLOADKEY);

                    JsonUtility.FromJsonOverwrite(Str, m_DownloadInfo);
                }
            }

            if (HasCatalogsUpdate)
            {
                //����Ŀ¼

                AsyncOperationHandle<List<IResourceLocator>> LocatorHandle = Addressables.UpdateCatalogs(m_DownloadInfo.CatalogsInfo, false);

                yield return LocatorHandle;

                if (LocatorHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("����Ŀ¼�ɹ�");

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
                    Debug.Log("����Ŀ¼ʧ��");
                }
            }
        }
        else
        {
            Debug.Log("���Ŀ¼����ʧ��");

        }


    }
    private IEnumerator DownloadAsset()
    {
        AsyncOperationHandle<long> SizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)DownloadCatalogsKey);

        yield return SizeHandle;

        long Size = SizeHandle.Result;

        if (Size == 0) yield break;

        Debug.Log("��Դ��Ҫ����");

        AsyncOperationHandle Handle = Addressables.DownloadDependenciesAsync((IEnumerable)DownloadCatalogsKey, Addressables.MergeMode.Union, false);

        yield return Handle;

        if (Handle.Status == AsyncOperationStatus.Succeeded)
        {
            PlayerPrefs.DeleteKey(DOWNLOADKEY);
            Debug.Log("�������");
        }
        else Debug.Log("����ʧ��");

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
            Debug.Log("��������ʧ��");
            yield break;
        }

        Debug.Log("�������سɹ�");
        yield return new WaitForSeconds(1);

    }
}
