using Archeo.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    private readonly PerScreen<double> nextLesson = new(() => 0);

    internal string LearningText(string lesson)
        => T("learn.text." + lesson, new { journal = Config.JournalKey.ToString() });

    private void UpdateLearning(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(30) || !Context.IsPlayerFree || states.Value == null || Game1.eventUp || Game1.isTimePaused) return;
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (now < nextLesson.Value) return;
        string? lesson = FieldLearning.TakeNotice(State, Research.Artifacts);
        if (lesson == null) return;
        nextLesson.Value = now + 14;
        Notify(T("learn.notice", new { title = T("learn.title." + lesson), tip = T("learn.tip." + lesson), journal = Config.JournalKey.ToString() }));
        SaveState();
    }
}
