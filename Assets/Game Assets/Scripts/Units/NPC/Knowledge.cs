using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


public class Knowledge {
    public Keyword name;
    public KeywordString[] descriptions;
}

public class Keyword {
    public string Name { get; init; }
    public KeywordString[] Description { get; init; }
    public Keyword[] Relevant { get; init; } = new Keyword[0];
    public KeywordString RewardInfo { get; init; }


    public override bool Equals(object obj) => obj is Keyword kwd && kwd.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
}

public class KeywordString {
    static readonly Regex keywordRegex = new(@"\{(\w+)\}([^{]*)");
    // public static readonly KeywordString Sample = new("{Keyword}s are not shown properly in {Description} unless you {Acquire} them first.");

    //TODO ? 단어(키워드)에 대한 지식을 알고 있다고 해도, 해당 자리에 어떤 키워드가 들어가야 하는지도 알아야 한다.
    //? 해당 기능은 Player/KnowledgeManager에서 담당해야 한다.

    readonly string first;
    readonly (Keyword key, string text)[] rest;

    public KeywordString(string text) {
        first = text[0..text.IndexOf('{')];
        var matches = keywordRegex.Matches(text);
        Debug.Log(string.Join(", ", matches.Cast<Match>().Select(m => m.Groups[1].Value)));
        rest = matches.Select(m => (Keywords.GetKeyword(m.Groups[1].Value), m.Groups[2].Value)).ToArray();
    }

    public string GetText(IEnumerable<Keyword> acquired) {
        StringBuilder s = new(first);
        foreach (var (key, text) in rest) {
            s.Append(acquired.Contains(key) ? key.Name : "???" );
            s.Append(text);
        }
        return s.ToString();
    }

    /// <summary>현재 문자열 내 해당 이름을 가진 키워드를 반환합니다. 존재하지 않으면 null을 반환합니다.</summary>
    public Keyword GetKeyword(string name) => rest.FirstOrDefault(k => k.key.Name == name).key;
}