﻿using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class 選択肢 : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    [SerializeField]
    private Button _button;

    public async UniTask<int> AwaitSelect(string message, int storyId, CancellationToken ct)
    {
        try
        {
            _text.text = message;

            await _button.OnClickAsync(ct);

            return storyId;
        }
        catch (Exception ex)
        {
            Debug.Log($"{message} is cancel {ex}");
            throw;
        }
    }
}

public struct SelectionContentModel
{
    public string Message { get; }
    public int StoryId { get; }

    public SelectionContentModel(string message, int storyId) => (Message, StoryId) = (message, storyId);
}

public static class 選択肢Extensions
{
    public static IEnumerable<UniTask<int>> Create選択肢(this SelectionContentModel[] selectionContents, GameObject prefab, Transform parent, Stack<選択肢> pool, CancellationToken ct)
    {
        foreach (var content in selectionContents)
        {
            var instance = pool?.Count > 0 ? pool.Pop().gameObject : UnityEngine.Object.Instantiate(prefab, parent, false);
            if (!instance.activeSelf)
            {
                instance.SetActive(true);
                instance.transform.SetAsLastSibling();
            }
            yield return instance.GetComponent<選択肢>().AwaitSelect(content.Message, content.StoryId, ct);
        }
    }
}
