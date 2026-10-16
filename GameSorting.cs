using System;
using System.Collections.Generic;

namespace PS5SavefileBatcher;

public enum GameSortColumn { Title, Id, Platform, SaveCount }
public static class GameSorting
{
    public static string Platform(string id) => id.StartsWith("CUSA", StringComparison.OrdinalIgnoreCase) ? "PS4"
        : id.StartsWith("PPSA", StringComparison.OrdinalIgnoreCase) ? "PS5" : "Unknown";
    public static void Sort(List<GameGroup> groups, GameSortColumn column, bool descending)
    {
        groups.Sort((a,b) =>
        {
            int result;
            if (column == GameSortColumn.Title)
            {
                bool missingA=string.IsNullOrWhiteSpace(a.Title), missingB=string.IsNullOrWhiteSpace(b.Title);
                if (missingA != missingB) return missingA ? 1 : -1;
                result=StringComparer.CurrentCultureIgnoreCase.Compare(a.Title,b.Title);
            }
            else result=column switch
            {
                GameSortColumn.Id => StringComparer.OrdinalIgnoreCase.Compare(a.Id,b.Id),
                GameSortColumn.Platform => StringComparer.OrdinalIgnoreCase.Compare(a.Platform,b.Platform),
                GameSortColumn.SaveCount => a.Saves.Length.CompareTo(b.Saves.Length),
                _ => 0
            };
            if (result != 0) return descending ? -result : result;
            return StringComparer.OrdinalIgnoreCase.Compare(a.Id,b.Id);
        });
    }
}
