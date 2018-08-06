using System;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class 選択肢 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    private IObservable<Unit> _onClick;

    private void Awake()
    {
        _onClick = _button.OnClickAsObservable();
    }

    public async UniTask<int> AwaitSelect(string message, int storyId, CancellationToken ct)
    {
        try
        {
            _text.text = message;

            await _onClick.ToUniTask(ct, true);

            return storyId;
        }
        catch (Exception ex)
        {
            Debug.Log($"{message} is cancel {ex}");
            throw;
        }
    }
}
