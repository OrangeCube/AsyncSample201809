using System;
using System.Threading.Tasks;
using UnityEngine;

public static partial class TaskExtensions
{
    /// <summary>
    /// タスクを呼び出しの終端処理。タスク実行中の未ハンドルな例外があったらログに出す
    /// </summary>
    /// <param name="task"></param>
    public static async void FireAndForget(this Task task)
    {
        if (task == null) return;

        Exception ex = null;
        await task.ContinueWith(t => ex = t.Exception);

        if (ex != null)
            Debug.LogException(ex);
        else if (!task.IsCanceled)
            Debug.Log($"{task}完了");
        else
            Debug.Log($"{task}キャンセル");
    }
}
