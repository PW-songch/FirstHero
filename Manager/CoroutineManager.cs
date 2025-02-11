using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 코루틴 컨트롤러
/// </summary>
public class CoroutineController
{
    // 코루틴 호출 오브젝트
    private GameObject m_obj;
    // 코루틴
    public Coroutine Coroutine;
    // 코루틴 취소
    public bool IsStopped = false;
    // 오브젝트 비활성 상태에서 업데이트 여부
    public bool IsUpdateOnInactive = false;
    // 코루틴 종료 후 콜백
    private Action<bool/*정상 종료 여부*/> m_finishCallBack;
    public Action<bool> FinishCallBack { set { m_finishCallBack = value; } }

    public CoroutineController(GameObject _obj)
    {
        m_obj = _obj;
    }

    /// <summary>
    /// 코루틴 내부 로직
    /// </summary>
    public IEnumerator CoroutineInternalRoutine(IEnumerator _coroutine)
    {
        while (true)
        {
            // 오브젝트 null이면 종료
            if (m_obj == null)
            {
                IsStopped = true;
                break;
            }
            else if (IsUpdateOnInactive == false)
            {
                // 오브젝트 활성 상태에서만 업데이트 가능시 대기
                while (IsUpdateOnInactive == false && m_obj != null && m_obj.activeInHierarchy == false && IsStopped == false)
                    yield return null;

                if (m_obj == null)
                {
                    IsStopped = true;
                    break;
                }
            }

            // 코루틴 취소거나 다음 동작 없을 경우 종료
            if (IsStopped == true || _coroutine.MoveNext() == false)
                break;

            // 코루틴 동작 실행
            yield return _coroutine.Current;
        }

        // 코루틴 종료
        CoroutineManager.StopCoroutine(m_obj, _coroutine, this);
    }

    /// <summary>
    /// 코루틴 종료 시킴
    /// </summary>
    public void FinishCoroutine(bool _bSuccess)
    {
        if (m_finishCallBack != null)
            m_finishCallBack(_bSuccess);
    }

    /// <summary>
    /// IEnumerator로 부터 함수명 얻음
    /// </summary>
    static public string GetCoroutineMethodName(IEnumerator _coroutine)
    {
        string strCoroutineMethod = string.Empty;
        if (_coroutine != null)
        {
            int nStart = strCoroutineMethod.IndexOf('<') + 1;
            if (nStart >= 0)
            {
                int nEnd = strCoroutineMethod.IndexOf('>', nStart);
                if (nStart < nEnd)
                    strCoroutineMethod = strCoroutineMethod.Substring(nStart, nEnd - nStart);
            }

            if (string.IsNullOrEmpty(strCoroutineMethod) == true)
                strCoroutineMethod = _coroutine.ToString();
        }

        return strCoroutineMethod;
    }
}

/// <summary>
/// 코루틴 매니져
/// 코루틴 호출 MonoBehaviour와 코루틴 함수명 및 매개변수로 코루틴 실행 및 종료
/// 같은 MonoBehaviour 내 중복되는 코루틴 함수명이 없어야함
/// </summary>
public class CoroutineManager : Singleton<CoroutineManager>
{
    static private bool IsValid { get { return instance != null; } }

    private bool m_bInit;
    private Dictionary<GameObject, Dictionary<string, List<CoroutineController>>> m_dicCoroutine;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        StopAllCoroutinesOnManager();
    }

    private void Initialize()
    {
        if (m_bInit == false)
        {
            m_bInit = true;

            if (m_dicCoroutine == null)
                m_dicCoroutine = new Dictionary<GameObject, Dictionary<string, List<CoroutineController>>>();

            transform.parent = DontDestroyObject.Instance.transform;
        }
    }

    static public CoroutineController StartCoroutineOnManager(IEnumerator _coroutine, bool _bStopExisting = false)
    {
        return StartCoroutine(null, _coroutine, _bStopExisting);
    }

    static public CoroutineController StartCoroutineOnManager(
        IEnumerator _coroutine, bool _bStopExisting, Action<bool> _endCallBack = null)
    {
        return StartCoroutine(null, _coroutine, _bStopExisting, _endCallBack);
    }

    static public CoroutineController StartCoroutine(GameObject _obj,
        IEnumerator _coroutine, bool _bStopExisting, Action<bool> _finishCallBack)
    {
        CoroutineController coroutineController = StartCoroutine(_obj, _coroutine, _bStopExisting);
        coroutineController.FinishCallBack = _finishCallBack;
        return coroutineController;
    }

    static public CoroutineController StartCoroutine(GameObject _obj, IEnumerator _coroutine, bool _bStopExisting = false)
    {
        Instance.Initialize();

        CoroutineController coroutine = null;
        if (_coroutine != null)
        {
            string strCoroutineMethod = CoroutineController.GetCoroutineMethodName(_coroutine);
            if (string.IsNullOrEmpty(strCoroutineMethod) == false)
            {
                if (_obj == null)
                    _obj = Instance.gameObject;

                try
                {
                    if (_bStopExisting == true)
                        StopCoroutine(_obj, _coroutine);

                    coroutine = new CoroutineController(_obj);
                    coroutine.Coroutine = Instance.StartCoroutine(coroutine.CoroutineInternalRoutine(_coroutine));

                    Dictionary<string, List<CoroutineController>> dicCoroutine = null;
                    bool bContains = Instance.m_dicCoroutine.TryGetValue(_obj, out dicCoroutine);
                    if (bContains == false)
                        dicCoroutine = new Dictionary<string, List<CoroutineController>>();

                    List<CoroutineController> coroutineList = null;
                    if (dicCoroutine.TryGetValue(strCoroutineMethod, out coroutineList) == true)
                        coroutineList.Add(coroutine);
                    else
                    {
                        coroutineList = new List<CoroutineController>();
                        coroutineList.Add(coroutine);
                        dicCoroutine.Add(strCoroutineMethod, coroutineList);
                    }

                    if (bContains == false)
                        Instance.m_dicCoroutine.Add(_obj, dicCoroutine);
                }
                catch (TargetException e)
                {
                    HwLog.LogError(eLogType.ERROR, string.Format(
                        "Target Method : {0} - {1}", strCoroutineMethod, e.Message));
                }
                catch (ArgumentException)
                {
                    HwLog.LogError(eLogType.ERROR, "Target Method : " +
                        strCoroutineMethod + " has wrong parameter matches.");
                }
                catch (TargetParameterCountException)
                {
                    HwLog.LogError(eLogType.ERROR, "Target Method : " +
                        strCoroutineMethod + " has different number of parameters.");
                }
            }
        }

        return coroutine;
    }

    static public void StopCoroutineOnManager(IEnumerator _coroutine, CoroutineController _controller = null)
    {
        StopCoroutine(null, _coroutine, _controller);
    }

    static public void StopCoroutine(GameObject _obj, IEnumerator _coroutine, CoroutineController _controller = null)
    {
        if (IsValid == true && _coroutine != null && Instance.m_dicCoroutine != null)
        {
            string strCoroutineMethod = CoroutineController.GetCoroutineMethodName(_coroutine);
            if (string.IsNullOrEmpty(strCoroutineMethod) == false)
            {
                if (_obj == null)
                    _obj = Instance.gameObject;

                List<CoroutineController> coroutineList = null;
                Dictionary<string, List<CoroutineController>> dicCoroutine = null;
                if (Instance.m_dicCoroutine.TryGetValue(_obj, out dicCoroutine) == true &&
                    dicCoroutine.TryGetValue(strCoroutineMethod, out coroutineList) == true)
                {
                    if (_controller == null)
                    {
                        foreach (CoroutineController coroutine in coroutineList)
                        {
                            if (coroutine != null)
                            {
                                coroutine.IsStopped = true;
                                Instance.StopCoroutineInternalRoutine(coroutine);
                            }
                        }

                        coroutineList.Clear();
                    }
                    else
                    {
                        coroutineList.Remove(_controller);
                        Instance.StopCoroutineInternalRoutine(_controller);
                    }

                    if (coroutineList.Count == 0)
                        dicCoroutine.Remove(strCoroutineMethod);
                    if (dicCoroutine.Count == 0)
                        Instance.m_dicCoroutine.Remove(_obj);
                }
            }
            else
            {
                List<GameObject> removeKeyList = new List<GameObject>();
                foreach (KeyValuePair<GameObject, Dictionary<string,
                    List<CoroutineController>>> coroutineValue in Instance.m_dicCoroutine)
                {
                    if (coroutineValue.Key == null)
                    {
                        removeKeyList.Add(coroutineValue.Key);

                        foreach (List<CoroutineController> coroutineList in coroutineValue.Value.Values)
                        {
                            foreach (CoroutineController coroutine in coroutineList)
                            {
                                if (coroutine != null)
                                {
                                    coroutine.IsStopped = true;
                                    Instance.StopCoroutineInternalRoutine(coroutine);
                                }
                            }

                            coroutineList.Clear();
                        }

                        coroutineValue.Value.Clear();
                    }
                }

                foreach (GameObject key in removeKeyList)
                    Instance.m_dicCoroutine.Remove(key);

                removeKeyList.Clear();
            }
        }
    }

    static public bool StopCoroutine(GameObject _obj, bool _bRemove = true)
    {
        if (IsValid == true && Instance.m_dicCoroutine != null)
        {
            if (_obj == null)
                _obj = Instance.gameObject;

            Dictionary<string, List<CoroutineController>> dicCoroutine = null;
            if (Instance.m_dicCoroutine.TryGetValue(_obj, out dicCoroutine) == true)
            {
                foreach (List<CoroutineController> coroutineList in dicCoroutine.Values)
                {
                    foreach (CoroutineController coroutine in coroutineList)
                    {
                        if (coroutine != null)
                        {
                            coroutine.IsStopped = true;
                            Instance.StopCoroutineInternalRoutine(coroutine);
                        }
                    }

                    coroutineList.Clear();
                }

                dicCoroutine.Clear();
                if (_bRemove == true)
                    Instance.m_dicCoroutine.Remove(_obj);
                return true;
            }
        }

        return false;
    }

    static public void StopAllCoroutinesOnManager()
    {
        if (IsValid == true && Instance.m_dicCoroutine != null)
        {
            foreach (KeyValuePair<GameObject, Dictionary<string,
                List<CoroutineController>>> coroutineValue in Instance.m_dicCoroutine)
            {
                if (StopCoroutine(coroutineValue.Key, false) == false)
                    coroutineValue.Value.Clear();
            }

            Instance.m_dicCoroutine.Clear();
        }
    }

    private void StopCoroutineInternalRoutine(CoroutineController _coroutine)
    {
        if (_coroutine != null && _coroutine.Coroutine != null)
        {
            StopCoroutine(_coroutine.Coroutine);
            _coroutine.FinishCoroutine(_coroutine.IsStopped == false);
        }
    }

	static public bool IsRunningCoroutine(GameObject _obj, IEnumerator _coroutine)
	{
		return IsRunningCoroutine(_obj, CoroutineController.GetCoroutineMethodName(_coroutine));
	}

    static public bool IsRunningCoroutine(GameObject _obj, string _coroutineMethod)
    {
        if (IsValid == true && Instance.m_dicCoroutine != null)
        {
            if (_obj == null)
                _obj = Instance.gameObject;

            Dictionary<string, List<CoroutineController>> dicCoroutine = null;
            if (Instance.m_dicCoroutine.TryGetValue(_obj, out dicCoroutine) == true &&
                dicCoroutine.ContainsKey(_coroutineMethod) == true)
                return true;
        }

        return false;
    }
}
