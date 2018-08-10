using System.Threading.Tasks;
using UniRx.Async;
using UnityEngine;

class LoadingPanel : MonoBehaviour
{
    [SerializeField]
    GameObject _panel;

    public async UniTask<T> LoadingOn<T>(UniTask<T> inner)
    {
        try
        {
            Loading(true);
            return await inner;
        }
        finally
        {
            Loading(false);
        }
    }

    private int _count;

    private void Loading(bool isOn)
    {
        if (isOn)
        {
            _count++;
            if (_count == 1)
                _panel.SetActive(true);
        }
        else
        {
            _count--;
            if (_count == 0)
                _panel.SetActive(false);
        }
    }
}
