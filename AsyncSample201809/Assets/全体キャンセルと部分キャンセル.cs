using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class 全体キャンセルと部分キャンセル : TypedMonoBehaviour
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
        var story = await _loadinPanel.LoadingOn(LoadStoryAsync(storyName, ct));
        await ページ送りAsync(story, ct);
    }

    private readonly struct StoryContent
    {
        public int Id { get; }
        public AsyncFunc<Texture2D> ImageTask { get; }
        public string Text { get; }
        public SelectionContent[] SelectionContents { get; }

        public StoryContent(int id, AsyncFunc<Texture2D> imageTask, string text, SelectionContent[] selectionContents)
            => (Id, ImageTask, Text, SelectionContents) = (id, imageTask, text, selectionContents);
    }

    private readonly struct SelectionContent
    {
        public string Message { get; }
        public int StoryId { get; }

        public SelectionContent(string message, int storyId) => (Message, StoryId) = (message, storyId);
    }

    private async UniTask<StoryContent[]> LoadStoryAsync(string storyName, CancellationToken ct)
    {
        byte[] result = null;
        using (var www = new WWW($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt?timestamp={DateTime.Now}"))
        {
            await www.ConfigureAwait(cancellation: ct);
            result = www.bytes;
        }

        const string BOM = "\uFEFF";
        var contents = System.Text.Encoding.UTF8.GetString(result)
            .Split(new[] { "\r\n", BOM }, StringSplitOptions.None)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x =>
            {
                var content = x.Split(',');
                var selectionContents = content.Skip(3).Select(y =>
                {
                    var selectionContentData = y.Split(':');
                    return new SelectionContent(selectionContentData[0], int.Parse(selectionContentData[1]));
                });

                var storyId = int.Parse(content[0]);

                return new StoryContent(storyId, ct0 => _loadinPanel.LoadingOn(LoadImageAsync(storyId != 3 ? content[1] : "NotFoundFileName", ct0)), content[2], selectionContents.ToArray());
            });

        return contents.ToArray();
    }

    public class ResourceLoadException : Exception
    {
        public ResourceLoadException(string resourceName, long statusCode, string message)
            : base($"{resourceName}\nStatusCode:{statusCode}\n{message}") { }
    }

    private async UniTask<Texture2D> LoadImageAsync(string imageName, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        var url = $"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Images/{imageName}.png";

        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            var a = request.SendWebRequest().ConfigureAwait(cancellation: ct);
            await a;

            if (!string.IsNullOrEmpty(request.error))
                throw new ResourceLoadException(imageName, request.responseCode, request.error);

            return DownloadHandlerTexture.GetContent(request);
        }
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
                var (isCanceled, texture) = await content.ImageTask(ct).SuppressCancellationThrow();
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
                    var 選択肢 = Create選択肢(content.SelectionContents, cts.Token);
                    var (isCanceled, firstTask) = await UniTask.WhenAny(選択肢.ToArray()).SuppressCancellationThrow();
                    nextContentId = isCanceled ? content.Id + 1 : firstTask.result;
                    cts.Cancel();
                }

                foreach (Transform c in _選択肢Container)
                {
                    if (!c.gameObject.activeSelf)
                        continue;
                    c.gameObject.SetActive(false);
                    _選択肢プール.Push(c.GetComponent<選択肢>());
                }
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

    private Stack<選択肢> _選択肢プール = new Stack<選択肢>();

    private IEnumerable<UniTask<int>> Create選択肢(SelectionContent[] selectionContents, CancellationToken ct)
    {
        foreach (var content in selectionContents)
        {
            var instance = _選択肢プール.Count > 0 ? _選択肢プール.Pop().gameObject : Instantiate(_選択肢Prefab, _選択肢Container, false);
            if (!instance.activeSelf)
            {
                instance.SetActive(true);
                instance.transform.SetAsLastSibling();
            }
            yield return instance.GetComponent<選択肢>().AwaitSelect(content.Message, content.StoryId, ct);
        }
    }
}
