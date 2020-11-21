using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{

    public event System.Action OnValuesUpdated;
    public bool autoUpdate;


#if UNITY_EDITOR //Only compile if in the editor
    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues; //add callback
        }
    }

    public void NotifyOfUpdatedValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues; //unsubscribe callback
        if (OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }

    }

#endif
}
