﻿using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class テキストページ送り : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    void Start()
    {
        RunAsync().FireAndForget();
    }

    private async Task RunAsync()
    {
        for (var i = 1; i < 10; i++)
        {
            _text.text = $"{i}ページ目を表示。画面タップを待つ";
            await _button.OnClickAsObservable().First();
        }
        _text.text = "おわり";
    }
}
