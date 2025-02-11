using System;
using System.Collections.Generic;


/// <summary>
/// 장비 옵션 데이타.
/// </summary>
public class EquipOptionData
{
    /// <summary>
    /// 합성 결과로 추가될 옵션 메타 아이디
    /// </summary>
    public const int SYNTHESIS_RESULT_OPTION_META_ID = -1;

    private int mMetaID;                                    //옵션 metaID
    private StatFloat mValue;                                   //옵션값
    private StatFloat mApplyValue;                              //적용 옵션값
    private bool mIsLock;                                   //잠김 여부
    private eEquipmentOptionValueApplyRate mApplyRate;      //값 적용 비율
    private EquipmentOptionMetaData mMetaData;              //옵션 메타 데이터

    public int MetaID { get { return mMetaID; } }
    public float Value { get { return mValue != null ? mValue.Value : 0; } }
    public bool IsLock { get { return mIsLock; } }
    public float ApplyValue
    {
        get
        {
            if (mApplyValue == null) return 0;

            return IsLock == false ? mApplyValue.Value : 0f;
        }
    }

    public eEquipmentOptionValueApplyRate ApplyRate { get { return mApplyRate; } }
    public EquipmentOptionMetaData MetaData { get { return mMetaData; } }

    public EquipOptionData(int aMetaID, float aValue)
    {
        mMetaID = aMetaID;
        mMetaData = null;
        mApplyRate = eEquipmentOptionValueApplyRate.DEFAULT;
        SetEquipOptionValue(aValue);
    }

    public EquipOptionData(EquipOptionData aOptionData)
    {
        if (aOptionData != null)
        {
            SetEquipOptionData(aOptionData.MetaID, aOptionData.Value);
            SetOptionValueApplyRate(aOptionData.ApplyRate);
            SetLockState(aOptionData.IsLock);
            SetMetaData(aOptionData.MetaData);
        }
    }

    /// <summary>
    /// 옵션 값 설정
    /// </summary>
    public void SetEquipOptionData(int aMetaID, float aValue)
    {
        mMetaID = aMetaID;
        SetEquipOptionValue(aValue);
    }

    /// <summary>
    /// 옵션 값 설정
    /// </summary>
    public void SetEquipOptionValue(float aValue)
    {
        if (mValue == null)
        {
            mValue = new StatFloat(aValue);
        }
        else mValue.Value = aValue;

        SetOptionValueApplyRate(mApplyRate);
    }

    /// <summary>
    /// 값 적용 비율 설정
    /// </summary>
    public void SetOptionValueApplyRate(eEquipmentOptionValueApplyRate aApplyRate)
    {
        mApplyRate = aApplyRate;
        mApplyValue = new StatFloat(Utility.CalculateFloat(Value, 
            ConvertDefineMetaData.ConvertToFactorFromEOVAR(mApplyRate)));
    }

    /// <summary>
    /// 잠김 상태 설정
    /// </summary>
    public void SetLockState(bool aIsLock)
    {
        mIsLock = aIsLock;
    }

    /// <summary>
    /// 메타 데이터 설정
    /// </summary>
    public void SetMetaData(EquipmentOptionMetaData aMetaData)
    {
        mMetaData = aMetaData;
    }

    /// <summary>
    /// 소울 스톤 메타 데이터 리턴
    /// </summary>
    public SoulStoneMetaData GetSoulStoneMetaData()
    {
        return MetaDataManager.GetSoulStoneMetaDataByMetaID(mMetaID);
    }

    /// <summary>
    /// 빈 옵션인지 여부
    /// </summary>
    public bool IsEmpty() { return mMetaID == 0; }

    /// <summary>
    /// 합성 결과로 추가될 옵션인지 여부
    /// </summary>
    public bool IsSynthesisResult() { return mMetaID == SYNTHESIS_RESULT_OPTION_META_ID; }
}

/// <summary>
/// 장착 슬롯 정보
/// </summary>
public class EquipSlotInfo
{
    public const int SLOT_COUNT = 5;

    private PlayerDataAccessToken mToken;
    private EquipmentData[] mPartEquipmentData;
    public EquipmentData[] PartEquipmentData { get { return mPartEquipmentData; } }

    public EquipSlotInfo(PlayerDataAccessToken aToken)
    {
        mToken = aToken;
    }

    /// <summary>
    /// 장비 정보 슬롯으로 리턴
    /// </summary>
    public EquipmentData GetEquipmentData(eEquipmentType slot)
    {
        int aSlotIndex = ((int)slot - 1);

        if (mPartEquipmentData == null || aSlotIndex == -1) return null;

        if (aSlotIndex < mPartEquipmentData.Length)
        {
            return mPartEquipmentData[aSlotIndex];
        }

        return null;
    }

    public void SetEquip(PlayerDataAccessToken aToken, eEquipmentType slot, EquipmentData equipData, eBattleUnitType applyUnitType = eBattleUnitType.NONE)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        if (mPartEquipmentData == null)
        {
            mPartEquipmentData = new EquipmentData[SLOT_COUNT];
        }

        int aSlotIndex = ((int)slot - 1);
        if (mPartEquipmentData == null || aSlotIndex == -1) return;

        if(aSlotIndex < mPartEquipmentData.Length)
        {
            mPartEquipmentData[aSlotIndex] = equipData;
        }

        UpdateSlots(applyUnitType);
    }

    public void ReleaseEquip(PlayerDataAccessToken aToken, eEquipmentType slot)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        int aSlotIndex = ((int)slot - 1);
        if (mPartEquipmentData == null || aSlotIndex == -1) return;

        if (aSlotIndex < mPartEquipmentData.Length)
        {
            mPartEquipmentData[aSlotIndex] = null;
        }
    }

    public void ReleaseEquipAll(PlayerDataAccessToken aToken)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        if (mPartEquipmentData != null)
        {
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null)
                {
                    ed.SetEquiped(aToken);
                    mPartEquipmentData[i] = null;
                }
            }
        }
    }

    /// <summary>
    /// 해당 세트의 갯수
    /// </summary>
    public int GetEquipSetCount(int setIndex)
    {
        int count = 0;
        if (mPartEquipmentData != null)
        {
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null && ed.SetTypeMetaID == setIndex)
                    count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 해당 장비의 장착된 세트 가운데 세트내 장착 번호
    /// </summary>
    public int GetEquipSetNumber(int setIndex, eEquipmentType type)
    {
        int number = 0;
        if (mPartEquipmentData != null)
        {
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null && ed.SetTypeMetaID == setIndex)
                {
                    number++;
                    if (ed.EquipmentType == type)
                        break;
                }
            }
        }

        return number;
    }

    /// <summary>
    /// 장비 스탯 추가 가져옴
    /// </summary>
    public int GetAddedStatEquip(eEquipmentOption option, int baseStat)
    {
        int stat = 0;
        //장비 스탯 값 +
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            eEquipmentType slot = (eEquipmentType)(i + 1);
            EquipmentData ed = GetEquipmentData(slot);
            if (ed != null)
                stat += ed.GetTotalOptionValue(option, baseStat);
        }
        return stat;
    }

    /// <summary>
    /// 슬롯 업데이트
    /// </summary>
    public void UpdateSlots(eBattleUnitType applyUnitType = eBattleUnitType.NONE)
    {
        if (mPartEquipmentData != null)
        {
            // 장비 정보 업데이트
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                // 적용된 병종에 따라 장비 옵션 적용
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null)
                    ed.ApplyOption(mToken, applyUnitType);
            }
        }
    }

    /// <summary>장착한 장비의 UID 리턴 : 영웅 스택 체크용</summary>
    public int GetEquipmentUID()
    {
        if (mPartEquipmentData != null)
        {
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null)
                    return ed.ObjectID;
            }
        }
        return 0;
    }

    /// <summary>장착한 장비가 있는지 여부 리턴</summary>
    public bool IsEquipped()
    {
        if (mPartEquipmentData != null)
        {
            for (int i = 0; i < mPartEquipmentData.Length; ++i)
            {
                EquipmentData ed = mPartEquipmentData[i];
                if (ed != null && ed.IsEquiped == true)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// equip 정보를 json 정보로 바꿔서 리턴
    /// </summary>
    /// <returns></returns>
    public JSONArray GetEquipInfoToJson()
    {
        JSONArray equipJsonArray = new JSONArray();

        for (int i = 0; i < mPartEquipmentData.Length; i++)
        {
            if (mPartEquipmentData[i] == null) continue;

            EquipmentData equipData = mPartEquipmentData[i];

            JSONObject jsonObj = new JSONObject();
            jsonObj.Add("eu", equipData.ObjectID);
            jsonObj.Add("ei", equipData.MetaID);
            jsonObj.Add("eg", equipData.Grade);
            jsonObj.Add("ee", equipData.Exp);
            jsonObj.Add("moi", equipData.MainOption.MetaID);
            jsonObj.Add("mov", equipData.MainOption.Value);
            jsonObj.Add("lk", equipData.IsLock);
            EquipOptionData[] subOptions = equipData.SubOption;
            for (int j = 0; j < subOptions.Length; j++)
            {
                if (subOptions[j] == null) continue;

                string index_key = "soi" + (j + 1);
                string value_key = "sov" + (j + 1);
                jsonObj.Add(index_key, subOptions[j].MetaID);
                jsonObj.Add(value_key, subOptions[j].Value);
            }
            //소울스톤 옵션
            if(equipData.SoulStoneOption != null)
            {
                jsonObj.Add("ssoi", equipData.SoulStoneOption.MetaID);
                jsonObj.Add("ssov", equipData.SoulStoneOption.Value);
            }
            //세트 옵션
            if (equipData.EquipmentSetIndex > 0)
            {
                jsonObj.Add("si", equipData.EquipmentSetIndex);
            }
            //슬롯 넘버
            jsonObj.Add("sn", equipData.SlotNum);
            //장착된 영웅 uID
            jsonObj.Add("hu", equipData.HeroUID);
            equipJsonArray.Add(jsonObj);
        }

        return equipJsonArray;
    }
}

/// <summary>
/// 장비 데이타.
/// </summary>
public class EquipmentData : CardData
{
    private eEquipmentType mEquipmentType;              //장착 위치
    private eEquipmentTier mTier;                       //티어
    private eEquipmentApplyUnitType mApplyUnitType;     //병종 타입
    private eBattleUnitType mAppliedUnitType;           //적용된 병종 타입
    private int mSetTypeMetaID;                          //세트타입 메타ID
    private EquipOptionData mMainOption;                //주 옵션
    private EquipOptionData[] mSubOption;               //서브 옵션
    private EquipOptionData mSoulStoneOption;           //소울 스톤 옵션
    private EquipOptionData mSetOption;                 //세트 옵션
    private int mExp;                                   //인챈트 경험치
    private int mHeroUID;                               //장착한 영웅 UID = 0 없음
    private int mSlotNum;                               //장착 슬롯 위치 1부터
    private int mSetIndex;                              //세트 인덱스
    private int mApplySetCount;                         //세트 적용 갯수
    private int mSetEquipNum;                           //세트 장착 번호
    private bool mApplySetOption;                       //세트 옵션 적용 여부
    private bool mInit = false;

    public eEquipmentType EquipmentType
    {
        get
        {
            if (mEquipmentType == eEquipmentType.NONE)
            {
                EquipmentMetaData metaData = MetaDataManager.GetEquipmentMetaDataByMetaID(MetaID);
                if (metaData == null)
                    HwLog.LogError("EquipmentMetaData is null");
                else
                    mEquipmentType = metaData.EquipmentType;
            }

            return mEquipmentType;
        }
    }
    public eEquipmentTier Tier { get { InitMetaData(); return mTier; } }
    public eEquipmentApplyUnitType ApplyUnitType { get { InitMetaData(); return mApplyUnitType; } }
    public eBattleUnitType AppliedUnitType { get { return mAppliedUnitType; } }
    public int SetTypeMetaID { get { InitMetaData(); return mSetTypeMetaID; } }
    public int HeroUID { get { return mHeroUID; } }
    public int SlotNum { get { return mSlotNum; } }
    public int EquipmentSetIndex { get { return mSetIndex; } }
    public bool IsEquiped { get { return mHeroUID != 0; } }
    public bool IsEquipedOnHero { get { return mPlayerCardData != null; } }
    public bool IsSet { get { return mSetTypeMetaID > 0; } }
    public eEquipmentGrade GradeType { get { return (eEquipmentGrade)Grade; } }
    public override int Grade { get { InitMetaData(); return base.Grade; } }
    public override int Level
    {
        get
        {
            if (mLevel == null)
                CalcurateLevel();
            return base.Level;
        }
    }
    public int Exp { get { return mExp; } }
    public EquipOptionData MainOption { get { InitMetaData(); return mMainOption; } }
    public EquipOptionData[] SubOption { get { InitMetaData(); return mSubOption; } }
    public EquipOptionData SoulStoneOption { get { InitMetaData(); return mSoulStoneOption; } }
    public EquipOptionData SetOption { get { UpdateSetOption(); return mSetOption; } }
    public int ApplySetCount { get { UpdateSetOption(); return mApplySetCount; } }
    public bool IsApplySet { get { UpdateSetOption(); return mApplySetCount > 0 && mSetOption.ApplyValue > 0; } }

    public EquipmentData(PlayerDataAccessToken aToken, int aMetaID, int aObjectID, eCardType aCardType,
        EquipOptionData aMainOption, EquipOptionData[] aSubObtions, EquipOptionData aSoulStoneOption, 
        int aGrade, int aExp, bool isLock, int aHeroUID, int aSlotNumber, int aSetIndex)
        : base(aToken, aObjectID, aCardType, isLock)
    {
        mMetaID = new StatInt(aMetaID, true);
        mGrade = new StatInt(aGrade, true);
        mExp = aExp;
        mMainOption = aMainOption;
        mSubOption = aSubObtions;
        mSoulStoneOption = aSoulStoneOption;
        if (mSoulStoneOption == null || mSoulStoneOption.IsEmpty() == true)
            mSetIndex = aSetIndex;
        ResetOption(aToken);
        SetEquiped(aToken, aHeroUID, aSlotNumber > 0 ? aSlotNumber : Constants.DEFAULT_CARD_SLOT_NUM);
    }

    public EquipmentData(PlayerDataAccessToken aToken, JSONObject aJsonObj) 
        : base(aToken, aJsonObj.GetInt("eu"), eCardType.EQUIP, aJsonObj.GetBoolean("lk"))
    {
        mMetaID = new StatInt(aJsonObj.GetInt("ei"), true);
        mGrade = new StatInt(aJsonObj.GetInt("eg"), true);
        mExp = aJsonObj.GetInt("ee");

        // 옵션 정보
        EquipOptionData mainOption = new EquipOptionData(aJsonObj.GetInt("moi"), (aJsonObj.GetFloat("mov")));
        EquipOptionData[] subOptions = new EquipOptionData[Constants.EQUIPMENT_SUB_OPTION_COUNT];
        for (int i = 0; i < subOptions.Length; i++)
        {
            string key = "soi" + (i + 1);
            if (aJsonObj.ContainsKey(key) == true)
                subOptions[i] = new EquipOptionData(aJsonObj.GetInt(key), (aJsonObj.GetFloat("sov" + (i + 1))));
        }

        EquipOptionData soulStoneOption = null;
        if (aJsonObj.ContainsKey("ssoi") == true)
            soulStoneOption = new EquipOptionData(aJsonObj.GetInt("ssoi"), (aJsonObj.GetFloat("ssov")));
        
        mMainOption = mainOption;
        mSubOption = subOptions;
        mSoulStoneOption = soulStoneOption;
        if (mSoulStoneOption == null || mSoulStoneOption.IsEmpty() == true)
            mSetIndex = aJsonObj.GetInt("si");

        ResetOption(aToken);

        int slotNumber = aJsonObj.GetInt("sn");
        SetEquiped(aToken, aJsonObj.GetInt("hu"), slotNumber > 0 ? slotNumber : Constants.DEFAULT_CARD_SLOT_NUM);
    }

    public EquipmentData(EquipmentData aEquipmentData) 
        : base(aEquipmentData.mToken, aEquipmentData.ObjectID, aEquipmentData.CardType, aEquipmentData.IsLock)
    {
        CopyEquipmentData(aEquipmentData);
    }

    public EquipmentData(EquipmentData aEquipmentData, int aExp, float aMainOptionValue)
        : base(aEquipmentData.mToken, aEquipmentData.ObjectID, aEquipmentData.CardType, aEquipmentData.IsLock)
    {
        CopyEquipmentData(aEquipmentData);
        SetExp(aEquipmentData.mToken, aExp);
        if (mMainOption != null)
            mMainOption.SetEquipOptionValue(aMainOptionValue);
    }

    /// <summary>
    /// 다른 EquipmentData 복사
    /// </summary>
    public void CopyEquipmentData(EquipmentData aEquipmentData)
    {
        mMetaID = new StatInt(aEquipmentData.MetaID, true);
        mGrade = new StatInt(aEquipmentData.Grade);

        if (aEquipmentData.MainOption != null)
            mMainOption = new EquipOptionData(aEquipmentData.MainOption);
        if (aEquipmentData.SubOption != null && aEquipmentData.SubOption.Length > 0)
        {
            mSubOption = new EquipOptionData[aEquipmentData.SubOption.Length];
            for (int i = 0; i < aEquipmentData.SubOption.Length; ++i)
                mSubOption[i] = new EquipOptionData(aEquipmentData.SubOption[i]);
        }
        if (aEquipmentData.SetOption != null)
            mSetOption = new EquipOptionData(aEquipmentData.SetOption);
        if (aEquipmentData.SoulStoneOption != null)
            mSoulStoneOption = new EquipOptionData(aEquipmentData.mSoulStoneOption);
        if (mSoulStoneOption == null || mSoulStoneOption.IsEmpty() == true)
            mSetIndex = aEquipmentData.EquipmentSetIndex;

        ResetOption(aEquipmentData.mToken);
        SetExp(aEquipmentData.mToken, aEquipmentData.Exp);
        SetEquiped(aEquipmentData.mToken, aEquipmentData.HeroUID, aEquipmentData.SlotNum);
        InitMetaData();
    }

    /// <summary>
    /// 장비를 장착하고 있는 영웅 데이터 리턴
    /// </summary>
    public override HeroData GetEquipedHeroData()
    {
        HeroData heroData = null;
        if (mPlayerCardData != null)
            heroData = mPlayerCardData.GetHeroData(mHeroUID);
        return heroData;
    }

    /// <summary>
    /// 영웅에 장착/해제 처리
    /// </summary>
    public void SetEquiped(PlayerDataAccessToken aToken, int aHeroUID = 0, int aSlotNum = Constants.DEFAULT_CARD_SLOT_NUM)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        mHeroUID = aHeroUID;
        if (IsEquiped == false)
            mPlayerCardData = null;

        mSlotNum = aSlotNum;
        mApplySetCount = -1;
        mSetEquipNum = 0;
        mInit = false;
    }

    /// <summary>
    /// 경험치 적용
    /// </summary>
    public void SetExp(PlayerDataAccessToken aToken, int aExp)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        mExp = aExp;

        // 레벨 계산
        CalcurateLevel();
    }

    /// <summary>
    /// 최대 레벨 리턴
    /// </summary>
    public int GetMaxLevel()
    {
        if (MetaDataManager.IsLoaded == true)
        {
            EquipmentUpgradeMetaData euMD = MetaDataManager.GetEquipmentUpgradeMetaData(Tier, GradeType);
            if (euMD != null)
                return euMD.GetMaxLevel();
        }

        return 0;
    }

    /// <summary>
    /// 옵션 정보 초기화
    /// </summary>
    public void ResetOption(PlayerDataAccessToken aToken)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        // 적용 병종 타입 초기화
        mAppliedUnitType = eBattleUnitType.END;
    }

    /// <summary>
    /// 옵션 적용
    /// </summary>
    public void ApplyOption(PlayerDataAccessToken aToken, eBattleUnitType aApplyUnitType = eBattleUnitType.NONE)
    {
        if (mToken != aToken)
        {
            throw new Exception();
        }

        if (MetaDataManager.IsLoaded == false)
            return;

        if (mInit == false)
        {
            InitMetaData(aApplyUnitType);
            return;
        }

        // 적용된 병종에 따라 옵션 적용
        ApplyOptionByApplyUnitType(aApplyUnitType);

        // 소울 스톤 옵션
        if (mSoulStoneOption != null && mSoulStoneOption.IsEmpty() == false)
        {
            if (mPlayerCardData != null)
            {
                SoulStoneMetaData soulStoneMD = mSoulStoneOption.GetSoulStoneMetaData();
                if (soulStoneMD != null)
                {
                    HeroData hd = GetEquipedHeroData();
                    // 장착된 영웅의 메타 아이디와 스킬이 적용되는 영웅 메타 아이디가 같은 경우 옵션 적용
                    mSoulStoneOption.SetOptionValueApplyRate(hd != null && soulStoneMD.HeroMetaID == hd.MetaID ?
                        eEquipmentOptionValueApplyRate.DEFAULT : eEquipmentOptionValueApplyRate.RATE_0);
                }
            }
        }
        else
        {
            // 세트 옵션
            UpdateSetOption(true);
        }
    }

    /// <summary>
    /// 적용된 병종에 따라 옵션 적용
    /// </summary>
    public void ApplyOptionByApplyUnitType(eBattleUnitType aApplyUnitType)
    {
        if (MetaDataManager.IsLoaded == false)
            return;

        if (mInit == false)
        {
            InitMetaData(aApplyUnitType);
            return;
        }

        if (mAppliedUnitType != eBattleUnitType.END && mAppliedUnitType == aApplyUnitType)
            return;

        if (aApplyUnitType == eBattleUnitType.END)
            aApplyUnitType = eBattleUnitType.NONE;

        // 적용 병종 타입
        mAppliedUnitType = aApplyUnitType;

        // 메인 옵션
        if (mMainOption != null)
        {
            EquipmentOptionValueMetaData optionMetaData = MetaDataManager.GetEquipmentOptionValueMetaDataByMetaID(mMainOption.MetaID);
            if (optionMetaData != null)
            {
                mMainOption.SetMetaData(MetaDataManager.GetEquipmentOptionMetaData(optionMetaData.GetOptionMetaID(mTier)));
                // 옵션 적용 비율 설정
                if (mMainOption.MetaData != null)
                {
                    mMainOption.SetOptionValueApplyRate(GetEquipmentOptionValueApplyRateUnitType(
                        mApplyUnitType, mMainOption.MetaData.ApplyUnitType, mAppliedUnitType));
                }
            }
        }

        // 부 옵션
        if (mSubOption != null)
        {
            Array applyLevels = Enum.GetValues(typeof(eEquipmentOptionApplyLevel));
            var enumerator = applyLevels.GetEnumerator();
            for (int i = 0; i < mSubOption.Length; ++i)
            {
                enumerator.MoveNext();
                EquipOptionData optionData = mSubOption[i];
                if (optionData != null && optionData.IsEmpty() == false)
                {
                    EquipmentOptionValueMetaData optionMetaData = MetaDataManager.GetEquipmentOptionValueMetaDataByMetaID(optionData.MetaID);
                    if (optionMetaData != null)
                    {
                        optionData.SetMetaData(MetaDataManager.GetEquipmentOptionMetaData(optionMetaData.GetOptionMetaID(mTier)));
                        // 옵션 적용 비율 설정
                        if (optionData.MetaData != null)
                        {
                            optionData.SetOptionValueApplyRate(GetEquipmentOptionValueApplyRateUnitType(
                                mApplyUnitType, optionData.MetaData.ApplyUnitType, mAppliedUnitType));
                        }
                    }

                    // 잠금 설정
                    optionData.SetLockState(Level < (int)enumerator.Current);
                }
            }
        }
    }

    /// <summary>
    /// 세트 옵션 업데이트
    /// </summary>
    private void UpdateSetOption(bool aForce = false)
    {
        InitMetaData();

        if (mInit == true && IsSet == true)
        {
            if (aForce == true || mApplySetCount < 0)
            {
                if (mSetOption == null)
                    mSetOption = new EquipOptionData(mSetTypeMetaID, 0);

                int applyCount = 0;
                // 장착된 장비면 장착돼 있는 세트 장착 번호 및 갯수 계산
                if (IsEquiped == true && mPlayerCardData != null)
                {
                    // 세트 장착 번호
                    int setEquipNumber = mPlayerCardData.GetEquipmentSetEquipNumberOnHero(HeroUID, mSetTypeMetaID, EquipmentType);
                    if (mSetEquipNum != setEquipNumber || aForce == true)
                    {
                        mSetEquipNum = setEquipNumber;

                        EquipmentSetMetaData setMetaData = MetaDataManager.GetEquipmentSetMetaData(mSetTypeMetaID);
                        if (setMetaData != null)
                        {
                            // 장착된 세트 총 갯수
                            int totalEquipSetCount = mPlayerCardData.GetEquipmentSetEquipCountOnHero(HeroUID, mSetTypeMetaID);
                            // 세트 구성 장비 갯수
                            int totalSetCount = setMetaData.TotalCount;
                            // 세트 적용 갯수 계산
                            applyCount = totalEquipSetCount;
                            if (setEquipNumber > 0 && totalEquipSetCount > totalSetCount)
                            {
                                if (setEquipNumber % totalSetCount == 0 || setEquipNumber < totalEquipSetCount / totalSetCount * totalSetCount)
                                    applyCount = totalSetCount;
                                else
                                    applyCount = totalEquipSetCount % totalSetCount;
                            }
                        }
                    }
                    else
                        applyCount = mApplySetCount;
                }
                else
                    mApplySetOption = false;

                // 세트 적용 갯수
                if (applyCount != mApplySetCount)
                {
                    mApplySetCount = applyCount;
                    // 옵션 설정
                    EquipmentSetMetaData setMetaData = MetaDataManager.GetEquipmentSetMetaData(mSetTypeMetaID);
                    if (setMetaData != null)
                    {
                        EquipmentSetOptionMetaData optionMD = setMetaData.GetSetOptionByEquipCount(mApplySetCount);
                        if (optionMD != null)
                        {
                            bool bAppy = mApplySetCount == optionMD.ApplyEquipCount;
                            mSetOption.SetEquipOptionData(optionMD.OptionMetaID, optionMD.OptionValue);
                            mSetOption.SetOptionValueApplyRate(bAppy == true ?
                                eEquipmentOptionValueApplyRate.RATE_100 : eEquipmentOptionValueApplyRate.RATE_0);
                            mSetOption.SetMetaData(MetaDataManager.GetEquipmentOptionMetaData(optionMD.OptionMetaID));
                            if (bAppy == true)
                                mApplySetOption = mSetEquipNum % optionMD.ApplyEquipCount == 0;
                        }
                    }
                }
            }
        }
        else
            mApplySetOption = false;
    }

    /// <summary>
    /// 메타 데이타 초기화.
    /// </summary>
    private void InitMetaData(eBattleUnitType aApplyUnitType = eBattleUnitType.NONE)
    {
        if (mInit == true || MetaDataManager.IsLoaded == false)
            return;

        mInit = true;

        EquipmentMetaData metaData = MetaDataManager.GetEquipmentMetaDataByMetaID(MetaID);
        if (metaData == null)
            HwLog.LogError("EquipmentMetaData is null");
        else
        {
            mEquipmentType = metaData.EquipmentType;
            mTier = metaData.EquipmentTier;
            mApplyUnitType = metaData.EquipmentApplyUnitType;
            mSetTypeMetaID = metaData.SetTypeMetaID;
        }

        // 옵션 적용
        ApplyOption(mToken, aApplyUnitType);
    }

    /// <summary>
    /// 레벨 계산
    /// </summary>
    private void CalcurateLevel()
    {
        if (MetaDataManager.IsLoaded == true)
            SetLevel(mToken, MetaDataManager.GetEquipmentLevelInfoByExp(Tier, GradeType, mExp));
    }

    /// <summary>
    /// 최대 레벨 여부
    /// </summary>
    public override bool IsMaxLevel()
    {
        return MetaDataManager.IsLoaded == true ? Level >= MetaDataManager.GetEquipmentMaxLevel(Tier, GradeType) : false;
    }

    /// <summary>
    /// 최대 합성치 여부
    /// </summary>
    public override bool IsMaxSynthesis()
    {
        return Grade >= Constants.GET_MAX_EQUIPMENT_SYNTHESIS_GRADE;
    }

    /// <summary>
    /// 옵션 변경 가능 여부
    /// </summary>
    public bool IsPossibleOptionChange()
    {
        // 부옵션이 존재하고 모두 개방된 경우에 가능
        if (mSubOption != null && mSubOption.Length > 0)
        {
            bool bExist = false;
            for (int i = 0; i < mSubOption.Length; ++i)
            {
                EquipOptionData optionData = mSubOption[i];
                if (optionData != null && optionData.IsEmpty() == false)
                {
                    bExist = true;
                    if (optionData.IsLock == true)
                        return false;
                }
            }

            return bExist;
        }

        return false;
    }

    /// <summary>
    /// 합성 결과 데이터 리턴
    /// </summary>
    public EquipmentData GetSynthesisResultData()
    {
        if (CardManagerUtility.IsPossibleSynthesisEquipment(this, false) == true)
        {
            EquipmentData resultData = new EquipmentData(this);
            // 한 등급 상승
            resultData.SetGrade(mToken, Grade + 1);
            // 부옵션 갯수 증가
            if (resultData.SubOption != null)
            {
                bool bAdd = false;
                for (int i = 0; i < resultData.SubOption.Length; ++i)
                {
                    EquipOptionData option = resultData.SubOption[i];
                    if (option == null || option.IsEmpty() == true)
                    {
                        if (option == null)
                        {
                            option = new EquipOptionData(EquipOptionData.SYNTHESIS_RESULT_OPTION_META_ID, 0);
                            resultData.SubOption[i] = option;
                        }
                        else
                            option.SetEquipOptionData(EquipOptionData.SYNTHESIS_RESULT_OPTION_META_ID, option.Value);

                        // 최초 추가된 옵션 이후 옵션은 잠금 상태 설정
                        if (bAdd == true)
                            option.SetLockState(true);
                        else
                            bAdd = true;
                    }
                }
            }

            return resultData;
        }

        return null;
    }

    /// <summary>
    /// 유효한 부옵션 갯수 리턴 False : isLock 옵션 포함.
    /// </summary>
    public int GetValidSubOptionCount(bool _bCheckLock)
    {
        int count = 0;
        EquipOptionData[] subOption = SubOption;
        if (subOption != null)
        {
            for (int i = 0; i < subOption.Length; ++i)
            {
                EquipOptionData option = subOption[i];
                if (option != null && option.IsEmpty() == false && (_bCheckLock == false || option.IsLock == false))
                    count++;
            }
        }

        return count;
    }

    //호출하면 예외 발생
    public override int GetBattleCost()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 옵션 총 값을 가져옴.
    /// </summary>
    public int GetTotalOptionValue(eEquipmentOption aOptionType, int aBaseValue)
    {
        if (!mInit) InitMetaData();

        int totalValue = 0;

        // 주 옵션
        totalValue = AddOptionValue(aOptionType, aBaseValue, totalValue, mMainOption);

        // 서브 옵션
        if (mSubOption != null)
        {
            for (int i = 0; i < mSubOption.Length; i++)
            {
                totalValue = AddOptionValue(aOptionType, aBaseValue, totalValue, mSubOption[i]);
            }
        }

        // 세트 옵션
        if (mApplySetOption == true)
            totalValue = AddOptionValue(aOptionType, aBaseValue, totalValue, mSetOption);

        HwLog.Log(eLogType.INFORM, "Total stat , {0}: {1}", aOptionType, totalValue);

        return totalValue;
    }

    /// <summary>
    /// 옵션 값 합산
    /// </summary>
    private int AddOptionValue(eEquipmentOption aOptionType, int aBaseValue, int aTotalValue, EquipOptionData aOptionData)
    {
        if (aOptionData != null && aOptionData.IsEmpty() == false && aOptionData.MetaData != null && aOptionData.MetaData.OptionType == aOptionType)
        {
            if (aOptionType == eEquipmentOption.ADD_DAMAGE ||
                aOptionType == eEquipmentOption.REDUCE_DAMAGE ||
                aOptionType == eEquipmentOption.MOVE_SPEED ||
                aOptionType == eEquipmentOption.ATTACK_SPEED)
            {
                //데미지 증가/감소는 기본값이 없으므로, 고정값 계산함.
                //이동 속도는 기본 값 * 총 이속증가 값이므로 고정값 계산함.
                aTotalValue += (int)aOptionData.ApplyValue;
            }
            else
            {
                if (aOptionData.MetaData.OptionValueType == eOptionValueType.ABSOLUTE_VALUE)
                {
                    aTotalValue += (int)aOptionData.ApplyValue;
                }
                else if (aOptionData.MetaData.OptionValueType == eOptionValueType.PERCENT_VALUE)
                {
                    aTotalValue += Utility.CalculateInt(aBaseValue, (int)aOptionData.ApplyValue);
                }
            }
        }

        return aTotalValue;
    }
}

/// <summary>
/// 장비 정렬
/// </summary>
public class EquipmentCompare : IComparer<CardData>
{
    eEquipmentSortType mSortType;
    eSortOrder mSortOrder;

    public EquipmentCompare(eEquipmentSortType sort, eSortOrder order)
    {
        mSortType = sort;
        mSortOrder = order;
    }

    int IComparer<CardData>.Compare(CardData a, CardData b)
    {
        int compare = 0;
        if (a.ObjectID != b.ObjectID)
        {
            switch (a.CardType)
            {
                case eCardType.EQUIP:
                    EquipmentData edA = a as EquipmentData;
                    EquipmentData edB = b as EquipmentData;
                    bool bStart = false;
                    for (eEquipmentSortType type = mSortType; type < eEquipmentSortType.END; ++type)
                    {
                        if (type != mSortType || bStart == false)
                        {
                            switch (type)
                            {
                                case eEquipmentSortType.TIER: compare = edA.Tier.CompareTo(edB.Tier); break;
                                case eEquipmentSortType.RARE: compare = edA.Grade.CompareTo(edB.Grade); break;
                                case eEquipmentSortType.LEVEL: compare = edA.Level.CompareTo(edB.Level); break;
                                //case eEquipmentSortType.UNIT_TYPE: compare = edA.ApplyUnitType.CompareTo(edB.ApplyUnitType); break;
                                case eEquipmentSortType.SET_TYPE: compare = edA.SetTypeMetaID.CompareTo(edB.SetTypeMetaID); break;
                            }

                            if (bStart == false)
                            {
                                bStart = true;
                                type = eEquipmentSortType.NONE;
                            }
                        }

                        if (compare != 0)
                            break;
                    }

                    if (compare == 0)
                    {
                        compare = edA.IsEquiped.CompareTo(edB.IsEquiped);
                        if (compare == 0)
                        {
                            compare = edA.IsLock.CompareTo(edB.IsLock);
                            if (compare == 0)
                                compare = a.MetaID.CompareTo(b.MetaID);
                        }
                    }
                    break;
            }
        }

        return mSortOrder == eSortOrder.ASC ? compare : -compare;
    }
}

/// <summary>장비 정렬 - 제작 가능 > 타입 > 병종 > 티어</summary>
public class EquipmentCompareByEquipmentCraft : IComparer<EquipmentCraftMetaData>
{
    /// <summary>
    /// 정렬 타입
    /// </summary>
    enum eSortType
    {
        START,
        EQUIPMENT_TYPE,
        UNIT_TYPE,
        TIER,
        END
    }

    eEquipmentApplyUnitType mSortType;      //장비 병종에 따른 정렬 타입
    eSortOrder mSortOrder;                  //오름차순 내림차순
    List<eSortType> mSortTypeOrder;         //정렬 타입 순서

    public EquipmentCompareByEquipmentCraft(int sort, eSortOrder order)
    {
        mSortType = (eEquipmentApplyUnitType)sort;
        mSortOrder = order;

        // 정렬 타입 순서 리스트 생성
        mSortTypeOrder = new List<eSortType>();
        // 장비 전체가 아닌 경우 장비 병종 우선 정렬
        if (mSortType != eEquipmentApplyUnitType.COMMON)
            mSortTypeOrder.Add(eSortType.UNIT_TYPE);
        for (eSortType type = eSortType.START + 1; type < eSortType.END; ++type)
        {
            if (mSortTypeOrder.Contains(type) == false)
                mSortTypeOrder.Add(type);
        }
    }

    int IComparer<EquipmentCraftMetaData>.Compare(EquipmentCraftMetaData x, EquipmentCraftMetaData y)
    {
        int compare = 0;
        if (x.MetaID != y.MetaID)
        {
            for (int i = 0; i < mSortTypeOrder.Count; ++i)
            {
                switch (mSortTypeOrder[i])
                {
                    case eSortType.UNIT_TYPE:
                        if (x.ApplyUnitType == mSortType)
                            compare = y.ApplyUnitType != mSortType ? -1 : 0;
                        else if (y.ApplyUnitType == mSortType)
                            compare = 1;
                        else
                            compare = x.ApplyUnitType.CompareTo(y.ApplyUnitType);
                        break;
                    case eSortType.EQUIPMENT_TYPE:
                        compare = x.EquipmentType.CompareTo(y.EquipmentType);
                        break;
                    case eSortType.TIER:
                        // 티어만 오름 내림 차순 적용
                        compare = x.EquipmentTier.CompareTo(y.EquipmentTier) * (mSortOrder == eSortOrder.ASC ? 1 : -1);
                        break;
                }

                if (compare != 0)
                    break;
            }

            if (compare == 0)
                compare = x.Group.CompareTo(y.Group) * (mSortOrder == eSortOrder.ASC ? 1 : -1);
        }

        return compare;
    }
}