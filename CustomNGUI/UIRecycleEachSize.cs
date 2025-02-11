using UnityEngine;
using System.Collections.Generic;

public class UIRecycleEachSize : UIRecycle
{
    /// <summary>
    /// 아이템 위치/크기 정보
    /// </summary>
    public class ItemTransformInfo
    {
        private Vector3 m_position;
        private Vector2 m_scale;
        public Vector3 Position { get { return m_position; } }
        public Vector2 Scale { get { return m_scale; } }

        public ItemTransformInfo()
        {
            Reset();
        }

        public void Reset()
        {
            m_position = Vector3.zero;
            m_scale = Vector2.zero;
        }

        public void SetPositionX(float _x, bool _bRound = true)
        {
            m_position.x = _bRound ? Mathf.RoundToInt(_x) : _x;
        }

        public void SetPositionY(float _y, bool _bRound = true)
        {
            m_position.y = _bRound ? Mathf.RoundToInt(_y) : _y;
        }

        public void SetPositionZ(float _z, bool _bRound = true)
        {
            m_position.z = _bRound ? Mathf.RoundToInt(_z) : _z;
        }

        public void SetPosition(Vector3 _pos, bool _bRound = true)
        {
            SetPositionX(_pos.x, _bRound);
            SetPositionY(_pos.y, _bRound);
            SetPositionZ(_pos.z, _bRound);
        }

        public bool SetPosition(ItemTransformInfo _prevInfo, UI.UIDirectType _direction)
        {
            // 이전 아이템의 정보로 위치 설정
            if (_prevInfo != null && _prevInfo.Scale != Vector2.zero)
            {
                switch (_direction)
                {
                    case UI.UIDirectType.DOWN:
                        SetPositionY(_prevInfo.Position.y - _prevInfo.Scale.y);
                        break;
                    case UI.UIDirectType.UP:
                        SetPositionY(_prevInfo.Position.y + _prevInfo.Scale.y);
                        break;
                    case UI.UIDirectType.LEFT:
                        SetPositionX(_prevInfo.Position.x - _prevInfo.Scale.x);
                        break;
                    case UI.UIDirectType.RIGHT:
                        SetPositionX(_prevInfo.Position.x + _prevInfo.Scale.x);
                        break;
                }

                return true;
            }

            return false;
        }

        public void SetScale(Vector2 _scale)
        {
            m_scale.x = Mathf.RoundToInt(_scale.x);
            m_scale.y = Mathf.RoundToInt(_scale.y);
        }

        public bool IsValid()
        {
            return m_position != Vector3.zero && m_scale != Vector2.zero;
        }

        /// <summary>
        /// 해당 위치가 아이템 위치/크기 정보내에 위치하는지 여부 리턴
        /// </summary>
        public bool IsInside(Vector3 _pos, UI.UIDirectType _direction)
        {
            switch (_direction)
            {
                case UI.UIDirectType.DOWN:
                    if (_pos.y <= m_position.y && _pos.y > m_position.y - m_scale.y)
                        return true;
                    break;
                case UI.UIDirectType.UP:
                    if (_pos.y >= m_position.y && _pos.y < m_position.y + m_scale.y)
                        return true;
                    break;
                case UI.UIDirectType.LEFT:
                    if (_pos.x <= m_position.x && _pos.x > m_position.x - m_scale.x)
                        return true;
                    break;
                case UI.UIDirectType.RIGHT:
                    if (_pos.x >= m_position.x && _pos.x < m_position.x + m_scale.x)
                        return true;
                    break;
            }

            return false;
        }
    }

    #region [Private Variables]
    /// <summary>
    /// 아이템 위치/크기 정보 리스트
    /// </summary>
    private List<ItemTransformInfo> m_itemTransformInfoList;
    #endregion

    #region [Property]
    /// <summary>
    /// 전체 아이템의 개수
    /// </summary>
    public override int MaxCount
    {
        set
        {
            int prevCount = MaxCount;
            base.MaxCount = value;

            // 아이템 개수에 맞게 아이템 위치/크기 정보 리스트 설정
            if (m_itemTransformInfoList == null)
                m_itemTransformInfoList = new List<ItemTransformInfo>();
            else
            {
                if (prevCount < value)
                {
                    for (int i = prevCount; i < value; ++i)
                        m_itemTransformInfoList.Add(new ItemTransformInfo());
                }
                else if (prevCount > value)
                    m_itemTransformInfoList.RemoveRange(value, prevCount - value);
            }
        }
    }
    #endregion

    #region [Public Method]
    /// <summary>
    /// 기본 생성
    /// </summary>
    public override void Create(OnUpdateItem _updateItemCB)
    {
        base.Create(_updateItemCB);
    }

    /// <summary>
    /// 전체 데이터에서의 아이템 인덱스 구하기
    /// </summary>
    public override int GetRealIndex(Vector3 _pos, bool _bAbs = false, int _iFindStartIndex = 0, int _iFindCount = int.MaxValue)
    {
        int index = 0;
        if (m_itemTransformInfoList != null)
        {
            // 해당 위치가 첫번째 아이템 위치보다 작은지 체크
            switch (direction)
            {
                case UI.UIDirectType.DOWN:
                    if (_pos.y > 0f)
                        index = 1;
                    break;
                case UI.UIDirectType.UP:
                    if (_pos.y < 0f)
                        index = -1;
                    break;
                case UI.UIDirectType.LEFT:
                    if (_pos.x > 0f)
                        index = 1;
                    break;
                case UI.UIDirectType.RIGHT:
                    if (_pos.x < 0f)
                        index = -1;
                    break;
            }

            if (index == 0)
            {
                index = int.MaxValue * (direction == UI.UIDirectType.DOWN || direction == UI.UIDirectType.LEFT ? 1 : -1);
                _pos.x = Mathf.RoundToInt(_pos.x);
                _pos.y = Mathf.RoundToInt(_pos.y);

                // 해당 위치에서의 인덱스 찾기
                bool bFind = false;
                int findCount = Mathf.Clamp(_iFindCount, 0, m_itemTransformInfoList.Count);
                int findStartIndex = Mathf.Max(0, _iFindStartIndex);
                for (int i = findStartIndex; i < findCount; ++i)
                {
                    ItemTransformInfo info = m_itemTransformInfoList[i];
                    if (i > 0 && info.IsValid() == false)
                    {
                        // 인덱스의 아이템 위치/크기 정보가 없으면 해당 인덱스로 설정
                        bFind = true;
                        index = direction == UI.UIDirectType.DOWN || direction == UI.UIDirectType.LEFT ? -i : i;
                        break;
                    }

                    // 해당 위치가 아이템 위치/크기 정보내에 위치하는지 체크
                    if (info.IsInside(_pos, direction) == true)
                    {
                        bFind = true;
                        switch (direction)
                        {
                            case UI.UIDirectType.DOWN:
                            case UI.UIDirectType.LEFT:
                                index = -i;
                                break;
                            case UI.UIDirectType.UP:
                            case UI.UIDirectType.RIGHT:
                                index = i;
                                break;
                        }
                    }

                    if (bFind == true)
                        break;
                }

                if (bFind == false && findStartIndex > 0)
                {
                    // 0보다 큰 찾기 시작하는 인덱스 부터 검색해 못찾은 경우 0번째 인덱스 부터 찾기 시작한 인덱스까지 다시 검색
                    for (int i = findStartIndex - 1; i >= 0; --i)
                    {
                        ItemTransformInfo info = m_itemTransformInfoList[i];
                        // 해당 위치가 아이템 위치/크기 정보내에 위치하는지 체크
                        if (info.IsInside(_pos, direction) == true)
                        {
                            bFind = true;
                            switch (direction)
                            {
                                case UI.UIDirectType.DOWN:
                                case UI.UIDirectType.LEFT:
                                    index = -i;
                                    break;
                                case UI.UIDirectType.UP:
                                case UI.UIDirectType.RIGHT:
                                    index = i;
                                    break;
                            }
                        }

                        if (bFind == true)
                            break;
                    }
                }
            }
        }

        return _bAbs == true ? Mathf.Abs(index) : index;
    }

    /// <summary>
    /// 현재 보여지는 UI 그대로 갱신
    /// </summary>
    public override void UpdateUI(bool _restrictWithinBounds, bool _instant = false)
    {
        if (mROCItemList != null && mROCItemList.Count > 0)
        {
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                // 아이템 위치/크기 정보 설정
                SetItemPosition(item);
            }

            int maxCount = MaxCount;
            int startIndex = 0;
            UIRecycleItem startItem = null;
            if (defaultCount < maxCount)
            {
                // 시작 인덱스 계산
                startIndex = int.MaxValue;
                for (int i = 0; i < mROCItemList.Count; ++i)
                {
                    UIRecycleItem item = mROCItemList[i];
                    if (item.Index < startIndex)
                    {
                        startIndex = item.Index;
                        startItem = item;
                    }
                }

                startIndex = GetRealIndex(startItem.transform.localPosition, true);
            }

            if (mROCItemList.Count > 0)
                m_iMinIndex = int.MaxValue;

            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                bool active = true;
                int index = startIndex + i;
                if (index >= maxCount)
                {
                    // 인덱스가 총 갯수보다 크면 위로 올림
                    index = startIndex - (index - maxCount + 1);
                    active = index >= 0;
                }

                UIRecycleItem item = null;
                if (active == true)
                {
                    // 기존 동일한 인덱스의 아이템으로 설정
                    for (int j = 0; j < mROCItemList.Count; ++j)
                    {
                        item = mROCItemList[j];
                        if (item.Index == index)
                        {
                            active = index < maxCount;
                            item.SetActive(active);
                            if (active == true)
                            {
                                mOnUpdateItem(item, index, recycleID);
                                // 위치 설정
                                SetItemPosition(item);
                            }
                            break;
                        }
                        else
                            item = null;
                    }
                }

                // 기존 동일한 인덱스의 아이템이 없는 경우 현재 인덱스의 아이템으로 설정
                if (item == null)
                {
                    item = mROCItemList[i];
                    item.Index = index;
                    item.SetActive(active);
                    if (active == true)
                    {
                        mOnUpdateItem(item, index, recycleID);
                        // 위치 설정
                        SetItemPosition(item);
                    }
                }

                // 최소 인덱스 설정
                if (item != null && item.Index < m_iMinIndex)
                    m_iMinIndex = item.Index;
            }

            // bounds용 더미 UIWidget 설정
            SetDummyUIWidgetPosition();

            if (_restrictWithinBounds == true)
            {
                mScrollView.InvalidateBounds();
                mScrollView.RestrictWithinBounds(_instant, mIsHorizontal, !mIsHorizontal);
            }
        }
    }

    /// <summary>
    /// 아이템 모두 제거
    /// </summary>
    public override void DeleateAllItem(bool _bResetPos)
    {
        if (m_itemTransformInfoList != null)
            m_itemTransformInfoList.Clear();
        base.DeleateAllItem(_bResetPos);
    }

    /// <summary>
    /// 아이템 위치/크기 정보들 초기화
    /// </summary>
    public void ResetItemTransformInfos()
    {
        if (m_itemTransformInfoList != null)
            m_itemTransformInfoList.ForEach(item => item.Reset());
    }

    /// <summary>
    /// 아이템 크기 정보 설정
    /// </summary>
    public void SetItemSize(int _iIndex, Vector2 _size)
    {
        if (_iIndex >= 0)
        {
            ItemTransformInfo info = GetItemTransformInfo(_iIndex);
            if (info != null)
            {
                // 아이템 크기 설정
                info.SetScale(_size);
            }
        }
    }

    /// <summary>
    /// 아이템 크기 정보 설정
    /// </summary>
    public void SetItemSize(int _iIndex, float _size)
    {
        SetItemSize(_iIndex, new Vector2(mIsHorizontal ? _size : 0f, mIsHorizontal ? 0f : _size));
    }

    /// <summary>
    /// 아이템 크기 정보 설정 및 위치 갱신
    /// </summary>
    public void SetItemSizeWithReposition(int _iIndex, Vector2 _size)
    {
        if (_iIndex >= 0)
        {
            ItemTransformInfo info = GetItemTransformInfo(_iIndex);
            if (info != null)
            {
                // 아이템 크기 설정
                info.SetScale(_size);

                // 아이템 인덱스 리스트 생성
                List<int> indexList = new List<int>();
                for (int i = 0; i < mROCItemList.Count; ++i)
                    indexList.Add(i);

                // 변경된 아이템의 다음 아이템 부터 아이템 위치 재설정
                for (int i = _iIndex + 1; i < m_itemTransformInfoList.Count; ++i)
                {
                    info = m_itemTransformInfoList[i];
                    if (info.Position == Vector3.zero || info.SetPosition(GetItemTransformInfo(i - 1), direction) == false)
                        break;
                    else if (indexList.Count <= mROCItemList.Count)
                    {
                        // 아이템들의 위치 재설정
                        for (int j = 0; j < indexList.Count; ++j)
                        {
                            UIRecycleItem item = mROCItemList[indexList[j]];
                            if (item.Index == i)
                            {
                                // 아이템 위치/크기 정보 설정
                                SetItemPosition(item);
                                indexList.RemoveAt(j);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 아이템 크기 정보 설정 및 위치 갱신
    /// </summary>
    public void SetItemSizeWithReposition(int _iIndex, float _size)
    {
        SetItemSizeWithReposition(_iIndex, new Vector2(mIsHorizontal ? _size : 0f, mIsHorizontal ? 0f : _size));
    }
    #endregion

    #region [Protected Method]
    /// <summary>
    /// 스크롤 이동
    /// </summary>
    protected override void OnMove(UIPanel panel)
    {
        if (mROCItemList != null && mROCItemList.Count > 0)
        {
            int iChildCount = 0;
            float extents = 0f;
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                if (item != null && NGUITools.GetActive(item.gameObject) == true)
                {
                    iChildCount++;
                    extents += mIsHorizontal ? item.ItemSize.x : item.ItemSize.y;
                }
            }

            WrapContent(iChildCount, extents);
        }
    }

    /// <summary>
    /// 패널영역 안에 보여질 아이템 갯수 계산
    /// </summary>
    protected override int GetCalculateViewCount()
    {
        int iCount = 0;
        if (m_itemTransformInfoList != null)
        {
            Vector2 size = Vector2.zero;
            Vector3[] corners = mPanel.localCorners;
            float viewSize = mIsHorizontal == false ? corners[1].y - corners[0].y : corners[3].x - corners[0].x;

            for (int i = 0; i < m_itemTransformInfoList.Count; ++i)
            {
                ItemTransformInfo info = m_itemTransformInfoList[i];
                if (info != null)
                {
                    size += info.Scale;
                    if (mIsHorizontal == false)
                    {
                        if (viewSize > size.y)
                            iCount++;
                        else
                            break;
                    }
                    else
                    {
                        if (viewSize > size.x)
                            iCount++;
                        else
                            break;
                    }
                }
            }
        }

        return iCount;
    }

    /// <summary>
    /// 아이템 위치 설정
    /// </summary>
    protected override void SetItemPosition(UIRecycleItem _item)
    {
        if (_item != null)
        {
            ItemTransformInfo info = SetItemTransformInfo(_item);
            if (info != null)
                _item.transform.localPosition = info.Position;
        }
    }

    /// <summary>
    /// 유효한 아이템 인덱스 구하기
    /// </summary>
    protected override int GetValidIndex(int _index, bool _bForward = true)
    {
        int validIndex = _index;
        if (mROCItemList != null)
        {
            // 아이템내 중복 인덱스 있는지 체크
            for (int i = 0; i < mROCItemList.Count; ++i)
            {
                UIRecycleItem item = mROCItemList[i];
                if (validIndex == item.Index)
                {
                    // 아이템 인덱스 리스트 설정
                    List<int> indexList = new List<int>();
                    for (int j = 0; j < mROCItemList.Count; ++j)
                    {
                        int index = mROCItemList[j].Index;
                        indexList.Add(index);
                    }

                    // 인덱스 정렬
                    indexList.Sort();

                    int findIndex = -1;
                    for (int j = 0; j < indexList.Count; ++j)
                    {
                        int index = indexList[j];
                        // 연속되지 않고 빠진 인덱스 검색
                        if (j + 1 < indexList.Count && ++index != indexList[j + 1])
                        {
                            findIndex = index;
                            break;
                        }
                    }

                    // 빠진 인덱스를 못찾은 경우
                    if (findIndex < 0)
                    {
                        // 최소이전/최대다음 인덱스
                        int minIndex = --indexList[0];
                        int maxIndex = ++indexList[indexList.Count - 1];
                        // 스크롤 방향에 따라 최대/최소 인덱스로 설정
                        validIndex = _bForward == true ? maxIndex : minIndex;
                        if (validIndex < 0)
                        {
                            // 0보다 작은 경우 아이템의 인덱스 중 가장 큰 인덱스로 설정
                            validIndex = maxIndex;
                        }
                        else if (validIndex >= MaxCount)
                        {
                            // 최대 갯수 보다 큰 경우 아이템의 인덱스 중 가장 작은 인덱스로 설정
                            validIndex = minIndex;
                        }
                    }
                    else
                    {
                        // 빠져 있는 인덱스로 설정
                        validIndex = findIndex;
                    }

                    break;
                }
            }

            // 중복 인덱스가 없는 경우
            if (_index == validIndex)
            {
                // 아이템 인덱스 리스트 설정
                List<int> indexList = new List<int>();
                for (int j = 0; j < mROCItemList.Count; ++j)
                {
                    int index = mROCItemList[j].Index;
                    indexList.Add(index);
                }

                // 인덱스 정렬
                indexList.Sort();

                // 최소이전/최대다음 인덱스
                int minIndex = --indexList[0];
                int maxIndex = ++indexList[indexList.Count - 1];

                if (_bForward == true)
                {
                    // 최대 인덱스 보다 크면 최대 인덱스로 설정
                    if (validIndex > maxIndex)
                        validIndex = maxIndex;
                }
                else
                {
                    // 최소 인덱스 보다 작으면 최소 인덱스로 설정
                    if (validIndex < minIndex)
                        validIndex = minIndex;
                }
            }
        }

        return validIndex;
    }
    #endregion

    #region [Private Method]
    /// <summary>
    /// 해당 인덱스의 아이템 위치/크기 정보 리턴
    /// </summary>
    private ItemTransformInfo GetItemTransformInfo(int _iIndex)
    {
        return m_itemTransformInfoList != null && _iIndex < m_itemTransformInfoList.Count ? m_itemTransformInfoList[_iIndex] : null;
    }

    /// <summary>
    /// 아이템 위치/크기 정보 설정
    /// </summary>
    private ItemTransformInfo SetItemTransformInfo(UIRecycleItem _item)
    {
        ItemTransformInfo info = null;
        if (_item != null)
        {
            info = GetItemTransformInfo(_item.Index);
            if (info != null)
            {
                // 크기 설정
                if (info.Scale == Vector2.zero)
                    info.SetScale(_item.ItemSize);

                // 이전 아이템의 정보로 위치 설정
                if (_item.Index > 0)
                    info.SetPosition(GetItemTransformInfo(_item.Index - 1), direction);
                else
                    info.SetPosition(Vector3.zero, false);
            }
        }

        return info;
    }

    /// <summary>
    /// 전체 아이템의 크기 구하기
    /// </summary>
    private Vector2 GetAllItemSize()
    {
        Vector2 size = Vector2.zero;
        if (m_itemTransformInfoList != null)
            m_itemTransformInfoList.ForEach((item) => { size += item.Scale; });
        return size;
    }
    #endregion

    #region [Event Receiver]
    #endregion
}