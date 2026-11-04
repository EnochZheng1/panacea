using System.Collections.Generic;

namespace Panacea.Paper;

/// <summary>A laid-out edition of the paper: what the views render. Plain data.</summary>
public sealed class Edition
{
    public string Masthead = "";
    public string Over = "";
    public string Motto = "";
    public string EarLeftTitle = "WEATHER.";
    public string EarLeft = "";
    public string EarRightTitle = "";
    public string EarRight = "";
    public string DatelineLeft = "";
    public string DatelineCentre = "";
    public string DatelineRight = "";
    /// <summary>The month at a glance (sold, books, dead, new and lost customers).</summary>
    public string Brief = "";
    public Clipping? BriefClip;

    public Story Lead = new();
    /// <summary>Left column (deaths, coroner, personals), right column (ads, notices).</summary>
    public List<Section> Left = new();
    public List<Section> Right = new();
    public List<Section> LeadFoot = new();
    public string Markets = "";
}

public sealed class Section
{
    public string Title = "";
    public List<Block> Blocks = new();
}

public enum BlockKind { Paragraph, Notice, Ad, BlackAd, Cut }

public sealed class Block
{
    public BlockKind Kind = BlockKind.Paragraph;
    /// <summary>Lead-in printed in small caps style (e.g. a surname in a death notice).</summary>
    public string Lead = "";
    public string Text = "";
    public string Head = "";
    public string Mid = "";
    public string Fine = "";
    /// <summary>Name of an engraved cut in res://assets/art/cuts (without extension).</summary>
    public string Cut = "";
    /// <summary>What opens when the reader clicks this item: the cause behind the print.</summary>
    public Clipping? Clip;
}

/// <summary>The cutting the proprietor keeps: who or what an item was about, and why.</summary>
public sealed class Clipping
{
    public string Kicker = "";
    public string Title = "";
    public string Cut = "";
    public List<string> Lines = new();
}

public sealed class Story
{
    public string Headline = "";
    public List<string> Decks = new();
    public string Cut = "";
    public string Cutline = "";
    public string Body = "";
    public bool IsAdvertisement;
    public Clipping? Clip;
}
