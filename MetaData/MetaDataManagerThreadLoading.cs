using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

/// <summary>
/// thread 로딩 관련 MetaDataManager
/// </summary>
public static partial class MetaDataManager
{
    /// <summary>
    /// thread 로딩 전에 예외 및 미리 로딩해야 하는 메타 데이터
    /// </summary>
    private enum eBeforehandLoadingMetaData
    {
        SYSTEM_TEXT_DATA,
        EVENT_META_DATA,
        // 상점 데이터
        SHOP_INFORMATION_META_DATA,
        SHOP_ITEM_META_DATA,
        UI_SHOP_INFORMATION_META_DATA,
        UI_SHOP_ITEM_META_DATA,
        TEXT_SHOP_INFORMATION_META_DATA,
        TEXT_SHOP_ITEM_META_DATA,
        // 패키지 데이터
        UI_PACKAGE_CATEGORY_META_DATA,
        UI_CYCLE_PACKAGE_META_DATA,
        UI_FLATRATE_META_DATA,
        UI_LEVELUP_PACKAGE_META_DATA,
        UI_WEEKLY_PACKAGE_META_DATA,
        UI_ALWAYS_PACKAGE_META_DATA,
        UI_LEVEL_LIMIT_PACKAGE_META_DATA,
        TEXT_PACKAGE_CATEGORY_META_DATA,
        TEXT_CYCLE_PACKAGE_META_DATA,
        TEXT_FLATRATE_META_DATA,
        TEXT_LEVEL_LIMIT_PACKAGE_META_DATA,
        TEXT_LEVELUP_PACKAGE_META_DATA,
        TEXT_WEEKLY_PACKAGE_META_DATA,
        TEXT_ALWAYS_PACKAGE_META_DATA,
        PACKAGE_CATEGORY_META_DATA,
        LIMITED_BM_META_DATA,
        CYCLE_PACKAGE_META_DATA,
        FLATRATE_META_DATA,
        LEVEL_LIMIT_PACKAGE_META_DATA,
        LEVELUP_PACKAGE_META_DATA,
        WEEKLY_PACKAGE_META_DATA,
        ALWAYS_PACKAGE_META_DATA,
        // 영웅 데이터 관련
        HERO_UPGRADE_REQUIRED_HERO_COUNT_TO_BREAK_LIMIT_META_DATA,
        HERO_UPGRADE_SIMPLE_META_DATA,
        RUNE_UPGRADE_META_DATA,
        EQUIPMENT_META_DATA,
        //재료 메타 데이터 관련.
        MATERIAL_META_DATA,
        // 마일리지 정보
        MILEAGE_META_DATA,
        REQUIRED_MILEAGE_EXP_META_DATA
    }

    /// <summary>
    /// 메타 데이터 파싱 순서
    /// 0 -> DEFAULT 순으로 순차적으로 파싱한다
    /// </summary>
    private enum eMetaDataParsingOrder
    {
        COMMON_UI_TEXT_DATA = 0,
        TEXT_TUTORIAL_META_DATA,
        SCENARIO_DIALOG_META_DATA,
        PLAY_SETTING_META_DATA,
        REQUIRED_EXP_META_DATA,
        PLAY_PARAMETER_INIT_DATA,
        HERO_EXTRA_SKILL_META_DATA,
        HERO_META_DATA,
        TROOP_META_DATA,
        WEAPON_META_DATA,
        ARTIFICE_META_DATA,
        RUNE_META_DATA,
        RUNE_CATEGORY_META_DATA,
        HERO_UPGRADE_META_DATA,
        BUILDING_LEVELUP_META_DATA,
        GACHA_COST_META_DATA,

        DEFAULT = 99999
    }

    /// <summary>
    /// thread 로딩시 thread pool 갯수
    /// </summary>
    private const int WORKER_THREADS_COUNT = 10;
    /// <summary>
    /// 팁UI에서 thread 로딩시 thread pool 갯수
    /// </summary>
    private const int WORKER_THREADS_COUNT_ON_TIPUI = 20;
    /// <summary>
    /// thread 로딩시 시간 초과 체크 시간
    /// </summary>
    private const long THREAD_TIME_OUT_MILLISECONDS = 20000;
    /// <summary>
    /// 로드 제외시킬 테이블 이름 리스트
    /// </summary>
    private static string[] mArrExcludeFromLoadTableName = new string[] 
    {
        "PROLOGUE",
        "key_file"
    };
    /// <summary>
    /// thread 로딩 핸들
    /// </summary>
    private static AsyncRoutineHandle mAsyncHandle;
    /// <summary>
    /// thread 로딩 실패 여부
    /// </summary>
    public static bool IsFailedLoadOnThread { get; private set; }

    /// <summary>
    /// thread로 메타 데이터 로딩하는 코루틴
    /// </summary>
    public static IEnumerator CoroutineLoadAllMetaOnThread()
    {
        mMetaConvertor.LoadKeyFile();

        // progress UI 활성화
        UIPatchProcess.ShowUI(UIPatchProcess.eType.SIMPLE);

        // 메타 데이터 로딩
        AsyncRoutineHandle asyncHandle = Async.Run(LoadAllMetaOnThread());
        mAsyncHandle = asyncHandle;

        while (asyncHandle.keepWaiting)
        {
            float rate = (float)mProgressCount / mTotalCount;
            UIPatchProcess.SetPatchValue(rate);
            yield return null;
        }

        mAsyncHandle = null;

        if (!IsFailedLoadOnThread)
        {
            mMetaConvertor.Flush();
            mMetaConvertor = null;
        }

        Resources.UnloadUnusedAssets();
        Async.Clear();

        GC.Collect();
    }

    public class MetaDataInfo
    {
        public string name;
        public string context;

        public int priorty = -1;                //-1일 경우 하지않음.

        public MetaDataInfo(string aName, string aContext)
        {
            name = aName;
            context = string.Copy(aContext);
        }
    }

    private static int mProgressCount = 0;
    private static int mTotalCount = 0;
    private static bool mIsRunningThreadLoading = false;     //로딩 진행중
    public static bool IsRunningThreadLoading { get { return mIsRunningThreadLoading; } }
    
    /// <summary>
    /// 스레드 로딩 종료
    /// </summary>
    public static void EndThread()
    {
        if(mIsRunningThreadLoading)
        {
            if (mAsyncHandle != null)
            {
                Async.StopRoutine(mAsyncHandle);
                Async.Clear();
            }
        }
    }

    /// <summary>
    /// thread로 메타 데이터 로드
    /// 파싱시 유니티 기능을 사용하는 테이블 들은 thread 전에 미리 로딩해야 함
    /// </summary>
    private static IEnumerator LoadAllMetaOnThread()
    {
        bool bSuccessLoad = false;
        IsFailedLoadOnThread = false;

        mProgressCount = 0;
        mTotalCount = 0;
        mIsRunningThreadLoading = true;

        ResetMetaData();

        // thread 로딩 전에 예외 및 미리 로딩해야 하는 메타데이터 로드
        if (BeforehandLoadingMetaBeforeThread() == true)
        {
            Async.ThrottleGameLoopExecutionTime = false;

            List<MetaDataInfo> metaDataList = new List<MetaDataInfo>();

            // bult_in 메타 데이터 로드
            TextAsset[] arrMetaDatas = Resources.LoadAll<TextAsset>(ResourcePath.BUILT_IN_META_DATA_TABLES);
            if (arrMetaDatas != null && arrMetaDatas.Length > 0)
            {
                for (int i = 0; i < arrMetaDatas.Length; i++)
                {
                    metaDataList.Add(new MetaDataInfo(arrMetaDatas[i].name, arrMetaDatas[i].text));
                    Resources.UnloadAsset(arrMetaDatas[i]);
                }
                // 번들 메타 데이터 로드
                if (GameSetting.USE_PATCH == true)
                {
                    arrMetaDatas = AssetBundleManager.GetAllAssets<TextAsset>(eAssetBundleName.meta_data + ".bundle");
                }
                else
                {
                    arrMetaDatas = Resources.LoadAll<TextAsset>(ResourcePath.BUNDLE_META_DATA_TABLES);
                }
                if (arrMetaDatas != null && arrMetaDatas.Length > 0)
                {
                    for (int i = 0; i < arrMetaDatas.Length; i++)
                    {
                        MetaDataInfo newMetaData = new MetaDataInfo(arrMetaDatas[i].name, arrMetaDatas[i].text);

                        MetaDataInfo oldMetaData = metaDataList.Find((f) => { return f.name == newMetaData.name; });
                        if (oldMetaData != null)
                        {
                            metaDataList.Remove(oldMetaData);
                        }
                        metaDataList.Add(newMetaData);
                        Resources.UnloadAsset(arrMetaDatas[i]);
                    }
                }

                arrMetaDatas = null;

                // thread 설정
                //int processCount = UIManager.Get<UIWinTip>(eUI_ID.WIN_TIP) == false ? WORKER_THREADS_COUNT : WORKER_THREADS_COUNT_ON_TIPUI;
                mProgressCount = 1;

                Type metaDataMngType = typeof(MetaDataManager);

                GetParsingOrderMetaDataList(metaDataList);

                //총 개수.
                mTotalCount = metaDataList.Count;

                //sort
                metaDataList.Sort((l1, l2) => { return l1.priorty.CompareTo(l2.priorty); });

                if (metaDataList.Count == 0)
                {
                    yield break;
                }

                bSuccessLoad = true;

                //우선순위 높은 것 부터 로딩.
                for (int i = 0; i < metaDataList.Count; ++i)
                {
                    if (bSuccessLoad == false)
                        break;

                    if (metaDataList[i].priorty == int.MaxValue) continue;

                    int isSuceess = 1;

                    yield return Async.ToAsync;
                    yield return Async.QueueUserWorkItem(metaDataList[i],
                        (metaData) =>
                        {
                            Interlocked.Increment(ref mProgressCount);

                            // 메타 데이터 파싱
                            MetaParseProcessOnThread(metaData, metaDataMngType, ref isSuceess);

                            Thread.Sleep(1);
                        });

                    yield return Async.ToGame;

                    bSuccessLoad = isSuceess == 1;
                }

                if (bSuccessLoad == true)
                {
                    //우선순위 없는 남은 것들 병행 로딩
                    List<MetaDataInfo> parallelList = new List<MetaDataInfo>();
                    for (int i = 0; i < metaDataList.Count; i++)
                    {
                        if (metaDataList[i].priorty == int.MaxValue)
                        {
                            parallelList.Add(metaDataList[i]);
                        }
                    }

                    int isSuceess = 1;

                    //타임아웃 체크.
                    CoroutineManager.StartCoroutineOnManager(CoroutineCheckThreadLoadTimeOut());

                    yield return Async.ToAsync;
                    yield return Async.ParallelForEach(parallelList,
                    (metaData) =>
                        {
                            Interlocked.Increment(ref mProgressCount);

                            // 메타 데이터 파싱
                            MetaParseProcessOnThread(metaData, metaDataMngType, ref isSuceess);
                            Thread.Sleep(1);
                        });

                    yield return Async.ToGame;

                    CoroutineManager.StopCoroutineOnManager(CoroutineCheckThreadLoadTimeOut());

                    if (metaDataList.Count > mProgressCount)
                    {
                        bSuccessLoad = false;
                    }
                    parallelList.Clear();
                }

                metaDataList.Clear();
            }
        }
        else
        {
            IsFailedLoadOnThread = true;
        }

        if (bSuccessLoad == true)
        {
            mIsLoaded = true;
        }

        mIsRunningThreadLoading = false;
    }  

    /// <summary>
    /// thread 로딩시 시간 초과 체크
    /// </summary>
    /// <returns></returns>
    private static IEnumerator CoroutineCheckThreadLoadTimeOut()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        // 시간 경과시 thread 로딩 종료
        while (sw.ElapsedMilliseconds < THREAD_TIME_OUT_MILLISECONDS)
            yield return null;

        mIsLoaded = false;
        IsFailedLoadOnThread = true;
        mIsRunningThreadLoading = false;
        if (mAsyncHandle != null)
            Async.StopRoutine(mAsyncHandle);
        CoroutineManager.StopCoroutineOnManager(CoroutineLoadAllMetaOnThread());
        HwLog.LogError(eLogType.ERROR, "Thread load time out");
    }

    /// <summary>
    /// thread에서 메타 데이터 파싱
    /// </summary>
    private static void MetaParseProcessOnThread(MetaDataInfo _metaData, Type _metaDataMngType, ref int _isSuccess)
    {
        if (_isSuccess == 1 && mMetaConvertor != null)
        {
            bool bParse = true;
            string fileName = _metaData.name;
            if (mArrExcludeFromLoadTableName != null)
            {
                // 메타 필터링
                for (int i = 0; i < mArrExcludeFromLoadTableName.Length; ++i)
                {
                    if (fileName.Contains(mArrExcludeFromLoadTableName[i]) == true)
                    {
                        bParse = false;
                        break;
                    }
                }
            }
            if (bParse == true)
            {
                try
                {
                    // 메타 데이터 파싱 함수 호출
                    if (ParseMetaData(_metaDataMngType, fileName, _metaData.context) == false)
                    {
                        Interlocked.Decrement(ref _isSuccess);
                        HwLog.LogError(eLogType.ERROR, "Failed meta parse - " + fileName);
                    }
                    _metaData.context = null;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }

    /// <summary>
    /// 메타 데이터 파싱 함수 호출
    /// </summary>
    private static bool InvokeMetaParse(Type _metaDataMngType, string _fileName, string _metaText = null)
    {
        bool bResult = false;
        // 파일명으로 메소드 정보 가져와 호출
        MethodInfo methodInfo = _metaDataMngType.GetMethod(_fileName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (methodInfo != null)
        {
            object[] parameter = null;
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length > 0)
            {
                if (parameterInfos.Length > 2)
                {
                    // 매개변수 갯수가 2개 이상인 경우 3번째 인자부터 기본값으로 설정
                    parameter = new object[parameterInfos.Length];
                    parameter[0] = _fileName;
                    parameter[1] = _metaText;
                    for (int i = 2; i < parameter.Length; ++i)
                        parameter[i] = parameterInfos[i].DefaultValue;
                }
                else
                    parameter = new object[] { _fileName, _metaText };
            }

            object result = methodInfo.Invoke(methodInfo, parameter);
            if (result != null)
                bResult = (bool)result;
            else
                bResult = true;
        }
        else
        {
#if UNITY_EDITOR
            HwLog.LogError(eLogType.ERROR, "Not exist meta parse function : " + _fileName);
#endif
            bResult = true;
        }

        return bResult;
    }

    /// <summary>
    /// thread 로딩 전에 예외 및 미리 로딩해야 하는 메타데이터 로드
    /// 파싱시 유니티 기능을 사용하는 테이블 들은 여기에서 미리 로딩
    /// </summary>
    private static bool BeforehandLoadingMetaBeforeThread()
    {
        bool bSuccess = true;
        Type metaDataMngType = typeof(MetaDataManager);
        string[] arrMeta = Enum.GetNames(typeof(eBeforehandLoadingMetaData));
        if (arrMeta != null)
        {
            for (int i = 0; i < arrMeta.Length; ++i)
            {
                bSuccess = ParseMetaData(metaDataMngType, arrMeta[i]);
                if (bSuccess == false)
                    break;
            }
        }

        return bSuccess;
    }

    //아래 두가지케이스 외에 리스트 구성.
    //필터링 제거,
    //우선 순위 높은 걸로 소팅. -우선 로딩할 메타
    //나머지 메타데이타.
    /// <summary>
    /// 메타 데이터 파싱 순서에 따른 메타 리스트 얻기
    /// </summary>
    private static void GetParsingOrderMetaDataList(List<MetaDataInfo> _metaList)
    {
        if (_metaList != null && _metaList.Count > 0)
        {
            List<string> filterList = new List<string>(Enum.GetNames(typeof(eBeforehandLoadingMetaData)));

            for (int i = 0; i < _metaList.Count;)
            {
                MetaDataInfo mInfo = _metaList[i];

                if (filterList.Contains(mInfo.name) == false)
                {
                    //추가.
                    if(Enum.IsDefined(typeof(eMetaDataParsingOrder), mInfo.name))
                    {
                        int key = (int)Utility.ParsingStringToEnumType<eMetaDataParsingOrder>(mInfo.name);
                        mInfo.priorty = key;
                    }
                    else
                    {
                        mInfo.priorty = int.MaxValue;
                    }

                    i++;
                }
                else
                {
                    _metaList.RemoveAt(i);
                }
            }
        
        }
    }
}