using Archeo.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Archeo;

public sealed partial class ModEntry
{
    internal List<FieldObjective> FieldObjectives() => FieldJournal.Build(State,Research.Artifacts,Today,Config.EnableWeatherEvents,Config.EnableNatureEvents)
        .Where(p => State.Journey.Started && (p.Culture == "" || Core.Journey.IsOpen(State, p.Culture))
            && (p.Tab == "overview" || Core.Journey.IsOpen(State, p.Tab))).ToList();

    internal string ObjectiveArtifact(FieldObjective goal)
    {
        var artifact = Research.Artifacts.FirstOrDefault(p => p.Id == goal.Artifact);
        return artifact != null ? ArtifactName(artifact) : goal.Artifact == Shipwreck.BellId ? T("shipwreck.bell-name")
            : goal.Topic == "collection" ? T("gallery.reward."+goal.Artifact) : T("journal.unknown");
    }

    internal string ObjectiveNpc(FieldObjective goal)
        => goal.Npc == "Gunther" ? T("field.npc.Gunther") : Game1.getCharacterFromName(goal.Npc)?.displayName ?? T("field.npc."+goal.Npc);

    internal string ObjectiveLocation(FieldObjective goal)
    {
        if (goal.Npc.Length > 0 && goal.Npc != "Gunther" && Game1.getCharacterFromName(goal.Npc)?.currentLocation is { } location) return location.DisplayName;
        return goal.Location.Length > 0 ? LocationName(goal.Location) : T("field.location-unknown");
    }

    internal string ObjectiveTopic(FieldObjective goal)
    {
        if (goal.Topic == "stars") return T("unusual.star-site");
        if (goal.Topic == "ship") return T("shipwreck.title");
        if (Shipwreck.Kinds.Contains(goal.Topic)) return T("shipwreck.site."+goal.Topic);
        if (NaturalEvents.Kinds.Contains(goal.Topic)) return T("nature.name."+goal.Topic);
        if (WeatherExpeditions.Kinds.Contains(goal.Topic)) return T("weather.name."+goal.Topic);
        var resident = ResidentStories.All.FirstOrDefault(p => p.SiteKind == goal.Topic);
        return resident != null ? T("residents.site."+resident.Id) : Core.Research.Cultures.Contains(goal.Culture) ? CultureName(goal.Culture) : T("journal.expeditions");
    }

    internal string ObjectiveTitle(FieldObjective goal) => goal.Topic.StartsWith("museum_") ? T("museum.goal."+goal.Topic[7..],new{name=goal.Artifact.Length>0?T("museum.furniture."+goal.Artifact):""}) : goal.Kind switch
    {
        ObjectiveKind.Reward => T("field.title.reward",new { name = ObjectiveArtifact(goal) }),
        ObjectiveKind.Dig => ObjectiveTopic(goal),
        ObjectiveKind.Talk => T("field.title.talk",new { name = ObjectiveNpc(goal) }),
        ObjectiveKind.Restore => T("field.title.restore",new { name = ObjectiveArtifact(goal) }),
        ObjectiveKind.Repair => T("field.title.repair"),
        ObjectiveKind.Part => T("field.title.part",new { part = T("workshop.part."+goal.Topic) }),
        _ => T("field.title.wait",new { name = ObjectiveTopic(goal) })
    };

    internal string ObjectiveStatus(FieldObjective goal)
    {
        if(goal.Topic=="museum_show")return T(goal.Kind==ObjectiveKind.Reward?"museum.show-ready":"museum.goal-status.show",new{count=goal.Remaining});
        if(goal.Topic=="museum_order")return T("museum.goal-status.order",new{days=goal.Expires-Today+1});
        if (goal.Kind == ObjectiveKind.Dig)
            return T(goal.Expires == int.MaxValue ? "field.status.permanent" : "field.status.dig",
                new { location = ObjectiveLocation(goal), count = goal.Remaining, days = goal.Expires == int.MaxValue ? 0 : goal.Expires-Today+1 });
        if (goal.Kind == ObjectiveKind.Reward) return T("field.status.reward");
        if (goal.Kind == ObjectiveKind.Talk || goal.Kind == ObjectiveKind.Repair) return ObjectiveLocation(goal);
        if (goal.Kind == ObjectiveKind.Part) return CultureName(goal.Culture);
        if (goal.Kind == ObjectiveKind.Restore) return T(goal.UseKeystone ? "field.status.restore-key" : "field.status.restore",new { cost = goal.Cost });
        if (goal.DetailKey == "field.detail.disabled") return T("field.status.disabled");
        string status = T(goal.Starts > Today ? "field.status.tomorrow" : "field.status.queued");
        return goal.Location.Length > 0 ? T("field.status.wait-place",new { location=ObjectiveLocation(goal),status }) : status;
    }

    internal string ObjectiveDescription(FieldObjective goal)
        => T(goal.DetailKey,new { name = goal.Npc.Length > 0 ? ObjectiveNpc(goal) : ObjectiveTitle(goal),
            artifact = ObjectiveArtifact(goal), location = ObjectiveLocation(goal), count = goal.Remaining,
            days = goal.Expires == int.MaxValue ? 0 : goal.Expires-Today+1, cost = goal.Cost,
            culture = Core.Research.Cultures.Contains(goal.Culture) ? CultureName(goal.Culture) : "", key = Config.SurveyKey.ToString(),
            part = goal.Kind == ObjectiveKind.Part ? T("workshop.part."+goal.Topic) : "",
            wood = Workshop.RepairWood, copper = Workshop.RepairCopper });

    internal void ToggleTrackedObjective(string id)
    {
        if (!FieldObjectives().Any(p => p.Id == id)) return;
        State.TrackedObjective = State.TrackedObjective == id ? "" : id;
        SaveState();
        Notify(T(State.TrackedObjective.Length > 0 ? "field.tracked" : "field.untracked"));
    }

    private void DrawTrackedObjective(SpriteBatch b)
    {
        if (!Config.ShowTrackedObjective || State.TrackedObjective.Length == 0) return;
        var goal = FieldJournal.Tracked(State,FieldObjectives());
        if (goal == null) return;
        int boxWidth = Math.Min(480,Game1.uiViewport.Width-40);
        var rect = new Rectangle(20,116,boxWidth,70);
        b.Draw(Game1.staminaRect,rect,new Color(41,31,28)*.86f);
        b.Draw(Atlas,new Vector2(rect.X+10,rect.Y+18),new Rectangle(5*16,2*16,16,16),Color.White,0,Vector2.Zero,2,SpriteEffects.None,1);
        string title = ObjectiveTitle(goal), status = ObjectiveStatus(goal);
        float Fit(string text,float scale) => Math.Min(scale,(boxWidth-68f)/Math.Max(1,Game1.smallFont.MeasureString(text).X));
        b.DrawString(Game1.smallFont,title,new Vector2(rect.X+52,rect.Y+10),new Color(255,230,178),0,Vector2.Zero,Fit(title,.80f),SpriteEffects.None,1);
        b.DrawString(Game1.smallFont,status,new Vector2(rect.X+52,rect.Y+38),new Color(218,205,175),0,Vector2.Zero,Fit(status,.70f),SpriteEffects.None,1);
    }
}
