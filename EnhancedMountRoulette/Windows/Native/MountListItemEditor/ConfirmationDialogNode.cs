using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace EnhancedMountRoulette.Windows.Native.MountListItemEditor;

public class ConfirmationDialogNode : ResNode
{
    private Action? onConfirm;

    private readonly SimpleComponentNode clickShield;
    private readonly ResNode panel;
    private readonly TextNode messageNode;
    private readonly TextButtonNode yesButton;
    private readonly TextButtonNode noButton;

    public ConfirmationDialogNode()
    {
        clickShield = AddClickShield();
        panel = AddPanel();
        messageNode = AddMessage(panel);
        (yesButton, noButton) = AddOptionButtons(panel);

        IsVisible = false;
    }

    public void Show(string message, Action confirm)
    {
        onConfirm = confirm;
        messageNode.String = message;
        IsVisible = true;
        RecenterPanel();
    }

    public void Hide()
    {
        IsVisible = false;
        onConfirm = null;
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        clickShield.Size = Size;
        RecenterPanel();
    }

    private SimpleComponentNode AddClickShield()
    {
        // Invisible layer covering the whole window. While the dialog is open, clicks
        // outside the Yes/No box hit this instead of buttons/lists behind it.
        var node = new SimpleComponentNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(400.0f, 300.0f),
        };
        node.AttachNode(this);
        return node;
    }

    private ResNode AddPanel()
    {
        var node = new ResNode
        {
            Size = new Vector2(380.0f, 130.0f),
        };
        node.AttachNode(this);

        var background = new SimpleNineGridNode
        {
            TexturePath = "ui/uld/ListB.tex",
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(32.0f, 32.0f),
            TopOffset = 10,
            BottomOffset = 12,
            LeftOffset = 10,
            RightOffset = 10,
            Size = node.Size,
        };
        background.AttachNode(node);

        return node;
    }

    private static TextNode AddMessage(ResNode panel)
    {
        var node = new TextNode
        {
            Position = new Vector2(16.0f, 16.0f),
            Size = new Vector2(348.0f, 60.0f),
            FontSize = 13,
            LineSpacing = 16,
            AlignmentType = AlignmentType.Center,
        };
        node.AttachNode(panel);
        return node;
    }

    private (TextButtonNode Yes, TextButtonNode No) AddOptionButtons(ResNode panel)
    {
        var yes = new TextButtonNode
        {
            Position = new Vector2(70.0f, 90.0f),
            Size = new Vector2(100.0f, 28.0f),
            String = "Yes",
            OnClick = () =>
            {
                var confirm = onConfirm;
                Hide();
                confirm?.Invoke();
            },
        };
        yes.AttachNode(panel);

        var no = new TextButtonNode
        {
            Position = new Vector2(210.0f, 90.0f),
            Size = new Vector2(100.0f, 28.0f),
            String = "No",
            OnClick = Hide,
        };
        no.AttachNode(panel);

        return (yes, no);
    }

    private void RecenterPanel()
    {
        panel.Position = new Vector2(
            Math.Max(0.0f, (Width - panel.Width) / 2.0f),
            Math.Max(0.0f, (Height - panel.Height) / 2.0f)
        );
    }
}
