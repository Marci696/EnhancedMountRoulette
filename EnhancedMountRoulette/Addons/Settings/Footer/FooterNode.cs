using System.Numerics;
using KamiToolKit.Nodes;

namespace EnhancedMountRoulette.Addons.Settings.Footer;

/// <summary>
/// Settings window footer: divider line and owned-mounts progress bar.
/// </summary>
public class FooterNode : VerticalListNode
{
    public const float DividerHeight = 4.0f;
    public const float ItemSpacingY = 6.0f;

    public const float PreferredHeight =
        DividerHeight + ItemSpacingY + OwnedMountsProgressNode.PreferredHeight;

    private readonly OwnedMountsProgressNode progressNode;

    public FooterNode(float width, float progressWidth)
    {
        Size = new Vector2(width, PreferredHeight);
        ItemSpacing = ItemSpacingY;
        FitWidth = false;

        AddDivider(width);
        progressNode = AddProgress(progressWidth);
        progressNode.Refresh();
    }

    public void Refresh()
    {
        progressNode.Refresh();
    }

    private void AddDivider(float width)
    {
        AddNode(
            new HorizontalLineNode
            {
                Size = new Vector2(width, DividerHeight),
            }
        );
    }

    private OwnedMountsProgressNode AddProgress(float progressWidth)
    {
        var progress = new OwnedMountsProgressNode
        {
            Size = new Vector2(progressWidth, OwnedMountsProgressNode.PreferredHeight),
        };
        AddNode(progress);
        return progress;
    }
}
