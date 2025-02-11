#if UNITY_EDITOR || !UNITY_FLASH
#define REFLECTION_SUPPORT
#endif

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 리턴 값을 갖는 EventDelegate
/// </summary>
[Serializable]
public class ReturnEventDelegate<T> : EventDelegate
{
    public delegate T ReturnCallback();

    protected override Type ReturnType { get { return typeof(T); } }

    public ReturnEventDelegate() : base() { }
    public ReturnEventDelegate(ReturnCallback call) : base() { Set(call); }
    public ReturnEventDelegate(MonoBehaviour target, string methodName) : base(target, methodName) { }

    protected override Delegate CreateDelegate()
    {
#if NETFX_CORE
		return (ReturnCallback)mMethod.CreateDelegate(typeof(ReturnCallback), mTarget);
#else
        return (ReturnCallback)Delegate.CreateDelegate(typeof(ReturnCallback), mTarget, mMethodName);
#endif
    }

    /// <summary>
    /// Set the delegate callback directly.
    /// </summary>

    protected override void Set(Delegate call)
    {
        Clear();

        if (call != null && IsValid(call))
        {
#if REFLECTION_SUPPORT
			mTarget = call.Target as MonoBehaviour;

			if (mTarget == null)
			{
				mRawDelegate = true;
				mCachedCallback = call as ReturnCallback;
				mMethodName = null;
			}
			else
			{
				mMethodName = GetMethodName(call);
				mRawDelegate = false;
			}
#else
            mRawDelegate = true;
            mCachedCallback = call;
#endif
        }
    }

    /// <summary>
    /// Assign a new event delegate.
    /// </summary>

    static public EventDelegate Set(List<EventDelegate> list, ReturnCallback callback)
    {
        if (list != null)
        {
            ReturnEventDelegate<T> del = new ReturnEventDelegate<T>(callback);
            list.Clear();
            list.Add(del);
            return del;
        }
        return null;
    }

    /// <summary>
    /// Append a new event delegate to the list.
    /// </summary>

    static public EventDelegate Add(List<EventDelegate> list, ReturnCallback callback) { return Add(list, callback, false); }

    /// <summary>
    /// Append a new event delegate to the list.
    /// </summary>

    static public EventDelegate Add(List<EventDelegate> list, ReturnCallback callback, bool oneShot)
    {
        if (list != null)
        {
            for (int i = 0, imax = list.Count; i < imax; ++i)
            {
                EventDelegate del = list[i];
                if (del != null && del.Equals(callback))
                    return del;
            }

            ReturnEventDelegate<T> ed = new ReturnEventDelegate<T>(callback);
            ed.oneShot = oneShot;
            list.Add(ed);
            return ed;
        }
        Debug.LogWarning("Attempting to add a callback to a list that's null");
        return null;
    }

    /// <summary>
    /// Append a new event delegate to the list.
    /// </summary>

    static public void Add(List<EventDelegate> list, ReturnEventDelegate<T> ev) { Add(list, ev, ev.oneShot); }

    /// <summary>
    /// Append a new event delegate to the list.
    /// </summary>

    static public void Add(List<EventDelegate> list, ReturnEventDelegate<T> ev, bool oneShot)
    {
        if (ev.mRawDelegate || ev.target == null || string.IsNullOrEmpty(ev.methodName))
        {
            Add(list, ev.mCachedCallback as Callback, oneShot);
        }
        else if (list != null)
        {
            for (int i = 0, imax = list.Count; i < imax; ++i)
            {
                EventDelegate del = list[i];
                if (del != null && del.Equals(ev))
                    return;
            }

            ReturnEventDelegate<T> copy = new ReturnEventDelegate<T>(ev.target, ev.methodName);
            copy.oneShot = oneShot;

            if (ev.mParameters != null && ev.mParameters.Length > 0)
            {
                copy.mParameters = new Parameter[ev.mParameters.Length];
                for (int i = 0; i < ev.mParameters.Length; ++i)
                    copy.mParameters[i] = ev.mParameters[i];
            }

            list.Add(copy);
        }
        else Debug.LogWarning("Attempting to add a callback to a list that's null");
    }

    /// <summary>
    /// Remove an existing event delegate from the list.
    /// </summary>

    static public bool Remove(List<EventDelegate> list, ReturnCallback callback)
    {
        if (list != null)
        {
            for (int i = 0, imax = list.Count; i < imax; ++i)
            {
                EventDelegate del = list[i];

                if (del != null && del.Equals(callback))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
        }
        return false;
    }
}

[Serializable]
public class ReturnObjectEventDelegate : ReturnEventDelegate<object>
{
}