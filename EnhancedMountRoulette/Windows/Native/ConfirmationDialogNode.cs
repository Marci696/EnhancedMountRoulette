using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace EnhancedMountRoulette.Windows.Native;

public class ConfirmationDialogNode : ResNode
{
    private Action? onConfirm;

    private readonly SimpleComponentNode blocker;
    private readonly ResNode panel;
    private readonly SimpleNineGridNode panelBackground;
    private readonly TextNode messageNode;
    private readonly TextButtonNode yesButton;
    private readonly TextButtonNode noButton;

    public ConfirmationDialogNode()
    {
        blocker = new SimpleComponentNode
        {
            Position = Vector2.Zero,
            Size = new Vector2(400.0f, 300.0f),
        };
        blocker.AttachNode(this);

        panel = new ResNode
        {
            Size = new Vector2(380.0f, 130.0f),
        };
        panel.AttachNode(this);

        panelBackground = new SimpleNineGridNode
        {
            TexturePath = "ui/uld/ListB.tex",
            TextureCoordinates = Vector2.Zero,
            TextureSize = new Vector2(32.0f, 32.0f),
            TopOffset = 10,
            BottomOffset = 12,
            LeftOffset = 10,
            RightOffset = 10,
            Size = panel.Size,
        };
        panelBackground.AttachNode(panel);

        messageNode = new TextNode
        {
            Position = new Vector2(16.0f, 16.0f),
            Size = new Vector2(348.0f, 60.0f),
            FontSize = 13,
            LineSpacing = 16,
            AlignmentType = AlignmentType.Center,
        };
        messageNode.AttachNode(panel);

        yesButton = new TextButtonNode
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
        yesButton.AttachNode(panel);

        noButton = new TextButtonNode
        {
            Position = new Vector2(210.0f, 90.0f),
            Size = new Vector2(100.0f, 28.0f),
            String = "No",
            OnClick = Hide,
        };
        noButton.AttachNode(panel);

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

        blocker.Size = Size;
        RecenterPanel();
    }

    private void RecenterPanel()
    {
        panel.Position = new Vector2(
            Math.Max(0.0f, (Width - panel.Width) / 2.0f),
            Math.Max(0.0f, (Height - panel.Height) / 2.0f)
        );
    }
}
