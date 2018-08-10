using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ローディング表示 : MonoBehaviour
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

    private IAsyncClickEventHandler _clickHandler;

    void Start()
    {
        _clickHandler = _button.GetAsyncClickEventHandler();
        RunAsync("003").FireAndForget();
    }

    void OnDestroy() => _clickHandler?.Dispose();

    private async UniTask RunAsync(string storyName)
    {
        var story = await _loadinPanel.LoadingOn(LoadStoryAsync(storyName));
        await ページ送りAsync(story);
    }

    private readonly struct StoryContent
    {
        public int Id { get; }
        public Texture2D Image { get; }
        public string Text { get; }
        public SelectionContent[] SelectionContents { get; }

        public StoryContent(int id, Texture2D image, string text, SelectionContent[] selectionContents)
            => (Id, Image, Text, SelectionContents) = (id, image, text, selectionContents);
    }

    private readonly struct SelectionContent
    {
        public string Message { get; }
        public int StoryId { get; }

        public SelectionContent(string message, int storyId) => (Message, StoryId) = (message, storyId);
    }

    private async UniTask<StoryContent[]> LoadStoryAsync(string storyName)
    {
        var www = new WWW($"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Story/{storyName}.txt?timestamp={DateTime.Now}");

        await www;

        const string BOM = "\uFEFF";
        var contents = System.Text.Encoding.UTF8.GetString(www.bytes)
            .Split(new[] { "\r\n", BOM }, StringSplitOptions.None)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select<string, UniTask<StoryContent>>(async x =>
            {
                var content = x.Split(',');
                var selectionContents = content.Skip(3).Select(y =>
                {
                    var selectionContentData = y.Split(':');
                    return new SelectionContent(selectionContentData[0], int.Parse(selectionContentData[1]));
                });

                var storyId = int.Parse(content[0]);
                Texture2D image = null;
                try
                {
                    image = await LoadImageAsync(storyId != 3 ? content[1] : "NotFoundFileName");
                }
                catch(ResourceLoadException ex)
                {
                    // 画像読み込み時の例外処理
                    // 今回はログを出しつつ処理を継続させる
                    Debug.LogWarning(ex);
                }

                return new StoryContent(storyId, image, content[2], selectionContents.ToArray());
            });

        return await UniTask.WhenAll(contents);
    }

    public class ResourceLoadException : Exception
    {
        public ResourceLoadException(string resourceName, long statusCode, string message)
            : base($"{resourceName}\nStatusCode:{statusCode}\n{message}") { }
    }

    private async UniTask<Texture2D> LoadImageAsync(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return null;

        var url = $"https://raw.githubusercontent.com/OrangeCube/AsyncSample201809/master/RemoteResources/Images/{imageName}.png";

        var request = UnityWebRequestTexture.GetTexture(url);

        await request.SendWebRequest();

        if (!string.IsNullOrEmpty(request.error))
            throw new ResourceLoadException(imageName, request.responseCode, request.error);

        return DownloadHandlerTexture.GetContent(request);
    }

    private async UniTask ページ送りAsync(StoryContent[] story)
    {
        var content = story.First();
        var contents = story.ToDictionary(x => x.Id);

        while (true)
        {
            _text.text = content.Text;
            _image.texture = content.Image;

            var nextContentId = 0;
            if (content.SelectionContents.Any())
            {
                using (var cts = new CancellationTokenSource())
                {
                    var 選択肢 = Create選択肢(content.SelectionContents, cts.Token);
                    nextContentId = (await UniTask.WhenAny(選択肢.ToArray())).result;
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
                await _clickHandler.OnClickAsync();

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
