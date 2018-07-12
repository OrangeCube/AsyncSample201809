using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    private CompositeDisposable _disposables;

    void Start()
    {
        テキストページ送りAsync().ContinueWith(_ => Debug.Log("おわり")).FireAndForget();
    }

    private async Task テキストページ送りAsync()
    {
        for (var i = 1; i < 10; i++)
        {
            _text.text = $"{i}ページ目を表示。ボタンを押すのを待つ";
            await _button.OnClickAsObservable().First().ToTask();
        }
        _text.text = "おわり";
    }
}

public static class TaskExtensions
{
    public static async void FireAndForget(this Task task)
    {
        if (task == null) return;

        Exception ex = null;
        await task.ContinueWith(t => ex = t.Exception);

        if (ex != null)
            Debug.LogError(ex);
    }

}
