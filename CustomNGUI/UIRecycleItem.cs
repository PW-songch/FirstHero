using UnityEngine;

/// <summary>
/// UIRecycle의 하위 아이템으로 사용되는 스크립트들은 UIRecycleItem를 상속 받아 사용.
/// </summary>
public abstract class UIRecycleItem : MonoBehaviour
{
    #if UNITY_EDITOR
    [Tooltip("아이템 인덱스 (에디터 확인용)")]
    // 에디터 표시용
    public int ItemIndex;
    #endif

    private int m_index = 0;
    public int Index
    {
        get { return m_index; }
        set
        {
            m_index = Mathf.Max(0, value);
            #if UNITY_EDITOR
            ItemIndex = m_index;
            #endif
        }
    }

    /// <summary>
    /// 설정한 아이템 사이즈
    /// </summary>
    [SerializeField] protected Vector2 m_settingItemSize;
    public Vector2 SettingItemSize { get { return m_settingItemSize; } }

    /// <summary>
    /// 아이템 사이즈 계산시 비활성 오브젝트 포함 여부
    /// </summary>
    [SerializeField] protected bool m_isConsiderInactiveSize;

    /// <summary>
    /// 아이템의 사이즈
    /// </summary>
    protected Vector2 m_itemSize;
    public virtual Vector2 ItemSize
    {
        get
        {
            if (m_itemSize == Vector2.zero)
                CalculateItemSize();
            return m_itemSize;
        }
    }

    private UIButtonScale[] m_arrayUIBtnScale;
    private UIButtonOffset[] m_arrayUIBtnOffset;
    private UIButtonColor[] m_arrayUIBtnColor;

    private bool m_bInit;

    protected virtual void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 활성/비활성
    /// </summary>
    /// <param name="_bActive"></param>
    public virtual void SetActive(bool _bActive)
    {
        NGUITools.SetActiveSelf(gameObject, _bActive);
        if (_bActive == false)
            Index = 0;
    }

    /// <summary>
    /// 활성화 여부
    /// </summary>
    public virtual bool GetActive()
    {
        return gameObject.activeSelf;
    }

    /// <summary>
    /// UI 정보 셋팅
    /// </summary>
    /// <typeparam name="T"></typeparam>    메인 데이터 타입
    /// <param name="_data"></param>        메인 데이터 값
    /// <param name="_dataList"></param>    그 외 데이터들 (Action 콜백 전달 가능 ex)(Action)this.callBack))
    public virtual void SetUI<T>(T _data, params object[] _dataList)
    {
        SetActive(_data != null);

        if (_data != null)
        {
            if (m_bInit == false)
                Initialize();

            // 버튼 스케일 원상태로
            for (int i = 0; i < m_arrayUIBtnScale.Length; ++i)
            {
                UIButtonScale btnScale = m_arrayUIBtnScale[i];
                if (btnScale.tweenTarget != null)
                    btnScale.SetNormal(true);
            }

            // 버튼 오프셋 원상태로
            for (int i = 0; i < m_arrayUIBtnOffset.Length; ++i)
            {
                UIButtonOffset btnOffset = m_arrayUIBtnOffset[i];
                if (btnOffset.tweenTarget != null)
                    btnOffset.SetNormal(true);
            }

            // 버튼 컬러 원상태로
            for (int i = 0; i < m_arrayUIBtnColor.Length; ++i)
            {
                UIButtonColor btnColor = m_arrayUIBtnColor[i];
                if (btnColor.tweenTarget != null)
                    btnColor.SetState(btnColor.isEnabled == true ? UIButtonColor.State.Normal : UIButtonColor.State.Disabled, true);
            }
        }
    }

    /// <summary>
    /// 초기화
    /// </summary>
    protected virtual void Initialize()
    {
        if (m_bInit == false)
        {
            m_arrayUIBtnScale = GetComponentsInChildren<UIButtonScale>();
            m_arrayUIBtnOffset = GetComponentsInChildren<UIButtonOffset>();
            m_arrayUIBtnColor = GetComponentsInChildren<UIButtonColor>();
        }

        // 아이템 사이즈 설정
        CalculateItemSize();

        m_bInit = true;
    }

    /// <summary>
    /// 재설정
    /// </summary>
    public virtual void Refresh() {}

    /// <summary>
    /// 아이템 크기 계산
    /// </summary>
    public virtual void CalculateItemSize()
    {
        if (m_settingItemSize == Vector2.zero)
            m_settingItemSize = NGUIMath.CalculateRelativeWidgetBounds(transform, m_isConsiderInactiveSize).size;
        m_itemSize = m_settingItemSize;
    }
}
