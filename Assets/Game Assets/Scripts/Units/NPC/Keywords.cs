using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Keywords
{
    public static readonly Keyword Keyword = new() {
        //? Name of the keyword
        Name = "Keyword",
        //? Description for getting hints. Avoid using the keyword itself with "{}" in description.
        Description = new KeywordString[] {
            new("Keywords are not shown properly in {Description} unless you acquire them first.")
        },
        //? Requirements for fully understanding the keyword
        Relevant = new[] { Description, Conversation },
        //? Reward for fully understanding the keyword
        RewardInfo = new("You can now use keywords.")
    },
    Description = new() {
        Name = "Description",
        Description = new KeywordString[] {
            new("Description is important document for Knowledge."),
            new("Most Descriptions has some {Keyword}s in it.")
        },
        RewardInfo = new("You can now see descriptions of {Keyword}s.")
    },
    Conversation = new() {
        Name = "Conversation",
        Description = new KeywordString[] {
            new("Conversation is the process of talking with {People}."),
            new("Conversation is the most common way to {Acquire} {Keyword}."),
        },
        Relevant = new[] { Keyword },
        RewardInfo = new("Now talking with people can give you {Keyword}s.")
    },
    Research = new() {
        Name = "Research",
        Description = new KeywordString[] {
            new("Research is the process of studying {}."), //! 보완 필요
            new("Research is the most common way to acquire {Keyword}."), //! 보완/제거 필요
        },
        Relevant = new[] { Keyword },
        RewardInfo = new("Researching is enabled.")
    },
    Training = new() {
        Name = "Training",
        Description = new KeywordString[] {
            new("Training is the process of learning {Job}s."),
            new("Many {Job} keywords can be acquired by training."),
        },
        Relevant = new[] { Job }
    },
    Job = new() {

    }
    ;

    
    static readonly List<Keyword> keywords = new() {
        Keyword, Description, Conversation, Research, Training, Job
    };
    
    public static Keyword GetKeyword(string name) => keywords.FirstOrDefault(k => k.Name == name);
    public static bool TryGetKeyword(string name, out Keyword keyword) => (keyword = GetKeyword(name)) != null;
}