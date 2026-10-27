namespace Archeo.Core;

/// <summary>Relative sidebar geometry, shared by the menu and offline bounds checks.</summary>
public readonly record struct ObjectiveListLayout(int PageSize,int RowHeight,int FirstRowY,int PageControlsY,int ToolsY)
{
    public static ObjectiveListLayout Create(int panelHeight,int itemCount)
    {
        int size=Math.Max(1,(panelHeight-140)/54);
        int rowHeight=Math.Min(64,Math.Max(1,(panelHeight-140)/Math.Max(1,Math.Min(size,itemCount))));
        return new(size,rowHeight,45,panelHeight-88,panelHeight-44);
    }
}
