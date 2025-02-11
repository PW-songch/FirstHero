using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// ScrollView의 아이템을 재사용하는 컴포넌트
/// UIWrapContent 기능 확장
/// 
/// + Scroll View
/// |- UIRecycle
/// |-- Item 1
/// |-- Item 2
/// |-- Item 3
/// </summary>

[AddComponentMenu("NGUI/Interaction/Recycle")]
public class UIRecycle : MonoBehaviour
{
    public enum PositionType { FRONT, MIDDLE }

    /// <summary>
    /// UIRecycle 아이디
    /// </summary>
    [SerializeField] protected int recycleID = 0;

    /// <summary>
    /// 방향
    /// </summary>
    public UI.UIDirectType Direction
    {
        get { return direction; }
        set { direction = value; }
    }
    [UnityEngine.Serialization.FormerlySerializedAs("direct")]
    [SerializeField] protected UI.UIDirectType direction = UI.UIDirectType.DOWN;

    /// <summary>
    /// 화면에 표시될 수 있는 아이템의 수
    /// </summary>
    [SerializeField] protected int defaultCount = 0;

    /// <summary>
    /// 전체 아이템의 개수
    /// </summary>
    public virtual int MaxCount { set { maxCount = value; m_iMinIndex = 0; } get { return maxCount; } }
    [SerializeField] int maxCount = 0;

    /// <summary>
    /// 자식 아이템 프리팹
    /// </summary>
    [SerializeField] GameObject prefItem;

    /// <summary>
    /// 아이템의 Height or Width
    /// </summary>
    [SerializeField] int itemSize = 0;
    public int ItemSize { get { return itemSize; } }

    public GameObject PrefItem { get { return prefItem; } }

    protected Transform mTrans;
    protected UIPanel mPanel;
    protected UIScrollView mScrollView;
    public UIScrollView ScrollView
    {
        get
        {
            if (mScrollView == null)
                CacheScrollView();
            return mScrollView;
        }
    }

    bool mInit = false;
    protected bool mIsHorizontal = false;
    public bool IsHorizontal { get { return mIsHorizontal; } }

    List<UIRecycleItem> mItemList;
    protected ReadOnlyCollection<UIRecycleItem> mROCItemList;
    public ReadOnlyCollection<UIRecycleItem> ItemList { get { return mROCItemList; } }
    public UIPanel Panel
    {
        get
        {
            if (mPanel == null)
                CacheScrollView();
            return mPanel;
        }
    }

    public int ViewCount { get { return defaultCount; } }

    /// <summary>
    /// 최소 인덱스
    /// </summary>
    protected int m_iMinIndex;

    /// <summary>
    /// UI패널의 센터 위치
    /// </summary>
    private Vector3 m_currentPanelCenter;

    /// <summary>
    /// 스크롤 등의 이유로 UI가 갱신되면 호출되는 델리게이트
    /// </summary>
    public delegate void OnUpdateItem(UIRecycleItem item, int index, int _iRecycleID);
    protected OnUpdateItem mOnUpdateItem;

    private void Awake()
    {
        // grid 제거
        NGUITools.Destroy(GetComponent<UIGrid>());

        if (mInit == false)
        {
            mItemList = new List<UIRecycleItem>();
            mROCItemList = mItemList.AsReadOnly();
            mInit = true;
        }
    }

    /// <summary>
    /// 기본 생성
    /// </summary>
    public virtual void Create(OnUpdateItem updateItemCB)
    {
        if (mInit == false)
            Awake();

        // 기본 정보 캐싱
        CacheScrollView();

        NGUITools.SetActiveSelf(prefItem, true);

        // 아이템 사이즈 자동 계산
        CalculateItemSize();

        // 화면에 표시될 수 있는 아이템의 수 계산
        if (defaultCount <= 0)
            defaultCount = GetCalculateViewCount();

        mOnUpdateItem = null;
        DeleateAllItem(true);
        mScrollView.DisableMovement();

        // bounds용 더미 UIWidget 설정
        mScrollView.SetDummyUIWidgetForBounds(itemSize, maxCount, direction);

        // 최소한의 아이템 생성한 후 감추기
        for (int i = 0; i <= defaultCount; ++i)
        {
            UIRecycleItem item = NGUITools.AddChild(gameObject, prefItem).GetComponent<UIRecycleItem>();
            item.transform.localScale = prefItem.transform.localScale;
            item.SetActive(false);

            // 아이템 저장
            mItemList.Add(item);
        }

        NGUITools.SetActiveSelf(prefItem, false);

        // Callback 연결
        mOnUpdateItem = updateItemCB;
    }

    /// <summary>
    /// 아이템 사이즈 계산
    /// </summary>
    private void CalculateItemSize()
    {
        // 아이템 사이즈 자동 계산
        if (itemSize == 0)
        {
            // 다이얼로그의 경우 스케일링 하는 패널루트의 스케일을 원래 사이즈로 변경
            Vector3 oriScale = Vector3.one;
            Dialog dlg = GetComponentInParent<Dialog>();
            if (dlg != null)
            {
                oriScale = dlg.mPanelRoot.transform.localScale;
                dlg.mPanelRoot.transform.localScale = Vector3.one;
            }

            Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(prefItem.transform);
            itemSize = (int)(mIsHorizontal == true ? bounds.size.x : bounds.size.y);

            // 다이얼로그 패널루트의 원래 스케일로 복원
            if (dlg != null)
                dlg.mPanelRoot.transform.localScale = oriScale;
        }
    }

    public T[] GetItemList<T>()
    {
        return GetComponentsInChildren<T>(true);
    }

    /// <summary>
    /// ScrollView 관련 데이터 캐싱
    /// </summary>
    public void CacheScrollView()
    {
        mTrans = transform;
        if (mPanel == null)
            mPanel = NGUITools.FindInParents<UIPanel>(gameObject);

        if (mPanel != null)
            mPanel.onClipMove = OnMove;

        if (mScrollView == null)
            mScrollView = mPanel.GetComponent<UIScrollView>();
        mIsHorizontal = direction == UI.UIDirectType.LEFT || direction == UI.UIDirectType.RIGHT;
    }

    /// <summary>
    /// 패널영역 안에 보여질 아이템 갯수 계산
    /// </summary>
    protected virtual int GetCalculateViewCount()
    {
        int nCount = 0;
        if (mPanel != null)
        {
            Vector3[] corners = mPanel.localCorners;
            if (mIsHorizontal == false)
                nCount = Mathf.CeilToInt((corners[1].y - corners[0].y) / itemSize);
            else
                nCount = Mathf.CeilToInt((corners[3].x - corners[0].x) / itemSize);
        }

        return nCount;
    }

    /// <summary>
    /// Panel이 움직일때 이벤트
    /// </summary>
    protected virtual void OnMove(UIPanel panel)
    {
        if (mROCItemList != null)
        {
            int iChildCount = GetChildCount();
            WrapContent(iChildCount, itemSize * iChildCount);
        }
    }

    /// <summary>
    /// 아이템 위치 설정
    /// </summary>
    protected virtual void SetItemPosition(UIRecycleItem _item)
    {
        if (_item != null)
        {
            float pos = _item.Index * itemSize;
            switch (direction)
            {
                case UI.UIDirectType.DOWN: _item.transform.localPosition = new Vector3(0f, -pos, 0f); break;
                case UI.UIDirectType.UP: _item.transform.localPosition = new Vector3(0f, pos, 0f); break;
                case UI.UIDirectType.LEFT: _item.transform.localPosition = new Vector3(-pos, 0f, 0f); break;
                case UI.UIDirectType.RIGHT: _item.transform.localPosition = new Vector3(pos, 0f, 0f); break;
            }
        }
    }

    /// <summary>
    /// bounds용 더미 UIWidget 위치 설정
    /// </summary>
    protected virtual void SetDummyUIWidgetPosition()
    {
        mScrollView.SetDummyUIWidgetPosition(itemSize, maxCount, direction);
    }

    /// <summary>
    /// 초기 환경으로 만들기
    /// </summary>
    public void ResetPosition(bool _bUpdateItem = true)
    {
        m_iMinIndex = 0;

        // 자식의 순서대로 배치
        for (int i = 0, imax = mROCItemList.Count; i < imax; ++i)
        {
            UIRecycleItem item = mROCItemList[i];
            item.Index = i;

            // 필요없는 UI 숨기기
            bool active = i < maxCount;
            item.SetActive(active);
            
            // 이벤트 호출
            if (_bUpdateItem && active && mOnUpdateItem != null)
                mOnUpdateItem(item, i, recycleID);

            // 위치 설정
            SetItemPosition(item);
        }

        // bounds용 더미 UIWidget 위치 설정
        SetDummyUIWidgetPosition();
        mScrollView.ResetPosition();
        mScrollView.currentMomentum = Vector3.zero;
    }

    /// <summary>
    /// 현재 보여지는 UI 그대로 갱신
    /// </summary>
    public virtual void UpdateUI(bool restrictWithinBounds, bool instant = false)
    {
        int startIndex = 0;
        if (defaultCount < maxCount)
        {
            // 시작 인덱스 계산
            startIndex = int.MaxValue;
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                if (item.Index < startIndex)
                    startIndex = item.Index;
            }
        }

        if (mROCItemList.Count > 0)
            m_iMinIndex = int.MaxValue;

        int realIndex = 0;

        for (int i = 0; i < mROCItemList.Count; ++i)
        {
            UIRecycleItem item = mROCItemList[i];

            // 필요없는 UI 숨기기
            bool active = i < maxCount;
            item.SetActive(active);
            if (active)
            {
                realIndex = GetRealIndex(item.transform.localPosition, true);

                if (realIndex >= maxCount)
                {
                    // 인덱스가 총 갯수보다 크면 위로 올림
                    realIndex = startIndex - (realIndex - maxCount + 1);
                    active = realIndex >= 0;
                }

                item.Index = realIndex;
                if (active)
                {
                    // 최소 인덱스 설정
                    if (realIndex < m_iMinIndex)
                        m_iMinIndex = realIndex;

                    mOnUpdateItem(item, realIndex, recycleID);
                    // 위치 설정
                    SetItemPosition(item);
                }
                else
                    item.SetActive(active);
            }
        }

        // bounds용 더미 UIWidget 설정
        SetDummyUIWidgetPosition();

        if (restrictWithinBounds == true)
        {
            mScrollView.InvalidateBounds();
            mScrollView.RestrictWithinBounds(instant, mIsHorizontal, !mIsHorizontal);
        }
    }

    /// <summary>
    /// 아이템 모두 제거
    /// </summary>
    public virtual void DeleateAllItem(bool _bResetPos)
    {
        MaxCount = 0;
        if (mItemList != null)
            mItemList.Clear();
        Utility.DestroyImmediateChildren(mTrans);
        if (_bResetPos == true)
            ResetPosition();
        else
            SetDummyUIWidgetPosition();
    }

    /// <summary>
    /// 자식 제거 - 실제 UI삭제는 아니고 1칸씩 올린다
    /// </summary>
    public void DeleteItem(UIRecycleItem deleteItem, bool isDecreaseMaxCount = true)
    {
        // 자식 유효성 확인
        if (deleteItem == null || mROCItemList.Contains(deleteItem) == false)
            return;

        // 아이템 카운트 자동 감소
        if (isDecreaseMaxCount)
            MaxCount--;

        // 삭제될 자식의 realIndex 체크
        int deleteIndex = GetRealIndex(deleteItem.transform.localPosition, true);

        // 스크롤을 리셋하는 경우
        if (maxCount <= defaultCount || deleteIndex <= defaultCount)
            ResetPosition();
        else
        {
            if (mROCItemList.Count > 0)
                m_iMinIndex = int.MaxValue;

            // 자식들의 위치를 1칸씩 올린다
            for (int i = 0, imax = mROCItemList.Count; i < imax; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                --item.Index;
                // 최소 인덱스 설정
                if (item.Index < m_iMinIndex)
                    m_iMinIndex = item.Index;

                // 이벤트 호출
                if (mOnUpdateItem != null)
                    mOnUpdateItem(item, item.Index, recycleID);

                // 위치 설정
                SetItemPosition(item);
            }

            // bounds용 더미 UIWidget 위치 설정
            SetDummyUIWidgetPosition();
            // 스크롤 업데이트
            mScrollView.UpdatePosition();
        }
    }

    /// <summary>
    /// 특정 인덱스 위치로 이동
    /// </summary>
    public void SetPosition(int realIndex, PositionType posType = PositionType.MIDDLE, bool instant = true)
    {
        // 화면에 모든 아이템이 온전히 보여지는 경우는 스크롤 X
        if (maxCount <= defaultCount)
        {
            ResetPosition();
            return;
        }

        switch (posType)
        {
            case PositionType.MIDDLE:
                // 보여지는 절반 갯수 만큼 앞 위치 인덱스로
                int viewCount = GetCalculateViewCount() / 2;
                if (realIndex < maxCount - viewCount)
                    realIndex = Mathf.Max(0, realIndex - viewCount);
                break;
        }

        realIndex = Mathf.Max(0, realIndex);

        // 시작 지점
        int startIndex = realIndex;
        // 현재 아이템 포함해서 그 뒤로의 아이템 개수
        int remainCount = maxCount - realIndex;
        // 공간이 남은경우 앞의 내용도 포함해서 보여주기
        if (remainCount < defaultCount)
        {
            // 표시 위치 변경
            startIndex = Mathf.Max(0, startIndex - (defaultCount - remainCount));
        }

        if (mROCItemList.Count > 0)
            m_iMinIndex = int.MaxValue;

        // 자식의 순서대로 배치
        int moveIndex = 0;
        for (int i = 0, imax = mROCItemList.Count; i < imax; ++i)
        {
            UIRecycleItem item = mROCItemList[i];
            int real = i + startIndex;

            // 최대 보다 넘어갔으면 위로 올린다.
            if (real >= maxCount)
                real = startIndex - 1;
            if (real == realIndex)
                moveIndex = i;

            item.Index = real;
            // 최소 인덱스 설정
            if (real < m_iMinIndex)
                m_iMinIndex = real;

            // 필요없는 UI 숨기기
            bool active = i < maxCount;
            item.SetActive(active);

            // 이벤트 호출
            if (active && mOnUpdateItem != null)
                mOnUpdateItem(item, real, recycleID);

            // 위치 설정
            SetItemPosition(item);
        }

        // scrollView panel 이동
        if (mROCItemList.Count >= moveIndex)
        {
            Vector3 vPos = mROCItemList[moveIndex].transform.localPosition;
            if (mIsHorizontal == true) vPos.y = 0f;
            else vPos.x = 0f;
            MovePosition(-vPos, instant);
        }

        // 가장 아래 있는경우 스크롤을 밑으로 내린다.
        if (realIndex == maxCount - 1)
        {
            if (mScrollView.horizontalScrollBar != null)
            {
                mScrollView.horizontalScrollBar.value = 1f;
            }
            else if (mScrollView.verticalScrollBar != null)
            {
                mScrollView.verticalScrollBar.value = 1f;
            }
        }

        // bounds용 더미 UIWidget 위치 설정
        SetDummyUIWidgetPosition();
        // 스크롤 업데이트
        mScrollView.UpdatePosition();
    }

    /// <summary>
    /// 해당 위치로 이동
    /// </summary>
    public void MovePosition(Vector3 _vPos, bool _bInstant = false)
    {
        if (mScrollView != null)
        {
            mScrollView.DisableMovement();
            // 이동
            mScrollView.MoveRelative(_vPos);
            // scrollView panel 영역 벗어나는지 체크
            mScrollView.InvalidateBounds();
            mScrollView.RestrictWithinBounds(_bInstant, mIsHorizontal, !mIsHorizontal);
        }
    }

    /// <summary>
    /// UI 스크롤 이벤트 처리
    /// </summary>
    protected void WrapContent(int _iChildCount, float _extents)
    {
        if (mROCItemList == null || _iChildCount == 0)
            return;

        Vector3[] corners = mPanel.worldCorners;
        Vector3 v = corners[0];
        v = mTrans.InverseTransformPoint(v);
        corners[0] = v;
        v = corners[2];
        v = mTrans.InverseTransformPoint(v);
        corners[2] = v;

        Vector3 center = Vector3.Lerp(corners[0], corners[2], 0.5f);
        bool allWithinRange = true;
        bool check = false;

        _extents *= 0.5f;
        float ext2 = _extents * 2f;

        // 최소 인덱스 설정
        if (m_iMinIndex == int.MaxValue)
            m_iMinIndex = 0;
        int minIndex = Mathf.Max(0, m_iMinIndex - 2);
        if (mROCItemList.Count > 0)
            m_iMinIndex = int.MaxValue;

        // 가로형
        if (mIsHorizontal)
        {
            // 오른쪽으로 스크롤 여부
            bool bScrollRight = m_currentPanelCenter != Vector3.zero ? center.x - m_currentPanelCenter.x < 0f : true;
            // 왼쪽으로 스크롤 여부
            bool bScrollLeft = m_currentPanelCenter != Vector3.zero ? center.x - m_currentPanelCenter.x > 0f : true;
            for (int i = 0; i < _iChildCount; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                // 최소 인덱스 설정
                if (item.Index < m_iMinIndex)
                    m_iMinIndex = item.Index;
                if (NGUITools.GetActive(item.gameObject) == false)
                    continue;

                Transform t = item.transform;
                float distance = t.localPosition.x - center.x;
                Vector3 pos = t.localPosition;

                if (bScrollRight == true && distance > _extents)
                {
                    // 아이템 왼쪽으로 이동
                    pos.x -= ext2;
                    allWithinRange = UpdateItem(item, pos, minIndex, direction == UI.UIDirectType.RIGHT ? false : true);
                    if (allWithinRange == true)
                        check = true;
                }
                else if (bScrollLeft == true && distance < -_extents)
                {
                    // 아이템 오른쪽으로 이동
                    pos.x += ext2;
                    allWithinRange = UpdateItem(item, pos, minIndex, direction == UI.UIDirectType.RIGHT ? true : false);
                    if (allWithinRange == true)
                        check = true;
                }
            }
        }
        // 세로형
        else
        {
            // 아래로 스크롤 여부
            bool bScrollDown = m_currentPanelCenter != Vector3.zero ? center.y - m_currentPanelCenter.y > 0f : true;
            // 위로 스크롤 여부
            bool bScrollUp = m_currentPanelCenter != Vector3.zero ? center.y - m_currentPanelCenter.y < 0f : true;
            for (int i = 0; i < _iChildCount; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                // 최소 인덱스 설정
                if (item.Index < m_iMinIndex)
                    m_iMinIndex = item.Index;
                if (NGUITools.GetActive(item.gameObject) == false)
                    continue;

                Transform t = item.transform;
                float distance = t.localPosition.y - center.y;
                Vector3 pos = t.localPosition;

                if (bScrollDown == true && distance < -_extents)
                {
                    // 아이템 위로 이동
                    pos.y += ext2;
                    allWithinRange = UpdateItem(item, pos, minIndex, direction == UI.UIDirectType.DOWN ? false : true);
                    if (allWithinRange == true)
                        check = true;
                }
                else if (bScrollUp == true && distance > _extents)
                {
                    // 아이템 아래로 이동
                    pos.y -= ext2;
                    allWithinRange = UpdateItem(item, pos, minIndex, direction == UI.UIDirectType.DOWN ? true : false);
                    if (allWithinRange == true)
                        check = true;
                }
            }
        }

        m_currentPanelCenter = center;

        if (check == false && allWithinRange == true)
        {
            mScrollView.InvalidateBounds();
            allWithinRange = !mScrollView.IsWithinBounds(mIsHorizontal, !mIsHorizontal);
        }

        mScrollView.restrictWithinPanel = !allWithinRange;
    }

    /// <summary>
    /// ScrollView의 영역에서 넘어갔는지 체크
    /// </summary>
    protected bool IsUpdateItem(int realIndex)
    {
        bool check = false;
        if (direction == UI.UIDirectType.DOWN || direction == UI.UIDirectType.LEFT)
        {
            check = (-maxCount < realIndex && realIndex <= 0);
        }
        else if (direction == UI.UIDirectType.UP || direction == UI.UIDirectType.RIGHT)
        {
            check = (0 <= realIndex && realIndex < maxCount);
        }

        return check;
    }

    /// <summary>
    /// ScrollView의 영역에서 넘어갔는지 체크 하여 이벤트 발생
    /// </summary>
    protected bool UpdateItem(UIRecycleItem item, Vector3 pos, int _iMinIndex, bool isForward)
    {
        int realIndex = GetRealIndex(pos, false, _iMinIndex);
        bool bpdate = IsUpdateItem(realIndex);
        if (bpdate == true)
        {
            realIndex = Mathf.Abs(realIndex);
            if (item.Index != realIndex)
            {
                int validIndex = GetValidIndex(realIndex, isForward);
                item.Index = validIndex;
                if (0 <= validIndex && validIndex < MaxCount)
                {
                    if (mOnUpdateItem != null)
                        mOnUpdateItem(item, validIndex, recycleID);
                    SetItemPosition(item);
                }
                else
                    NGUITools.SetActiveSelf(item, false);
            }
        }

        return bpdate;
    }

    /// <summary>
    /// 유효한 아이템 인덱스 구하기
    /// </summary>
    protected virtual int GetValidIndex(int index, bool _bForward = true)
    {
        return index;
    }

    /// <summary>
    /// 전체 데이터에서의 아이템 인덱스 구하기
    /// </summary>
    public virtual int GetRealIndex(Vector3 pos, bool abs = false, int findStartIndex = 0, int findCount = int.MaxValue)
    {
        float value = mIsHorizontal ? pos.x : pos.y;
        int realIndex = Mathf.RoundToInt(value / itemSize);
        return abs ? Mathf.Abs(realIndex) : realIndex;
    }

    /// <summary>
    /// 가운데 아이템의 인덱스 가져오기
    /// </summary>
    public int GetMiddleIndex(List<UIRecycleItem> _itemList = null)
    {
        int index = 0;
        if (_itemList == null)
            _itemList = new List<UIRecycleItem>(mItemList);
        if (_itemList != null && _itemList.Count > 0)
        {
            _itemList.Sort(delegate (UIRecycleItem item1, UIRecycleItem item2)
            {
                return item1.Index.CompareTo(item2.Index);
            });
            index = _itemList[_itemList.Count / 2].Index;
        }

        return index;
    }

    /// <summary>
    /// 보여지는 아이템중 가운데 아이템의 인덱스 가져오기
    /// </summary>
    public int GetVisibleMiddleIndex(List<UIRecycleItem> _itemList = null)
    {
        int index = 0;
        if (_itemList == null)
            _itemList = mItemList;
        if (maxCount > ViewCount && Panel != null && _itemList != null && _itemList.Count > 0)
        {
            List<UIRecycleItem> viewList = new List<UIRecycleItem>();
            _itemList.ForEach(
                (item) =>
                {
                    if (mPanel.IsVisible(item.transform.position) == true)
                        viewList.Add(item);
                });
            index = GetMiddleIndex(viewList);
        }

        return index;
    }

    /// <summary>
    /// 인덱스의 아이템 얻기
    /// </summary>
    public UIRecycleItem GetItem(int _index)
    {
        if (mROCItemList != null && 0 <= _index && _index < maxCount)
        {
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                if (_index == item.Index)
                    return item;
            }
        }

        return null;
    }

    /// <summary>
    /// 스크롤이 시작 위치에 있는지
    /// </summary>
    public bool IsScrollStartPosition()
    {
        if (Panel != null)
        {
            UIRecycleItem item = mItemList.Find((f) => { return f.Index == 0; });
            return item != null && mPanel.IsVisible(item.transform.position);
        }

        return false;
    }

    protected int GetChildCount()
    {
        int iCount = 0;
        if (mROCItemList != null)
        {
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                if (NGUITools.GetActive(mROCItemList[i].gameObject) == true)
                    iCount++;
            }
        }

        return iCount;
    }

    //아이템 항목들 모두에게 sendMessage 보내기.
    public void SendMessageItem(string message, object obj)
    {
        if (mROCItemList != null)
        {
            for (int i = 0; i < mROCItemList.Count; i++)
            {
                mROCItemList[i].SendMessage(message, obj, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
