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

public class 画像の逐次読み込み : MonoBehaviour
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
        public Func<UniTask<Texture2D>> ImageTask { get; }
        public string Text { get; }
        public SelectionContentModel[] SelectionContents { get; }

        public StoryContent(int id, Func<UniTask<Texture2D>> imageTask, string text, SelectionContentModel[] selectionContents)
            => (Id, ImageTask, Text, SelectionContents) = (id, imageTask, text, selectionContents);
    }

    private async UniTask<StoryContent[]> LoadStoryAsync(string storyName)
    {
        var story = await storyName.LoadStoryTextAsync();

        var contents = story
            .Select(x =>
            {
                var content = x.Split(',');
                var selectionContents = content.ParseSelectionContentModels();

                var storyId = int.Parse(content[0]);

                return new StoryContent(storyId, () => _loadinPanel.LoadingOn(content[1].LoadImageAsync()), content[2], selectionContents.ToArray());
            });

        return contents.ToArray();
    }

    private async UniTask ページ送りAsync(StoryContent[] story)
    {
        var content = story.First();
        var contents = story.ToDictionary(x => x.Id);

        while (true)
        {
            _text.text = content.Text;
            Texture2D image = null;
            try
            {
                image = await content.ImageTask();
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
                using (var cts = new CancellationTokenSource())
                {
                    var 選択肢待ち = content.SelectionContents.Await選択肢(_選択肢Prefab, _選択肢Container, cts.Token, _選択肢プール);
                    nextContentId = await 選択肢待ち;
                    cts.Cancel();
                }

                Release選択肢();
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
