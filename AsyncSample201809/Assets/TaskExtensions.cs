using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

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

    /// <summary>
    /// タスクを呼び出しの終端処理。タスク実行中の未ハンドルな例外があったらログに出す
    /// </summary>
    /// <param name="task"></param>
    public static void FireAndForget(this UniTask task)
    {
        task.Forget(ex =>
        {
            if (task.Status == AwaiterStatus.Canceled)
                Debug.Log($"{task}キャンセル");
            else
                Debug.LogException(ex);
        });
    }

    public static async UniTask<IEnumerable<string>> LoadStoryTextAsync(this string storyName, CancellationToken ct = default(CancellationToken))
    {
        using (var req = UnityWebRequest.Get($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt"))
        {
            await req.SendWebRequest().ConfigureAwait(cancellation: ct);

            return req.downloadHandler.text
                .Split(new[] { "\r\n" }, StringSplitOptions.None)
                .Where(x => !string.IsNullOrEmpty(x));
        }
    }

    public static async UniTask<Texture2D> LoadImageAsync(this string imageName, CancellationToken ct = default(CancellationToken))
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        var url = $"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Images/{imageName}.png";
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            await req.SendWebRequest().ConfigureAwait(progress => Debug.Log($"{imageName} dounloading.. {progress}"), ct);

            if (!string.IsNullOrEmpty(req.error))
                throw new ResourceLoadException(imageName, req.responseCode, req.error);

            return DownloadHandlerTexture.GetContent(req);
        }
    }

    public class ResourceLoadException : Exception
    {
        public ResourceLoadException(string resourceName, long statusCode, string message)
            : base($"{resourceName}\nStatusCode:{statusCode}\n{message}") { }
    }

    public static UniTask ConfigureAwait(this AsyncOperation operation, Action<float> onProgress, CancellationToken ct)
        => operation.ConfigureAwait(new ProgressHandler(onProgress), cancellation: ct);

    private struct ProgressHandler : IProgress<float>
    {
        private Action<float> _onProgress;

        public ProgressHandler(Action<float> onProgress)
        {
            _onProgress = onProgress;
        }

        public void Report(float value)
        {
            _onProgress?.Invoke(value);
        }
    }
}

/// <summary>
/// 非同期メソッド用共通デリゲート。
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <param name="ct"></param>
/// <returns></returns>
public delegate UniTask<TResult> AsyncFunc<TResult>(CancellationToken ct);
