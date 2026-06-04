using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace LemonFramework.UProfiler.Components
{
/// <summary>
/// Text with clickable hyperlinks. See:
/// https://blog.csdn.net/weixin_43737238/article/details/104377121
/// </summary>
public class HyperlinkText : Text, IPointerClickHandler
{
    /// <summary>Bounds for one hyperlink segment.</summary>
    private class HyperlinkInfo
    {
        public int startIndex;
        public int endIndex;
        public string name;
        public readonly List<Rect> boxes = new List<Rect>();
    }

    /// <summary>Processed text passed to the mesh generator.</summary>
    private string m_OutputText;

    /// <summary>Hyperlink regions parsed from the source text.</summary>
    private readonly List<HyperlinkInfo> m_HrefInfos = new List<HyperlinkInfo>();

    [Serializable]
    public class HrefClickEvent : UnityEvent<string> { }

    [SerializeField]
    private HrefClickEvent m_OnHrefClick = new HrefClickEvent();

    /// <summary>Invoked when a hyperlink is clicked.</summary>
    public HrefClickEvent OnHrefClick
    {
        get { return m_OnHrefClick; }
        set { m_OnHrefClick = value; }
    }

    private static readonly StringBuilder s_TextBuilder = new StringBuilder();

    private static readonly Regex s_HrefRegex = new Regex(@"<a href=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);

    private HyperlinkText mHyperlinkText;

    public string GetHyperlinkInfo
    {
        get { return text; }
    }

    protected override void Awake()
    {
        base.Awake();
        mHyperlinkText = GetComponent<HyperlinkText>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        mHyperlinkText.OnHrefClick.AddListener(OnHyperlinkTextInfo);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        mHyperlinkText.OnHrefClick.RemoveListener(OnHyperlinkTextInfo);
    }

    private void OnHyperlinkTextInfo(string info)
    {
        Debug.Log($"Hyperlink clicked: {info}");
        Application.OpenURL(info);
    }

    public override void SetVerticesDirty()
    {
        base.SetVerticesDirty();
        text = GetHyperlinkInfo;
        m_OutputText = GetOutputText(text);
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        var orignText = m_Text;
        m_Text = m_OutputText;
        base.OnPopulateMesh(toFill);
        m_Text = orignText;
        var vert = new UIVertex();

        foreach (var hrefInfo in m_HrefInfos)
        {
            hrefInfo.boxes.Clear();
            if (hrefInfo.startIndex >= toFill.currentVertCount)
            {
                continue;
            }

            toFill.PopulateUIVertex(ref vert, hrefInfo.startIndex);
            var pos = vert.position;
            var bounds = new Bounds(pos, Vector3.zero);
            for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
            {
                if (i >= toFill.currentVertCount)
                {
                    break;
                }

                toFill.PopulateUIVertex(ref vert, i);
                pos = vert.position;
                if (pos.x < bounds.min.x)
                {
                    hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                    bounds = new Bounds(pos, Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(pos);
                }
            }

            hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
        }
    }

    protected virtual string GetOutputText(string outputText)
    {
        s_TextBuilder.Length = 0;
        m_HrefInfos.Clear();
        var indexText = 0;
        foreach (Match match in s_HrefRegex.Matches(outputText))
        {
            s_TextBuilder.Append(outputText.Substring(indexText, match.Index - indexText));
            s_TextBuilder.Append("<color=red>");

            var group = match.Groups[1];
            var hrefInfo = new HyperlinkInfo
            {
                startIndex = s_TextBuilder.Length + 4,
                endIndex = (s_TextBuilder.Length + match.Groups[2].Length - 1) * 4 + 3,
                name = group.Value,
            };
            m_HrefInfos.Add(hrefInfo);

            s_TextBuilder.Append(match.Groups[2].Value);
            s_TextBuilder.Append("</color>");
            indexText = match.Index + match.Length;
        }
        s_TextBuilder.Append(outputText.Substring(indexText, outputText.Length - indexText));
        return s_TextBuilder.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 lp = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out lp);

        foreach (var hrefInfo in m_HrefInfos)
        {
            var boxes = hrefInfo.boxes;
            for (var i = 0; i < boxes.Count; ++i)
            {
                if (boxes[i].Contains(lp))
                {
                    m_OnHrefClick.Invoke(hrefInfo.name);
                    return;
                }
            }
        }
    }
}
}
