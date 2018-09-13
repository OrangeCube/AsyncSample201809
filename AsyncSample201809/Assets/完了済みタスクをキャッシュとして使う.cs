using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static TaskExtensions;

public class 完了済みタスクをキャッシュとして使う : TypedMonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private RawImage _image;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private RectTransform _選択肢Container;

    [SerializeField]
    private GameObject _選択肢Prefab;

    [SerializeField]
    private LoadingPanel _loadinPanel;

    [SerializeField]
    private Button _skipButton;

    private CancellationTokenSource _skipCts;

    void Start()
    {
        _skipCts = CancellationTokenSource.CreateLinkedTokenSource(CancelOnDestroy);
        _skipButton.OnClickAsObservable().Subscribe(_ => _skipCts.Cancel()).AddTo(Disposables);

        RunAsync("003", _skipCts.Token).FireAndForget();
    }

    private async UniTask RunAsync(string storyName, CancellationToken ct)
    {
        await UniTask.WhenAll(RunAsyncInternal(storyName, ct), 重い処理Async());
    }

    private async UniTask RunAsyncInternal(string storyName, CancellationToken ct)
    {
        var story = await _loadinPanel.LoadingOn(LoadStoryAsync(storyName, ct));
        await ページ送りAsync(story, ct);
    }

    private async UniTask 重い処理Async()
    {
        await UniTask.Run(() =>
        {
            for (var i = 0; i < 99999999; i++)
            {
                if (i % 10000 == 0)
                    Debug.Log("重い処理中");
            }
        });
        Debug.Log("重い処理終わり");
    }

    private class StoryContent
    {
        public int Id { get; }

        /// <summary>
        /// 画像を読み込むタスクを取得する。
        /// </summary>
        /// <remarks>
        /// 2回目以降は最初のタスクの結果を使う。
        /// 完了済みのタスクの await はほぼコストがかからないので<see cref="UniTask{T}.Result"/>をキャッシュとして使うことができる。
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        public UniTask<Texture2D> GetImageTask(CancellationToken ct) => _imageTaskCache ?? (_imageTaskCache = _getImageTask(ct)).GetValueOrDefault();
        private UniTask<Texture2D>? _imageTaskCache;
        private AsyncFunc<Texture2D> _getImageTask;

        public string Text { get; }
        public SelectionContentModel[] SelectionContents { get; }

        public StoryContent(int id, AsyncFunc<Texture2D> geImageTask, string text, SelectionContentModel[] selectionContents)
            => (Id, _getImageTask, Text, SelectionContents) = (id, geImageTask, text, selectionContents);
    }

    private async UniTask<StoryContent[]> LoadStoryAsync(string storyName, CancellationToken ct)
    {
        var story = await storyName.LoadStoryTextAsync(ct);

        var contents = story
            .Select(x =>
            {
                var content = x.Split(',');
                var selectionContents = content.ParseSelectionContentModels();

                var storyId = int.Parse(content[0]);

                return new StoryContent(storyId, ct0 => _loadinPanel.LoadingOn(content[1].LoadImageAsync(ct0)), content[2], selectionContents.ToArray());
            });

        return contents.ToArray();
    }

    private async UniTask ページ送りAsync(StoryContent[] story, CancellationToken ct)
    {
        var content = story.First();
        var contents = story.ToDictionary(x => x.Id);

        while (true)
        {
            _text.text = content.Text;
            Texture2D image = null;
            try
            {
                var (isCanceled, texture) = await content.GetImageTask(ct).SuppressCancellationThrow();
                if (!isCanceled)
                    image = texture;
            }
            catch (ResourceLoadException ex)
            {
                // 画像読み込み時の例外処理
                // 今回はログを出しつつ処理を継続させる
                Debug.LogWarning(ex);
            }
            _image.texture = image;

            var nextContentId = 0;
            if (content.SelectionContents.Any())
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    var 選択肢待ち = content.SelectionContents.Await選択肢(_選択肢Prefab, _選択肢Container, cts.Token, _選択肢プール);
                    var (isCanceled, result) = await 選択肢待ち.SuppressCancellationThrow();
                    nextContentId = isCanceled ? content.Id + 1 : result;
                    cts.Cancel();
                }

                Release選択肢();
            }
            else
            {
                await _button.OnClickAsync(ct).SuppressCancellationThrow();

                nextContentId = content.Id + 1;
            }

            if (!contents.TryGetValue(nextContentId, out content))
                break;
        }

        _text.text = "おわり";
    }

    private void Release選択肢()
    {
        foreach (Transform c in _選択肢Container)
        {
            if (!c.gameObject.activeSelf)
                continue;
            c.gameObject.SetActive(false);
            _選択肢プール.Push(c.GetComponent<選択肢>());
        }
    }

    private Stack<選択肢> _選択肢プール = new Stack<選択肢>();
}
