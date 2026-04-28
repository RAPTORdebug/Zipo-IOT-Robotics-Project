using System;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    DatabaseReference dbReference;

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void SetPathAndDrive(int[] values, Action callback)
    {
        dbReference.Child("car/path").SetValueAsync(values).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Path set successfully");

                dbReference.Child("car/drive").SetValueAsync(true).ContinueWithOnMainThread(task2 =>
                {
                    if (task2.IsCompletedSuccessfully)
                    {
                        Debug.Log("Drive set to: true");
                        callback?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Failed to set bool: " + task2.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("Failed to set array: " + task.Exception);
            }
        });
    }

    public void SetBool(string id, bool value)
    {
        dbReference.Child(id).SetValueAsync(value).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Bool set to: " + value);
            }
            else
            {
                Debug.LogError("Failed to set bool");
            }
        });
    }
    
    public void SetInt(string id, int value, Action callback)
    {
        dbReference.Child(id).SetValueAsync(value).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Int set to: " + value);
                callback?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to set int");
            }
        });
    }
    
    public void SetArray(string id, int[] values)
    {
        dbReference.Child(id).SetValueAsync(values).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Array set successfully");
            }
            else
            {
                Debug.LogError("Failed to set array");
            }
        });
    }

    public void ReadBool(string id, Action<bool> callback)
    {
        dbReference.Child(id).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                bool output = Convert.ToBoolean(snapshot.Value);

                callback(output);
            }
            else
            {
                Debug.LogError("Failed to read value");
                callback(false);
            }
        });
    }

    public void ReadInt(string id, Action<int> callback)
    {
        dbReference.Child(id).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int output = Convert.ToInt32(snapshot.Value);

                callback(output);
            }
            else
            {
                Debug.LogError("Failed to read value");
                callback(-10000);
            }
        });
    }

    public void ReadArray(string id, Action<int> callback)
    {
        dbReference.Child(id).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot child in snapshot.Children)
                {
                    int output = Convert.ToInt32(child.Value);
                    Debug.Log("Value: " + output);
                }
            }
            else
            {
                Debug.LogError("Failed to read array");
            }
        });
    }
}